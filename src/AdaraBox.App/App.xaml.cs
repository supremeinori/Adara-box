using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using AdaraBox.App.Resources;
using AdaraBox.App.Services;
using AdaraBox.App.ViewModels;
using AdaraBox.App.Views;
using AdaraBox.App.Views.Dialogs;
using AdaraBox.Core.Common;
using AdaraBox.Core.Hotkeys;
using AdaraBox.Core.Launcher;
using AdaraBox.Core.Media;
using AdaraBox.Core.Modules;
using AdaraBox.Core.Resources;
using AdaraBox.Core.Settings;
using AdaraBox.Core.Stats;
using AdaraBox.Core.Visibility;
using AdaraBox.Core.Workspaces;
using AdaraBox.Data.Infrastructure;
using AdaraBox.Data.Migrations;
using AdaraBox.Data.Repositories;
using AdaraBox.Platform.Availability;
using AdaraBox.Platform.Hotkeys;
using AdaraBox.Platform.Icons;
using AdaraBox.Platform.Launcher;
using AdaraBox.Platform.Media;
using AdaraBox.Platform.Startup;
using AdaraBox.Platform.Stats;
using AdaraBox.Platform.Tray;

namespace AdaraBox.App;

public partial class App : Application
{
    private const string MutexName = @"Local\AdaraBox.SingleInstance";
    private const string WakeupEventName = @"Local\AdaraBox.WakeupEvent";
    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _wakeupEvent;
    private IServiceProvider? _serviceProvider;
    private TrayIconManager? _trayIconManager;
    private TopBarWindow? _topBarWindow;
    private TriggerStripWindow? _triggerStripWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Single-Instance Check via named Mutex
        _singleInstanceMutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            // Another instance is already running; signal it to wake up and show itself
            try
            {
                using var wakeHandle = EventWaitHandle.OpenExisting(WakeupEventName);
                wakeHandle.Set();
            }
            catch
            {
                // Fallback if event is not yet accessible
            }

