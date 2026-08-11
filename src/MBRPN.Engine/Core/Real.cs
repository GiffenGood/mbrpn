using System.Globalization;

namespace MBRPN.Engine.Core;

/// <summary>
/// A number as the HP-12C stores it: a signed mantissa of at most 10 significant digits
/// and a power-of-ten exponent in −99…99. The mantissa is kept normalized to [1, 10)
/// (zero is M = 0, E = 0). Every arithmetic result is rounded to 10 significant digits,
/// matching the calculator's BCD registers; results beyond ±9.999999999×10⁹⁹ saturate to
/// that value (the calculator's overflow display) and results below 10⁻⁹⁹ flush to zero.
/// </summary>
public readonly struct Real : IEquatable<Real>, IComparable<Real>
{
    /// <summary>Normalized mantissa: 0, or |M| in [1, 10) with at most 10 significant digits.</summary>
    public decimal M { get; }

    /// <summary>Power-of-ten exponent, −99…99.</summary>
    public int E { get; }

    public static readonly Real Zero = default;
    public static readonly Real One = new(1m, 0);
    public static readonly Real Max = new(9.999999999m, 99);

    private Real(decimal m, int e) { M = m; E = e; }

    public bool IsZero => M == 0m;
    public bool IsNegative => M < 0m;

    /// <summary>True when the value saturated the ±9.999999999E99 limit (overflow display).</summary>
    public bool IsOverflow => E == 99 && Math.Abs(M) == 9.999999999m;

    // ---- Construction ------------------------------------------------------------

    /// <summary>Builds a value from an arbitrary mantissa/exponent, normalizing and rounding to 10 digits.</summary>
    public static Real FromParts(decimal mantissa, int exponent)
    {
        if (mantissa == 0m) return Zero;
        decimal abs = Math.Abs(mantissa);
        while (abs >= 10m) { abs /= 10m; exponent++; }
        while (abs < 1m) { abs *= 10m; exponent--; }
        abs = Math.Round(abs, 9, MidpointRounding.AwayFromZero);
        if (abs >= 10m) { abs /= 10m; exponent++; }
        if (exponent > 99) return mantissa < 0m ? -Max : Max;
        if (exponent < -99) return Zero;
        return new Real(mantissa < 0m ? -abs : abs, exponent);
    }

    public static Real FromDecimal(decimal d) => FromParts(d, 0);

    public static Real FromLong(long v) => FromParts(v, 0);

    /// <summary>
    /// Converts a double (e.g. the result of a transcendental function or an iterative
    /// solver). NaN/Infinity raise Error 0.
    /// </summary>
    public static Real FromDouble(double v)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) throw new CalcException(CalcError.Math);
        if (v == 0.0) return Zero;
        // Round-trip through scientific text so the exponent extraction is exact.
        string s = v.ToString("E15", CultureInfo.InvariantCulture);
        int eIdx = s.IndexOf('E');
        decimal mantissa = decimal.Parse(s[..eIdx], CultureInfo.InvariantCulture);
        int exponent = int.Parse(s[(eIdx + 1)..], CultureInfo.InvariantCulture);
        return FromParts(mantissa, exponent);
    }

    // ---- Conversion --------------------------------------------------------------

    /// <summary>
    /// The value as a decimal, saturating at the decimal type's range (|E| ≳ 28).
    /// Financial and date quantities are always comfortably inside that range.
    /// </summary>
    public decimal ToDecimal()
    {
        if (IsZero) return 0m;
        if (E >= 28) return M < 0m ? decimal.MinValue : decimal.MaxValue;
        if (E <= -28) return 0m;
        return E >= 0 ? M * Pow10Dec(E) : M / Pow10Dec(-E);
    }

    public double ToDouble() => (double)M * Math.Pow(10, E);

    // ---- Arithmetic (each result rounded to 10 significant digits) ---------------

    public static Real operator +(Real a, Real b)
    {
        if (a.IsZero) return b;
        if (b.IsZero) return a;
        if (a.E < b.E) (a, b) = (b, a);
        int diff = a.E - b.E;
        if (diff > 25) return a; // b is beyond any influence on 10 significant digits
        decimal aligned = b.M / Pow10Dec(diff);
        return FromParts(a.M + aligned, a.E);
    }

    public static Real operator -(Real a) => a.IsZero ? Zero : new Real(-a.M, a.E);

    public static Real operator -(Real a, Real b) => a + (-b);

    public static Real operator *(Real a, Real b)
    {
        if (a.IsZero || b.IsZero) return Zero;
        return FromParts(a.M * b.M, a.E + b.E);
    }

    public static Real operator /(Real a, Real b)
    {
        if (b.IsZero) throw new CalcException(CalcError.Math);
        if (a.IsZero) return Zero;
        return FromParts(a.M / b.M, a.E - b.E);
    }

    // ---- Comparison --------------------------------------------------------------

    public int CompareTo(Real other)
    {
        if (M == other.M && E == other.E) return 0;
        bool aNeg = M < 0m, bNeg = other.M < 0m;
        if (IsZero) return bNeg ? 1 : -1;
        if (other.IsZero) return aNeg ? -1 : 1;
        if (aNeg != bNeg) return aNeg ? -1 : 1;
        int magnitude = E != other.E
            ? E.CompareTo(other.E)
            : Math.Abs(M).CompareTo(Math.Abs(other.M));
        return aNeg ? -magnitude : magnitude;
    }

    public bool Equals(Real other) => M == other.M && E == other.E;
    public override bool Equals(object? obj) => obj is Real r && Equals(r);
    public override int GetHashCode() => HashCode.Combine(M, E);
    public static bool operator ==(Real a, Real b) => a.Equals(b);
    public static bool operator !=(Real a, Real b) => !a.Equals(b);
    public static bool operator <(Real a, Real b) => a.CompareTo(b) < 0;
    public static bool operator <=(Real a, Real b) => a.CompareTo(b) <= 0;
    public static bool operator >(Real a, Real b) => a.CompareTo(b) > 0;
    public static bool operator >=(Real a, Real b) => a.CompareTo(b) >= 0;

    // ---- Pieces ------------------------------------------------------------------

    /// <summary>Integer part (INTG), truncated toward zero.</summary>
    public Real IntegerPart()
    {
        if (E < 0) return Zero;
        if (E >= 9) return this; // all 10 mantissa digits are integer digits
        decimal scaled = M * Pow10Dec(E);
        return FromDecimal(decimal.Truncate(scaled));
    }

    /// <summary>Fractional part (FRAC), keeping the sign of the value.</summary>
    public Real FractionalPart() => this - IntegerPart();

    public Real Abs() => IsNegative ? -this : this;

    private static readonly decimal[] Pow10Table = CreatePow10();

    private static decimal[] CreatePow10()
    {
        var table = new decimal[29];
        table[0] = 1m;
        for (int i = 1; i < table.Length; i++) table[i] = table[i - 1] * 10m;
        return table;
    }

    private static decimal Pow10Dec(int e) => e < Pow10Table.Length
        ? Pow10Table[e]
        : Pow10Table[^1]; // beyond alignment relevance; callers guard the range

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{M}E{E}");
}
