using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.App.Services;
using AdaraBox.App.ViewModels;
using AdaraBox.Core.Common;
using AdaraBox.Core.Launcher;
using AdaraBox.Core.Media;
using AdaraBox.Core.Resources;
using NSubstitute;
using Xunit;

namespace AdaraBox.App.Tests;

public class MediaPanelViewModelTests
{
    private readonly IMediaLinkService _mediaService = Substitute.For<IMediaLinkService>();
    private readonly IResourceLauncher _launcher = Substitute.For<IResourceLauncher>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly DateTimeOffset _fixedTime = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    private MediaPanelViewModel CreateVm() => new(_mediaService, _launcher, _dialogService);

    [Fact]
    public async Task InitialLoad_WithLinks_PopulatesList()
    {
        var link = new MediaLink(Guid.NewGuid(), "Lofi Girl", "https://youtube.com/live", 0, _fixedTime, _fixedTime);
        _mediaService.ListAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<MediaLink>>(new[] { link }));

        var vm = CreateVm();
        await vm.LoadLinksAsync();

        Assert.Single(vm.Links);
        Assert.Equal("Lofi Girl", vm.Links[0].Name);
        Assert.True(vm.HasLinks);
        Assert.False(vm.IsEmpty);
    }

    [Fact]
    public void BeginAdd_OpensForm_WithEmptyFields()
    {
        var vm = CreateVm();
        vm.BeginAdd();

        Assert.True(vm.IsFormOpen);
        Assert.Empty(vm.FormName);
        Assert.Empty(vm.FormUrl);
        Assert.Null(vm.FormError);
    }

    [Fact]
    public void CancelForm_ClosesForm()
    {
        var vm = CreateVm();
        vm.BeginAdd();
        vm.CancelForm();

        Assert.False(vm.IsFormOpen);
    }

    [Fact]
    public async Task SaveForm_Success_AddsItemAndClosesForm()
    {
        var created = new MediaLink(Guid.NewGuid(), "Spotify", "https://open.spotify.com", 0, _fixedTime, _fixedTime);
        _mediaService.CreateAsync("Spotify", "https://open.spotify.com", Arg.Any<CancellationToken>())
            .Returns(OperationResult<MediaLink>.Ok(created));

        var vm = CreateVm();
        vm.BeginAdd();
        vm.FormName = "Spotify";
        vm.FormUrl = "https://open.spotify.com";

        await vm.SaveFormAsync();

        Assert.False(vm.IsFormOpen);
        Assert.Single(vm.Links);
        Assert.Equal("Spotify", vm.Links[0].Name);
        Assert.Equal("open.spotify.com", vm.Links[0].Domain);
    }

    [Fact]
    public async Task SaveForm_FailsValidation_DisplaysError()
    {
        _mediaService.CreateAsync("", "", Arg.Any<CancellationToken>())
            .Returns(OperationResult<MediaLink>.Fail("media.name.empty", "Nama tidak boleh kosong"));

        var vm = CreateVm();
        vm.BeginAdd();

        await vm.SaveFormAsync();

        Assert.True(vm.IsFormOpen);
        Assert.True(vm.HasFormError);
        Assert.Equal("Nama tidak boleh kosong", vm.FormError);
    }

    [Fact]
    public async Task LaunchLink_CallsLauncher()
    {
        var linkId = Guid.NewGuid();
        _launcher.LaunchAsync(Arg.Any<Resource>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LaunchResult(linkId, "Spotify", true)));

        var link = new MediaLink(linkId, "Spotify", "https://spotify.com", 0, _fixedTime, _fixedTime);
        var vm = CreateVm();
        var item = new MediaLinkItemViewModel(link, vm);

        await vm.LaunchLinkAsync(item);

        await _launcher.Received(1).LaunchAsync(Arg.Is<Resource>(r => r.Target == "https://spotify.com" && r.Type == ResourceType.Link), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteLink_Confirmed_DeletesLink()
    {
        var id = Guid.NewGuid();
        var link = new MediaLink(id, "Track", "https://track.com", 0, _fixedTime, _fixedTime);
        _mediaService.ListAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<MediaLink>>(new[] { link }));
        _dialogService.ConfirmDeleteResourceAsync("Track").Returns(true);
        _mediaService.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(OperationResult.Ok());

        var vm = CreateVm();
        await vm.LoadLinksAsync();
        var item = vm.Links[0];

        await vm.DeleteLinkAsync(item);

        Assert.Empty(vm.Links);
        await _mediaService.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}
