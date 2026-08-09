using Mb12c.Engine.Core;

namespace Mb12c.Engine.Financial;

/// <summary>
/// Time-value-of-money mathematics (HP-12C Owner's Handbook, Appendix D).
///
/// The governing equation, with i the periodic rate as a fraction, S = 1 in BEGIN mode
/// else 0, n = n̂ + k split into whole periods n̂ and an odd first-period fraction k:
///
///   0 = PV·C₀·(1+i)^n̂ + PMT·(1+iS)·[((1+i)^n̂ − 1)/i] + FV·? …with C₀ = (1 + ki) for
///   simple interest over the odd period, or (1+i)^k when compounding (C annunciator).
///
/// PV, PMT and FV solve in closed form; n solves by logarithm and is rounded up to the
/// next whole period exactly as the calculator does; i is found numerically.
/// </summary>
public static class TvmSolver
{
    /// <summary>Residual of the TVM equation, all quantities as doubles; i is a fraction.</summary>
    private static double Residual(double n, double i, double pv, double pmt, double fv, bool begin, bool oddCompound)
    {
        double nInt = Math.Truncate(n);
        double k = n - nInt;
        if (i == 0.0) return pv + pmt * nInt + fv;
        double c0 = k == 0.0 ? 1.0 : (oddCompound ? Math.Pow(1 + i, k) : 1 + k * i);
        double a = Math.Pow(1 + i, nInt);
        double annuity = pmt * (1 + i * (begin ? 1 : 0)) * ((a - 1) / i);
        return pv * c0 * a + annuity + fv;
    }

    private static (double a, double c0, double factor) Terms(FinancialRegisters fin)
    {
        double n = fin.N.ToDouble();
        double i = fin.I.ToDouble() / 100.0;
        ValidateRate(fin, i);
        double nInt = Math.Truncate(n);
        double k = n - nInt;
        double c0 = k == 0.0 ? 1.0 : (fin.OddPeriodCompound ? Math.Pow(1 + i, k) : 1 + k * i);
        double a = Math.Pow(1 + i, nInt);
        double factor = 1 + i * (fin.BeginMode ? 1 : 0); // payment-timing multiplier
        return (a, c0, factor);
    }

    private static void ValidateRate(FinancialRegisters fin, double i)
    {
        if (i <= -1.0) throw new CalcException(CalcError.CompoundInterest);
        if (!fin.N.FractionalPart().IsZero && i < 0.0)
            throw new CalcException(CalcError.CompoundInterest);
    }

    public static Real SolveFv(FinancialRegisters fin)
    {
        double i = fin.I.ToDouble() / 100.0;
        double nInt = Math.Truncate(fin.N.ToDouble());
        if (i == 0.0)
            return Real.FromDouble(-(fin.PV.ToDouble() + fin.PMT.ToDouble() * nInt));
        var (a, c0, factor) = Terms(fin);
        double annuity = fin.PMT.ToDouble() * factor * ((a - 1) / i);
        return Real.FromDouble(-(fin.PV.ToDouble() * c0 * a + annuity));
    }

    public static Real SolvePv(FinancialRegisters fin)
    {
        double i = fin.I.ToDouble() / 100.0;
        double nInt = Math.Truncate(fin.N.ToDouble());
        if (i == 0.0)
            return Real.FromDouble(-(fin.FV.ToDouble() + fin.PMT.ToDouble() * nInt));
        var (a, c0, factor) = Terms(fin);
        double annuity = fin.PMT.ToDouble() * factor * ((a - 1) / i);
        return Real.FromDouble(-(fin.FV.ToDouble() + annuity) / (c0 * a));
    }

    public static Real SolvePmt(FinancialRegisters fin)
    {
        double i = fin.I.ToDouble() / 100.0;
        double nInt = Math.Truncate(fin.N.ToDouble());
        if (i == 0.0)
        {
            if (nInt == 0.0) throw new CalcException(CalcError.CompoundInterest);
            return Real.FromDouble(-(fin.PV.ToDouble() + fin.FV.ToDouble()) / nInt);
        }
        var (a, c0, factor) = Terms(fin);
        double denom = factor * ((a - 1) / i);
        if (denom == 0.0) throw new CalcException(CalcError.CompoundInterest);
        return Real.FromDouble(-(fin.PV.ToDouble() * c0 * a + fin.FV.ToDouble()) / denom);
    }

