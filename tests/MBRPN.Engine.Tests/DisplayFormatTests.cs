using Xunit;

namespace MBRPN.Engine.Tests;

public class DisplayFormatTests
{
    [Fact]
    public void FixControlsDecimalPlaces()
    {
        var c = new TestCalculator().Type("14.87").Press("ENTER").Type("250").Press("*");
        c.DisplayIs("3,717.50");
        c.Press("f", "4").DisplayIs("3,717.5000");
        c.Press("f", "0").DisplayIs("3,718.");
        c.Press("f", "9").DisplayIs("3,717.500000");   // only six decimals fit after four integer digits
        c.Press("f", "2").DisplayIs("3,717.50");
    }

    [Fact]
    public void ScientificDisplayMode()
    {
        var c = new TestCalculator().Type("14.87").Press("ENTER").Type("250").Press("*");
        c.Press("f", ".").DisplayIs("3.717500 03");
        c.Press("f", "2").DisplayIs("3,717.50");
    }

    [Fact]
    public void RoundingIsAwayFromZeroAtMidpoint()
    {
        new TestCalculator().Type("2.5").Press("f", "0").DisplayIs("3.");
        new TestCalculator().Type("2.5").Press("CHS", "f", "0").DisplayIs("-3.");
    }

    [Fact]
    public void FixNineOnSmallNumber()
    {
        var c = new TestCalculator().Type("1").Press("ENTER").Type("3").Press("/");
        c.Press("f", "9").DisplayIs("0.333333333");
    }

    [Fact]
    public void TooLargeForFixFallsBackToScientific()
    {
        var c = new TestCalculator().Type("1234567").Press("ENTER").Type("100000").Press("*");
        c.DisplayIs("1.234567 11");
    }

    [Fact]
    public void OverflowShowsAllNines()
    {
        var c = new TestCalculator().Type("70").Press("g", "n!");
        c.DisplayIs("9.999999 99");
    }

    [Fact]
    public void RndRoundsXToDisplayedValue()
    {
        var c = new TestCalculator().Type("2.6666").Press("ENTER");
        c.Press("f", "RND").DisplayIs("2.67");
        c.Press("f", "4").DisplayIs("2.6700");
    }

    [Fact]
    public void TinyValueShowsAsZeroInFix()
    {
        new TestCalculator().Type("0.0004").Press("ENTER").DisplayIs("0.00");
    }

    [Fact]
    public void NegativeNumberShowsMinusSign()
    {
        new TestCalculator().Type("123.456").Press("CHS", "ENTER").DisplayIs("-123.46");
    }
}
