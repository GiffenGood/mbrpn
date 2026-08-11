using MBRPN.Engine.History;
using MBRPN.Engine.Keys;
using Xunit;

namespace MBRPN.Engine.Tests;

public class HistoryLabelerTests
{
    [Theory]
    [InlineData("+", new[] { KeyId.Add })]
    [InlineData("ENTER", new[] { KeyId.Enter })]
    [InlineData("f AMORT", new[] { KeyId.F, KeyId.N })]
    [InlineData("g DATE", new[] { KeyId.G, KeyId.CHS })]
    [InlineData("g √x", new[] { KeyId.G, KeyId.YtoX })]
    [InlineData("STO + 4", new[] { KeyId.STO, KeyId.Add, KeyId.D4 })]
    [InlineData("STO .5", new[] { KeyId.STO, KeyId.Point, KeyId.D5 })]
    [InlineData("RCL .5", new[] { KeyId.RCL, KeyId.Point, KeyId.D5 })]
    [InlineData("RCL PV", new[] { KeyId.RCL, KeyId.PV })]
    [InlineData("RCL g CFj", new[] { KeyId.RCL, KeyId.G, KeyId.PMT })]
    [InlineData("STO EEX", new[] { KeyId.STO, KeyId.EEX })]
    [InlineData("f .", new[] { KeyId.F, KeyId.Point })]
    [InlineData("g GTO 12", new[] { KeyId.G, KeyId.RollDown, KeyId.D1, KeyId.D2 })]
    public void RendersKeySequences(string expected, KeyId[] keys)
    {
        Assert.Equal(expected, HistoryLabeler.Label(keys));
    }
}
