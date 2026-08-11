namespace MBRPN.Engine.Keys;

/// <summary>
/// Every logical function the calculator can perform. A physical key resolves to one of
/// these depending on the active prefix (none / f / g).
/// </summary>
public enum Fn
{
    // Digit / entry
    Digit0, Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9,
    Point, Chs, Eex, Enter, CLx,

    // Stack
    RollDown, XExchangeY, LastX,

    // Arithmetic / math
    Add, Subtract, Multiply, Divide,
    YtoX, Reciprocal, Sqrt, Exp, Ln, Factorial, Frac, Intg, Round,

    // Percent
    Percent, DeltaPercent, PercentT,

    // Display
    Fix, Scientific, Mantissa,

    // Storage
    Sto, Rcl,

    // Financial
    NKey, IKey, PvKey, PmtKey, FvKey,
    Amort, SimpleInterest, Npv, Irr,
    Times12, Divide12, CashFlow0, CashFlowJ, CashFlowNj,
    BeginMode, EndMode,
    Price, Ytm,
    DepreciationSl, DepreciationSoyd, DepreciationDb,
    ClearFin,

    // Dates
    Date, DeltaDays, SetDmy, SetMdy,

    // Statistics
    SigmaPlus, SigmaMinus, Mean, WeightedMean, StdDev, EstimateX, EstimateY,
    ClearSigma,

    // Clearing
    ClearReg, ClearPrefix, ClearPrgm,

    // Programming
    ProgramRun, RunStop, Sst, Bst, Gto, Pse, XLessOrEqualY, XEqualZero, Mem,

    // Power
    OnOff,

    // Prefixes themselves (dispatch targets for the state machine)
    PrefixF, PrefixG,
}
