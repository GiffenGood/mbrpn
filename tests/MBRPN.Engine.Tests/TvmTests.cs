using Xunit;

namespace MBRPN.Engine.Tests;

public class TvmTests
{
    [Fact]
    public void MortgagePayment()
    {
        // $100,000 loan, 30 years monthly at 6%/yr → payment −599.55
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Type("30").Press("g", "12x").DisplayIs("360.00");
        c.Type("6").Press("g", "12/").DisplayIs("0.50");
        c.Type("100000").Press("PV");
        c.Type("0").Press("FV");
        c.Press("PMT").DisplayIs("-599.55");
    }

    [Fact]
    public void CompoundGrowthFutureValue()
    {
        // 3 periods at 10%, PV −100 → FV 133.10
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Type("3").Press("n").Type("10").Press("i").Type("-100").Press("PV").Type("0").Press("PMT");
        c.Press("FV").DisplayIs("133.10");
    }

    [Fact]
    public void SolveInterestRate()
    {
        // −100 grows to 133.10 in 3 periods → i = 10.00
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Type("3").Press("n").Type("-100").Press("PV").Type("0").Press("PMT").Type("133.1").Press("FV");
        c.Press("i").DisplayIs("10.00");
    }

    [Fact]
    public void SolveNRoundsUpToWholePeriod()
    {
        // Saving 1,000/period at 1% to reach 20,000: mathematically 18.32 periods → shows 19
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Type("1").Press("i").Type("0").Press("PV").Type("-1000").Press("PMT").Type("20000").Press("FV");
        c.Press("n").DisplayIs("19.00");
    }

    [Fact]
    public void SolvePaymentThenRecall()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Type("360").Press("n").Type("1").Press("i").Type("50000").Press("PV").Type("0").Press("FV");
        c.Press("PMT").DisplayIs("-514.31");
        c.Press("RCL", "n").DisplayIs("360.00");
        c.Press("RCL", "i").DisplayIs("1.00");
        c.Press("RCL", "PV").DisplayIs("50,000.00");
    }

    [Fact]
    public void BeginModeAnnuityDue()
    {
        // PV of 3 payments of 100 at 10%, payments at start of period → −273.55… sign: PMT −100 → PV 273.55
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN").Press("g", "BEG");
        c.Type("3").Press("n").Type("10").Press("i").Type("-100").Press("PMT").Type("0").Press("FV");
        c.Press("PV").DisplayIs("273.55");
        c.Press("g", "END");
        c.Press("PV").DisplayIs("248.69");   // ordinary annuity
    }

    [Fact]
    public void StoreVersusComputeRule()
    {
        // A computed result followed by a TVM key stores it; a TVM key right after
        // another TVM key computes.
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Type("30").Press("ENTER").Type("12").Press("*").Press("n").DisplayIs("360.00");
        c.Press("RCL", "n").DisplayIs("360.00");
    }

    [Fact]
    public void OddPeriodSimpleInterest()
    {
        // 2.25 periods at 12%, PV 1,000: simple interest over the odd quarter-period
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Type("2.25").Press("n").Type("12").Press("i").Type("1000").Press("PV").Type("0").Press("PMT");
        c.Press("FV").DisplayIs("-1,292.03");
    }

    [Fact]
    public void OddPeriodCompoundInterest()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Press("STO", "EEX"); // C annunciator: compound over the odd period
        c.Type("2.25").Press("n").Type("12").Press("i").Type("1000").Press("PV").Type("0").Press("PMT");
        c.Press("FV").DisplayIs("-1,290.45");
        c.Press("STO", "EEX"); // toggle back off
    }

    [Fact]
    public void InterestAtMinus100PercentIsError5()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Type("3").Press("n").Type("-100").Press("i").Type("100").Press("PV").Type("0").Press("PMT");
        c.Press("FV").DisplayIs("Error 5");
    }

    [Fact]
    public void SimpleInterest360And365()
    {
        // Handbook: $450 for 60 days at 7% → 5.25 (360-day); R↓ x≷y shows 5.18 (365-day)
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Type("60").Press("n").Type("7").Press("i").Type("-450").Press("PV");
        c.Press("f", "INT").DisplayIs("5.25");
        c.Press("Rv", "x<>y").DisplayIs("5.18");
    }

    [Fact]
    public void ClearFinLeavesStackAlone()
    {
        var c = new TestCalculator();
        c.Type("42").Press("ENTER");
        c.Press("f", "CLEARFIN").DisplayIs("42.00");
        c.Press("RCL", "n").DisplayIs("0.00");
    }
}
