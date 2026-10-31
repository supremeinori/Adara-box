using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using AdaraBox.Core.Launcher;
using AdaraBox.Core.Resources;

namespace AdaraBox.Platform.Launcher;

/// <summary>
/// Platform implementation of IResourceLauncher using ShellExecute via IProcessStarter.
/// Complies with Golden Rule #3 (no shell strings, direct start) and privacy rules.
/// </summary>
public sealed class ResourceLauncher : IResourceLauncher
{
    private readonly IProcessStarter _processStarter;
    private readonly ILogger _logger;

    public ResourceLauncher(IProcessStarter processStarter, ILogger? logger = null)
    {
        _processStarter = processStarter ?? throw new ArgumentNullException(nameof(processStarter));
        _logger = logger ?? Log.ForContext<ResourceLauncher>();
    }

    public Task<LaunchResult> LaunchAsync(Resource resource, CancellationToken ct = default)
    {
        if (resource == null) throw new ArgumentNullException(nameof(resource));

        if (ct.IsCancellationRequested)
        {
            return Task.FromResult(new LaunchResult(
                resource.Id,
                resource.Name,
                Success: false,
                Reason: LaunchFailureReason.Cancelled,
                Message: "Tindakan dibatalkan."));
        }

        // 1. Revalidate target (Rule 1)
        var validation = ResourceTargetValidator.Validate(resource.Type, resource.Target, resource.Name);
        if (!validation.Success)
        {
            _logger.Warning("Resource target validation failed: Type={ResourceType}, Code={ErrorCode}", resource.Type, validation.Error?.Code);
            _logger.Debug("Invalid target was: {Target}", resource.Target);
            return Task.FromResult(new LaunchResult(
                resource.Id,
                resource.Name,
                Success: false,
                Reason: LaunchFailureReason.InvalidTarget,
                Message: validation.Error?.Message ?? "Target tidak valid."));
        }

        var normalizedTarget = validation.Value.NormalizedTarget;

        try
        {
            // 2. Pre-existence check & Launch per type
            switch (resource.Type)
            {
                case ResourceType.Link:
                    // Handled by default browser
                    _processStarter.Start(normalizedTarget, null);
                    break;

                case ResourceType.File:
                    if (!File.Exists(normalizedTarget))
                    {
                        _logger.Warning("File not found on disk: Type={ResourceType}", resource.Type);
                        _logger.Debug("Target missing: {Target}", normalizedTarget);
                        return Task.FromResult(new LaunchResult(
                            resource.Id,
                            resource.Name,
                            Success: false,
                            Reason: LaunchFailureReason.NotFound,
                            Message: "File tidak ditemukan di disk."));
                    }
                    _processStarter.Start(normalizedTarget, null);
                    break;

                case ResourceType.Folder:
                    if (!Directory.Exists(normalizedTarget))
                    {
                        _logger.Warning("Folder not found on disk: Type={ResourceType}", resource.Type);
                        _logger.Debug("Target missing: {Target}", normalizedTarget);
                        return Task.FromResult(new LaunchResult(
                            resource.Id,
                            resource.Name,
                            Success: false,
                            Reason: LaunchFailureReason.NotFound,
                            Message: "Folder tidak ditemukan di disk."));
                    }
                    _processStarter.Start(normalizedTarget, null);
                    break;

                case ResourceType.Application:
                    if (normalizedTarget.StartsWith(@"shell:AppsFolder\", StringComparison.OrdinalIgnoreCase))
                    {
                        _processStarter.Start(normalizedTarget, null);
                    }
                    else
                    {
                        if (!File.Exists(normalizedTarget))
                        {
                            _logger.Warning("Application not found on disk: Type={ResourceType}", resource.Type);
                            _logger.Debug("Target missing: {Target}", normalizedTarget);
                            return Task.FromResult(new LaunchResult(
                                resource.Id,
                                resource.Name,
                                Success: false,
                                Reason: LaunchFailureReason.NotFound,
                                Message: "Aplikasi tidak ditemukan di disk."));
                        }

                        var workingDirectory = Path.GetDirectoryName(normalizedTarget);
                        _processStarter.Start(normalizedTarget, workingDirectory);
                    }
                    break;

                default:
                    return Task.FromResult(new LaunchResult(
                        resource.Id,
                        resource.Name,
                        Success: false,
                        Reason: LaunchFailureReason.InvalidTarget,
                        Message: "Jenis resource tidak didukung."));
            }

            // Log Information: type + result ONLY (no path for privacy)
            _logger.Information("Resource launched successfully: Type={ResourceType}, Result=Success", resource.Type);
            _logger.Debug("Launched target path: {Target}", normalizedTarget);

            return Task.FromResult(new LaunchResult(resource.Id, resource.Name, Success: true));
        }
        catch (Win32Exception w32Ex)
        {
            var (reason, message) = MapWin32Error(w32Ex.NativeErrorCode);
            _logger.Warning("Win32 error launching resource: Type={ResourceType}, NativeErrorCode={NativeErrorCode}, Reason={Reason}",
                resource.Type, w32Ex.NativeErrorCode, reason);
            _logger.Debug(w32Ex, "Win32 launch exception for target: {Target}", normalizedTarget);

            return Task.FromResult(new LaunchResult(
                resource.Id,
                resource.Name,
                Success: false,
                Reason: reason,
                Message: message));
        }
        catch (UnauthorizedAccessException uEx)
        {
            _logger.Warning("Access denied launching resource: Type={ResourceType}", resource.Type);
            _logger.Debug(uEx, "Access denied for target: {Target}", normalizedTarget);

            return Task.FromResult(new LaunchResult(
                resource.Id,
                resource.Name,
                Success: false,
                Reason: LaunchFailureReason.AccessDenied,
                Message: "Akses ke file/folder ditolak (memerlukan izin khusus)."));
        }
        catch (FileNotFoundException fnfEx)
        {
            _logger.Warning("File not found launching resource: Type={ResourceType}", resource.Type);
            _logger.Debug(fnfEx, "File not found for target: {Target}", normalizedTarget);

            return Task.FromResult(new LaunchResult(
                resource.Id,
                resource.Name,
                Success: false,
                Reason: LaunchFailureReason.NotFound,
                Message: "Target tidak ditemukan di lokasi yang ditentukan."));
        }
        catch (DirectoryNotFoundException dnfEx)
        {
            _logger.Warning("Directory not found launching resource: Type={ResourceType}", resource.Type);
            _logger.Debug(dnfEx, "Directory not found for target: {Target}", normalizedTarget);

            return Task.FromResult(new LaunchResult(
                resource.Id,
                resource.Name,
                Success: false,
                Reason: LaunchFailureReason.NotFound,
                Message: "Folder tidak ditemukan di lokasi yang ditentukan."));
        }
        catch (InvalidOperationException ioEx)
        {
            _logger.Warning("Invalid operation launching resource: Type={ResourceType}", resource.Type);
            _logger.Debug(ioEx, "Invalid operation for target: {Target}", normalizedTarget);

            return Task.FromResult(new LaunchResult(
                resource.Id,
                resource.Name,
                Success: false,
                Reason: LaunchFailureReason.InvalidTarget,
                Message: "Target tidak dapat dijalankan."));
        }
        catch (IOException ioEx)
        {
            _logger.Warning("IO exception launching resource: Type={ResourceType}", resource.Type);
            _logger.Debug(ioEx, "IO exception for target: {Target}", normalizedTarget);

            return Task.FromResult(new LaunchResult(
                resource.Id,
                resource.Name,
                Success: false,
                Reason: LaunchFailureReason.Unknown,
                Message: "Terjadi kesalahan I/O saat membuka resource."));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger.Error(ex, "Unexpected error launching resource: Type={ResourceType}", resource.Type);

            return Task.FromResult(new LaunchResult(
                resource.Id,
                resource.Name,
                Success: false,
                Reason: LaunchFailureReason.Unknown,
                Message: "Terjadi kesalahan tidak terduga saat membuka resource."));
        }
    }

    private static (LaunchFailureReason Reason, string Message) MapWin32Error(int nativeErrorCode) => nativeErrorCode switch
    {
        2 or 3 => (LaunchFailureReason.NotFound, "Target tidak ditemukan di lokasi yang ditentukan."),
        5 => (LaunchFailureReason.AccessDenied, "Akses ke file/folder ditolak (memerlukan izin khusus)."),
        1155 => (LaunchFailureReason.NoHandler, "Tidak ada aplikasi default di Windows untuk membuka jenis file ini."),
        1223 => (LaunchFailureReason.Cancelled, "Tindakan dibatalkan oleh pengguna (prompt UAC ditolak)."),
        _ => (LaunchFailureReason.Unknown, $"Gagal membuka resource (Kode kesalahan Windows: {nativeErrorCode}).")
    };
}
