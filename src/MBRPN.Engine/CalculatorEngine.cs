using System.Globalization;
using MBRPN.Engine.Core;
using MBRPN.Engine.Display;
using MBRPN.Engine.Financial;
using MBRPN.Engine.History;
using MBRPN.Engine.Keys;
using MBRPN.Engine.Programming;
using MBRPN.Engine.Statistics;

namespace MBRPN.Engine;

/// <summary>
/// The MB-RPN engine, compatible with the HP-12C. Feed it physical key presses with <see cref="Press"/>,
/// read the LCD from <see cref="Display"/>. Program execution is driven externally via
/// <see cref="StepProgram"/> so a UI can keep itself responsive (and honor PSE pauses).
/// </summary>
public sealed partial class CalculatorEngine
{
    internal RpnStack Stack { get; } = new();
    internal EntryBuffer Entry { get; } = new();
    internal RegisterFile Registers { get; } = new();
    internal FinancialRegisters Fin { get; } = new();
    internal DisplayFormatter Formatter { get; } = new();
    internal PrefixStateMachine Prefix { get; } = new();
    internal ProgramMemory Program { get; }
    internal StatisticsAccumulator Stats { get; }
    internal CashFlows Cash { get; }

    /// <summary>Chronological record of meaningful interactions; entries replay via <see cref="Replay"/>.</summary>
    public HistoryRecorder History { get; } = new();

    private bool _off;
    private bool _prgmMode;
    private bool _dmyMode;
    private CalcError? _error;
    private string? _transient;          // mantissa peek, MEM, date result — until the next key
    private bool _numberPending;         // decides store-vs-compute for the five TVM keys

    public bool IsOff => _off;
    public bool IsRunning { get; private set; }
    public bool ProgramMode => _prgmMode;
    public bool DmyMode => _dmyMode;

    public event Action? Changed;

    public CalculatorEngine()
    {
        Program = new ProgramMemory(Registers);
        Stats = new StatisticsAccumulator(Registers);
        Cash = new CashFlows(Registers, Fin);
    }

    // ---- Input -------------------------------------------------------------------

    public void Press(KeyId key)
    {
        try
        {
            PressCore(key);
        }
        catch (CalcException ex)
        {
            AbortEntry();
            Prefix.Reset();
            History.DiscardPending();
            IsRunning = false;
            _error = ex.Error;
        }
        Changed?.Invoke();
    }

    private void PressCore(KeyId key)
    {
        if (_off)
        {
            if (key == KeyId.On) _off = false;
            return;
        }
        if (IsRunning)
        {
            IsRunning = false; // any key halts a running program
            return;
        }
        if (_error is not null)
        {
            _error = null;     // any key clears the error display and is swallowed
            return;
        }
        _transient = null;
        if (key == KeyId.On && !Prefix.AnyPending)
        {
            _off = true;
            return;
        }

        var action = Prefix.Accept(key);
        if (action is null) return; // prefix pending
        HandleAction(action);
    }

    private void HandleAction(ResolvedAction action)
    {
        if (_prgmMode && !IsProgramModeControl(action))
        {
            Program.Record(new Instruction(action.Sequence));
            return;
        }
        // Capture entry state before executing: ExecuteAction commits/destroys the entry
        // buffer, and f P/R flips _prgmMode inside. A CalcException propagates past the
        // history call, so error outcomes are never recorded.
        bool inProgramMode = _prgmMode;
        bool wasEntering = Entry.IsEntering;
        string? entryText = wasEntering ? DisplayFormatter.FormatEntry(Entry) : null;
        ExecuteAction(action);
        if (!inProgramMode)
            History.OnActionExecuted(action, wasEntering, entryText, IsRunning ? null : DisplayText);
    }

    /// <summary>
    /// Re-executes a recorded interaction against the CURRENT calculator state, exactly
    /// as though the user pressed its keys again. The replayed operation records itself
    /// as a new history entry through the normal pipeline.
    /// </summary>
    public void Replay(HistoryEntry entry)
    {
        if (_off || IsRunning || _prgmMode || _error is not null) return;
        foreach (KeyId key in entry.Keys) Press(key);
    }

    /// <summary>Actions that operate on the program editor / mode instead of being recorded.</summary>
    private static bool IsProgramModeControl(ResolvedAction action) => action switch
    {
        GtoDotAction => true,
        FnAction f => f.Fn is Fn.ProgramRun or Fn.Sst or Fn.Bst or Fn.ClearPrgm
            or Fn.ClearPrefix or Fn.Mem or Fn.OnOff,
        _ => false,
    };

    // ---- Display -----------------------------------------------------------------

    public DisplayState Display
    {
        get
        {
            var ann = Annunciators.None;
            if (Prefix.FPending) ann |= Annunciators.F;
            if (Prefix.GPending) ann |= Annunciators.G;
            if (Fin.BeginMode) ann |= Annunciators.Begin;
            if (_dmyMode) ann |= Annunciators.Dmy;
            if (Fin.OddPeriodCompound) ann |= Annunciators.C;
            if (_prgmMode) ann |= Annunciators.Prgm;

            if (_off) return DisplayState.Blank();
            if (_error is CalcError err) return DisplayState.FromText(DisplayFormatter.FormatError(err), ann);
            if (IsRunning && !_showXWhilePaused) return DisplayState.FromText("running", ann);
            if (_transient is not null) return DisplayState.FromText(_transient, ann);
            if (_prgmMode) return DisplayState.FromText(Program.DisplayText(), ann);
            if (Entry.IsEntering) return DisplayState.FromText(DisplayFormatter.FormatEntry(Entry), ann);
            return DisplayState.FromText(Formatter.Format(Stack.X), ann);
        }
    }

    /// <summary>The current display text — convenient for tests.</summary>
    public string DisplayText => Display.Text;

    // ---- Number entry helpers ------------------------------------------------------

    private void BeginEntryIfNeeded()
    {
        if (Entry.IsEntering) return;
        Entry.Begin();
        if (Stack.LiftEnabled) Stack.Lift();
        Stack.LiftEnabled = true;
    }

    /// <summary>Terminates digit entry (any non-entry key does this first).</summary>
    private void CommitEntry()
    {
        if (!Entry.IsEntering) return;
        Stack.X = Entry.ToReal();
        Entry.End();
        _numberPending = true;
    }

    private void AbortEntry()
    {
        if (Entry.IsEntering) Entry.End();
    }

    /// <summary>All ten mantissa digits of X (f ENTER, CLEAR PREFIX).</summary>
    private string MantissaText()
    {
        decimal m = Math.Abs(Stack.X.M);
        long digits = (long)decimal.Round(m * 1_000_000_000m, 0, MidpointRounding.AwayFromZero);
        return digits.ToString("0000000000", CultureInfo.InvariantCulture);
    }
}
