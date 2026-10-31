using System.Windows.Input;

namespace AdaraBox.App.Services;

/// <summary>
/// Contract for data-driven actions in the Top Bar (e.g., quick add buttons).
/// </summary>
public interface ITopBarAction
{
    string Id { get; }
    string DisplayName { get; }
    string Tooltip { get; }
    int Order { get; }
    ICommand Command { get; }
}
