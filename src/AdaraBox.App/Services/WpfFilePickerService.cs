using System.Threading.Tasks;
using Microsoft.Win32;

namespace AdaraBox.App.Services;

public sealed class WpfFilePickerService : IFilePickerService
{
    public Task<string[]?> PickFilesAsync()
    {
        var dlg = new OpenFileDialog
        {
            Multiselect = true,
            Title = "Pilih File"
        };

        var result = dlg.ShowDialog();
        return Task.FromResult(result == true ? dlg.FileNames : null);
    }

    public Task<string?> PickFolderAsync()
    {
        var dlg = new OpenFolderDialog
        {
            Multiselect = false,
            Title = "Pilih Folder"
        };

        var result = dlg.ShowDialog();
        return Task.FromResult(result == true ? dlg.FolderName : null);
    }

    public Task<string?> PickApplicationAsync()
    {
        var dlg = new OpenFileDialog
        {
            Multiselect = false,
            Title = "Pilih Aplikasi",
            Filter = "Aplikasi (*.exe;*.lnk)|*.exe;*.lnk|Semua File (*.*)|*.*"
        };

        var result = dlg.ShowDialog();
        return Task.FromResult(result == true ? dlg.FileName : null);
    }
}
