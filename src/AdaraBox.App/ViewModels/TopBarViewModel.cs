using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AdaraBox.App.Services;
using AdaraBox.Core.Media;
using AdaraBox.Core.Modules;
using AdaraBox.Core.Visibility;

namespace AdaraBox.App.ViewModels;

public sealed partial class TopBarViewModel : ObservableObject
{
    private readonly ITopBarVisibilityService _visibilityService;
    private readonly WorkspacePanelViewModel _workspaceVm;
    private readonly MediaPanelViewModel _mediaVm;
    private readonly StatsPanelViewModel _statsVm;
    private readonly SettingsPanelViewModel _settingsVm;
    private readonly IMediaPlaybackController? _mediaController;

    [ObservableProperty]
    private IAppModule? _selectedModule;

    [ObservableProperty]
    private ObservableObject? _activePanelViewModel;

    [ObservableProperty]
    private string _nowPlayingText = string.Empty;

    [ObservableProperty]
    private bool _hasActiveMedia;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseIcon))]
    private bool _isPlaying;

    public string PlayPauseIcon => IsPlaying ? "⏸" : "▶";

    public IReadOnlyList<IAppModule> Modules { get; }
    public IReadOnlyList<ITopBarAction> Actions { get; }
    public ITopBarVisibilityService VisibilityService => _visibilityService;

    public bool IsPanelOpen => SelectedModule != null;

    public TopBarViewModel(
        IEnumerable<IAppModule> modules,
        IEnumerable<ITopBarAction> actions,
        ITopBarVisibilityService visibilityService,
        WorkspacePanelViewModel workspaceVm,
        MediaPanelViewModel mediaVm,
        StatsPanelViewModel statsVm,
        SettingsPanelViewModel settingsVm,
        IMediaPlaybackController? mediaController = null)
    {
        Modules = modules.OrderBy(m => m.Order).ToList();
        Actions = actions.OrderBy(a => a.Order).ToList();
        _visibilityService = visibilityService;
        _workspaceVm = workspaceVm;
        _mediaVm = mediaVm;
        _statsVm = statsVm;
        _settingsVm = settingsVm;
        _mediaController = mediaController;

        if (_mediaController != null)
        {
            _mediaController.TrackChanged += (s, track) =>
            {
                var app = Application.Current;
                if (app != null && !app.Dispatcher.HasShutdownStarted)
                {
                    app.Dispatcher.InvokeAsync(() =>
                    {
                        NowPlayingText = track?.FormattedDisplayName ?? string.Empty;
                        HasActiveMedia = !string.IsNullOrWhiteSpace(NowPlayingText);
                    });
                }
                else
                {
                    NowPlayingText = track?.FormattedDisplayName ?? string.Empty;
                    HasActiveMedia = !string.IsNullOrWhiteSpace(NowPlayingText);
                }
            };

            _mediaController.PlaybackStateChanged += (s, state) =>
            {
                var app = Application.Current;
                if (app != null && !app.Dispatcher.HasShutdownStarted)
                {
                    app.Dispatcher.InvokeAsync(() =>
                    {
                        IsPlaying = state == MediaPlaybackState.Playing;
                    });
                }
                else
                {
                    IsPlaying = state == MediaPlaybackState.Playing;
                }
            };

            NowPlayingText = _mediaController.CurrentTrack?.FormattedDisplayName ?? string.Empty;
            HasActiveMedia = !string.IsNullOrWhiteSpace(NowPlayingText);
            IsPlaying = _mediaController.CurrentState == MediaPlaybackState.Playing;
        }

        _visibilityService.DisplayStateChanged += (s, state) =>
        {
            if (state == TopBarDisplayState.Hidden)
            {
                _statsVm.StopPolling();
            }
        };
    }

    [RelayCommand]
    public async Task TogglePlayPauseAsync()
    {
        if (_mediaController != null)
        {
            await _mediaController.PlayPauseAsync();
        }
    }

    [RelayCommand]
    public async Task NextTrackAsync()
    {
        if (_mediaController != null)
        {
            await _mediaController.NextAsync();
        }
    }

    [RelayCommand]
    public async Task PreviousTrackAsync()
    {
        if (_mediaController != null)
        {
            await _mediaController.PreviousAsync();
        }
    }

    [RelayCommand]
    public void OpenMediaPanel()
    {
        var mediaModule = Modules.FirstOrDefault(m => m.Id == "media");
        if (mediaModule != null)
        {
            SelectModule(mediaModule);
        }
    }

    [RelayCommand]
    public void SelectModule(IAppModule? module)
    {
        if (module == null || SelectedModule?.Id == module.Id)
        {
            ClosePanel();
            return;
        }

        if (SelectedModule?.Id == "stats" && module.Id != "stats")
        {
            _statsVm.StopPolling();
        }

        SelectedModule = module;
        ActivePanelViewModel = module.Id switch
        {
            "workspace" => _workspaceVm,
            "media" => _mediaVm,
            "stats" => _statsVm,
            "settings" => _settingsVm,
            _ => null
        };

        if (module.Id == "stats")
        {
            _statsVm.StartPolling();
        }

        OnPropertyChanged(nameof(IsPanelOpen));
        _visibilityService.SetPanelOpen(true);
    }

    [RelayCommand]
    public void ClosePanel()
    {
        if (SelectedModule?.Id == "stats")
        {
            _statsVm.StopPolling();
        }

        SelectedModule = null;
        ActivePanelViewModel = null;
        OnPropertyChanged(nameof(IsPanelOpen));
        _visibilityService.SetPanelOpen(false);
    }
}
