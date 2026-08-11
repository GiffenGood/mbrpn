using Xunit;

namespace MBRPN.Engine.Tests;

/// <summary>Pins the MBRPN application identity after the MB12C → MBRPN refactor.</summary>
public class RenameVerificationTests
{
    [Fact]
    public void EngineAssemblyIsNamedMbrpn()
    {
        Assert.Equal("MBRPN.Engine", typeof(CalculatorEngine).Assembly.GetName().Name);
    }

    [Fact]
    public void EngineNamespaceIsMbrpn()
    {
        Assert.StartsWith("MBRPN.Engine", typeof(CalculatorEngine).Namespace);
    }

    [Fact]
    public void NoTypeLivesInAnMb12cNamespace()
    {
        var offenders = typeof(CalculatorEngine).Assembly.GetTypes()
            .Where(t => t.Namespace is { } ns && ns.Contains("Mb12c", StringComparison.OrdinalIgnoreCase))
            .Select(t => t.FullName)
            .ToList();
        Assert.Empty(offenders);
    }
}
