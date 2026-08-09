using Mb12c.Engine.Keys;

namespace Mb12c.Engine.Core;

/// <summary>
/// Storage registers R0–R9 and R.0–R.9. Indices 0–9 are R0–R9, indices 10–19 are R.0–R.9.
/// The highest-numbered registers can be temporarily converted into program memory
/// (R.9 first); touching a converted register raises Error 6. Statistics use R1–R6.
/// </summary>
public sealed class RegisterFile
{
    public const int Count = 20;
    private readonly Real[] _registers = new Real[Count];

    /// <summary>Number of registers currently converted to program lines (taken from the top down).</summary>
    public int ConvertedToProgram { get; set; }

    /// <summary>First register index no longer available for storage.</summary>
    public int AvailableCount => Count - ConvertedToProgram;

    public Real this[int index]
    {
        get { Validate(index); return _registers[index]; }
        set { Validate(index); _registers[index] = value; }
    }

    /// <summary>Raw access without availability checks (used by statistics and cash-flow internals).</summary>
    public Real GetRaw(int index) => _registers[index];
    public void SetRaw(int index, Real value) => _registers[index] = value;

    public void StoreArithmetic(int index, Fn op, Real x)
    {
        Validate(index);
        Real current = _registers[index];
        Real result = op switch
        {
            Fn.Add => current + x,
            Fn.Subtract => current - x,
            Fn.Multiply => current * x,
            Fn.Divide => x.IsZero ? throw new CalcException(CalcError.StorageOverflow) : current / x,
            _ => throw new ArgumentOutOfRangeException(nameof(op)),
        };
        if (result.IsOverflow) throw new CalcException(CalcError.StorageOverflow);
        _registers[index] = result;
    }

    public void ClearAll()
    {
        Array.Clear(_registers);
    }

    private void Validate(int index)
    {
        if (index < 0 || index >= Count) throw new CalcException(CalcError.StorageRegister);
        if (index >= AvailableCount) throw new CalcException(CalcError.StorageRegister);
    }

    public IReadOnlyList<Real> Snapshot() => _registers.ToArray();

    public void Restore(IReadOnlyList<Real> values)
    {
        for (int i = 0; i < Count && i < values.Count; i++) _registers[i] = values[i];
    }
}
