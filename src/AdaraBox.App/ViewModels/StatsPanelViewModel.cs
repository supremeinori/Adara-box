using System;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using AdaraBox.Core.Stats;

namespace AdaraBox.App.ViewModels;

public sealed class DriveItemViewModel : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string UsageText { get; set; } = string.Empty;
    public string PercentText { get; set; } = string.Empty;
    public double PercentValue { get; set; }

    public DriveItemViewModel() { }

    public DriveItemViewModel(DriveUsage drive)
    {
        Name = drive.Name;
        UsageText = $"{SizeFormatter.FormatBytes(drive.UsedBytes)} / {SizeFormatter.FormatBytes(drive.TotalBytes)}";
        PercentText = $"{drive.UsedPercent:F0}%";
        PercentValue = drive.UsedPercent;
    }
}

public sealed partial class StatsPanelViewModel : ObservableObject, IDisposable
{
    private readonly StatsPollingService _pollingService;

    [ObservableProperty]
    private string _cpuText = "Mengukur...";

    [ObservableProperty]
    private double _cpuValue;

    [ObservableProperty]
    private bool _isCpuAvailable = true;

    [ObservableProperty]
    private string _memoryDetailText = "Mengukur...";

    [ObservableProperty]
    private string _memoryPercentText = "";

    [ObservableProperty]
    private double _memoryValue;

    [ObservableProperty]
    private bool _isMemoryAvailable = true;

    [ObservableProperty]
    private string _gpuText = "Mengukur...";

    [ObservableProperty]
    private double _gpuValue;

    [ObservableProperty]
    private bool _isGpuAvailable = true;

    [ObservableProperty]
    private string _gpuTooltip = "Data GPU diambil dari Windows Performance Counters (engtype_3D).";

    [ObservableProperty]
    private ObservableCollection<DriveItemViewModel> _drives = new();

    public StatsPanelViewModel(ISystemStatsProvider statsProvider)
    {
        _pollingService = new StatsPollingService(statsProvider);
        _pollingService.SnapshotAvailable += OnSnapshotAvailable;
    }

    public void StartPolling() => _pollingService.Start();
    public void StopPolling() => _pollingService.Stop();

    private void OnSnapshotAvailable(object? sender, StatsSnapshot snapshot)
    {
        var app = Application.Current;
        if (app != null && !app.Dispatcher.HasShutdownStarted)
        {
            app.Dispatcher.InvokeAsync(() => ApplySnapshot(snapshot));
        }
        else
        {
            ApplySnapshot(snapshot);
        }
    }

    public void ApplySnapshot(StatsSnapshot snapshot)
    {
        if (snapshot.CpuPercent.HasValue)
        {
            CpuText = SizeFormatter.FormatPercent(snapshot.CpuPercent.Value);
            CpuValue = snapshot.CpuPercent.Value;
            IsCpuAvailable = true;
        }
        else
        {
            CpuText = "Tidak tersedia";
            CpuValue = 0;
            IsCpuAvailable = false;
        }

        if (snapshot.MemoryUsedBytes.HasValue && snapshot.MemoryTotalBytes.HasValue)
        {
            MemoryDetailText = $"{SizeFormatter.FormatBytes(snapshot.MemoryUsedBytes.Value)} / {SizeFormatter.FormatBytes(snapshot.MemoryTotalBytes.Value)}";
            MemoryPercentText = snapshot.MemoryPercent.HasValue ? SizeFormatter.FormatPercent(snapshot.MemoryPercent.Value) : "";
            MemoryValue = snapshot.MemoryPercent ?? 0.0;
            IsMemoryAvailable = true;
        }
        else
        {
            MemoryDetailText = "Tidak tersedia";
            MemoryPercentText = "";
            MemoryValue = 0;
            IsMemoryAvailable = false;
        }

        if (snapshot.GpuPercent.HasValue)
        {
            GpuText = SizeFormatter.FormatPercent(snapshot.GpuPercent.Value);
            GpuValue = snapshot.GpuPercent.Value;
            IsGpuAvailable = true;
            GpuTooltip = $"Penggunaan GPU 3D: {GpuText}";
        }
        else
        {
            GpuText = "Tidak tersedia";
            GpuValue = 0;
            IsGpuAvailable = false;
            GpuTooltip = "Data GPU tidak tersedia (bergantung pada arsitektur GPU dan driver sistem).";
        }

        // Update drives
        Drives.Clear();
        foreach (var drive in snapshot.Drives)
        {
            Drives.Add(new DriveItemViewModel(drive));
        }
    }

    public void Dispose()
    {
        _pollingService.SnapshotAvailable -= OnSnapshotAvailable;
        _pollingService.Dispose();
    }
}
