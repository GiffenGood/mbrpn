namespace Mb12c.Engine.Core;

/// <summary>HP-12C error numbers (Appendix "Error Conditions" of the Owner's Handbook).</summary>
public enum CalcError
{
    /// <summary>Error 0 — mathematics (÷0, √ of negative, LN of non-positive, etc.).</summary>
    Math = 0,
    /// <summary>Error 1 — storage register overflow (STO arithmetic result too large).</summary>
    StorageOverflow = 1,
    /// <summary>Error 2 — statistics (n = 0, or would divide by zero).</summary>
    Statistics = 2,
    /// <summary>Error 3 — IRR: cannot compute without further information (possibly multiple answers).</summary>
    IrrAmbiguous = 3,
    /// <summary>Error 4 — memory: program line doesn't exist or register unavailable.</summary>
    Memory = 4,
    /// <summary>Error 5 — compound interest (i ≤ −100%, no solution, invalid inputs).</summary>
    CompoundInterest = 5,
    /// <summary>Error 6 — storage registers: register doesn't exist or has been converted to program lines.</summary>
    StorageRegister = 6,
    /// <summary>Error 7 — IRR: no sign change in cash flows.</summary>
    IrrNoSolution = 7,
    /// <summary>Error 8 — calendar: improper date format or out-of-range date.</summary>
    Calendar = 8,
    /// <summary>Error 9 — service (self-test).</summary>
    Service = 9,
}

/// <summary>Thrown by engine components; the engine catches it and shows "Error n".</summary>
public sealed class CalcException(CalcError error) : Exception($"Error {(int)error}")
{
    public CalcError Error { get; } = error;
}
