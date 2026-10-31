using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AdaraBox.Platform.Tray;

public sealed class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _toggleVisibilityMenuItem;
    private readonly Func<string> _getToggleLabel;

    public TrayIconManager(string appName, Func<string> getToggleLabel, Action onToggleVisibility, Action onExit)
    {
        _getToggleLabel = getToggleLabel;

        var contextMenu = new ContextMenuStrip();

        _toggleVisibilityMenuItem = new ToolStripMenuItem(_getToggleLabel(), null, (s, e) =>
        {
            onToggleVisibility();
        });
        contextMenu.Items.Add(_toggleVisibilityMenuItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        var exitMenuItem = new ToolStripMenuItem("Keluar", null, (s, e) =>
        {
            onExit();
        });
        contextMenu.Items.Add(exitMenuItem);

        Icon appIcon = SystemIcons.Application;
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconPath))
            {
                appIcon = new Icon(iconPath);
            }
        }
        catch { }

        _notifyIcon = new NotifyIcon
        {
            Icon = appIcon,
            Text = appName,
            Visible = true,
            ContextMenuStrip = contextMenu
        };

        _notifyIcon.DoubleClick += (s, e) =>
        {
            onToggleVisibility();
        };
    }

    public void UpdateToggleLabel()
    {
        _toggleVisibilityMenuItem.Text = _getToggleLabel();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
