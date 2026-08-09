using Mb12c.Engine.Core;

namespace Mb12c.Engine.Financial;

/// <summary>
/// Discounted cash-flow analysis. Amounts live where the real calculator puts them:
/// CF₀ in R0, CF₁–CF₁₉ in R1–R.9, and CF₂₀ in the FV register. Repeat counts Nⱼ (1–99)
/// live in a private area, recallable with RCL g Nⱼ. The n register tracks the highest
/// cash-flow group number j.
/// </summary>
public sealed class CashFlows
{
    public const int MaxFlows = 20;

    private readonly RegisterFile _registers;
    private readonly FinancialRegisters _fin;

    /// <summary>Repeat count per group, index 0–20.</summary>
    public int[] RepeatCounts { get; } = new int[MaxFlows + 1];

    public CashFlows(RegisterFile registers, FinancialRegisters fin)
    {
        _registers = registers;
        _fin = fin;
        Array.Fill(RepeatCounts, 1);
    }

    private int CurrentIndex => (int)_fin.N.IntegerPart().ToDecimal();

    /// <summary>g CF₀ — stores the initial flow in R0 and resets the group counter.</summary>
    public void StoreInitial(Real amount)
    {
        _registers[0] = amount;   // raises Error 6 if R0 is unavailable
        _fin.N = Real.Zero;
        Array.Fill(RepeatCounts, 1);
    }

    /// <summary>g CFⱼ — stores the next flow amount and advances the group counter.</summary>
    public void StoreNext(Real amount)
    {
        int j = CurrentIndex + 1;
        if (j > MaxFlows) throw new CalcException(CalcError.StorageRegister);
        SetAmount(j, amount);
        RepeatCounts[j] = 1;
        _fin.N = Real.FromLong(j);
    }

    /// <summary>g Nⱼ — sets the repeat count (1–99) for the current group.</summary>
    public void StoreRepeat(Real count)
    {
        if (count.IsNegative || !count.FractionalPart().IsZero)
            throw new CalcException(CalcError.StorageRegister);
        decimal c = count.IntegerPart().ToDecimal();
        if (c < 1m || c > 99m) throw new CalcException(CalcError.StorageRegister);
        int j = CurrentIndex;
        if (j < 0 || j > MaxFlows) throw new CalcException(CalcError.StorageRegister);
        RepeatCounts[j] = (int)c;
    }

    /// <summary>RCL g CFⱼ — recalls the current group's amount and steps the counter back.</summary>
    public Real RecallCurrentAmount()
    {
        int j = CurrentIndex;
        if (j < 1 || j > MaxFlows) throw new CalcException(CalcError.StorageRegister);
        Real amount = GetAmount(j);
        _fin.N = Real.FromLong(j - 1);
        return amount;
    }

    /// <summary>RCL g Nⱼ — recalls the current group's repeat count.</summary>
    public Real RecallCurrentRepeat()
    {
        int j = CurrentIndex;
        if (j < 0 || j > MaxFlows) throw new CalcException(CalcError.StorageRegister);
        return Real.FromLong(RepeatCounts[j]);
    }

    public Real GetAmount(int j) => j == MaxFlows ? _fin.FV : _registers[j];

    private void SetAmount(int j, Real amount)
    {
        if (j == MaxFlows) _fin.FV = amount;
        else _registers[j] = amount; // raises Error 6 when converted to program lines
    }

    /// <summary>f NPV — net present value at the rate in the i register.</summary>
    public Real Npv()
    {
        int top = CurrentIndex;
        if (top < 0 || top > MaxFlows) throw new CalcException(CalcError.StorageRegister);
        decimal rate = _fin.I.ToDecimal() / 100m;
        decimal discount = 1m;
        decimal onePlus = 1m + rate;
        if (onePlus <= 0m) throw new CalcException(CalcError.CompoundInterest);
        Real npv = GetAmount(0);
        for (int j = 1; j <= top; j++)
        {
            Real amount = GetAmount(j);
            for (int r = 0; r < RepeatCounts[j]; r++)
            {
                discount /= onePlus;
                npv += amount * Real.FromDecimal(discount);
            }
        }
        return npv;
    }

    /// <summary>f IRR — the rate (as a %) making NPV zero. Error 7 without a sign change;
    /// Error 3 when the flows admit no single answer the solver can settle on.</summary>
    public Real Irr()
    {
        int top = CurrentIndex;
        if (top < 1 || top > MaxFlows) throw new CalcException(CalcError.StorageRegister);

        var amounts = new List<double>();
        amounts.Add(GetAmount(0).ToDouble());
        for (int j = 1; j <= top; j++)
        {
            double amount = GetAmount(j).ToDouble();
            for (int r = 0; r < RepeatCounts[j]; r++) amounts.Add(amount);
        }

        bool hasPositive = amounts.Any(a => a > 0);
        bool hasNegative = amounts.Any(a => a < 0);
        if (!hasPositive || !hasNegative) throw new CalcException(CalcError.IrrNoSolution);

        double F(double rate)
        {
            double npv = 0, d = 1;
            foreach (double amount in amounts)
            {
                npv += amount * d;
                d /= 1 + rate;
            }
            // The loop discounts flow t by (1+r)^-t with t starting at 0 for CF0.
            return npv;
        }

        var brackets = new List<(double lo, double hi)>();
        double[] grid = IrrGrid();
        double prev = grid[0], prevF = F(prev);
        for (int idx = 1; idx < grid.Length; idx++)
        {
            double cur = grid[idx], curF = F(cur);
            if (!double.IsNaN(prevF) && !double.IsNaN(curF) && !double.IsInfinity(prevF) && !double.IsInfinity(curF) &&
                (prevF == 0.0 || Math.Sign(prevF) != Math.Sign(curF)))
                brackets.Add((prev, cur));
            prev = cur; prevF = curF;
        }
        if (brackets.Count == 0) throw new CalcException(CalcError.IrrAmbiguous);
        if (brackets.Count > 1) throw new CalcException(CalcError.IrrAmbiguous);

        double root = TvmSolver.Bisect(F, brackets[0].lo, brackets[0].hi);
        return Real.FromDouble(root * 100.0);
    }

    private static double[] IrrGrid()
    {
        var points = new List<double> { -0.999999, -0.9999, -0.999, -0.99, -0.95, -0.9, -0.8, -0.6, -0.4, -0.2, -0.1, -0.05, -0.02, -0.01, -0.001, 0.0 };
        for (double x = 0.001; x <= 100.0; x *= 1.6) points.Add(x);
        points.Add(100.0);
        return points.ToArray();
    }
}
