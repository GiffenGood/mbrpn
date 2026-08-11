using MBRPN.Engine.Core;
using MBRPN.Engine.Display;

namespace MBRPN.Engine.Financial;

/// <summary>
/// f AMORT — amortizes x payments starting from the current n, using i, PV and PMT.
/// Faithful detail: each period's interest is rounded to the current display precision
/// before it is accumulated (the results therefore depend on the FIX setting, exactly
/// as on the real calculator).
/// </summary>
public static class Amortization
{
    public sealed record Result(Real TotalInterest, Real TotalPrincipal, Real NewBalance, Real NewN);

    public static Result Run(FinancialRegisters fin, Real x, int displayDecimals)
    {
        if (x.IsNegative || x.IsZero || !x.FractionalPart().IsZero)
            throw new CalcException(CalcError.CompoundInterest);
        decimal count = x.IntegerPart().ToDecimal();
        if (count > 10_000m) throw new CalcException(CalcError.CompoundInterest);

        Real i = fin.I / Real.FromLong(100);
        Real balance = fin.PV;
        Real pmt = fin.PMT;
        Real totalInterest = Real.Zero;
        Real totalPrincipal = Real.Zero;
        int pmtSign = pmt.IsNegative ? -1 : 1;
        bool firstEver = fin.N.IsZero;

        for (decimal j = 1m; j <= count; j++)
        {
            Real interest;
            if (firstEver && fin.BeginMode && j == 1m)
            {
                interest = Real.Zero;
            }
            else
            {
                Real raw = (balance * i).Abs();
                interest = DisplayFormatter.RoundToFix(raw, displayDecimals);
                if (pmtSign < 0) interest = -interest;
            }
            Real principal = pmt - interest;
            balance += principal;
            totalInterest += interest;
            totalPrincipal += principal;
        }

        return new Result(totalInterest, totalPrincipal, balance, fin.N + x);
    }
}
