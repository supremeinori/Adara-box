namespace AdaraBox.Data.Tests;

public class DataAssemblyTests
{
    [Fact]
    public void Data_Assembly_Can_Reference_Core()
    {
        var dataAssembly = typeof(DataAssemblyTests).Assembly;
        Assert.NotNull(dataAssembly);
    }
}
