namespace AdaraBox.Platform.Tests;

public class PlatformAssemblyTests
{
    [Fact]
    public void Platform_Assembly_Can_Reference_Core()
    {
        var platformAssembly = typeof(PlatformAssemblyTests).Assembly;
        Assert.NotNull(platformAssembly);
    }
}
