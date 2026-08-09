namespace Mb12c.Engine.Keys;

/// <summary>
/// The full HP-12C keyboard: every physical key with its position, program keycode,
/// printed legends (primary / gold f / blue g), and the functions each shift state maps to.
/// A digit key's f-shift is FIX n (no printed gold legend on the real unit); keys with no
/// shifted meaning fall back to their primary function at dispatch time.
/// </summary>
public static class KeyboardMap
{
    public static readonly IReadOnlyList<KeyDef> Keys = new List<KeyDef>
    {
        // Row 1
        new(KeyId.N,          1, 1,  "11", "n",     "AMORT",        "12x",   Fn.NKey,        Fn.Amort,          Fn.Times12),
        new(KeyId.I,          1, 2,  "12", "i",     "INT",          "12÷",   Fn.IKey,        Fn.SimpleInterest, Fn.Divide12),
        new(KeyId.PV,         1, 3,  "13", "PV",    "NPV",          "CF0",   Fn.PvKey,       Fn.Npv,            Fn.CashFlow0),
        new(KeyId.PMT,        1, 4,  "14", "PMT",   "RND",          "CFj",   Fn.PmtKey,      Fn.Round,          Fn.CashFlowJ),
        new(KeyId.FV,         1, 5,  "15", "FV",    "IRR",          "Nj",    Fn.FvKey,       Fn.Irr,            Fn.CashFlowNj),
        new(KeyId.CHS,        1, 6,  "16", "CHS",   null,           "DATE",  Fn.Chs,         null,              Fn.Date),
        new(KeyId.D7,         1, 7,  "7",  "7",     null,           "BEG",   Fn.Digit7,      Fn.Fix,            Fn.BeginMode),
        new(KeyId.D8,         1, 8,  "8",  "8",     null,           "END",   Fn.Digit8,      Fn.Fix,            Fn.EndMode),
        new(KeyId.D9,         1, 9,  "9",  "9",     null,           "MEM",   Fn.Digit9,      Fn.Fix,            Fn.Mem),
        new(KeyId.Divide,     1, 10, "10", "÷",     null,           null,    Fn.Divide,      null,              null),

        // Row 2
        new(KeyId.YtoX,       2, 1,  "21", "yˣ",    "PRICE",        "√x",    Fn.YtoX,        Fn.Price,          Fn.Sqrt),
        new(KeyId.Reciprocal, 2, 2,  "22", "1/x",   "YTM",          "eˣ",    Fn.Reciprocal,  Fn.Ytm,            Fn.Exp),
        new(KeyId.PercentT,   2, 3,  "23", "%T",    "SL",           "LN",    Fn.PercentT,    Fn.DepreciationSl, Fn.Ln),
        new(KeyId.DeltaPercent,2,4,  "24", "Δ%",    "SOYD",         "FRAC",  Fn.DeltaPercent,Fn.DepreciationSoyd,Fn.Frac),
        new(KeyId.Percent,    2, 5,  "25", "%",     "DB",           "INTG",  Fn.Percent,     Fn.DepreciationDb, Fn.Intg),
        new(KeyId.EEX,        2, 6,  "26", "EEX",   null,           "ΔDYS",  Fn.Eex,         null,              Fn.DeltaDays),
        new(KeyId.D4,         2, 7,  "4",  "4",     null,           "D.MY",  Fn.Digit4,      Fn.Fix,            Fn.SetDmy),
        new(KeyId.D5,         2, 8,  "5",  "5",     null,           "M.DY",  Fn.Digit5,      Fn.Fix,            Fn.SetMdy),
        new(KeyId.D6,         2, 9,  "6",  "6",     null,           "x̄w",    Fn.Digit6,      Fn.Fix,            Fn.WeightedMean),
        new(KeyId.Multiply,   2, 10, "20", "×",     null,           null,    Fn.Multiply,    null,              null),

        // Row 3
        new(KeyId.RunStop,    3, 1,  "31", "R/S",   "P/R",          "PSE",   Fn.RunStop,     Fn.ProgramRun,     Fn.Pse),
        new(KeyId.SST,        3, 2,  "32", "SST",   "CLEAR Σ",      "BST",   Fn.Sst,         Fn.ClearSigma,     Fn.Bst),
        new(KeyId.RollDown,   3, 3,  "33", "R↓",    "CLEAR PRGM",   "GTO",   Fn.RollDown,    Fn.ClearPrgm,      Fn.Gto),
        new(KeyId.XExchangeY, 3, 4,  "34", "x≷y",   "CLEAR FIN",    "x≤y",   Fn.XExchangeY,  Fn.ClearFin,       Fn.XLessOrEqualY),
        new(KeyId.CLx,        3, 5,  "35", "CLx",   "CLEAR REG",    "x=0",   Fn.CLx,         Fn.ClearReg,       Fn.XEqualZero),
        new(KeyId.Enter,      3, 6,  "36", "ENTER", "CLEAR PREFIX", "LSTx",  Fn.Enter,       Fn.ClearPrefix,    Fn.LastX),
        new(KeyId.D1,         3, 7,  "1",  "1",     null,           "x̂,r",   Fn.Digit1,      Fn.Fix,            Fn.EstimateX),
        new(KeyId.D2,         3, 8,  "2",  "2",     null,           "ŷ,r",   Fn.Digit2,      Fn.Fix,            Fn.EstimateY),
        new(KeyId.D3,         3, 9,  "3",  "3",     null,           "n!",    Fn.Digit3,      Fn.Fix,            Fn.Factorial),
        new(KeyId.Subtract,   3, 10, "30", "−",     null,           null,    Fn.Subtract,    null,              null),

        // Row 4 (ENTER occupies column 6, spanning from row 3)
        new(KeyId.On,         4, 1,  "41", "ON",    null,           null,    Fn.OnOff,       null,              null),
        new(KeyId.F,          4, 2,  "42", "f",     null,           null,    Fn.PrefixF,     null,              null),
        new(KeyId.G,          4, 3,  "43", "g",     null,           null,    Fn.PrefixG,     null,              null),
        new(KeyId.STO,        4, 4,  "44", "STO",   null,           null,    Fn.Sto,         null,              null),
        new(KeyId.RCL,        4, 5,  "45", "RCL",   null,           null,    Fn.Rcl,         null,              null),
        new(KeyId.D0,         4, 7,  "0",  "0",     null,           "x̄",     Fn.Digit0,      Fn.Fix,            Fn.Mean),
        new(KeyId.Point,      4, 8,  "48", ".",     null,           "s",     Fn.Point,       Fn.Scientific,     Fn.StdDev),
        new(KeyId.SigmaPlus,  4, 9,  "49", "Σ+",    null,           "Σ−",    Fn.SigmaPlus,   null,              Fn.SigmaMinus),
        new(KeyId.Add,        4, 10, "40", "+",     null,           null,    Fn.Add,         null,              null),
    };

    private static readonly Dictionary<KeyId, KeyDef> ById = Keys.ToDictionary(k => k.Id);

    public static KeyDef Get(KeyId id) => ById[id];

    /// <summary>Digit value of a digit key, or null if the key is not a digit.</summary>
    public static int? DigitOf(KeyId id) => id switch
    {
        KeyId.D0 => 0, KeyId.D1 => 1, KeyId.D2 => 2, KeyId.D3 => 3, KeyId.D4 => 4,
        KeyId.D5 => 5, KeyId.D6 => 6, KeyId.D7 => 7, KeyId.D8 => 8, KeyId.D9 => 9,
        _ => null,
    };
}
