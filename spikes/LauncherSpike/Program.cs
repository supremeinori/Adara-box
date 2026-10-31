using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace AdaraBox.LauncherSpike;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("Adara Box — Spike C: Resource Launcher Verification");
        Console.WriteLine("==================================================");

        var tempDir = Path.Combine(Path.GetTempPath(), "AdaraBox_LauncherSpike_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Test 1: File with space and non-ASCII character (Dokumén ü.txt)
            Console.WriteLine("\n[1] Testing file with spaces and Unicode (Dokumén ü.txt)...");
            var unicodeFilePath = Path.Combine(tempDir, "Dokumén ü.txt");
            File.WriteAllText(unicodeFilePath, "Isi dokumen uji Adara Box non-ASCII üéá.");
            var resultFile = TryLaunchTarget(unicodeFilePath);
            Console.WriteLine($"Result: {resultFile}");

            // Test 2: Folder with spaces
            Console.WriteLine("\n[2] Testing folder with spaces...");
            var spaceFolderPath = Path.Combine(tempDir, "Folder Uji Coba");
            Directory.CreateDirectory(spaceFolderPath);
            var resultFolder = TryLaunchTarget(spaceFolderPath);
            Console.WriteLine($"Result: {resultFolder}");

            // Test 3: URL
            Console.WriteLine("\n[3] Testing URL (https://example.com)...");
            var resultUrl = TryLaunchTarget("https://example.com");
            Console.WriteLine($"Result: {resultUrl}");

            // Test 4: Applications
            Console.WriteLine("\n[4] Testing Application (notepad.exe)...");
            var resultApp = TryLaunchTarget("notepad.exe");
            Console.WriteLine($"Result: {resultApp}");

            // Test 5: Error Scenarios (NativeErrorCode Verification)
            Console.WriteLine("\n[5] Verifying Win32 Error Codes:");

            // 5a. File not found -> Expected ErrorCode 2 (ERROR_FILE_NOT_FOUND)
            var nonExistentTarget = Path.Combine(tempDir, "TidakAda_File_Pasti_Hilang.xyz");
            var codeNotFound = GetLaunchErrorCode(nonExistentTarget);
            Console.WriteLine($"  - Non-existent file error code: {codeNotFound} (Expected: 2 ERROR_FILE_NOT_FOUND)");

            // 5b. Unassociated extension file -> Expected ErrorCode 1155 (ERROR_NO_ASSOCIATION) or 2
            var dummyUnassociatedFile = Path.Combine(tempDir, "test.unassociated_extension_adarabox");
            File.WriteAllText(dummyUnassociatedFile, "dummy");
            var codeNoAssoc = GetLaunchErrorCode(dummyUnassociatedFile);
            Console.WriteLine($"  - No association file error code: {codeNoAssoc} (Expected: 1155 or 2 depending on Windows Shell)");

            // Summary of Win32 Error Mapping
            Console.WriteLine("\n[6] Win32 Error Code Mapping for Adara Box Core/M04:");
            Console.WriteLine("  2    -> ResourceNotFoundError ('Target tidak ditemukan')");
            Console.WriteLine("  5    -> AccessDeniedError ('Akses ditolak')");
            Console.WriteLine("  1155 -> NoAssociationError ('Tidak ada aplikasi default untuk jenis file ini')");
            Console.WriteLine("  1223 -> UserCancelledError ('Aksi dibatalkan oleh pengguna')");

            Console.WriteLine("\n[7] WorkingDirectory finding:");
            Console.WriteLine("  When target is an executable file (.exe), setting WorkingDirectory to Path.GetDirectoryName(target)");
            Console.WriteLine("  ensures relative DLLs, configuration files, and assets are found by the application.");

            Console.WriteLine("\nSpike C verification complete.");
            return 0;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch
            {
                // ignore temp cleanup
            }
        }
    }

    public static string TryLaunchTarget(string target)
    {
        try
        {
            var psi = new ProcessStartInfo(target)
            {
                UseShellExecute = true
            };

            // If target is an executable with a known parent directory, set working dir
            if (File.Exists(target) && Path.GetExtension(target).Equals(".exe", StringComparison.OrdinalIgnoreCase))
            {
                var dir = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(dir))
                {
                    psi.WorkingDirectory = dir;
                }
            }

            using var process = Process.Start(psi);
            return $"Success (ProcessId: {process?.Id ?? 0})";
        }
        catch (Win32Exception ex)
        {
            return $"Failed (Win32Exception: NativeErrorCode={ex.NativeErrorCode}, Message={ex.Message})";
        }
        catch (Exception ex)
        {
            return $"Failed ({ex.GetType().Name}: {ex.Message})";
        }
    }

    public static int GetLaunchErrorCode(string target)
    {
        try
        {
            var psi = new ProcessStartInfo(target)
            {
                UseShellExecute = true
            };
            using var p = Process.Start(psi);
            return 0;
        }
        catch (Win32Exception ex)
        {
            return ex.NativeErrorCode;
        }
        catch
        {
            return -1;
        }
    }
}
