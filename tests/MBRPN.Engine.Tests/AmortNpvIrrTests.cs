using Xunit;

namespace MBRPN.Engine.Tests;

public class AmortNpvIrrTests
{
    [Fact]
    public void AmortizeFirstPayment()
    {
        // $50,000 at 1%/month, 360 payments: PMT = −514.31; first payment splits into
        // −500.00 interest (rounded to the FIX 2 display, as on the real unit) and the rest principal.
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Type("360").Press("n").Type("1").Press("i").Type("50000").Press("PV").Type("0").Press("FV");
        c.Press("PMT").DisplayIs("-514.31");
        c.Type("0").Press("n");                    // restart payment counter
        c.Type("1").Press("f", "AMORT").DisplayIs("-500.00");
        c.Press("x<>y").DisplayIs("-14.31");       // principal portion
        c.Press("RCL", "PV").DisplayIs("49,985.69");
        c.Press("RCL", "n").DisplayIs("1.00");
    }

    [Fact]
    public void AmortizeYearThenNextYear()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Type("360").Press("n").Type("1").Press("i").Type("50000").Press("PV").Type("0").Press("FV");
        c.Press("PMT");
        c.Type("0").Press("n");
        c.Type("12").Press("f", "AMORT");          // first year
        Assert.Equal(12m, c.Engine.Stack.Z.ToDecimal());   // x was saved in Z
        c.Press("RCL", "n").DisplayIs("12.00");
    }

    [Fact]
    public void AmortRequiresPositiveInteger()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Type("12").Press("n").Type("1").Press("i").Type("1000").Press("PV");
        c.Type("1.5").Press("f", "AMORT").DisplayIs("Error 5");
    }

    [Fact]
    public void NetPresentValue()
    {
        // CF0 −1,000; CF1 600; CF2 600 at 10% → NPV 41.32
        var c = new TestCalculator();
        c.Press("f", "CLEARREG");
        c.Type("-1000").Press("g", "CF0");
        c.Type("600").Press("g", "CFj");
        c.Type("600").Press("g", "CFj");
        c.Type("10").Press("i");
        c.Press("f", "NPV").DisplayIs("41.32");
    }

    [Fact]
    public void InternalRateOfReturn()
    {
        // Same flows → IRR 13.07 (root of 1000 = 600/x + 600/x²)
        var c = new TestCalculator();
        c.Press("f", "CLEARREG");
        c.Type("-1000").Press("g", "CF0");
        c.Type("600").Press("g", "CFj");
        c.Type("600").Press("g", "CFj");
        c.Press("f", "IRR").DisplayIs("13.07");
        c.Press("RCL", "i").DisplayIs("13.07");
    }

    [Fact]
    public void GroupedCashFlowsWithNj()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARREG");
        c.Type("-1000").Press("g", "CF0");
        c.Type("600").Press("g", "CFj");
        c.Type("2").Press("g", "Nj");              // 600 repeats twice
        c.Press("f", "IRR").DisplayIs("13.07");
    }

    [Fact]
    public void CashFlowReviewWithRclG()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARREG");
        c.Type("-1000").Press("g", "CF0");
        c.Type("600").Press("g", "CFj");
        c.Type("3").Press("g", "Nj");
        c.Press("RCL", "g", "Nj").DisplayIs("3.00");
        c.Press("RCL", "g", "CFj").DisplayIs("600.00");
        c.Press("RCL", "n").DisplayIs("0.00");     // review stepped the counter back
    }

    [Fact]
    public void AllPositiveFlowsIsError7()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARREG");
        c.Type("1000").Press("g", "CF0");
        c.Type("600").Press("g", "CFj");
        c.Press("f", "IRR").DisplayIs("Error 7");
    }

    [Fact]
    public void NpvKeepsFlowsForIrr()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARREG");
        c.Type("-1000").Press("g", "CF0");
        c.Type("600").Press("g", "CFj");
        c.Type("600").Press("g", "CFj");
        c.Type("10").Press("i");
        c.Press("f", "NPV").DisplayIs("41.32");
        c.Press("f", "IRR").DisplayIs("13.07");
    }
}
