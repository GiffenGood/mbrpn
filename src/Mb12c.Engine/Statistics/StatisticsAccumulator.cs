using Mb12c.Engine.Core;

namespace Mb12c.Engine.Statistics;

/// <summary>
/// Σ+ / Σ− accumulation and the derived statistics, stored exactly where the HP-12C
/// keeps them: R1 = n, R2 = Σx, R3 = Σx², R4 = Σy, R5 = Σy², R6 = Σxy.
/// Division by zero anywhere here is Error 2.
/// </summary>
public sealed class StatisticsAccumulator(RegisterFile registers)
{
    private Real N { get => registers.GetRaw(1); set => registers.SetRaw(1, value); }
    private Real Sx { get => registers.GetRaw(2); set => registers.SetRaw(2, value); }
    private Real Sxx { get => registers.GetRaw(3); set => registers.SetRaw(3, value); }
    private Real Sy { get => registers.GetRaw(4); set => registers.SetRaw(4, value); }
    private Real Syy { get => registers.GetRaw(5); set => registers.SetRaw(5, value); }
    private Real Sxy { get => registers.GetRaw(6); set => registers.SetRaw(6, value); }

    /// <summary>Σ+ (or Σ− with negative sign): returns the new n for the display.</summary>
    public Real Accumulate(Real x, Real y, bool subtract)
    {
        Real one = subtract ? -Real.One : Real.One;
        Real sign = one;
        N += sign;
        Sx += sign * x;
        Sxx += sign * x * x;
        Sy += sign * y;
        Syy += sign * y * y;
        Sxy += sign * x * y;
        return N;
    }

    public void Clear()
    {
        for (int i = 1; i <= 6; i++) registers.SetRaw(i, Real.Zero);
    }

    public (Real MeanX, Real MeanY) Mean()
    {
        RequireN();
        return (Sx / N, Sy / N);
    }

    /// <summary>x̄w — weighted mean: values in y, weights in x (Σxy / Σx).</summary>
    public Real WeightedMean()
    {
        if (Sx.IsZero) throw new CalcException(CalcError.Statistics);
        return Sxy / Sx;
    }

    public (Real Sx, Real Sy) SampleStdDev()
    {
        RequireN();
        Real nMinus1 = N - Real.One;
        if (nMinus1.IsZero || nMinus1.IsNegative) throw new CalcException(CalcError.Statistics);
        Real varX = (N * Sxx - Sx * Sx) / (N * nMinus1);
        Real varY = (N * Syy - Sy * Sy) / (N * nMinus1);
        if (varX.IsNegative || varY.IsNegative) throw new CalcException(CalcError.Statistics);
        return (MathX.Sqrt(varX), MathX.Sqrt(varY));
    }

    /// <summary>ŷ,r — linear estimate of y at the given x, with the correlation coefficient.</summary>
    public (Real Estimate, Real R) EstimateY(Real x)
    {
        var (slope, intercept, r) = Regression();
        return (intercept + slope * x, r);
    }

    /// <summary>x̂,r — linear estimate of x at the given y, with the correlation coefficient.</summary>
    public (Real Estimate, Real R) EstimateX(Real y)
    {
        var (slope, intercept, r) = Regression();
        if (slope.IsZero) throw new CalcException(CalcError.Statistics);
        return ((y - intercept) / slope, r);
    }

    private (Real Slope, Real Intercept, Real R) Regression()
    {
        RequireN();
        Real covTerm = Sxy - Sx * Sy / N;
        Real varXTerm = Sxx - Sx * Sx / N;
        Real varYTerm = Syy - Sy * Sy / N;
        if (varXTerm.IsZero) throw new CalcException(CalcError.Statistics);
        Real slope = covTerm / varXTerm;
        Real intercept = (Sy - slope * Sx) / N;
        Real denom = varXTerm * varYTerm;
        if (denom.IsZero || denom.IsNegative) throw new CalcException(CalcError.Statistics);
        Real r = covTerm / MathX.Sqrt(denom);
        return (slope, intercept, r);
    }

    private void RequireN()
    {
        if (N.IsZero) throw new CalcException(CalcError.Statistics);
    }
}
