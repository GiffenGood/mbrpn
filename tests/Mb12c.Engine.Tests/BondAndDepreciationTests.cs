using Xunit;

namespace Mb12c.Engine.Tests;

public class BondAndDepreciationTests
{
    [Fact]
    public void BondPriceOnCouponDate()
    {
        // 10-year 6% semiannual bond priced to yield 8%, settling on a coupon date:
        // the classic 86.41 per 100 face, no accrued interest.
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN").Press("g", "M.DY");
        c.Type("8").Press("i");
        c.Type("6").Press("PMT");
        c.Type("10.012010").Press("ENTER").Type("10.012020");
        c.Press("f", "PRICE").DisplayIs("86.41");
        c.Press("x<>y").DisplayIs("0.00");         // accrued interest
    }

    [Fact]
    public void BondYieldRoundTrip()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN").Press("g", "M.DY");
        c.Type("6").Press("PMT");
        c.Type("86.41").Press("PV");
        c.Type("10.012010").Press("ENTER").Type("10.012020");
        c.Press("f", "YTM").DisplayIs("8.00");
    }

    [Fact]
    public void BondPriceBetweenCoupons_AccruedIsPositive()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN").Press("g", "M.DY");
        c.Type("8").Press("i");
        c.Type("6").Press("PMT");
        c.Type("1.012011").Press("ENTER").Type("10.012020");
        c.Press("f", "PRICE");
        var price = c.X.ToDouble();
        Assert.InRange(price, 80, 95);
        c.Press("x<>y");
        Assert.True(c.X.ToDouble() > 1.4 && c.X.ToDouble() < 1.6); // ~92/182 of a 3.00 coupon
    }

    [Fact]
    public void StraightLineDepreciation()
    {
        // Cost 10,000, salvage 1,000, life 5: 1,800/year; remaining after year 1 = 7,200
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Type("10000").Press("PV").Type("1000").Press("FV").Type("5").Press("n");
        c.Type("1").Press("f", "SL").DisplayIs("1,800.00");
        c.Press("x<>y").DisplayIs("7,200.00");
    }

    [Fact]
    public void SumOfYearsDigitsDepreciation()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Type("10000").Press("PV").Type("1000").Press("FV").Type("5").Press("n");
        c.Type("1").Press("f", "SOYD").DisplayIs("3,000.00");   // 5/15 × 9,000
        c.Type("5").Press("f", "SOYD").DisplayIs("600.00");     // 1/15 × 9,000
    }

    [Fact]
    public void DecliningBalanceDepreciation()
    {
        // 200% declining balance: rate 40%/year, floored at salvage
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Type("10000").Press("PV").Type("1000").Press("FV").Type("5").Press("n").Type("200").Press("i");
        c.Type("1").Press("f", "DB").DisplayIs("4,000.00");
        c.Type("2").Press("f", "DB").DisplayIs("2,400.00");
        c.Type("5").Press("f", "DB").DisplayIs("296.00");       // limited by salvage floor
        c.Press("x<>y").DisplayIs("0.00");                      // fully depreciated
    }

    [Fact]
    public void DepreciationYearBeyondLifeIsZero()
    {
        var c = new TestCalculator();
        c.Press("f", "CLEARFIN");
        c.Type("10000").Press("PV").Type("1000").Press("FV").Type("5").Press("n");
        c.Type("6").Press("f", "SL").DisplayIs("0.00");
    }
}
