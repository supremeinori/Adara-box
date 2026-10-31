using System;
using System.Linq;
using AdaraBox.Core.Common;

namespace AdaraBox.Core.Workspaces;

public static class WorkspaceNameValidator
{
    public const int MaxNameLength = 60;

    public static OperationResult<string> Validate(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return OperationResult<string>.Fail("workspace.name.empty", "Nama workspace tidak boleh kosong.");
        }

        var trimmed = name.Trim();

        if (trimmed.Length > MaxNameLength)
        {
            return OperationResult<string>.Fail("workspace.name.too_long", $"Nama workspace maksimal {MaxNameLength} karakter.");
        }

        if (trimmed.Any(char.IsControl))
        {
            return OperationResult<string>.Fail("workspace.name.invalid", "Nama workspace mengandung karakter kontrol yang tidak valid.");
        }

        return OperationResult<string>.Ok(trimmed);
    }
}
