using Xunit;

namespace Mb12c.Engine.Tests;

public class StorageRegisterTests
{
    [Fact]
    public void StoreAndRecall()
    {
        var c = new TestCalculator();
        c.Type("42").Press("STO", "3").DisplayIs("42.00");
        c.Press("CLx").DisplayIs("0.00");
        c.Press("RCL", "3").DisplayIs("42.00");
    }

    [Fact]
    public void DotRegisters()
    {
        var c = new TestCalculator();
        c.Type("7").Press("STO", ".", "5");
        c.Press("CLx");
        c.Press("RCL", ".", "5").DisplayIs("7.00");
    }

    [Fact]
    public void StorageRegisterArithmetic()
    {
        var c = new TestCalculator();
        c.Type("100").Press("STO", "1");
        c.Type("25").Press("STO", "-", "1");
        c.Press("RCL", "1").DisplayIs("75.00");
        c.Type("3").Press("STO", "/", "1");
        c.Press("RCL", "1").DisplayIs("25.00");
        c.Type("4").Press("STO", "*", "1");
        c.Press("RCL", "1").DisplayIs("100.00");
        c.Type("1").Press("STO", "+", "1");
        c.Press("RCL", "1").DisplayIs("101.00");
    }

    [Fact]
    public void RecallLiftsTheStack()
    {
        var c = new TestCalculator();
        c.Type("11").Press("STO", "0");
        c.Type("5").Press("ENTER");
        c.Press("RCL", "0");
        c.Press("+").DisplayIs("16.00");
    }

    [Fact]
    public void ClearRegClearsEverything()
    {
        var c = new TestCalculator();
        c.Type("42").Press("STO", "4").Type("13").Press("n");
        c.Press("f", "CLEARREG");
        c.DisplayIs("0.00");
        c.Press("RCL", "4").DisplayIs("0.00");
        c.Press("RCL", "n").DisplayIs("0.00");
    }

    [Fact]
    public void StoreArithmeticOverflowIsError1()
    {
        var c = new TestCalculator();
        c.Type("9").Press("EEX", "9", "9").Press("STO", "1");
        c.Type("10").Press("STO", "*", "1").DisplayIs("Error 1");
    }
}
