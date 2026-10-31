using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Resources;
using AdaraBox.Platform.Availability;
using AdaraBox.Platform.Icons;
using Xunit;

namespace AdaraBox.Platform.Tests;

public class PlatformResourceTests
{
    [Fact]
    public async Task AvailabilityChecker_Returns_Available_For_Existing_File()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var res = new Resource(Guid.NewGuid(), Guid.NewGuid(), "Temp File", ResourceType.File, tempFile, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            var checker = new ResourceAvailabilityChecker();

            var result = await checker.CheckAsync(res);
            Assert.Equal(ResourceAvailability.Available, result);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AvailabilityChecker_Returns_Available_For_Existing_Folder()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"adarabox_test_dir_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var res = new Resource(Guid.NewGuid(), Guid.NewGuid(), "Temp Dir", ResourceType.Folder, tempDir, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            var checker = new ResourceAvailabilityChecker();

            var result = await checker.CheckAsync(res);
            Assert.Equal(ResourceAvailability.Available, result);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir);
        }
    }

    [Fact]
    public async Task AvailabilityChecker_Returns_Missing_For_NonExistent_Path()
    {
        var missingPath = @"C:\NonExistent_AdaraBox_Test_Path\File_123456789.xyz";
        var res = new Resource(Guid.NewGuid(), Guid.NewGuid(), "Missing File", ResourceType.File, missingPath, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var checker = new ResourceAvailabilityChecker();

        var result = await checker.CheckAsync(res);
        Assert.Equal(ResourceAvailability.Missing, result);
    }

    [Fact]
    public async Task AvailabilityChecker_Returns_Unknown_For_Links_Without_Network_Request()
    {
        // Golden Rule #5: Never make network requests for links
        var res = new Resource(Guid.NewGuid(), Guid.NewGuid(), "Link", ResourceType.Link, "https://google.com", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var checker = new ResourceAvailabilityChecker();

        var result = await checker.CheckAsync(res);
        Assert.Equal(ResourceAvailability.Unknown, result);
    }

    [Fact]
    public async Task AvailabilityChecker_Returns_Unknown_When_Timeout_Expires()
    {
        var checker = new ResourceAvailabilityChecker(maxConcurrency: 1, timeout: TimeSpan.FromMilliseconds(1));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var res = new Resource(Guid.NewGuid(), Guid.NewGuid(), "File", ResourceType.File, @"C:\Docs\file.txt", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var result = await checker.CheckAsync(res, cts.Token);

        Assert.Equal(ResourceAvailability.Unknown, result);
    }

    [Theory]
    [InlineData(ResourceType.File, @"C:\NonExistent\test.txt")]
    [InlineData(ResourceType.Folder, @"C:\NonExistent\TestFolder")]
    [InlineData(ResourceType.Application, @"C:\NonExistent\app.exe")]
    [InlineData(ResourceType.Link, "https://example.com")]
    public void IconProvider_Returns_Non_Null_ImageSource_Without_Throwing(ResourceType type, string target)
    {
        var provider = new IconProvider();
        var icon = provider.GetIcon(type, target);

        Assert.NotNull(icon);
    }
}
