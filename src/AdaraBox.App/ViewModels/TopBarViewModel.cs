using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AdaraBox.App.Services;
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

    [ObservableProperty]
    private IAppModule? _selectedModule;

    [ObservableProperty]
    private ObservableObject? _activePanelViewModel;

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
        SettingsPanelViewModel settingsVm)
    {
        Modules = modules.OrderBy(m => m.Order).ToList();
        Actions = actions.OrderBy(a => a.Order).ToList();
        _visibilityService = visibilityService;
        _workspaceVm = workspaceVm;
        _mediaVm = mediaVm;
        _statsVm = statsVm;
        _settingsVm = settingsVm;

        _visibilityService.DisplayStateChanged += (s, state) =>
        {
            if (state == TopBarDisplayState.Hidden)
            {
                _statsVm.StopPolling();
            }
        };
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
