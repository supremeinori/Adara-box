using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AdaraBox.Core.Media;

namespace AdaraBox.App.ViewModels;

public sealed partial class MediaLinkItemViewModel : ObservableObject
{
    private readonly MediaPanelViewModel _parent;

    public Guid Id { get; }
    public string Name { get; private set; }
    public string Url { get; private set; }
    public int SortOrder { get; }
    public string Domain { get; }

    public MediaLinkItemViewModel(MediaLink link, MediaPanelViewModel parent)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        Id = link.Id;
        Name = link.Name;
        Url = link.Url;
        SortOrder = link.SortOrder;

        if (Uri.TryCreate(link.Url, UriKind.Absolute, out var uri))
        {
            Domain = uri.Host;
        }
        else
        {
            Domain = link.Url;
        }
    }

    [RelayCommand]
    public Task OpenAsync() => _parent.LaunchLinkAsync(this);

    [RelayCommand]
    public Task DeleteAsync() => _parent.DeleteLinkAsync(this);
}