            Shutdown();
            return;
        }

        // Initialize wakeup event listener for subsequent double-clicks/invocations
        try
        {
            _wakeupEvent = new EventWaitHandle(false, EventResetMode.AutoReset, WakeupEventName, out _);
            ThreadPool.RegisterWaitForSingleObject(_wakeupEvent, (state, timedOut) =>
            {
                Dispatcher.Invoke(ShowAndActivateTopBar);
            }, null, -1, false);
        }
        catch
        {
            // Non-critical fallback
        }

        // 2. Configure Serilog (Master Guide §8: %LOCALAPPDATA%\AdaraBox\logs\adarabox-.log, 14 days retention)
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDataDir = Path.Combine(localAppData, "AdaraBox");
        var logDirectory = Path.Combine(appDataDir, "logs");
        Directory.CreateDirectory(logDirectory);
        var logFilePath = Path.Combine(logDirectory, "adarabox-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Adara Box application starting up...");

        // 3. Register Global Unhandled Exception Handlers
        SetupExceptionHandling();

        // 4. Initialize Database, Check Integrity & Run Migrations (T1)
        var dbPath = Path.Combine(appDataDir, "adarabox.db");
        var backupsDir = Path.Combine(appDataDir, "backups");
        IDatabaseBackupService backupService = new DatabaseBackupService();

        var integrity = DatabaseIntegrityChecker.Check(dbPath, maxKnownVersion: 3);
        if (integrity.Status != DatabaseIntegrityStatus.Ok)
        {
            Log.Warning("Database integrity issue detected: Status={Status}, Message={Message}", integrity.Status, integrity.Message);

            var recoveryDialog = new DatabaseRecoveryWindow(dbPath, backupsDir, integrity, backupService);
            var recovered = recoveryDialog.ShowDialog();
            if (recovered != true || !recoveryDialog.RecoveredSuccessfully)
            {
                Log.Information("Database recovery cancelled by user. Terminating application.");
                Shutdown(0);
                return;
            }
        }

        IDbConnectionFactory dbConnectionFactory = new SqliteConnectionFactory(dbPath);
        var migrator = new DatabaseMigrator(dbConnectionFactory, null, backupService, dbPath, backupsDir);

        try
        {
            migrator.Migrate();
            Log.Information("Database migrations executed successfully on {DbPath}", dbPath);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to execute database migrations on {DbPath}. Showing recovery dialog.", dbPath);

            var recoveryDialog = new DatabaseRecoveryWindow(
                dbPath,
                backupsDir,
                new DatabaseIntegrityResult(DatabaseIntegrityStatus.Corrupt, $"Gagal migrasi: {ex.Message}"),
                backupService);

            var recovered = recoveryDialog.ShowDialog();
            if (recovered != true || !recoveryDialog.RecoveredSuccessfully)
            {
                Shutdown(1);
                return;
            }

            try
            {
                migrator.Migrate();
            }
            catch (Exception retryEx)
            {
                Log.Fatal(retryEx, "Database migration failed again after recovery attempt.");
                Shutdown(1);
                return;
            }
        }

        // 5. Configure Application Shell Settings
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // 6. Build Dependency Injection Container
        var services = new ServiceCollection();
        ConfigureServices(services, dbConnectionFactory);
        _serviceProvider = services.BuildServiceProvider();

        // 7. Initialize UI Windows & Services
        var visibilityService = _serviceProvider.GetRequiredService<ITopBarVisibilityService>();
        _topBarWindow = _serviceProvider.GetRequiredService<TopBarWindow>();
        _triggerStripWindow = _serviceProvider.GetRequiredService<TriggerStripWindow>();

        _trayIconManager = new TrayIconManager(
            UiText.AppName,
            () => visibilityService.CurrentMode == TopBarVisibilityMode.AlwaysVisible ? UiText.TrayHide : UiText.TrayShow,
            () => visibilityService.ToggleMode(),
            () => Dispatcher.Invoke(Shutdown));

        // Wire visibility state to window visibility
        visibilityService.DisplayStateChanged += (s, state) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (state == TopBarDisplayState.Shown || state == TopBarDisplayState.TemporarilyShown)
                {
                    _topBarWindow.Show();
                }
                else
                {
                    _topBarWindow.Hide();
                }
            });
        };

        visibilityService.ModeChanged += (s, mode) =>
        {
            _trayIconManager?.UpdateToggleLabel();
            Dispatcher.Invoke(() =>
            {
                if (mode == TopBarVisibilityMode.Hidden)
                {
                    _triggerStripWindow.Show();
                }
                else
                {
                    _triggerStripWindow.Hide();
                }
            });
        };

        // Initial display
        if (visibilityService.CurrentMode == TopBarVisibilityMode.AlwaysVisible)
        {
            _topBarWindow.Show();
            _triggerStripWindow.Hide();
        }
        else
        {
            _topBarWindow.Hide();
            _triggerStripWindow.Show();
        }

        // 8. Register Global Hotkey (FR-TB-06)
        var hotkeyService = _serviceProvider.GetRequiredService<IGlobalHotkeyService>();
        var settingsStore = _serviceProvider.GetRequiredService<ISettingsStore>();
        var savedHotkeyText = Task.Run(() => settingsStore.GetAsync(SettingKeys.GlobalHotkey)).GetAwaiter().GetResult();
        var hotkeyResult = HotkeyParser.Parse(string.IsNullOrEmpty(savedHotkeyText) ? HotkeyParser.DefaultTopbarHotkey.DisplayText : savedHotkeyText);
        if (hotkeyResult.Success)
        {
            hotkeyService.Register(hotkeyResult.Value!);
        }

        hotkeyService.HotkeyPressed += (s, ev) =>
        {
            Dispatcher.Invoke(ShowAndActivateTopBar);
        };

        // 9. Show Top Bar immediately on initial start
        Dispatcher.InvokeAsync(ShowAndActivateTopBar);

        Log.Information("Adara Box application shell successfully initialized.");
    }

    private void ShowAndActivateTopBar()
    {
        if (_topBarWindow == null) return;
        if (!_topBarWindow.IsVisible)
        {
            _topBarWindow.Show();
        }
        _topBarWindow.Activate();
        _topBarWindow.Focus();

        if (_topBarWindow.DataContext is TopBarViewModel topBarVm)
        {
            if (!topBarVm.IsPanelOpen)
            {
                var wsModule = topBarVm.Modules.FirstOrDefault(m => m.Id == "workspace");
                if (wsModule != null)
                {
                    topBarVm.SelectModuleCommand.Execute(wsModule);
                }
            }
        }
    }

    private void ConfigureServices(IServiceCollection services, IDbConnectionFactory dbConnectionFactory)
    {
        // Core & Data Services
        services.AddSingleton(dbConnectionFactory);
        services.AddSingleton<IDatabaseBackupService, DatabaseBackupService>();
        services.AddSingleton<ISettingsStore, SqliteSettingsStore>();
        services.AddSingleton<IWorkspaceRepository, SqliteWorkspaceRepository>();
        services.AddSingleton<IResourceRepository, SqliteResourceRepository>();
        services.AddSingleton<IMediaLinkRepository, SqliteMediaLinkRepository>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IWorkspaceService, WorkspaceService>();
        services.AddSingleton<IResourceService, ResourceService>();
        services.AddSingleton<IMediaLinkService, MediaLinkService>();
        services.AddSingleton<IResourceAvailabilityChecker, ResourceAvailabilityChecker>();
        services.AddSingleton<IIconProvider, IconProvider>();
        services.AddSingleton<IFilePickerService, WpfFilePickerService>();
        services.AddSingleton<IDialogService, WpfDialogService>();
        services.AddSingleton<IProcessStarter, ShellProcessStarter>();
        services.AddSingleton<IResourceLauncher, ResourceLauncher>();
        services.AddSingleton<IMultiLaunchService, MultiLaunchService>();
        services.AddSingleton<IAutoStartService, RegistryAutoStartService>();
        services.AddSingleton<IGlobalHotkeyService, Win32GlobalHotkeyService>();
        services.AddSingleton<ISystemStatsProvider, SystemStatsProvider>();
        services.AddSingleton<IMediaPlaybackController, WindowsMediaPlaybackController>();

        // Top Bar Visibility with initial mode loaded from ISettingsStore
        services.AddSingleton<ITopBarVisibilityService>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsStore>();
            var savedMode = Task.Run(() => settings.GetAsync(SettingKeys.TopBarVisibility)).GetAwaiter().GetResult();
            var initialMode = TopBarVisibilityMode.AlwaysVisible;
            if (Enum.TryParse<TopBarVisibilityMode>(savedMode, out var parsed))
            {
                initialMode = parsed;
            }
            return new TopBarVisibilityService(settings, initialMode);
        });

        // Top Bar Actions
        services.AddSingleton<ITopBarAction, AddWorkspaceTopBarAction>();

        // Modules
        services.AddSingleton<IAppModule, WorkspaceModule>();
        services.AddSingleton<IAppModule, MediaModule>();
        services.AddSingleton<IAppModule, StatsModule>();
        services.AddSingleton<IAppModule, SettingsModule>();

        // ViewModels
        services.AddSingleton<WorkspacePanelViewModel>();
        services.AddSingleton<MediaPanelViewModel>();
        services.AddSingleton<StatsPanelViewModel>();
        services.AddSingleton<SettingsPanelViewModel>();
        services.AddSingleton<TopBarViewModel>();

        // Views
        services.AddSingleton<TopBarWindow>(sp =>
        {
            var vm = sp.GetRequiredService<TopBarViewModel>();
            return new TopBarWindow { DataContext = vm };
        });

        services.AddSingleton<TriggerStripWindow>(sp =>
        {
            var visibility = sp.GetRequiredService<ITopBarVisibilityService>();
            return new TriggerStripWindow(visibility);
        });
    }

    private int _consecutiveDispatcherExceptions;
    private DateTime _lastDispatcherExceptionUtc = DateTime.MinValue;

    private void SetupExceptionHandling()
    {
        DispatcherUnhandledException += (s, e) =>
        {
            var now = DateTime.UtcNow;
            if ((now - _lastDispatcherExceptionUtc).TotalSeconds < 2)
            {
                _consecutiveDispatcherExceptions++;
            }
            else
            {
                _consecutiveDispatcherExceptions = 1;
            }
            _lastDispatcherExceptionUtc = now;

            Log.Error(e.Exception, "Unhandled dispatcher exception encountered (Consecutive={Count}).", _consecutiveDispatcherExceptions);

            // Circuit breaker: prevent infinite modal dialog loops if layout/render keeps failing
            if (_consecutiveDispatcherExceptions >= 3)
            {
                Log.Fatal("Dispatcher exception threshold exceeded. Terminating application to prevent UI freeze loop.");
                e.Handled = false;
                Shutdown(-1);
                return;
            }

            try
            {
                MessageBox.Show(
                    UiText.UnhandledExceptionMessage,
                    UiText.UnhandledExceptionTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // Ignore dialog display error during crash
            }

            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                Log.Fatal(ex, "Fatal unhandled domain exception encountered (IsTerminating={IsTerminating}).", e.IsTerminating);
            }
            else
            {
                Log.Fatal("Fatal unhandled domain exception encountered without Exception object.");
            }
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Log.Error(e.Exception, "Unobserved task exception encountered.");
            e.SetObserved();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Adara Box application shutting down...");

        _trayIconManager?.Dispose();
        _topBarWindow?.Close();
        _triggerStripWindow?.Close();

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (_singleInstanceMutex != null)
        {
            try { _singleInstanceMutex.ReleaseMutex(); } catch { }
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }

        _wakeupEvent?.Dispose();
        _wakeupEvent = null;

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
