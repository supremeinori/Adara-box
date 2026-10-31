using CommunityToolkit.Mvvm.ComponentModel;
using AdaraBox.Core.Common;

namespace AdaraBox.App.ViewModels;

public sealed class StatsPanelViewModel : ObservableObject
{
    public ModuleStatus Status => ModuleStatus.InDevelopment;
    public string StatusMessage => "Masih dalam pengembangan";
}
