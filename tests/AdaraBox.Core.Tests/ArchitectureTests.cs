namespace AdaraBox.Core.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Core_Assembly_Must_Not_Reference_Forbidden_Assemblies()
    {
        // Arrange
        var coreAssembly = typeof(Common.OperationResult).Assembly;
        var referencedAssemblies = coreAssembly.GetReferencedAssemblies();
        var referencedNames = referencedAssemblies.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var forbiddenAssemblies = new[]
        {
            "PresentationFramework",
            "PresentationCore",
            "WindowsBase",
            "System.Windows",
            "Microsoft.Data.Sqlite",
            "AdaraBox.Platform",
            "AdaraBox.Data",
            "AdaraBox.App"
        };

        // Act & Assert
        foreach (var forbidden in forbiddenAssemblies)
        {
            Assert.False(
                referencedNames.Contains(forbidden),
                $"Architecture violation: AdaraBox.Core must not reference '{forbidden}'."
            );
        }
    }
}
