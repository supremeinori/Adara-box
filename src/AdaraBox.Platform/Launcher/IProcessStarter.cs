namespace AdaraBox.Platform.Launcher;

/// <summary>
/// Abstraction for starting a process via Windows Shell.
/// Used to enable automated unit testing without launching real programs.
/// </summary>
public interface IProcessStarter
{
    void Start(string fileName, string? workingDirectory);
}
