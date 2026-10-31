namespace AdaraBox.Platform.Startup;

public interface IAutoStartService
{
    bool IsAutoStartEnabled();
    bool SetAutoStartEnabled(bool enabled);
}
