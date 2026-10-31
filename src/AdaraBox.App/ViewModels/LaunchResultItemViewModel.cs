using System;
using AdaraBox.Core.Launcher;

namespace AdaraBox.App.ViewModels;

public sealed class LaunchResultItemViewModel
{
    public Guid ResourceId { get; }
    public string Name { get; }
    public bool Success { get; }
    public string ReasonBadge { get; }
    public string? Message { get; }

    public LaunchResultItemViewModel(LaunchResult result)
    {
        ResourceId = result.ResourceId;
        Name = result.ResourceName;
        Success = result.Success;
        ReasonBadge = result.Reason switch
        {
            LaunchFailureReason.NotFound => "Tidak Ditemukan",
            LaunchFailureReason.AccessDenied => "Akses Ditolak",
            LaunchFailureReason.InvalidTarget => "Target Tidak Valid",
            LaunchFailureReason.NoHandler => "Aplikasi Default Tidak Ada",
            LaunchFailureReason.Cancelled => "Dibatalkan",
            _ => "Gagal"
        };
        Message = result.Message;
    }
}
