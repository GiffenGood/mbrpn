using MBRPN.Engine.History;
using Xunit;

namespace MBRPN.Engine.Tests;

public class HistoryTests
{
    private static IReadOnlyList<HistoryEntry> Entries(TestCalculator c) => c.Engine.History.Entries;

    [Fact]
    public void NumberPlusEnterCoalescesIntoOneEntry()
    {
        var c = new TestCalculator().Type("100").Press("ENTER");
        var entry = Assert.Single(Entries(c));
        Assert.Equal("100 ENTER", entry.Label);
        Assert.Equal("100.00", entry.ResultText);
    }

    [Fact]
    public void NumberPlusOperationRecordsOperationAndResult()
    {
        var c = new TestCalculator().Type("100").Press("ENTER").Type("25").Press("+");
        Assert.Equal(2, Entries(c).Count);
        Assert.Equal("25 +", Entries(c)[1].Label);
        Assert.Equal("125.00", Entries(c)[1].ResultText);
    }

    [Fact]
    public void StandaloneOperationRecordsAlone()
    {
        var c = new TestCalculator().Type("3").Press("ENTER").Press("+");
        Assert.Equal("+", Entries(c)[^1].Label);
    }

    [Fact]
    public void TvmStoreRecordsNumberAndKey()
    {
        var c = new TestCalculator().Type("360").Press("n");
        Assert.Equal("360 n", Entries(c)[^1].Label);
    }

    [Fact]
    public void ChsDuringEntryMergesIntoTheNumber()
    {
        var c = new TestCalculator().Type("-5").Press("ENTER");
        var entry = Assert.Single(Entries(c));
        Assert.Equal("-5 ENTER", entry.Label);
    }

    [Fact]
    public void ChsOutsideEntryIsItsOwnEntry()
    {
        var c = new TestCalculator().Type("5").Press("ENTER").Press("CHS");
        Assert.Equal("CHS", Entries(c)[^1].Label);
        Assert.Equal("-5.00", Entries(c)[^1].ResultText);
    }

    [Fact]
    public void ClxDuringEntryDiscardsPendingSilently()
    {
        var c = new TestCalculator().Type("5").Press("ENTER");
        c.Type("123").Press("CLx");                 // correction gesture — not recorded
        Assert.Single(Entries(c));
        c.Type("7").Press("+");
        Assert.Equal("7 +", Entries(c)[^1].Label);  // no stray digits from the cancelled entry
        Assert.Equal("12.00", Entries(c)[^1].ResultText);
    }

    [Fact]
    public void ClxOutsideEntryRecords()
    {
        var c = new TestCalculator().Type("5").Press("ENTER").Press("CLx");
        Assert.Equal("CLx", Entries(c)[^1].Label);
    }

    [Fact]
    public void PendingDigitsAloneAreNotYetHistory()
    {
        var c = new TestCalculator().Type("123");
        Assert.Empty(Entries(c));
    }

    [Fact]
    public void LabelsForShiftedAndSequenceActions()
    {
        var c = new TestCalculator();
        c.Type("1").Press("STO", "+", "4");
        Assert.Equal("1 STO + 4", Entries(c)[^1].Label);
        c.Press("RCL", ".", "5");
        Assert.Equal("RCL .5", Entries(c)[^1].Label);
        c.Press("f", "4");
        Assert.Equal("FIX 4", Entries(c)[^1].Label);
        c.Press("f", "RND");
        Assert.Equal("f RND", Entries(c)[^1].Label);
        c.Press("STO", "EEX");
        Assert.Equal("STO EEX", Entries(c)[^1].Label);
        c.Press("STO", "EEX"); // toggle C back off
        c.Press("f", ".");
        Assert.Equal("f .", Entries(c)[^1].Label);
        c.Press("f", "2");     // restore FIX 2
    }

    [Fact]
    public void ErrorRecordsNothingAndClearsPending()
    {
        var c = new TestCalculator().Type("5").Press("ENTER");
        c.Type("0").Press("/");                      // Error 0
        Assert.Single(Entries(c));                   // only "5 ENTER"
        c.Press("CLx");                              // clear the error latch (swallowed)
        c.Type("4").Press("ENTER");
        Assert.Equal("4 ENTER", Entries(c)[^1].Label);
    }

