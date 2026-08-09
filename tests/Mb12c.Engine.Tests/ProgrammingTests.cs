using Xunit;

namespace Mb12c.Engine.Tests;

public class ProgrammingTests
{
    private static TestCalculator FreshProgrammable()
    {
        var c = new TestCalculator();
        c.Press("f", "P/R");                 // program mode
        c.Press("f", "CLEARPRGM");
        return c;
    }

    [Fact]
    public void ProgramModeShowsLineZero()
    {
        FreshProgrammable().DisplayIs("00-");
    }

    [Fact]
    public void KeycodesAppearAsRowColumn()
    {
        var c = FreshProgrammable();
        c.Press("1").DisplayIs("01-       1");
        c.Press("ENTER").DisplayIs("02-      36");
        c.Press("2").DisplayIs("03-       2");
        c.Press("+").DisplayIs("04-      40");
    }

    [Fact]
    public void ShiftedKeycodesJoinWithComma()
    {
        var c = FreshProgrammable();
        c.Press("g", "sqrt").DisplayIs("01-   43,21");
        c.Press("f", "AMORT").DisplayIs("02-   42,11");
        c.Press("g", "GTO", "0", "0").DisplayIs("03-43,33 00");
    }

    [Fact]
    public void StoWithArithmeticRecordsThreeKeycodes()
    {
        var c = FreshProgrammable();
        c.Press("STO", "+", "4").DisplayIs("01- 44 40 4");
        c.Press("RCL", ".", "5").DisplayIs("02- 45 48 5");
    }

    [Fact]
    public void SimpleProgramRuns()
    {
        // Program: ×2 then +1
        var c = FreshProgrammable();
        c.Press("2", "*", "1", "+");
        c.Press("f", "P/R");                 // back to run mode
        c.Type("10").Press("ENTER");
        c.Press("R/S");
        c.RunProgram().DisplayIs("21.00");
    }

    [Fact]
    public void RunStopHaltsMidProgram()
    {
        var c = FreshProgrammable();
        c.Press("3", "*", "R/S", "2", "+");
        c.Press("f", "P/R");
        c.Type("5").Press("R/S");
        c.RunProgram().DisplayIs("15.00");   // halted at R/S
        c.Press("R/S");
        c.RunProgram().DisplayIs("17.00");   // resumes after the R/S line
    }

    [Fact]
    public void ConditionalLoopCountsDownToZero()
    {
        // 01: 1  02: −  03: g x=0  04: R/S  05: GTO 01  — counts X down to zero
        var c = FreshProgrammable();
        c.Press("1", "-");
        c.Press("g", "x=0");
        c.Press("R/S");
        c.Press("g", "GTO", "0", "1");
        c.Press("f", "P/R");
        c.Type("3").Press("R/S");
        c.RunProgram().DisplayIs("0.00");
    }

    [Fact]
    public void XLessOrEqualYConditional()
    {
        // 01: g x≤y  02: R/S  03: CLx — halts with X intact when X ≤ Y, else clears
        var c = FreshProgrammable();
        c.Press("g", "x<=y");
        c.Press("R/S");
        c.Press("CLx");
        c.Press("f", "P/R");
        c.Type("5").Press("ENTER").Type("3").Press("R/S");
        c.RunProgram().DisplayIs("3.00");    // 3 ≤ 5: halted at R/S
        c.Press("f", "CLEARPRGM");           // run mode: just repositions to line 00
        c.Type("3").Press("ENTER").Type("5").Press("R/S");
        c.RunProgram().DisplayIs("0.00");    // 5 > 3: skipped R/S, CLx ran
    }

    [Fact]
    public void PauseShowsIntermediateResult()
    {
        var c = FreshProgrammable();
        c.Press("2", "*");
        c.Press("g", "PSE");
        c.Press("3", "*");
        c.Press("f", "P/R");
        c.Type("5").Press("R/S");
        Assert.Equal(Engine.ProgramStepResult.Running, c.Engine.StepProgram()); // 2
        Assert.Equal(Engine.ProgramStepResult.Running, c.Engine.StepProgram()); // ×
        Assert.Equal(Engine.ProgramStepResult.Paused, c.Engine.StepProgram());
        c.DisplayIs("10.00");                // PSE shows X mid-run
        c.RunProgram().DisplayIs("30.00");
    }

    [Fact]
    public void SstStepsThroughProgramInRunMode()
    {
        var c = FreshProgrammable();
        c.Press("2", "*");
        c.Press("f", "P/R");
        c.Type("10");
        c.Press("SST").DisplayIs("2.");      // digit entry from the program
        c.Press("SST").DisplayIs("20.00");
    }

    [Fact]
    public void SstAndBstNavigateInProgramMode()
    {
        var c = FreshProgrammable();
        c.Press("1", "2", "+");
        c.Press("g", "GTO", ".", "0", "0").DisplayIs("00-");
        c.Press("SST").DisplayIs("01-       1");
        c.Press("SST").DisplayIs("02-       2");
        c.Press("g", "BST").DisplayIs("01-       1");
    }

    [Fact]
    public void MemShowsAllocation()
    {
        var c = FreshProgrammable();
        c.Press("f", "P/R");
        c.Press("g", "MEM").DisplayIs("P-08 r-20");
    }

    [Fact]
    public void RecordingBeyondEightLinesConvertsRegisters()
    {
        var c = FreshProgrammable();
        for (int i = 0; i < 9; i++) c.Press("1");
        c.DisplayIs("09-       1");
        c.Press("g", "MEM").DisplayIs("P-15 r-19");
        c.Press("f", "CLEARPRGM");
        c.Press("g", "MEM").DisplayIs("P-08 r-20");
    }

    [Fact]
    public void EditingOverwritesALine()
    {
        // Per the handbook, position to the line PRECEDING the one to change:
        // recording always writes to the following line.
        var c = FreshProgrammable();
        c.Press("1", "2", "3");
        c.Press("g", "GTO", ".", "0", "1").DisplayIs("01-       1");
        c.Press("9").DisplayIs("02-       9");
        c.Press("f", "P/R");
        c.Press("R/S");
        c.RunProgram().DisplayIs("193.");
    }

    [Fact]
    public void GtoBeyondProgramIsError4()
    {
        var c = FreshProgrammable();
        c.Press("1", "+");
        c.Press("f", "P/R");
        c.Press("g", "GTO", "9", "9").DisplayIs("Error 4");
    }

    [Fact]
    public void FallingOffTheEndReturnsToLineZeroAndHalts()
    {
        var c = FreshProgrammable();
        c.Press("1", "+");
        c.Press("f", "P/R");
        c.Type("5").Press("R/S");
        c.RunProgram().DisplayIs("6.00");
        Assert.Equal(0, c.Engine.Program.Pointer);
    }
}
