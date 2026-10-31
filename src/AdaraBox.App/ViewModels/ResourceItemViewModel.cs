using System;
using System.IO;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AdaraBox.App.Resources;
using AdaraBox.Core.Resources;
using AdaraBox.Platform.Icons;

namespace AdaraBox.App.ViewModels;

public sealed partial class ResourceItemViewModel : ObservableObject
{
    private readonly WorkspacePanelViewModel _parent;

    [ObservableProperty]
    private Resource _resource;

    [ObservableProperty]
    private ImageSource _icon;

    [ObservableProperty]
    private ResourceAvailability _availability = ResourceAvailability.Unknown;

    [ObservableProperty]
    private bool _isSelected;

    public Guid Id => Resource.Id;
    public Guid WorkspaceId => Resource.WorkspaceId;
    public string Name => Resource.Name;
    public ResourceType Type => Resource.Type;
    public string Target => Resource.Target;
    public int SortOrder => Resource.SortOrder;

    public string TypeDisplayName => Resource.Type switch
    {
        ResourceType.Application => UiText.ResourceTypeApplication,
        ResourceType.File => UiText.ResourceTypeFile,
        ResourceType.Folder => UiText.ResourceTypeFolder,
        ResourceType.Link => UiText.ResourceTypeLink,
        _ => "Resource"
    };

    public string DistinctiveInfo => GetDistinctiveInfo(Resource);
    public string FullTooltip => $"{Name}\n{Target}\n({TypeDisplayName})";

    public bool IsMissing => Availability == ResourceAvailability.Missing;
    public bool IsAccessDenied => Availability == ResourceAvailability.AccessDenied;
    public bool HasAvailabilityWarning => IsMissing || IsAccessDenied;

    public string AvailabilityWarningText => IsMissing
        ? UiText.AvailabilityMissing
        : (IsAccessDenied ? UiText.AvailabilityAccessDenied : string.Empty);

    public bool IsOpenMode => _parent.IsOpenMode;
    public bool CanManage => !_parent.IsOpenMode;

    public ResourceItemViewModel(
        Resource resource,
        WorkspacePanelViewModel parent,
        IIconProvider iconProvider)
    {
        _parent = parent;
        _resource = resource;
        _icon = iconProvider.GetIcon(resource.Type, resource.Target);
    }

    partial void OnIsSelectedChanged(bool value)
    {
        _parent.NotifySelectionChanged();
    }

    public void NotifyCanManageChanged()
    {
        OnPropertyChanged(nameof(IsOpenMode));
        OnPropertyChanged(nameof(CanManage));
        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    public void UpdateResource(Resource updatedResource)
    {
        Resource = updatedResource;
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Target));
        OnPropertyChanged(nameof(DistinctiveInfo));
        OnPropertyChanged(nameof(FullTooltip));
    }

    public void SetAvailability(ResourceAvailability availability)
    {
        Availability = availability;
        OnPropertyChanged(nameof(IsMissing));
        OnPropertyChanged(nameof(IsAccessDenied));
        OnPropertyChanged(nameof(HasAvailabilityWarning));
        OnPropertyChanged(nameof(AvailabilityWarningText));
    }

    [RelayCommand(CanExecute = nameof(CanManage))]
    private void Edit() => _parent.BeginEditResource(this);

    [RelayCommand(CanExecute = nameof(CanManage))]
    private void Delete() => _ = _parent.DeleteResourceAsync(this);

    [RelayCommand(CanExecute = nameof(CanManage))]
    private void MoveUp() => _ = _parent.MoveResourceAsync(this, -1);

    [RelayCommand(CanExecute = nameof(CanManage))]
    private void MoveDown() => _ = _parent.MoveResourceAsync(this, 1);

    [RelayCommand]
    private void ToggleSelect()
    {
        if (_parent.IsOpenMode)
        {
            IsSelected = !IsSelected;
        }
    }

    private static string GetDistinctiveInfo(Resource res)
    {
        try
        {
            if (res.Type == ResourceType.Link)
            {
                if (Uri.TryCreate(res.Target, UriKind.Absolute, out var uri))
                {
                    return uri.Host;
                }
                return res.Target;
            }

            var dir = Path.GetDirectoryName(res.Target);
            if (!string.IsNullOrEmpty(dir))
            {
                var dirName = Path.GetFileName(dir);
                return string.IsNullOrEmpty(dirName) ? dir : "...\\" + dirName;
            }
        }
        catch { }

        return res.Target;
    }
}
