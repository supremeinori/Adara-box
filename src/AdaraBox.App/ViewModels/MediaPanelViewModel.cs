using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AdaraBox.App.Resources;
using AdaraBox.App.Services;
using AdaraBox.Core.Launcher;
using AdaraBox.Core.Media;
using AdaraBox.Core.Resources;

namespace AdaraBox.App.ViewModels;

public sealed partial class MediaPanelViewModel : ObservableObject
{
    private readonly IMediaLinkService _mediaLinkService;
    private readonly IResourceLauncher _launcher;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private string _formTitle = UiText.AddMediaLinkTitle;

    [ObservableProperty]
    private string _formName = string.Empty;

    [ObservableProperty]
    private string _formUrl = string.Empty;

    [ObservableProperty]
    private string? _formError;

    public ObservableCollection<MediaLinkItemViewModel> Links { get; } = new();

    public bool HasLinks => Links.Count > 0;
    public bool IsEmpty => !IsLoading && Links.Count == 0;
    public bool HasFormError => !string.IsNullOrEmpty(FormError);

    public string BrowserNoticeText => UiText.MediaBrowserNotice;
    public string ControlsNoticeText => UiText.MediaControlsNotice;
    public string EmptyTitle => UiText.MediaEmpty;
    public string EmptyDescription => UiText.MediaEmptyDescription;

    public MediaPanelViewModel(
        IMediaLinkService mediaLinkService,
        IResourceLauncher launcher,
        IDialogService dialogService)
    {
        _mediaLinkService = mediaLinkService ?? throw new ArgumentNullException(nameof(mediaLinkService));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        _ = LoadLinksAsync();
    }

    [RelayCommand]
    public async Task LoadLinksAsync()
    {
        try
        {
            IsLoading = true;
            NotifyListState();

            Links.Clear();
            var list = await _mediaLinkService.ListAsync();
            foreach (var link in list)
            {
                Links.Add(new MediaLinkItemViewModel(link, this));
            }
        }
        finally
        {
            IsLoading = false;
            NotifyListState();
        }
    }

    [RelayCommand]
    public void BeginAdd()
    {
        FormTitle = UiText.AddMediaLinkTitle;
        FormName = string.Empty;
        FormUrl = string.Empty;
        FormError = null;
        IsFormOpen = true;
    }

    [RelayCommand]
    public void CancelForm()
    {
        IsFormOpen = false;
        FormName = string.Empty;
        FormUrl = string.Empty;
        FormError = null;
    }

    [RelayCommand]
    public async Task SaveFormAsync()
    {
        FormError = null;

        var result = await _mediaLinkService.CreateAsync(FormName, FormUrl);
        if (!result.Success)
        {
            FormError = result.Error?.Message ?? UiText.ErrorUnknown;
            return;
        }

        Links.Add(new MediaLinkItemViewModel(result.Value!, this));
        IsFormOpen = false;
        FormName = string.Empty;
        FormUrl = string.Empty;
        NotifyListState();
    }

    public async Task LaunchLinkAsync(MediaLinkItemViewModel item)
    {
        if (item == null) return;

        var dummyResource = new Resource(
            item.Id,
            Guid.Empty,
            item.Name,
            ResourceType.Link,
            item.Url,
            item.SortOrder,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        var result = await _launcher.LaunchAsync(dummyResource);
        if (!result.Success)
        {
            await _dialogService.ShowErrorAsync(UiText.StatusError, result.Message ?? "Gagal membuka tautan di browser.");
        }
    }

    public async Task DeleteLinkAsync(MediaLinkItemViewModel item)
    {
        if (item == null) return;

        var confirmed = await _dialogService.ConfirmDeleteResourceAsync(item.Name);
        if (!confirmed) return;

        var result = await _mediaLinkService.DeleteAsync(item.Id);
        if (!result.Success)
        {
            await _dialogService.ShowErrorAsync(UiText.StatusError, result.Error?.Message ?? UiText.ErrorUnknown);
            return;
        }

        Links.Remove(item);
        NotifyListState();
    }

    partial void OnFormErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HasFormError));
    }

    private void NotifyListState()
    {
        OnPropertyChanged(nameof(HasLinks));
        OnPropertyChanged(nameof(IsEmpty));
    }
}