    /// <summary>Solves n and rounds it up to the next whole period, as the HP-12C does.</summary>
    public static Real SolveN(FinancialRegisters fin)
    {
        double i = fin.I.ToDouble() / 100.0;
        double pv = fin.PV.ToDouble(), pmt = fin.PMT.ToDouble(), fv = fin.FV.ToDouble();
        double n;
        if (i == 0.0)
        {
            if (pmt == 0.0) throw new CalcException(CalcError.CompoundInterest);
            n = -(pv + fv) / pmt;
        }
        else
        {
            if (i <= -1.0) throw new CalcException(CalcError.CompoundInterest);
            double factor = 1 + i * (fin.BeginMode ? 1 : 0);
            double p = pmt * factor / i;
            double numerator = p - fv;
            double denominator = p + pv;
            if (denominator == 0.0 || numerator / denominator <= 0.0)
                throw new CalcException(CalcError.CompoundInterest);
            n = Math.Log(numerator / denominator) / Math.Log(1 + i);
        }
        if (double.IsNaN(n) || double.IsInfinity(n)) throw new CalcException(CalcError.CompoundInterest);
        double rounded = Math.Ceiling(n - 1e-9);
        return Real.FromDouble(rounded);
    }

    /// <summary>Solves the periodic rate (as a percentage) numerically.</summary>
    public static Real SolveI(FinancialRegisters fin)
    {
        double n = fin.N.ToDouble();
        double pv = fin.PV.ToDouble(), pmt = fin.PMT.ToDouble(), fv = fin.FV.ToDouble();
        if (n == 0.0) throw new CalcException(CalcError.CompoundInterest);

        double F(double i) => Residual(n, i, pv, pmt, fv, fin.BeginMode, fin.OddPeriodCompound);

        // Bracket the root over −99.9999% … +1000% per period.
        double[] grid = BuildGrid();
        double prevI = grid[0], prevF = F(prevI);
        double? lo = null, hi = null;
        for (int idx = 1; idx < grid.Length; idx++)
        {
            double curI = grid[idx];
            double curF = F(curI);
            if (!double.IsNaN(prevF) && !double.IsNaN(curF) &&
                (prevF == 0.0 || Math.Sign(prevF) != Math.Sign(curF)))
            {
                lo = prevI; hi = curI;
                break;
            }
            prevI = curI; prevF = curF;
        }
        if (lo is null || hi is null) throw new CalcException(CalcError.CompoundInterest);

        double root = Bisect(F, lo.Value, hi.Value);
        return Real.FromDouble(root * 100.0);
    }

    private static double[] BuildGrid()
    {
        var points = new List<double> { -0.999999, -0.9999, -0.999, -0.99, -0.95, -0.9, -0.8, -0.6, -0.4, -0.2, -0.1, -0.05, -0.02, -0.01, -0.005, -0.001, -0.0001, 0.0 };
        for (double x = 0.0001; x <= 10.0; x *= 1.5) points.Add(x);
        points.Add(10.0);
        return points.ToArray();
    }

    /// <summary>Bisection with a secant acceleration step; robust for monotone-ish residuals.</summary>
    internal static double Bisect(Func<double, double> f, double lo, double hi)
    {
        double flo = f(lo), fhi = f(hi);
        if (flo == 0.0) return lo;
        if (fhi == 0.0) return hi;
        for (int iter = 0; iter < 200; iter++)
        {
            double mid;
            // Secant candidate, clamped inside the bracket for safety.
            double secant = hi - fhi * (hi - lo) / (fhi - flo);
            mid = secant > lo && secant < hi ? secant : (lo + hi) / 2.0;
            // Alternate with pure bisection to guarantee bracket shrinkage.
            if ((iter & 1) == 1) mid = (lo + hi) / 2.0;
            double fmid = f(mid);
            if (fmid == 0.0 || Math.Abs(hi - lo) < Math.Abs(mid) * 1e-15 + 1e-18) return mid;
            if (Math.Sign(fmid) == Math.Sign(flo)) { lo = mid; flo = fmid; }
            else { hi = mid; fhi = fmid; }
        }
        return (lo + hi) / 2.0;
    }
}
