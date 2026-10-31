using System;
using System.IO;
using System.Linq;
using AdaraBox.Core.Common;

namespace AdaraBox.Core.Resources;

public static class ResourceTargetValidator
{
    public const int MaxTargetLength = 2048;
    public const int MaxNameLength = 80;

    public static OperationResult<(string NormalizedTarget, string DisplayName)> Validate(
        ResourceType type,
        string? target,
        string? displayName)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return OperationResult<(string, string)>.Fail("resource.target.empty", "Target resource tidak boleh kosong.");
        }

        var trimmedTarget = target.Trim();
        if (trimmedTarget.Length > MaxTargetLength)
        {
            return OperationResult<(string, string)>.Fail("resource.target.invalid", $"Target terlalu panjang (maksimal {MaxTargetLength} karakter).");
        }

        string normalizedTarget;
        string defaultDisplayName;

        switch (type)
        {
            case ResourceType.Link:
            {
                // Normalize scheme if missing
                var candidate = trimmedTarget;
                if (!candidate.Contains("://", StringComparison.Ordinal))
                {
                    candidate = "https://" + candidate;
                }

                if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
                {
                    return OperationResult<(string, string)>.Fail("resource.target.invalid", "Format tautan (URL) tidak valid.");
                }

                if (!uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) &&
                    !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                {
                    return OperationResult<(string, string)>.Fail("resource.target.invalid", "Tautan hanya mendukung skema http atau https.");
                }

                normalizedTarget = uri.AbsoluteUri;
                defaultDisplayName = string.IsNullOrEmpty(uri.Host) ? uri.AbsoluteUri : uri.Host;
                break;
            }

            case ResourceType.File:
            {
                if (trimmedTarget.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    return OperationResult<(string, string)>.Fail("resource.target.invalid", "Path file mengandung karakter tidak valid.");
                }

                if (!Path.IsPathFullyQualified(trimmedTarget))
                {
                    return OperationResult<(string, string)>.Fail("resource.target.invalid", "Path file harus berupa path absolut.");
                }

                normalizedTarget = trimmedTarget;
                var fileName = Path.GetFileNameWithoutExtension(trimmedTarget);
                defaultDisplayName = string.IsNullOrEmpty(fileName) ? Path.GetFileName(trimmedTarget) : fileName;
                if (string.IsNullOrEmpty(defaultDisplayName)) defaultDisplayName = trimmedTarget;
                break;
            }

            case ResourceType.Folder:
            {
                if (trimmedTarget.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    return OperationResult<(string, string)>.Fail("resource.target.invalid", "Path folder mengandung karakter tidak valid.");
                }

                if (!Path.IsPathFullyQualified(trimmedTarget))
                {
                    return OperationResult<(string, string)>.Fail("resource.target.invalid", "Path folder harus berupa path absolut.");
                }

                normalizedTarget = trimmedTarget;
                var trimmedDir = trimmedTarget.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var folderName = Path.GetFileName(trimmedDir);
                defaultDisplayName = string.IsNullOrEmpty(folderName) ? trimmedTarget : folderName;
                break;
            }

            case ResourceType.Application:
            {
                if (trimmedTarget.StartsWith(@"shell:AppsFolder\", StringComparison.OrdinalIgnoreCase))
                {
                    normalizedTarget = trimmedTarget;
                    var appName = trimmedTarget.Substring(@"shell:AppsFolder\".Length);
                    defaultDisplayName = string.IsNullOrEmpty(appName) ? "Aplikasi Windows" : appName;
                }
                else
                {
                    if (trimmedTarget.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                    {
                        return OperationResult<(string, string)>.Fail("resource.target.invalid", "Path aplikasi mengandung karakter tidak valid.");
                    }

                    if (!Path.IsPathFullyQualified(trimmedTarget))
                    {
                        return OperationResult<(string, string)>.Fail("resource.target.invalid", "Path aplikasi harus berupa path absolut.");
                    }

                    var ext = Path.GetExtension(trimmedTarget);
                    if (!ext.Equals(".exe", StringComparison.OrdinalIgnoreCase) &&
                        !ext.Equals(".lnk", StringComparison.OrdinalIgnoreCase))
                    {
                        return OperationResult<(string, string)>.Fail("resource.type.mismatch", "Aplikasi harus berekstensi .exe atau .lnk.");
                    }

                    normalizedTarget = trimmedTarget;
                    var appName = Path.GetFileNameWithoutExtension(trimmedTarget);
                    defaultDisplayName = string.IsNullOrEmpty(appName) ? Path.GetFileName(trimmedTarget) : appName;
                }
                break;
            }

            default:
                return OperationResult<(string, string)>.Fail("resource.type.mismatch", "Tipe resource tidak dikenali.");
        }

        // Validate or resolve DisplayName
        string finalName;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            finalName = defaultDisplayName;
        }
        else
        {
            var trimmedName = displayName.Trim();
            if (trimmedName.Length == 0)
            {
                return OperationResult<(string, string)>.Fail("resource.name.empty", "Nama tampilan resource tidak boleh kosong.");
            }

            if (trimmedName.Length > MaxNameLength)
            {
                return OperationResult<(string, string)>.Fail("resource.name.too_long", $"Nama tampilan resource maksimal {MaxNameLength} karakter.");
            }

            if (trimmedName.Any(char.IsControl))
            {
                return OperationResult<(string, string)>.Fail("resource.name.invalid", "Nama tampilan resource mengandung karakter tidak valid.");
            }

            finalName = trimmedName;
        }

        return OperationResult<(string, string)>.Ok((normalizedTarget, finalName));
    }
}
