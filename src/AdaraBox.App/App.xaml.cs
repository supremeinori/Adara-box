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
using AdaraBox.Core.Modules;
using AdaraBox.Core.Settings;
using AdaraBox.Core.Visibility;
using AdaraBox.Platform.Tray;

namespace AdaraBox.App;

public partial class App : Application
{
    private const string MutexName = @"Global\AdaraBox.SingleInstance";
    private Mutex? _singleInstanceMutex;
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
            // Another instance is already running; silently terminate this instance
            Shutdown();
            return;
        }

        // 2. Configure Serilog (Master Guide §8: %LOCALAPPDATA%\AdaraBox\logs\adarabox-.log, 14 days retention)
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logDirectory = Path.Combine(localAppData, "AdaraBox", "logs");
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

        // 4. Configure Application Shell Settings
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // 5. Build Dependency Injection Container
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // 6. Initialize UI Windows & Services
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

        Log.Information("Adara Box application shell successfully initialized.");
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Core Services & Modules
        services.AddSingleton<ISettingsStore, InMemorySettingsStore>();
        services.AddSingleton<ITopBarVisibilityService, TopBarVisibilityService>();

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

    private void SetupExceptionHandling()
    {
        DispatcherUnhandledException += (s, e) =>
        {
            Log.Error(e.Exception, "Unhandled dispatcher exception encountered.");
            MessageBox.Show(
                UiText.UnhandledExceptionMessage,
                UiText.UnhandledExceptionTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
