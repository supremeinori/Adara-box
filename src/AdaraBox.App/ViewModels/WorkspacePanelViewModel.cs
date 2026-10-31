using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AdaraBox.App.Resources;
using AdaraBox.App.Services;
using AdaraBox.Core.Launcher;
using AdaraBox.Core.Resources;
using AdaraBox.Core.Settings;
using AdaraBox.Core.Workspaces;
using AdaraBox.Platform.Availability;
using AdaraBox.Platform.Icons;

namespace AdaraBox.App.ViewModels;

public sealed partial class WorkspacePanelViewModel : ObservableObject
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IResourceService _resourceService;
    private readonly IResourceAvailabilityChecker _availabilityChecker;
    private readonly IIconProvider _iconProvider;
    private readonly IFilePickerService _filePickerService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsStore _settingsStore;
    private readonly IMultiLaunchService _multiLaunchService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLoadingResources;

    // Workspace Form State (Create / Rename Workspace)
    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private Guid? _editingWorkspaceId;

    [ObservableProperty]
    private string _formTitle = string.Empty;

    [ObservableProperty]
    private string _formName = string.Empty;

    [ObservableProperty]
    private string? _formError;

    // Workspace Detail State
    [ObservableProperty]
    private WorkspaceItemViewModel? _selectedWorkspace;

    // ViewMode: "Grid" | "List" (FR-VW-01..05)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGridView))]
    [NotifyPropertyChangedFor(nameof(IsListView))]
    private string _viewMode = "Grid";

    // Resource Form State (Add Link / Edit Resource)
    [ObservableProperty]
    private bool _isResourceFormOpen;

    [ObservableProperty]
    private bool _isEditingResource;

    [ObservableProperty]
    private Guid? _editingResourceId;

    [ObservableProperty]
    private ResourceType _formResourceType = ResourceType.Link;

    [ObservableProperty]
    private string _resourceFormTitle = string.Empty;

    [ObservableProperty]
    private string _resourceFormName = string.Empty;

    [ObservableProperty]
    private string _resourceFormTarget = string.Empty;

    [ObservableProperty]
    private string? _resourceFormError;

    // Open the File (M04) State
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanManageResources))]
    private bool _isOpenMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOpen))]
    [NotifyPropertyChangedFor(nameof(OpenButtonText))]
    private bool _isLaunching;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OpenButtonText))]
    private int _launchProgressCurrent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OpenButtonText))]
    private int _launchProgressTotal;

    [ObservableProperty]
    private bool _isLaunchSummaryOpen;

    [ObservableProperty]
    private string _launchSummaryTitle = UiText.LaunchSummaryTitle;

    [ObservableProperty]
    private string _launchSummaryMessage = string.Empty;

    [ObservableProperty]
    private LaunchSummary? _lastLaunchSummary;

    public ObservableCollection<WorkspaceItemViewModel> Workspaces { get; } = new();
    public ObservableCollection<ResourceItemViewModel> Resources { get; } = new();
    public ObservableCollection<LaunchResultItemViewModel> FailedResults { get; } = new();
    public ObservableCollection<LaunchResultItemViewModel> SuccessfulResults { get; } = new();

    public int SelectedCount => Resources.Count(r => r.IsSelected);
    public string SelectedCountText => string.Format(UiText.SelectedCountFormat, SelectedCount);
    public bool CanOpen => IsOpenMode && SelectedCount > 0 && !IsLaunching;
    public bool HasSelected => SelectedCount > 0;
    public bool HasManySelected => IsOpenMode && SelectedCount > 10;
    public string ManySelectedWarningText => UiText.ManyItemsWarning;
    public bool HasFailedResults => FailedResults.Count > 0;
    public bool HasSuccessfulResults => SuccessfulResults.Count > 0;
    public string OpenButtonText => IsLaunching
        ? string.Format(UiText.OpeningWithProgress, LaunchProgressCurrent, LaunchProgressTotal)
        : UiText.OpenButton;

    public bool CanManageResources => !IsOpenMode;

    public bool HasWorkspaces => Workspaces.Count > 0;
    public bool IsEmpty => !IsLoading && Workspaces.Count == 0;
    public bool HasFormError => !string.IsNullOrEmpty(FormError);
    public bool HasResourceFormError => !string.IsNullOrEmpty(ResourceFormError);

    public bool IsDetailViewActive => SelectedWorkspace != null;
    public string DetailTitle => SelectedWorkspace?.Name ?? string.Empty;
    public string DetailResourceCountText => SelectedWorkspace != null ? $"{SelectedWorkspace.ResourceCount} resource" : string.Empty;

    public bool HasResources => Resources.Count > 0;
    public bool IsResourcesEmpty => !IsLoadingResources && Resources.Count == 0;

    public bool IsGridView => ViewMode == "Grid";
    public bool IsListView => ViewMode == "List";

    // Text Resources
    public string EmptyTitle => UiText.WorkspaceEmpty;
    public string EmptyDescription => UiText.WorkspaceEmptyDescription;
    public string CreateFirstButtonText => UiText.CreateFirstWorkspace;

    public string ResourceEmptyTitle => UiText.ResourceEmpty;
    public string ResourceEmptyDescription => UiText.ResourceEmptyDescription;
    public string AddResourceButtonText => UiText.AddResourceButton;

    public WorkspacePanelViewModel(
        IWorkspaceService workspaceService,
        IResourceService resourceService,
        IResourceAvailabilityChecker availabilityChecker,
        IIconProvider iconProvider,
        IFilePickerService filePickerService,
        IDialogService dialogService,
        ISettingsStore settingsStore,
        IMultiLaunchService multiLaunchService)
    {
        _workspaceService = workspaceService;
        _resourceService = resourceService;
        _availabilityChecker = availabilityChecker;
        _iconProvider = iconProvider;
        _filePickerService = filePickerService;
        _dialogService = dialogService;
        _settingsStore = settingsStore;
        _multiLaunchService = multiLaunchService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        // Load global view mode preference
        var savedViewMode = await _settingsStore.GetAsync(SettingKeys.ViewMode);
        if (!string.IsNullOrEmpty(savedViewMode) && (savedViewMode == "Grid" || savedViewMode == "List"))
        {
            ViewMode = savedViewMode;
        }

        await LoadWorkspacesAsync();
    }

    #region Workspace Actions

    [RelayCommand]
    public async Task LoadWorkspacesAsync()
    {
        try
        {
            IsLoading = true;
            NotifyListStateChanged();

            Workspaces.Clear();
            var list = await _workspaceService.ListAsync();
            foreach (var ws in list)
            {
                var item = new WorkspaceItemViewModel(ws, this);
                item.ResourceCount = await _workspaceService.GetResourceCountAsync(ws.Id);
                Workspaces.Add(item);
            }
        }
        finally
        {
            IsLoading = false;
            NotifyListStateChanged();
        }
    }

    [RelayCommand]
    public void BeginCreate()
    {
        EditingWorkspaceId = null;
        FormTitle = UiText.CreateWorkspaceTitle;
        FormName = string.Empty;
        FormError = null;
        IsFormOpen = true;
    }

    [RelayCommand]
    public void BeginRename(WorkspaceItemViewModel item)
    {
        EditingWorkspaceId = item.Id;
        FormTitle = UiText.RenameWorkspaceTitle;
        FormName = item.Name;
        FormError = null;
        IsFormOpen = true;
    }

    [RelayCommand]
    public async Task SaveFormAsync()
    {
        FormError = null;

        if (EditingWorkspaceId == null)
        {
            var result = await _workspaceService.CreateAsync(FormName);
            if (!result.Success)
            {
                FormError = MapWorkspaceError(result.Error?.Code);
                return;
            }

            var item = new WorkspaceItemViewModel(result.Value!, this);
            Workspaces.Add(item);
            IsFormOpen = false;
            FormName = string.Empty;
            NotifyListStateChanged();
            OpenWorkspace(item);
        }
        else
        {
            var id = EditingWorkspaceId.Value;
            var result = await _workspaceService.RenameAsync(id, FormName);
            if (!result.Success)
            {
                FormError = MapWorkspaceError(result.Error?.Code);
                return;
            }

            var item = Workspaces.FirstOrDefault(w => w.Id == id);
            item?.UpdateName(FormName.Trim());

            if (SelectedWorkspace?.Id == id)
            {
                OnPropertyChanged(nameof(DetailTitle));
            }

            IsFormOpen = false;
            EditingWorkspaceId = null;
            FormName = string.Empty;
            NotifyListStateChanged();
        }
    }

    [RelayCommand]
    public void CancelForm()
    {
        IsFormOpen = false;
        EditingWorkspaceId = null;
        FormName = string.Empty;
        FormError = null;
    }

    public void OpenWorkspace(WorkspaceItemViewModel item)
    {
        IsOpenMode = false;
        IsLaunchSummaryOpen = false;
        ClearSelection();
        SelectedWorkspace = item;
        OnPropertyChanged(nameof(IsDetailViewActive));
        OnPropertyChanged(nameof(DetailTitle));
        OnPropertyChanged(nameof(DetailResourceCountText));

        _ = LoadResourcesAsync(item.Id);
    }

    [RelayCommand]
    public void BackToList()
    {
        IsOpenMode = false;
        IsLaunchSummaryOpen = false;
        ClearSelection();
        SelectedWorkspace = null;
        Resources.Clear();
        IsResourceFormOpen = false;
        OnPropertyChanged(nameof(IsDetailViewActive));
        OnPropertyChanged(nameof(DetailTitle));
        OnPropertyChanged(nameof(DetailResourceCountText));
    }

    [RelayCommand]
    public async Task DeleteWorkspaceAsync(WorkspaceItemViewModel? item)
    {
        if (item == null) return;

        var count = await _workspaceService.GetResourceCountAsync(item.Id);
        var confirmed = await _dialogService.ConfirmDeleteWorkspaceAsync(item.Name, count);
        if (!confirmed) return;

        var result = await _workspaceService.DeleteAsync(item.Id);
        if (!result.Success)
        {
            await _dialogService.ShowErrorAsync(UiText.StatusError, MapWorkspaceError(result.Error?.Code));
            return;
        }

        Workspaces.Remove(item);
        if (SelectedWorkspace?.Id == item.Id)
        {
            IsOpenMode = false;
            IsLaunchSummaryOpen = false;
            ClearSelection();
            SelectedWorkspace = null;
            Resources.Clear();
            OnPropertyChanged(nameof(IsDetailViewActive));
        }

        NotifyListStateChanged();
    }

    #endregion

    #region Resource Actions (M03)

    [RelayCommand]
    public async Task LoadResourcesAsync(Guid workspaceId)
    {
        try
        {
            IsLoadingResources = true;
            NotifyResourcesStateChanged();

            Resources.Clear();
            var list = await _resourceService.ListAsync(workspaceId);
            foreach (var res in list)
            {
                Resources.Add(new ResourceItemViewModel(res, this, _iconProvider));
            }

            if (SelectedWorkspace != null)
            {
                SelectedWorkspace.ResourceCount = Resources.Count;
                OnPropertyChanged(nameof(DetailResourceCountText));
            }
        }
        finally
        {
            IsLoadingResources = false;
            NotifyResourcesStateChanged();
        }

        // Trigger background availability check
        _ = CheckAvailabilityAsync();
    }

    [RelayCommand]
    public async Task CheckAvailabilityAsync()
    {
        var items = Resources.ToList();
        foreach (var item in items)
        {
            var status = await _availabilityChecker.CheckAsync(item.Resource);
            item.SetAvailability(status);
        }
    }

    [RelayCommand]
    public async Task ToggleViewModeAsync(string? mode)
    {
        var targetMode = mode ?? (ViewMode == "Grid" ? "List" : "Grid");
        if (ViewMode != targetMode)
        {
            ViewMode = targetMode;
            await _settingsStore.SetAsync(SettingKeys.ViewMode, targetMode);
        }
    }

    [RelayCommand]
    public async Task AddFilesAsync()
    {
        if (SelectedWorkspace == null || IsOpenMode) return;

        var files = await _filePickerService.PickFilesAsync();
        if (files == null || files.Length == 0) return;

        var result = await _resourceService.AddManyAsync(SelectedWorkspace.Id, ResourceType.File, files);
        if (result.Success && result.Value != null)
        {
            foreach (var res in result.Value)
            {
                var item = new ResourceItemViewModel(res, this, _iconProvider);
                Resources.Add(item);
            }
            SelectedWorkspace.ResourceCount = Resources.Count;
            OnPropertyChanged(nameof(DetailResourceCountText));
            NotifyResourcesStateChanged();
            _ = CheckAvailabilityAsync();
        }
    }

    [RelayCommand]
    public async Task AddFolderAsync()
    {
        if (SelectedWorkspace == null || IsOpenMode) return;

        var folder = await _filePickerService.PickFolderAsync();
        if (string.IsNullOrEmpty(folder)) return;

        var result = await _resourceService.AddAsync(SelectedWorkspace.Id, ResourceType.Folder, folder, null);
        if (result.Success && result.Value != null)
        {
            var item = new ResourceItemViewModel(result.Value, this, _iconProvider);
            Resources.Add(item);
            SelectedWorkspace.ResourceCount = Resources.Count;
            OnPropertyChanged(nameof(DetailResourceCountText));
            NotifyResourcesStateChanged();
            _ = CheckAvailabilityAsync();
        }
    }

    [RelayCommand]
    public async Task AddApplicationAsync()
    {
        if (SelectedWorkspace == null || IsOpenMode) return;

        var app = await _filePickerService.PickApplicationAsync();
        if (string.IsNullOrEmpty(app)) return;

        var result = await _resourceService.AddAsync(SelectedWorkspace.Id, ResourceType.Application, app, null);
        if (result.Success && result.Value != null)
        {
            var item = new ResourceItemViewModel(result.Value, this, _iconProvider);
            Resources.Add(item);
            SelectedWorkspace.ResourceCount = Resources.Count;
            OnPropertyChanged(nameof(DetailResourceCountText));
            NotifyResourcesStateChanged();
            _ = CheckAvailabilityAsync();
        }
    }

    [RelayCommand]
    public void BeginAddLink()
    {
        if (IsOpenMode) return;
        IsEditingResource = false;
        EditingResourceId = null;
        FormResourceType = ResourceType.Link;
        ResourceFormTitle = UiText.AddLinkTitle;
        ResourceFormName = string.Empty;
        ResourceFormTarget = string.Empty;
        ResourceFormError = null;
        IsResourceFormOpen = true;
    }

    public void BeginEditResource(ResourceItemViewModel item)
    {
        if (IsOpenMode) return;
        IsEditingResource = true;
        EditingResourceId = item.Id;
        FormResourceType = item.Type;
        ResourceFormTitle = $"{UiText.EditResourceTitle} ({item.TypeDisplayName})";
        ResourceFormName = item.Name;
        ResourceFormTarget = item.Target;
        ResourceFormError = null;
        IsResourceFormOpen = true;
    }

    [RelayCommand]
    public async Task SaveResourceFormAsync()
    {
        if (SelectedWorkspace == null) return;
        ResourceFormError = null;

        if (!IsEditingResource)
        {
            // Adding a link
            var result = await _resourceService.AddAsync(
                SelectedWorkspace.Id,
                FormResourceType,
                ResourceFormTarget,
                string.IsNullOrWhiteSpace(ResourceFormName) ? null : ResourceFormName);

            if (!result.Success)
            {
                ResourceFormError = MapResourceError(result.Error?.Code);
                return;
            }

            var item = new ResourceItemViewModel(result.Value!, this, _iconProvider);
            Resources.Add(item);
            SelectedWorkspace.ResourceCount = Resources.Count;
            OnPropertyChanged(nameof(DetailResourceCountText));
            NotifyResourcesStateChanged();

            IsResourceFormOpen = false;
            ResourceFormTarget = string.Empty;
            ResourceFormName = string.Empty;
            _ = CheckAvailabilityAsync();
        }
        else
        {
            // Editing existing resource
            if (EditingResourceId == null) return;

            var id = EditingResourceId.Value;
            var result = await _resourceService.UpdateAsync(id, ResourceFormName, ResourceFormTarget);
            if (!result.Success)
            {
                ResourceFormError = MapResourceError(result.Error?.Code);
                return;
            }

            var item = Resources.FirstOrDefault(r => r.Id == id);
            if (item != null)
            {
                var updated = await _resourceService.ListAsync(SelectedWorkspace.Id);
                var fresh = updated.FirstOrDefault(r => r.Id == id);
                if (fresh != null)
                {
                    item.UpdateResource(fresh);
                }
            }

            IsResourceFormOpen = false;
            EditingResourceId = null;
            ResourceFormTarget = string.Empty;
            ResourceFormName = string.Empty;
            _ = CheckAvailabilityAsync();
        }
    }

    [RelayCommand]
    public void CancelResourceForm()
    {
        IsResourceFormOpen = false;
        EditingResourceId = null;
        ResourceFormName = string.Empty;
        ResourceFormTarget = string.Empty;
        ResourceFormError = null;
    }

    [RelayCommand]
    public async Task DeleteResourceAsync(ResourceItemViewModel? item)
    {
        if (item == null || SelectedWorkspace == null || IsOpenMode) return;

        var confirmed = await _dialogService.ConfirmDeleteResourceAsync(item.Name);
        if (!confirmed) return;

        var result = await _resourceService.RemoveAsync(item.Id);
        if (!result.Success)
        {
            await _dialogService.ShowErrorAsync(UiText.StatusError, MapResourceError(result.Error?.Code));
            return;
        }

        Resources.Remove(item);
        SelectedWorkspace.ResourceCount = Resources.Count;
        OnPropertyChanged(nameof(DetailResourceCountText));
        NotifyResourcesStateChanged();
    }

    public async Task MoveResourceAsync(ResourceItemViewModel? item, int direction)
    {
        if (item == null || SelectedWorkspace == null || IsOpenMode) return;

        var result = await _resourceService.MoveAsync(item.Id, direction);
        if (result.Success)
        {
            var currentIndex = Resources.IndexOf(item);
            var newIndex = currentIndex + direction;
            if (newIndex >= 0 && newIndex < Resources.Count)
            {
                Resources.Move(currentIndex, newIndex);
            }
        }
    }

    #endregion

    #region Open the File Actions (M04)

    [RelayCommand]
    public void ToggleOpenMode()
    {
        if (IsLaunching) return;

        IsOpenMode = !IsOpenMode;
        if (!IsOpenMode)
        {
            ClearSelection();
        }
        NotifyModeChanged();
    }

    [RelayCommand]
    public void ExitOpenMode()
    {
        if (IsLaunching) return;

        IsOpenMode = false;
        ClearSelection();
        NotifyModeChanged();
    }

    [RelayCommand]
    public void SelectAll()
    {
        if (!IsOpenMode || IsLaunching) return;

        foreach (var res in Resources)
        {
            res.IsSelected = true;
        }
        NotifySelectionChanged();
    }

    [RelayCommand]
    public void ClearSelection()
    {
        foreach (var res in Resources)
        {
            res.IsSelected = false;
        }
        NotifySelectionChanged();
    }

    [RelayCommand]
    public async Task OpenSelectedAsync()
    {
        if (!CanOpen) return;

        var selectedItems = Resources.Where(r => r.IsSelected).ToList();
        if (selectedItems.Count == 0) return;

        try
        {
            IsLaunching = true;
            LaunchProgressTotal = selectedItems.Count;
            LaunchProgressCurrent = 0;
            NotifyOpenStateChanged();

            var targetResources = selectedItems.Select(s => s.Resource).ToList();
            var progress = new Progress<LaunchResult>(_ =>
            {
                LaunchProgressCurrent++;
                OnPropertyChanged(nameof(OpenButtonText));
            });

            var summary = await _multiLaunchService.LaunchAllAsync(targetResources, progress);
            LastLaunchSummary = summary;

            FailedResults.Clear();
            SuccessfulResults.Clear();
            foreach (var r in summary.Results)
            {
                if (r.Success)
                {
                    SuccessfulResults.Add(new LaunchResultItemViewModel(r));
                }
                else
                {
                    FailedResults.Add(new LaunchResultItemViewModel(r));
                }
            }

            if (summary.FailureCount == 0)
            {
                LaunchSummaryMessage = string.Format(UiText.LaunchSummaryAllSuccessFormat, summary.SuccessCount);
            }
            else
            {
                LaunchSummaryMessage = string.Format(UiText.LaunchSummaryPartialFormat, summary.SuccessCount, summary.FailureCount);
            }

            OnPropertyChanged(nameof(HasFailedResults));
            OnPropertyChanged(nameof(HasSuccessfulResults));
            IsLaunchSummaryOpen = true;
        }
        finally
        {
            IsLaunching = false;
            NotifyOpenStateChanged();
        }
    }

    [RelayCommand]
    public async Task RetryFailedAsync()
    {
        if (IsLaunching || LastLaunchSummary == null || FailedResults.Count == 0) return;

        var failedIds = FailedResults.Select(f => f.ResourceId).ToHashSet();
        var itemsToRetry = Resources.Where(r => failedIds.Contains(r.Id)).ToList();
        if (itemsToRetry.Count == 0) return;

        IsLaunchSummaryOpen = false;

        // Keep only failed items selected
        foreach (var res in Resources)
        {
            res.IsSelected = failedIds.Contains(res.Id);
        }
        NotifySelectionChanged();

        await OpenSelectedAsync();
    }

    [RelayCommand]
    public void CloseLaunchSummary()
    {
        IsLaunchSummaryOpen = false;
    }

    public void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedCountText));
        OnPropertyChanged(nameof(CanOpen));
        OnPropertyChanged(nameof(HasSelected));
        OnPropertyChanged(nameof(HasManySelected));
        OpenSelectedCommand.NotifyCanExecuteChanged();
    }

    private void NotifyModeChanged()
    {
        OnPropertyChanged(nameof(IsOpenMode));
        OnPropertyChanged(nameof(CanManageResources));
        NotifySelectionChanged();
        foreach (var item in Resources)
        {
            item.NotifyCanManageChanged();
        }
    }

    private void NotifyOpenStateChanged()
    {
        OnPropertyChanged(nameof(CanOpen));
        OnPropertyChanged(nameof(OpenButtonText));
        OpenSelectedCommand.NotifyCanExecuteChanged();
    }

    #endregion

    private void NotifyListStateChanged()
    {
        OnPropertyChanged(nameof(HasWorkspaces));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void NotifyResourcesStateChanged()
    {
        OnPropertyChanged(nameof(HasResources));
        OnPropertyChanged(nameof(IsResourcesEmpty));
    }

    partial void OnFormErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HasFormError));
    }

    partial void OnResourceFormErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HasResourceFormError));
    }

    private static string MapWorkspaceError(string? errorCode) => errorCode switch
    {
        "workspace.name.empty" => UiText.ErrorWorkspaceNameEmpty,
        "workspace.name.too_long" => UiText.ErrorWorkspaceNameTooLong,
        "workspace.name.invalid" => UiText.ErrorWorkspaceNameInvalid,
        "workspace.not_found" => UiText.ErrorWorkspaceNotFound,
        _ => UiText.ErrorUnknown
    };

    private static string MapResourceError(string? errorCode) => errorCode switch
    {
        "resource.target.empty" => "Target resource tidak boleh kosong.",
        "resource.target.invalid" => "Target tidak valid (periksa format URL atau path absolut).",
        "resource.type.mismatch" => "Tipe resource tidak cocok dengan format target yang dimasukkan.",
        "resource.name.empty" => "Nama tampilan tidak boleh kosong.",
        "resource.name.too_long" => "Nama tampilan maksimal 80 karakter.",
        "resource.name.invalid" => "Nama tampilan mengandung karakter yang tidak valid.",
        "resource.not_found" => "Resource tidak ditemukan.",
        "workspace.not_found" => "Workspace tidak ditemukan.",
        _ => UiText.ErrorUnknown
    };
}
