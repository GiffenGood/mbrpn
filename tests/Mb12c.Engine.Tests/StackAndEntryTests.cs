using Xunit;

namespace Mb12c.Engine.Tests;

public class StackAndEntryTests
{
    [Fact]
    public void PowerOnShowsZero() => new TestCalculator().DisplayIs("0.00");

    [Fact]
    public void SimpleAddition() =>
        new TestCalculator().Type("3").Press("ENTER").Type("4").Press("+").DisplayIs("7.00");

    [Fact]
    public void EnterDisablesStackLift_DigitOverwritesX()
    {
        // 5 ENTER 6 +  →  11 (the 6 overwrites the copy of 5 in X)
        new TestCalculator().Type("5").Press("ENTER").Type("6").Press("+").DisplayIs("11.00");
    }

    [Fact]
    public void TRegisterReplicatesOnDrop()
    {
        var c = new TestCalculator();
        c.Type("1").Press("ENTER").Type("2").Press("ENTER").Type("3").Press("ENTER").Type("4");
        c.Press("+").DisplayIs("7.00");   // X=7 Y=2 Z=1 T=1
        c.Press("+").DisplayIs("9.00");   // X=9 Y=1 Z=1 T=1
        c.Press("+").DisplayIs("10.00");  // T keeps replicating
        c.Press("+").DisplayIs("11.00");
    }

    [Fact]
    public void ClxDisablesLift()
    {
        // 5 ENTER 8 CLx 7 +  →  12 (7 overwrites the cleared X)
        new TestCalculator().Type("5").Press("ENTER").Type("8").Press("CLx").Type("7").Press("+")
            .DisplayIs("12.00");
    }

    [Fact]
    public void RollDownCyclesStack()
    {
        var c = new TestCalculator();
        c.Type("1").Press("ENTER").Type("2").Press("ENTER").Type("3").Press("ENTER").Type("4");
        c.Press("Rv").DisplayIs("3.00");
        c.Press("Rv").DisplayIs("2.00");
        c.Press("Rv").DisplayIs("1.00");
        c.Press("Rv").DisplayIs("4.00");
    }

    [Fact]
    public void SwapExchangesXAndY()
    {
        var c = new TestCalculator().Type("5").Press("ENTER").Type("9");
        c.Press("x<>y").DisplayIs("5.00");
        Assert.Equal(9m, c.Y.ToDecimal());
    }

    [Fact]
    public void LastXAfterDivision()
    {
        var c = new TestCalculator().Type("12").Press("ENTER").Type("3").Press("/");
        c.DisplayIs("4.00");
        c.Press("g", "LSTx").DisplayIs("3.00");
        Assert.Equal(4m, c.Y.ToDecimal());
    }

    [Fact]
    public void ChsDuringEntryNegatesMantissa() =>
        new TestCalculator().Type("5").Press("CHS").DisplayIs("-5.");

    [Fact]
    public void ChsOutsideEntryNegatesX()
    {
        new TestCalculator().Type("5").Press("ENTER").Press("CHS").DisplayIs("-5.00");
    }

    [Fact]
    public void EexEntry()
    {
        var c = new TestCalculator().Type("1.5").Press("EEX").Type("6");
        c.DisplayIs("1.5 06");
        c.Press("ENTER").DisplayIs("1,500,000.00");
    }

    [Fact]
    public void EexChsMakesExponentNegative()
    {
        var c = new TestCalculator().Type("2").Press("EEX").Type("3").Press("CHS");
        c.DisplayIs("2.-03");
        c.Press("ENTER").DisplayIs("0.00");   // 0.002 rounds to 0.00 at FIX 2
        c.Press("f", "4").DisplayIs("0.0020");
    }

    [Fact]
    public void EntryLimitedToTenDigits()
    {
        new TestCalculator().Type("12345678901234").Press("ENTER")
            .DisplayIs("1,234,567,890.");
    }

    [Fact]
    public void LeadingZeroIsReplaced() =>
        new TestCalculator().Type("07").DisplayIs("7.");

    [Fact]
    public void DigitEntryAfterResultLiftsStack()
    {
        // 3 ENTER 4 + then 5 ×  →  35 (7 lifted into Y)
        new TestCalculator().Type("3").Press("ENTER").Type("4").Press("+").Type("5").Press("*")
            .DisplayIs("35.00");
    }

    [Fact]
    public void MantissaPeekShowsAllTenDigits()
    {
        var c = new TestCalculator().Type("14.87").Press("ENTER").Type("250").Press("*");
        c.DisplayIs("3,717.50");
        c.Press("f", "CLEARPREFIX").DisplayIs("3717500000");
        c.Press("CLx").DisplayIs("0.00");
    }

    [Fact]
    public void ErrorIsClearedByNextKeyWhichIsSwallowed()
    {
        var c = new TestCalculator().Type("5").Press("ENTER").Type("0").Press("/");
        c.DisplayIs("Error 0");
        c.Press("9").DisplayIs("0.00"); // the 9 only clears the error
        c.Type("9").DisplayIs("9.");
    }
}
