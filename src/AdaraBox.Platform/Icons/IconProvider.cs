using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AdaraBox.Core.Resources;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace AdaraBox.Platform.Icons;

public sealed class IconProvider : IIconProvider
{
    private readonly ConcurrentDictionary<string, ImageSource> _cache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Lazy<ImageSource> FallbackLinkIcon = new(() => CreateBitmapIcon(ResourceType.Link));
    private static readonly Lazy<ImageSource> FallbackFolderIcon = new(() => CreateBitmapIcon(ResourceType.Folder));
    private static readonly Lazy<ImageSource> FallbackFileIcon = new(() => CreateBitmapIcon(ResourceType.File));
    private static readonly Lazy<ImageSource> FallbackAppIcon = new(() => CreateBitmapIcon(ResourceType.Application));

    public ImageSource GetIcon(ResourceType type, string target)
    {
        try
        {
            switch (type)
            {
                case ResourceType.Link:
                    return FallbackLinkIcon.Value;

                case ResourceType.Folder:
                    return _cache.GetOrAdd("::folder::", _ => ExtractFolderIcon() ?? FallbackFolderIcon.Value);

                case ResourceType.Application:
                    if (target.StartsWith(@"shell:AppsFolder\", StringComparison.OrdinalIgnoreCase))
                    {
                        return FallbackAppIcon.Value;
                    }
                    return _cache.GetOrAdd(target, path => ExtractFileOrAppIcon(path, isApp: true) ?? FallbackAppIcon.Value);

                case ResourceType.File:
                default:
                    var ext = Path.GetExtension(target);
                    var cacheKey = string.IsNullOrEmpty(ext) ? target : ("ext:" + ext);
                    return _cache.GetOrAdd(cacheKey, _ => ExtractFileOrAppIcon(target, isApp: false) ?? FallbackFileIcon.Value);
            }
        }
        catch
        {
            return GetFallback(type);
        }
    }

    private static ImageSource GetFallback(ResourceType type) => type switch
    {
        ResourceType.Link => FallbackLinkIcon.Value,
        ResourceType.Folder => FallbackFolderIcon.Value,
        ResourceType.Application => FallbackAppIcon.Value,
        _ => FallbackFileIcon.Value
    };

    private static ImageSource? ExtractFolderIcon()
    {
        try
        {
            var shinfo = new SHFILEINFO();
            var flags = SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES;
            var res = SHGetFileInfo("dummy", FILE_ATTRIBUTE_DIRECTORY, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);

            if (res != IntPtr.Zero && shinfo.hIcon != IntPtr.Zero)
            {
                try
                {
                    var source = Imaging.CreateBitmapSourceFromHIcon(
                        shinfo.hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    source.Freeze();
                    return source;
                }
                finally
                {
                    DestroyIcon(shinfo.hIcon);
                }
            }
        }
        catch { }

        return null;
    }

    private static ImageSource? ExtractFileOrAppIcon(string path, bool isApp)
    {
        try
        {
            // If file actually exists and is an exe or lnk, extract its specific icon without SHGFI_USEFILEATTRIBUTES
            if (isApp && File.Exists(path))
            {
                var shinfo = new SHFILEINFO();
                var flags = SHGFI_ICON | SHGFI_LARGEICON;
                var res = SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);

                if (res != IntPtr.Zero && shinfo.hIcon != IntPtr.Zero)
                {
                    try
                    {
                        var source = Imaging.CreateBitmapSourceFromHIcon(
                            shinfo.hIcon,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        source.Freeze();
                        return source;
                    }
                    finally
                    {
                        DestroyIcon(shinfo.hIcon);
                    }
                }
            }

            // Otherwise, get icon by extension or file attributes (works even if file is missing)
            var ext = Path.GetExtension(path);
            var queryPath = string.IsNullOrEmpty(ext) ? "file" : ("*" + ext);

            var fallbackShInfo = new SHFILEINFO();
            var fallbackFlags = SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES;
            var resFallback = SHGetFileInfo(queryPath, FILE_ATTRIBUTE_NORMAL, ref fallbackShInfo, (uint)Marshal.SizeOf(fallbackShInfo), fallbackFlags);

            if (resFallback != IntPtr.Zero && fallbackShInfo.hIcon != IntPtr.Zero)
            {
                try
                {
                    var source = Imaging.CreateBitmapSourceFromHIcon(
                        fallbackShInfo.hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    source.Freeze();
                    return source;
                }
                finally
                {
                    DestroyIcon(fallbackShInfo.hIcon);
                }
            }
        }
        catch { }

        return null;
    }

    private static ImageSource CreateBitmapIcon(ResourceType type)
    {
        const int width = 32;
        const int height = 32;
        var pixels = new byte[width * height * 4];

        byte b, g, r;
        switch (type)
        {
            case ResourceType.Link:
                b = 0xEB; g = 0x63; r = 0x25; // Blue
                break;
            case ResourceType.Folder:
                b = 0x0B; g = 0x9E; r = 0xF5; // Amber
                break;
            case ResourceType.Application:
                b = 0xEA; g = 0x33; r = 0x93; // Purple
                break;
            case ResourceType.File:
            default:
                b = 0x71; g = 0x71; r = 0x7A; // Zinc
                break;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width + x) * 4;
                if (x >= 2 && x < width - 2 && y >= 2 && y < height - 2)
                {
                    pixels[index] = b;
                    pixels[index + 1] = g;
                    pixels[index + 2] = r;
                    pixels[index + 3] = 0xFF;
                }
            }
        }

        var source = BitmapSource.Create(
            width, height,
            96, 96,
            PixelFormats.Bgra32,
            null,
            pixels,
            width * 4);

        source.Freeze();
        return source;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_LARGEICON = 0x000000000;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
