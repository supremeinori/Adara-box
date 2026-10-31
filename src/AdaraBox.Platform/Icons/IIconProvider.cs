using System.Windows.Media;
using AdaraBox.Core.Resources;

namespace AdaraBox.Platform.Icons;

public interface IIconProvider
{
    ImageSource GetIcon(ResourceType type, string target);
}
