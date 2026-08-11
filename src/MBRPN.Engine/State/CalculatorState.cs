namespace MBRPN.Engine.State;

/// <summary>
/// Serializable snapshot of continuous memory. Numbers travel as "mantissa|exponent"
/// strings so no precision is lost through JSON; program lines are arrays of key names.
/// </summary>
public sealed class CalculatorState
{
    public int Version { get; set; } = 1;

    public string X { get; set; } = "0|0";
    public string Y { get; set; } = "0|0";
    public string Z { get; set; } = "0|0";
    public string T { get; set; } = "0|0";
    public string LastX { get; set; } = "0|0";

    public List<string> Registers { get; set; } = new();

    public string FinN { get; set; } = "0|0";
    public string FinI { get; set; } = "0|0";
    public string FinPv { get; set; } = "0|0";
    public string FinPmt { get; set; } = "0|0";
    public string FinFv { get; set; } = "0|0";
    public bool BeginMode { get; set; }
    public bool OddPeriodCompound { get; set; }

    public List<int> CashFlowRepeats { get; set; } = new();

    public int FixDigits { get; set; } = 2;
    public bool Scientific { get; set; }
    public bool DmyMode { get; set; }
    public bool ProgramMode { get; set; }

    public List<List<string>> ProgramLines { get; set; } = new();
    public int ConvertedRegisters { get; set; }
    public int ProgramPointer { get; set; }
}
