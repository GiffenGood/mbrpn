using MBRPN.Engine.Core;

namespace MBRPN.Engine.Financial;

/// <summary>
/// f SL / f SOYD / f DB — depreciation for year j (taken from X). Inputs: PV = cost,
/// FV = salvage, n = useful life in years, i = declining-balance factor % (DB only).
/// Returns the year's depreciation (X) and the remaining depreciable value (Y).
/// </summary>
public static class Depreciation
{
    public sealed record Result(Real Amount, Real Remaining);

    public static Result StraightLine(FinancialRegisters fin, Real yearValue)
    {
        var (n, j) = ValidateYears(fin, yearValue);
        decimal depreciable = fin.PV.ToDecimal() - fin.FV.ToDecimal();
        if (j > n) return new Result(Real.Zero, Real.Zero);
        Real amount = Real.FromDecimal(depreciable) / fin.N;
        Real remaining = Real.FromDecimal(depreciable) - amount * Real.FromDecimal(j);
        return new Result(amount, remaining);
    }

    public static Result SumOfYearsDigits(FinancialRegisters fin, Real yearValue)
    {
        var (n, j) = ValidateYears(fin, yearValue);
        decimal depreciable = fin.PV.ToDecimal() - fin.FV.ToDecimal();
        if (j > n) return new Result(Real.Zero, Real.Zero);
        decimal soyd = n * (n + 1) / 2m;
        Real amount = Real.FromDecimal((n - j + 1) / soyd * depreciable);
        decimal taken = j * n - j * (j - 1) / 2m; // Σ_{k=1..j} (n−k+1)
        Real remaining = Real.FromDecimal(depreciable * (1 - taken / soyd));
        return new Result(amount, remaining);
    }

    public static Result DecliningBalance(FinancialRegisters fin, Real yearValue)
    {
        var (n, j) = ValidateYears(fin, yearValue);
        decimal rate = fin.I.ToDecimal() / 100m / n;
        if (rate <= 0m) throw new CalcException(CalcError.Math);
        decimal salvage = fin.FV.ToDecimal();
        decimal book = fin.PV.ToDecimal();
        decimal amount = 0m;
        for (decimal k = 1m; k <= j; k++)
        {
            amount = Math.Max(0m, Math.Min(book * rate, book - salvage));
            book -= amount;
        }
        if (j > n) amount = 0m;
        return new Result(Real.FromDecimal(amount), Real.FromDecimal(book - salvage));
    }

    private static (decimal n, decimal j) ValidateYears(FinancialRegisters fin, Real yearValue)
    {
        if (!fin.N.FractionalPart().IsZero || fin.N.IsZero || fin.N.IsNegative)
            throw new CalcException(CalcError.Math);
        if (yearValue.IsNegative || yearValue.IsZero || !yearValue.FractionalPart().IsZero)
            throw new CalcException(CalcError.Math);
        decimal n = fin.N.ToDecimal();
        decimal j = yearValue.IntegerPart().ToDecimal();
        if (n > 10_000m || j > 100_000m) throw new CalcException(CalcError.Math);
        return (n, j);
    }
}
