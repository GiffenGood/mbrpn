using MBRPN.Engine.Keys;
using MBRPN.Engine.State;
using Xunit;

namespace MBRPN.Engine.Tests;

public class KeyboardMapAndStateTests
{
    [Fact]
    public void ThirtyNineKeysWithUniquePositionsAndKeycodes()
    {
        Assert.Equal(39, KeyboardMap.Keys.Count);
        Assert.Equal(39, KeyboardMap.Keys.Select(k => (k.Row, k.Col)).Distinct().Count());
        Assert.Equal(39, KeyboardMap.Keys.Select(k => k.Keycode).Distinct().Count());
    }

    [Fact]
    public void KeycodesFollowRowColumnConvention()
    {
        Assert.Equal("36", KeyboardMap.Get(KeyId.Enter).Keycode);
        Assert.Equal("10", KeyboardMap.Get(KeyId.Divide).Keycode);   // column 10 → 0
        Assert.Equal("40", KeyboardMap.Get(KeyId.Add).Keycode);
        Assert.Equal("7", KeyboardMap.Get(KeyId.D7).Keycode);        // digits show their value
        Assert.Equal("48", KeyboardMap.Get(KeyId.Point).Keycode);
        Assert.Equal("43", KeyboardMap.Get(KeyId.G).Keycode);
    }

    [Fact]
    public void SnapshotRoundTripsThroughJson()
    {
        var c = new TestCalculator();
        c.Type("42.5").Press("STO", "7");
        c.Type("360").Press("n").Type("1.5").Press("i");
        c.Press("g", "BEG").Press("g", "D.MY").Press("f", "4");
        c.Press("f", "P/R");
        c.Press("2", "*");
        c.Press("f", "P/R");

        string json = StateSerializer.ToJson(c.Engine.Snapshot());
        var restored = new TestCalculator();
        restored.Engine.Restore(StateSerializer.FromJson(json)!);

        restored.Press("RCL", "7").DisplayIs("42.5000");
        restored.Press("RCL", "n").DisplayIs("360.0000");
        restored.Press("RCL", "i").DisplayIs("1.5000");
        Assert.True(restored.Engine.Fin.BeginMode);
        Assert.True(restored.Engine.DmyMode);
        Assert.Equal(2, restored.Engine.Program.Count);

        restored.Type("10").Press("R/S").RunProgram().DisplayIs("20.0000");
    }

    [Fact]
    public void OnKeyTogglesOffAndPreservesState()
    {
        var c = new TestCalculator();
        c.Type("77").Press("ENTER");
        c.Press("ON");
        Assert.True(c.Engine.IsOff);
        Assert.Equal(string.Empty, c.Engine.DisplayText);
        c.Press("ON");
        c.DisplayIs("77.00");
    }
}
