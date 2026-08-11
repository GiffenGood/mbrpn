namespace MBRPN.Engine.Core;

/// <summary>
/// Transcendental and special functions on <see cref="Real"/> values. Computation uses
/// double precision internally (15–16 significant digits) and rounds back to the
/// calculator's 10, working in mantissa/exponent form so the full ±10⁹⁹ range survives.
/// Domain violations raise Error 0 exactly as the HP-12C does.
/// </summary>
public static class MathX
{
    private const double Log10E = 0.43429448190325182765; // log10(e)

    public static Real Sqrt(Real x)
    {
        if (x.IsNegative) throw new CalcException(CalcError.Math);
        if (x.IsZero) return Real.Zero;
        // sqrt(M × 10^E): pull an even power of ten out so the double stays comfortable.
        int e = x.E;
        double m = (double)x.M;
        if ((e & 1) != 0) { m *= 10; e--; }
        return Real.FromParts((decimal)Math.Sqrt(m), 0) * Real.FromParts(1m, e / 2);
    }

    public static Real Ln(Real x)
    {
        if (x.IsZero || x.IsNegative) throw new CalcException(CalcError.Math);
        double value = Math.Log((double)x.M) + x.E * Math.Log(10);
        return Real.FromDouble(value);
    }

    public static Real Exp(Real x) => PowerOf10(x.ToDouble() * Log10E);

    /// <summary>y^x with HP-12C domain rules: negative y requires an integer x.</summary>
    public static Real Pow(Real y, Real x)
    {
        if (y.IsZero)
        {
            if (x.IsZero || x.IsNegative) throw new CalcException(CalcError.Math);
            return Real.Zero;
        }
        bool negBase = y.IsNegative;
        bool resultNegative = false;
        if (negBase)
        {
            if (!x.FractionalPart().IsZero) throw new CalcException(CalcError.Math);
            // Negative base, integer exponent: sign follows the exponent's parity.
            decimal xInt = x.IntegerPart().ToDecimal();
            resultNegative = Math.Abs(xInt % 2m) == 1m;
        }
        double log10y = Math.Log10(Math.Abs((double)y.M)) + y.E;
        Real magnitude = PowerOf10(x.ToDouble() * log10y);
        return resultNegative ? -magnitude : magnitude;
    }

    /// <summary>n! for non-negative integers; ≥ 70 saturates to the overflow display value.</summary>
    public static Real Factorial(Real x)
    {
        if (x.IsNegative || !x.FractionalPart().IsZero) throw new CalcException(CalcError.Math);
        decimal n = x.IntegerPart().ToDecimal();
        if (n >= 70m) return Real.Max;
        double result = 1;
        for (int k = 2; k <= (int)n; k++) result *= k;
        return Real.FromDouble(result);
    }

    /// <summary>10^p for a possibly huge p, saturating at the calculator's range limits.</summary>
    private static Real PowerOf10(double p)
    {
        if (double.IsNaN(p)) throw new CalcException(CalcError.Math);
        if (p > 100) return Real.Max;
        if (p < -100) return Real.Zero;
        int e = (int)Math.Floor(p);
        double mantissa = Math.Pow(10, p - e);
        return Real.FromParts((decimal)mantissa, e);
    }
}
