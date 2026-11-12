import { Cpu, MemoryStick, HardDrive, Monitor, ArrowUpDown, BatteryCharging, Battery, Clock } from "lucide-react";

export interface SystemMetricsData {
  cpu: number;
  ram: number;
  ram_used_gb?: number;
  ram_total_gb?: number;
  disk_free_gb: number;
  disk_used_gb?: number;
  disk_total_gb?: number;
  net_up: number;
  net_down: number;
  cpu_name?: string;
  cpu_cores?: number;
  uptime_secs?: number;
  gpu_name?: string | null;
  gpu_vram_used_mb?: number | null;
  gpu_vram_total_mb?: number | null;
}

interface SystemStatsPanelProps {
  metrics: SystemMetricsData;
  hasBattery: boolean;
  batteryLevel: number;
  isCharging: boolean;
  online: boolean;
  connectionLabel: string;
}

function formatRate(bytesPerSec: number): string {
  if (bytesPerSec < 1024) return `${bytesPerSec} B/s`;
  if (bytesPerSec < 1024 * 1024) return `${(bytesPerSec / 1024).toFixed(1)} KB/s`;
  return `${(bytesPerSec / (1024 * 1024)).toFixed(1)} MB/s`;
}

function formatUptime(secs?: number): string {
  if (!secs || secs < 0) return "--";
  const d = Math.floor(secs / 86400);
  const h = Math.floor((secs % 86400) / 3600);
  const m = Math.floor((secs % 3600) / 60);
  if (d > 0) return `${d}d ${h}h ${m}m`;
  if (h > 0) return `${h}h ${m}m`;
  return `${m}m`;
}

function formatVramMb(mb: number): string {
  if (mb >= 1024) return `${(mb / 1024).toFixed(1)} GB`;
  return `${mb} MB`;
}

export function SystemStatsPanel({
  metrics,
  hasBattery,
  batteryLevel,
  isCharging,
  online,
  connectionLabel,
}: SystemStatsPanelProps) {
  const cpuPct = Math.max(0, Math.min(100, Math.round(metrics.cpu)));
  const ramPct = Math.max(0, Math.min(100, Math.round(metrics.ram)));
  const diskTotal = metrics.disk_total_gb ?? 0;
  const diskUsed = metrics.disk_used_gb ?? 0;
  const diskPct = diskTotal > 0 ? Math.max(0, Math.min(100, Math.round((diskUsed / diskTotal) * 100))) : 0;

  const ramDetail =
    metrics.ram_total_gb && metrics.ram_total_gb > 0
      ? `${(metrics.ram_used_gb ?? 0).toFixed(1)} / ${metrics.ram_total_gb.toFixed(1)} GB`
      : `${ramPct}%`;

  const diskDetail =
    diskTotal > 0
      ? `${diskUsed} / ${diskTotal} GB (${metrics.disk_free_gb} GB free)`
      : `${metrics.disk_free_gb} GB free`;

  const gpuTotalMb = metrics.gpu_vram_total_mb ?? null;
  const gpuUsedMb = metrics.gpu_vram_used_mb ?? null;
  const gpuPct =
    gpuTotalMb && gpuTotalMb > 0 && gpuUsedMb !== null
      ? Math.max(0, Math.min(100, Math.round((gpuUsedMb / gpuTotalMb) * 100)))
      : null;

  return (
    <div className="sys-stats-panel" onClick={(e) => e.stopPropagation()}>
      <div className="sys-stats-header">
        <span className="sys-stats-cpu-model" title={metrics.cpu_name || "Processor"}>
          {metrics.cpu_name || "PC Hardware Monitor"}
        </span>
        <div className="sys-stats-badges">
          {metrics.cpu_cores ? <span className="sys-stats-chip">{metrics.cpu_cores}T</span> : null}
          <span className="sys-stats-chip">
            <Clock size={10} strokeWidth={2} />
            {formatUptime(metrics.uptime_secs)}
          </span>
        </div>
      </div>

      <div className="sys-stats-grid">
        <div className="sys-stat-card">
          <div className="sys-stat-top">
            <span className="sys-stat-title">
              <Cpu size={13} strokeWidth={2} /> CPU
            </span>
            <span className="sys-stat-value">{cpuPct}%</span>
          </div>
          <div className="sys-stat-bar">
            <div className="sys-stat-bar-fill" style={{ width: `${cpuPct}%` }} />
          </div>
          <div className="sys-stat-sub">System processor load</div>
        </div>

        <div className="sys-stat-card">
          <div className="sys-stat-top">
            <span className="sys-stat-title">
              <MemoryStick size={13} strokeWidth={2} /> RAM
            </span>
            <span className="sys-stat-value">{ramPct}%</span>
          </div>
          <div className="sys-stat-bar">
            <div className="sys-stat-bar-fill" style={{ width: `${ramPct}%` }} />
          </div>
          <div className="sys-stat-sub">{ramDetail}</div>
        </div>

        <div className="sys-stat-card">
          <div className="sys-stat-top">
            <span className="sys-stat-title">
              <HardDrive size={13} strokeWidth={2} /> Disk (C:)
            </span>
            <span className="sys-stat-value">{diskTotal > 0 ? `${diskPct}%` : `${metrics.disk_free_gb} GB`}</span>
          </div>
          <div className="sys-stat-bar">
            <div className="sys-stat-bar-fill" style={{ width: `${diskPct}%` }} />
          </div>
          <div className="sys-stat-sub">{diskDetail}</div>
        </div>

        <div className="sys-stat-card">
          <div className="sys-stat-top">
            <span className="sys-stat-title">
              <Monitor size={13} strokeWidth={2} /> GPU
            </span>
            <span className="sys-stat-value">
              {gpuPct !== null ? `${gpuPct}%` : gpuTotalMb ? formatVramMb(gpuTotalMb) : "Active"}
            </span>
          </div>
          <div className="sys-stat-bar">
            <div className="sys-stat-bar-fill" style={{ width: `${gpuPct ?? (metrics.gpu_name ? 32 : 0)}%` }} />
          </div>
          <div className="sys-stat-sub" title={metrics.gpu_name || undefined}>
            {metrics.gpu_name
              ? gpuUsedMb !== null && gpuTotalMb
                ? `${metrics.gpu_name} · ${formatVramMb(gpuUsedMb)}/${formatVramMb(gpuTotalMb)}`
                : metrics.gpu_name
              : "Display adapter"}
          </div>
        </div>
      </div>

      <div className="sys-stats-footer">
        <div className="sys-stats-net">
          <ArrowUpDown size={12} strokeWidth={2} />
          <span>{connectionLabel || (online ? "Online" : "Offline")}</span>
          <span className="sys-stats-net-rates">
            ↑ {formatRate(metrics.net_up)} · ↓ {formatRate(metrics.net_down)}
          </span>
        </div>
        {hasBattery && (
          <div className="sys-stats-power">
            {isCharging ? <BatteryCharging size={12} strokeWidth={2} /> : <Battery size={12} strokeWidth={2} />}
            <span>
              {batteryLevel}%{isCharging ? " Charging" : ""}
            </span>
          </div>
        )}
      </div>
    </div>
  );
}
