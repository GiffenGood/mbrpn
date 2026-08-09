using Xunit;

namespace Mb12c.Engine.Tests;

public class DateAndStatisticsTests
{
    [Fact]
    public void DaysBetweenDates_ActualAnd360()
    {
        // 4/15/2004 → 6/3/2004: 49 actual days, 48 on a 30/360 basis
        var c = new TestCalculator();
        c.Press("g", "M.DY");
        c.Type("4.152004").Press("ENTER").Type("6.032004").Press("g", "DDYS");
        c.DisplayIs("49.00");
        c.Press("x<>y").DisplayIs("48.00");
    }

    [Fact]
    public void FutureDateWithWeekday()
    {
        // 3/1/2004 + 60 days = 4/30/2004, a Friday (weekday 5)
        var c = new TestCalculator();
        c.Press("g", "M.DY");
        c.Type("3.012004").Press("ENTER").Type("60").Press("g", "DATE");
        c.DisplayIs("4,30,2004 5");
        c.Press("f", "6");                         // any key returns to the numeric display
        c.DisplayIs("4.302004");
    }

    [Fact]
    public void PastDate()
    {
        var c = new TestCalculator();
        c.Press("g", "M.DY");
        c.Type("3.012004").Press("ENTER").Type("-30").Press("g", "DATE");
        c.DisplayIs("1,31,2004 6");                // Jan 31 2004 was a Saturday
    }

    [Fact]
    public void DmyModeParsesDayFirst()
    {
        // In D.MY mode 15.042004 is 15 April 2004; +49 days = 3 June 2004 (Thursday)
        var c = new TestCalculator();
        c.Press("g", "D.MY");
        c.Type("15.042004").Press("ENTER").Type("49").Press("g", "DATE");
        c.DisplayIs("3,06,2004 4");
        c.Press("g", "M.DY");
    }

    [Fact]
    public void ThirtyOneDayRule()
    {
        // Handbook 30/360 rule: 31sts count as 30ths when the earlier date is ≥ 30
        var c = new TestCalculator();
        c.Press("g", "M.DY");
        c.Type("1.312004").Press("ENTER").Type("3.312004").Press("g", "DDYS");
        c.DisplayIs("60.00");
        c.Press("x<>y").DisplayIs("60.00");
    }

    [Fact]
    public void InvalidDateIsError8()
    {
        var c = new TestCalculator();
        c.Press("g", "M.DY");
        c.Type("2.302004").Press("ENTER").Type("10").Press("g", "DATE").DisplayIs("Error 8");
    }

    [Fact]
    public void MeanAndStandardDeviation()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARS");
        c.Type("2").Press("S+").DisplayIs("1.00");
        c.Type("4").Press("S+").DisplayIs("2.00");
        c.Type("6").Press("S+").DisplayIs("3.00");
        c.Press("g", "mean").DisplayIs("4.00");
        c.Press("g", "s").DisplayIs("2.00");
    }

    [Fact]
    public void SigmaMinusCorrectsAnEntry()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARS");
        c.Type("2").Press("S+").Type("4").Press("S+").Type("9").Press("S+");
        c.Type("9").Press("g", "S-").DisplayIs("2.00");   // n back to 2
        c.Type("6").Press("S+");
        c.Press("g", "mean").DisplayIs("4.00");
    }

    [Fact]
    public void TwoVariableAccumulationAndLinearEstimation()
    {
        // Points (1,2) (2,4) (3,6): perfect line y = 2x
        var c = new TestCalculator();
        c.Press("f", "CLEARS");
        c.Type("2").Press("ENTER").Type("1").Press("S+");
        c.Type("4").Press("ENTER").Type("2").Press("S+");
        c.Type("6").Press("ENTER").Type("3").Press("S+");
        c.Type("4").Press("g", "yr").DisplayIs("8.00");   // ŷ at x = 4
        c.Press("x<>y").DisplayIs("1.00");                // r = 1
        c.Type("10").Press("g", "xr").DisplayIs("5.00");  // x̂ at y = 10
    }

    [Fact]
    public void WeightedMean()
    {
        // 30 at weight 2, 40 at weight 3 → (60+120)/5 = 36
        var c = new TestCalculator();
        c.Press("f", "CLEARS");
        c.Type("30").Press("ENTER").Type("2").Press("S+");
        c.Type("40").Press("ENTER").Type("3").Press("S+");
        c.Press("g", "xw").DisplayIs("36.00");
    }

    [Fact]
    public void StatisticsRegistersLiveInR1ThroughR6()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARS");
        c.Type("3").Press("ENTER").Type("4").Press("S+");
        c.Press("RCL", "1").DisplayIs("1.00");    // n
        c.Press("RCL", "2").DisplayIs("4.00");    // Σx
        c.Press("RCL", "3").DisplayIs("16.00");   // Σx²
        c.Press("RCL", "4").DisplayIs("3.00");    // Σy
        c.Press("RCL", "6").DisplayIs("12.00");   // Σxy
    }

    [Fact]
    public void MeanWithNoDataIsError2()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARS");
        c.Press("g", "mean").DisplayIs("Error 2");
    }

    [Fact]
    public void SigmaPlusDisablesLift()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARS");
        c.Type("5").Press("S+");
        c.Type("7").Press("S+");        // 7 must overwrite the count in X, not push it
        c.Press("g", "mean").DisplayIs("6.00");
    }
}
