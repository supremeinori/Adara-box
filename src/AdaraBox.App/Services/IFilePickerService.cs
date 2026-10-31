using System.Threading.Tasks;

namespace AdaraBox.App.Services;

public interface IFilePickerService
{
    Task<string[]?> PickFilesAsync();
    Task<string?> PickFolderAsync();
    Task<string?> PickApplicationAsync();
    Task<string?> PickImageAsync();
}
