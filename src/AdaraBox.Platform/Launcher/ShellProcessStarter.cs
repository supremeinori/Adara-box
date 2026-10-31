using System.Diagnostics;

namespace AdaraBox.Platform.Launcher;

/// <summary>
/// Starts processes using Windows ShellExecute directly.
/// Adheres strictly to Golden Rule #3: zero shell command strings, zero arguments.
/// </summary>
public sealed class ShellProcessStarter : IProcessStarter
{
    public void Start(string fileName, string? workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = true,
            WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? string.Empty : workingDirectory
        };

        Process.Start(psi);
    }
}
