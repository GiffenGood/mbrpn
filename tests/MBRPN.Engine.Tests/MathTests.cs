using Xunit;

namespace MBRPN.Engine.Tests;

public class MathTests
{
    [Fact]
    public void PowerFunction() =>
        new TestCalculator().Type("2").Press("ENTER").Type("10").Press("y^x").DisplayIs("1,024.00");

    [Fact]
    public void PowerWithFractionalExponent() =>
        new TestCalculator().Type("2").Press("ENTER").Type("1.4").Press("y^x")
            .Press("f", "4").DisplayIs("2.6390");

    [Fact]
    public void NegativeBaseIntegerExponent() =>
        new TestCalculator().Type("-3").Press("ENTER").Type("3").Press("y^x").DisplayIs("-27.00");

    [Fact]
    public void NegativeBaseFractionalExponentIsError0() =>
        new TestCalculator().Type("-2").Press("ENTER").Type("0.5").Press("y^x").DisplayIs("Error 0");

    [Fact]
    public void Reciprocal() =>
        new TestCalculator().Type("4").Press("1/x").DisplayIs("0.25");

    [Fact]
    public void SquareRoot() =>
        new TestCalculator().Type("196").Press("g", "sqrt").DisplayIs("14.00");

    [Fact]
    public void SquareRootOfNegativeIsError0() =>
        new TestCalculator().Type("-4").Press("g", "sqrt").DisplayIs("Error 0");

    [Fact]
    public void NaturalLogAndExp()
    {
        new TestCalculator().Type("2.718281828").Press("g", "LN").DisplayIs("1.00");
        new TestCalculator().Type("1").Press("g", "e^x").Press("f", "4").DisplayIs("2.7183");
    }

    [Fact]
    public void LnOfZeroIsError0() =>
        new TestCalculator().Press("CLx", "g", "LN").DisplayIs("Error 0");

    [Fact]
    public void Factorials()
    {
        new TestCalculator().Type("5").Press("g", "n!").DisplayIs("120.00");
        new TestCalculator().Type("0").Press("g", "n!").DisplayIs("1.00");
        new TestCalculator().Type("69").Press("g", "n!").Press("f", ".").DisplayIs("1.711225 98");
    }

    [Fact]
    public void FactorialOfNonIntegerIsError0() =>
        new TestCalculator().Type("2.5").Press("g", "n!").DisplayIs("Error 0");

    [Fact]
    public void FracAndIntg()
    {
        new TestCalculator().Type("14.87").Press("g", "INTG").DisplayIs("14.00");
        new TestCalculator().Type("14.87").Press("g", "FRAC").DisplayIs("0.87");
        new TestCalculator().Type("-14.87").Press("g", "INTG").DisplayIs("-14.00");
    }

    [Fact]
    public void PercentKeepsYUnchanged()
    {
        // 300 ENTER 25 % → 75, with 300 still in Y so + gives 375
        var c = new TestCalculator().Type("300").Press("ENTER").Type("25").Press("%");
        c.DisplayIs("75.00");
        c.Press("+").DisplayIs("375.00");
    }

    [Fact]
    public void PercentDifference()
    {
        // Handbook: old price 58, new 53 → Δ% = −8.62
        new TestCalculator().Type("58").Press("ENTER").Type("53").Press("d%").DisplayIs("-8.62");
    }

    [Fact]
    public void PercentOfTotal()
    {
        var c = new TestCalculator().Type("500").Press("ENTER").Type("125").Press("%T");
        c.DisplayIs("25.00");
        Assert.Equal(500m, c.Y.ToDecimal());
    }

    [Fact]
    public void ChainCalculationUsesHiddenPrecision()
    {
        // 1 ÷ 3 at FIX 2 shows 0.33 but keeps full precision: ×3 → 1.00
        new TestCalculator().Type("1").Press("ENTER").Type("3").Press("/").DisplayIs("0.33")
            .Type("3").Press("*").DisplayIs("1.00");
    }
}
