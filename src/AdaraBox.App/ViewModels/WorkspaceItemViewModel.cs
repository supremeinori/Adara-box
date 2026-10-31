using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AdaraBox.Core.Workspaces;

namespace AdaraBox.App.ViewModels;

public sealed partial class WorkspaceItemViewModel : ObservableObject
{
    private readonly WorkspacePanelViewModel _parent;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCustomImage))]
    [NotifyPropertyChangedFor(nameof(DisplayEmoji))]
    [NotifyPropertyChangedFor(nameof(ImageSource))]
    private string _icon = "📁";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResourceCountText))]
    private int _resourceCount;

    public Guid Id { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }
    public int SortOrder { get; }
    public string ResourceCountText => $"{ResourceCount} resource";

    public bool HasCustomImage => !string.IsNullOrWhiteSpace(Icon) &&
        (Icon.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
         Icon.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
         Icon.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
         Icon.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
         Icon.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
         Icon.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)) &&
        File.Exists(Icon);

    public string DisplayEmoji => HasCustomImage ? string.Empty : (string.IsNullOrWhiteSpace(Icon) ? "📁" : Icon);

    public ImageSource? ImageSource
    {
        get
        {
            if (!HasCustomImage) return null;
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(Icon, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }

    public WorkspaceItemViewModel(Workspace workspace, WorkspacePanelViewModel parent)
    {
        _parent = parent;
        Id = workspace.Id;
        _name = workspace.Name;
        _icon = string.IsNullOrWhiteSpace(workspace.Icon) ? "📁" : workspace.Icon;
        CreatedAt = workspace.CreatedAt;
        UpdatedAt = workspace.UpdatedAt;
        SortOrder = workspace.SortOrder;
    }

    public void UpdateName(string newName)
    {
        Name = newName;
    }

    public void UpdateWorkspace(string newName, string newIcon)
    {
        Name = newName;
        Icon = string.IsNullOrWhiteSpace(newIcon) ? "📁" : newIcon;
    }

    [RelayCommand]
    private void Open() => _parent.OpenWorkspace(this);

    [RelayCommand]
    private void Rename() => _parent.BeginRename(this);

    [RelayCommand]
    private void Delete() => _ = _parent.DeleteWorkspaceAsync(this);
}