    [Fact]
    public void ProgramModeRecordsNothing()
    {
        var c = new TestCalculator();
        c.Press("f", "P/R");
        c.Press("2", "*", "1", "+");
        c.Press("f", "P/R");
        Assert.Empty(Entries(c));
    }

    [Fact]
    public void ProgramRunRecordsOnlyTheRunStopPress()
    {
        var c = new TestCalculator();
        c.Press("f", "P/R").Press("f", "CLEARPRGM");
        c.Press("2", "*");
        c.Press("f", "P/R");
        c.Type("10").Press("R/S");
        int countAtStart = Entries(c).Count;
        Assert.Equal("10 R/S", Entries(c)[^1].Label);
        Assert.Null(Entries(c)[^1].ResultText);      // result arrives asynchronously
        c.RunProgram();
        Assert.Equal(countAtStart, Entries(c).Count); // program steps add nothing
        c.DisplayIs("20.00");
    }

    [Fact]
    public void ReplayExecutesAgainstCurrentState()
    {
        var c = new TestCalculator().Type("100").Press("ENTER").Type("25").Press("+");
        c.DisplayIs("125.00");
        var plus25 = Entries(c)[^1];

        c.Press("CLx").Type("10").Press("ENTER");
        c.Engine.Replay(plus25);
        c.DisplayIs("35.00");                        // 25 + against the NEW state
        Assert.Equal("25 +", Entries(c)[^1].Label);  // replay recorded itself
        Assert.Equal("35.00", Entries(c)[^1].ResultText);
    }

    [Fact]
    public void ReplayOfEnterEntryBehavesLikeTypingIt()
    {
        var c = new TestCalculator().Type("100").Press("ENTER");
        var hundredEnter = Entries(c)[0];
        c.Type("55").Press("ENTER").Type("3").Press("+");   // X = 58, lift enabled
        c.Engine.Replay(hundredEnter);
        c.DisplayIs("100.00");                              // X = Y = 100 (ENTER duplicates)
        Assert.Equal(58m, c.Z.ToDecimal());                 // prior X pushed up, as if typed
    }

    [Fact]
    public void ReplayIsNoOpInProgramMode()
    {
        var c = new TestCalculator().Type("100").Press("ENTER");
        var entry = Entries(c)[0];
        c.Press("f", "P/R");
        c.Engine.Replay(entry);
        Assert.Single(Entries(c));
        Assert.Equal(0, c.Engine.Program.Count);     // nothing recorded into the program
        c.Press("f", "P/R");
    }

    [Fact]
    public void ReplayIsNoOpWhileErrorLatched()
    {
        var c = new TestCalculator().Type("100").Press("ENTER");
        var entry = Entries(c)[0];
        c.Type("0").Press("g", "LN");                // Error 0 latched
        c.Engine.Replay(entry);
        c.DisplayIs("Error 0");                      // untouched: replay didn't clear it
        Assert.Single(Entries(c));
    }

    [Fact]
    public void ClearEmptiesHistoryAndFiresChanged()
    {
        var c = new TestCalculator().Type("1").Press("ENTER").Type("2").Press("+");
        bool changed = false;
        c.Engine.History.Changed += () => changed = true;
        c.Engine.History.Clear();
        Assert.Empty(Entries(c));
        Assert.True(changed);
    }

    [Fact]
    public void CapacityEvictsOldestAndKeepsSequenceMonotonic()
    {
        var c = new TestCalculator();
        for (int i = 0; i < HistoryRecorder.Capacity + 5; i++)
            c.Type("1").Press("+");
        Assert.Equal(HistoryRecorder.Capacity, Entries(c).Count);
        Assert.Equal(6, Entries(c)[0].Sequence);     // first five evicted
        Assert.True(Entries(c)[^1].Sequence > Entries(c)[0].Sequence);
    }
}
