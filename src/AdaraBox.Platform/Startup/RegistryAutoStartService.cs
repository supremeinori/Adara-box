using System;
using Microsoft.Win32;

namespace AdaraBox.Platform.Startup;

public sealed class RegistryAutoStartService : IAutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppValueName = "AdaraBox";

    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            var val = key?.GetValue(AppValueName) as string;
            return !string.IsNullOrEmpty(val);
        }
        catch
        {
            return false;
        }
    }

    public bool SetAutoStartEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null) return false;

            if (enabled)
            {
                var processPath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(processPath)) return false;

                key.SetValue(AppValueName, $"\"{processPath}\"");
            }
            else
            {
                key.DeleteValue(AppValueName, false);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
