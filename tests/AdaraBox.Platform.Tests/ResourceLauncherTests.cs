using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Launcher;
using AdaraBox.Core.Resources;
using AdaraBox.Platform.Launcher;
using Xunit;

namespace AdaraBox.Platform.Tests;

public class ResourceLauncherTests
{
    private class FakeProcessStarter : IProcessStarter
    {
        public string? LastFileName { get; private set; }
        public string? LastWorkingDirectory { get; private set; }
        public int CallCount { get; private set; }
        public Action<string, string?>? OnStart { get; set; }

        public void Start(string fileName, string? workingDirectory)
        {
            CallCount++;
            LastFileName = fileName;
            LastWorkingDirectory = workingDirectory;
            OnStart?.Invoke(fileName, workingDirectory);
        }
    }

    private static Resource CreateResource(ResourceType type, string target, string name = "Test Item")
    {
        return new Resource(
            Guid.NewGuid(),
            Guid.NewGuid(),
            name,
            type,
            target,
            0,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task LaunchAsync_ValidLink_CallsStartWithNormalizedUriAndNullWorkingDir()
    {
        // Arrange
        var starter = new FakeProcessStarter();
        var launcher = new ResourceLauncher(starter);
        var resource = CreateResource(ResourceType.Link, "example.com/test");

        // Act
        var result = await launcher.LaunchAsync(resource);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, starter.CallCount);
        Assert.Equal("https://example.com/test", starter.LastFileName);
        Assert.Null(starter.LastWorkingDirectory);
    }

    [Fact]
    public async Task LaunchAsync_DangerousLink_ReturnsInvalidTarget_WithoutCallingStarter()
    {
        // Arrange
        var starter = new FakeProcessStarter();
        var launcher = new ResourceLauncher(starter);
        var resource = CreateResource(ResourceType.Link, "javascript:alert(1)");

        // Act
        var result = await launcher.LaunchAsync(resource);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(LaunchFailureReason.InvalidTarget, result.Reason);
        Assert.Equal(0, starter.CallCount);
    }

    [Fact]
    public async Task LaunchAsync_ExistingFileWithSpacesAndNonAscii_CallsStartWithExactPath()
    {
        // Arrange
        var starter = new FakeProcessStarter();
        var launcher = new ResourceLauncher(starter);

        var tempDir = Path.Combine(Path.GetTempPath(), "AdaraBox_Launcher_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "Dokumén ü spasi.txt");
        File.WriteAllText(filePath, "test content");

        try
        {
            var resource = CreateResource(ResourceType.File, filePath, "Dokumén ü");

            // Act
            var result = await launcher.LaunchAsync(resource);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, starter.CallCount);
            // Verify path is passed verbatim without manual quoting
            Assert.Equal(filePath, starter.LastFileName);
            Assert.Null(starter.LastWorkingDirectory);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LaunchAsync_MissingFile_ReturnsNotFound_WithoutCallingStarter()
    {
        // Arrange
        var starter = new FakeProcessStarter();
        var launcher = new ResourceLauncher(starter);
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "NonExistent_" + Guid.NewGuid().ToString("N") + ".txt");
        var resource = CreateResource(ResourceType.File, nonExistentPath, "Missing");

        // Act
        var result = await launcher.LaunchAsync(resource);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(LaunchFailureReason.NotFound, result.Reason);
        Assert.Equal(0, starter.CallCount);
    }

    [Fact]
    public async Task LaunchAsync_ExistingFolder_CallsStartWithNullWorkingDir()
    {
        // Arrange
        var starter = new FakeProcessStarter();
        var launcher = new ResourceLauncher(starter);

        var tempDir = Path.Combine(Path.GetTempPath(), "AdaraBox_Folder_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var resource = CreateResource(ResourceType.Folder, tempDir, "Test Folder");

            // Act
            var result = await launcher.LaunchAsync(resource);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, starter.CallCount);
            Assert.Equal(tempDir, starter.LastFileName);
            Assert.Null(starter.LastWorkingDirectory);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LaunchAsync_MissingFolder_ReturnsNotFound_WithoutCallingStarter()
    {
        // Arrange
        var starter = new FakeProcessStarter();
        var launcher = new ResourceLauncher(starter);
        var nonExistentDir = Path.Combine(Path.GetTempPath(), "NonExistentDir_" + Guid.NewGuid().ToString("N"));
        var resource = CreateResource(ResourceType.Folder, nonExistentDir, "Missing Dir");

        // Act
        var result = await launcher.LaunchAsync(resource);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(LaunchFailureReason.NotFound, result.Reason);
        Assert.Equal(0, starter.CallCount);
    }

    [Fact]
    public async Task LaunchAsync_ExistingApplication_SetsWorkingDirectoryToParentDir()
    {
        // Arrange
        var starter = new FakeProcessStarter();
        var launcher = new ResourceLauncher(starter);

        var tempDir = Path.Combine(Path.GetTempPath(), "AdaraBox_App_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var appPath = Path.Combine(tempDir, "sample_app.exe");
        File.WriteAllText(appPath, "mock binary");

        try
        {
            var resource = CreateResource(ResourceType.Application, appPath, "Sample App");

            // Act
            var result = await launcher.LaunchAsync(resource);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, starter.CallCount);
            Assert.Equal(appPath, starter.LastFileName);
            Assert.Equal(tempDir, starter.LastWorkingDirectory);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LaunchAsync_StoreAppProtocol_CallsStartDirectly()
    {
        // Arrange
        var starter = new FakeProcessStarter();
        var launcher = new ResourceLauncher(starter);
        const string storeApp = @"shell:AppsFolder\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App";
        var resource = CreateResource(ResourceType.Application, storeApp, "Kalkulator");

        // Act
        var result = await launcher.LaunchAsync(resource);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, starter.CallCount);
        Assert.Equal(storeApp, starter.LastFileName);
        Assert.Null(starter.LastWorkingDirectory);
    }

    [Theory]
    [InlineData(2, LaunchFailureReason.NotFound)]
    [InlineData(3, LaunchFailureReason.NotFound)]
    [InlineData(5, LaunchFailureReason.AccessDenied)]
    [InlineData(1155, LaunchFailureReason.NoHandler)]
    [InlineData(1223, LaunchFailureReason.Cancelled)]
    [InlineData(999, LaunchFailureReason.Unknown)]
    public async Task LaunchAsync_Win32Exception_MapsToCorrectReason(int errorCode, LaunchFailureReason expectedReason)
    {
        // Arrange
        var starter = new FakeProcessStarter();
        starter.OnStart = (fn, wd) => throw new Win32Exception(errorCode);

        var launcher = new ResourceLauncher(starter);
        var resource = CreateResource(ResourceType.Link, "https://example.com");

        // Act
        var result = await launcher.LaunchAsync(resource);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(expectedReason, result.Reason);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task LaunchAsync_UnauthorizedAccessException_ReturnsAccessDenied()
    {
        // Arrange
        var starter = new FakeProcessStarter();
        starter.OnStart = (fn, wd) => throw new UnauthorizedAccessException("Permission denied");

        var launcher = new ResourceLauncher(starter);
        var resource = CreateResource(ResourceType.Link, "https://example.com");

        // Act
        var result = await launcher.LaunchAsync(resource);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(LaunchFailureReason.AccessDenied, result.Reason);
    }

    [Fact]
    public async Task LaunchAsync_PreCancelledToken_ReturnsCancelled_WithoutStarting()
    {
        // Arrange
        var starter = new FakeProcessStarter();
        var launcher = new ResourceLauncher(starter);
        var resource = CreateResource(ResourceType.Link, "https://example.com");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await launcher.LaunchAsync(resource, cts.Token);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(LaunchFailureReason.Cancelled, result.Reason);
        Assert.Equal(0, starter.CallCount);
    }
}
