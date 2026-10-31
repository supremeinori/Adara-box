using System;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using AdaraBox.App.Resources;
using AdaraBox.App.ViewModels;

namespace AdaraBox.App.Services;

/// <summary>
/// Top bar quick action to create a new workspace.
/// </summary>
public sealed class AddWorkspaceTopBarAction : ITopBarAction
{
    private readonly IServiceProvider _serviceProvider;

    public string Id => "add_workspace";
    public string DisplayName => UiText.AddWorkspaceButton;
    public string Tooltip => UiText.CreateWorkspaceTitle;
    public int Order => 1;

    public ICommand Command { get; }

    public AddWorkspaceTopBarAction(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        Command = new RelayCommand(Execute);
    }

    private void Execute()
    {
        var topBarVm = _serviceProvider.GetRequiredService<TopBarViewModel>();
        var workspaceVm = _serviceProvider.GetRequiredService<WorkspacePanelViewModel>();

        var workspaceModule = topBarVm.Modules.FirstOrDefault(m => m.Id == "workspace");
        if (workspaceModule != null)
        {
            topBarVm.SelectModule(workspaceModule);
        }

        workspaceVm.BeginCreate();
    }
}
