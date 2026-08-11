using MBRPN.Engine.Core;
using MBRPN.Engine.Dates;

namespace MBRPN.Engine.Financial;

/// <summary>
/// f PRICE / f YTM — U.S. Treasury-convention bonds: semiannual coupons, actual/actual
/// day counts, redemption at 100. Inputs: i = annual yield %, PMT = annual coupon %,
/// Y register = settlement date, X register = maturity date (calculator date format).
/// PRICE returns the quoted price in X and accrued interest in Y.
/// </summary>
public static class Bonds
{
    public sealed record Schedule(int CouponsRemaining, double DaysInPeriod, double DaysToNextCoupon, double AccruedDays);

    public static Schedule BuildSchedule(Real settlementValue, Real maturityValue, bool dmyMode)
    {
        DateOnly settlement = DateMath.Parse(settlementValue, dmyMode);
        DateOnly maturity = DateMath.Parse(maturityValue, dmyMode);
        if (settlement >= maturity) throw new CalcException(CalcError.Calendar);

        // Walk coupon dates back from maturity in 6-month steps until we straddle settlement.
        DateOnly next = maturity;
        int coupons = 1;
        while (true)
        {
            DateOnly previous = AddMonthsClamped(next, -6, maturity.Day);
            if (previous <= settlement)
            {
                double e = next.DayNumber - previous.DayNumber;
                double dsc = next.DayNumber - settlement.DayNumber;
                return new Schedule(coupons, e, dsc, e - dsc);
            }
            next = previous;
            coupons++;
            if (coupons > 5000) throw new CalcException(CalcError.Calendar);
        }
    }

    private static DateOnly AddMonthsClamped(DateOnly date, int months, int anchorDay)
    {
        int totalMonths = date.Year * 12 + (date.Month - 1) + months;
        int year = totalMonths / 12;
        int month = totalMonths % 12 + 1;
        int day = Math.Min(anchorDay, DateTime.DaysInMonth(year, month));
        return new DateOnly(year, month, day);
    }

    /// <summary>Clean price per 100 face value, plus accrued interest.</summary>
    public static (Real Price, Real Accrued) Price(Real annualYieldPercent, Real annualCouponPercent,
        Real settlementValue, Real maturityValue, bool dmyMode)
    {
        var s = BuildSchedule(settlementValue, maturityValue, dmyMode);
        double y = annualYieldPercent.ToDouble() / 200.0;   // semiannual yield fraction
        double c = annualCouponPercent.ToDouble() / 2.0;    // semiannual coupon per 100
        double accrued = c * (s.AccruedDays / s.DaysInPeriod);
        double price = DirtyPrice(y, c, s) - accrued;
        return (Real.FromDouble(price), Real.FromDouble(accrued));
    }

    private static double DirtyPrice(double y, double c, Schedule s)
    {
        double q = s.DaysToNextCoupon / s.DaysInPeriod;
        if (s.CouponsRemaining == 1)
        {
            // Final coupon period: simple interest (handbook formula).
            return (100.0 + c) / (1.0 + q * y);
        }
        if (y <= -1.0) throw new CalcException(CalcError.CompoundInterest);
        double sum = 0;
        for (int k = 1; k <= s.CouponsRemaining; k++)
            sum += c / Math.Pow(1 + y, k - 1 + q);
        return 100.0 / Math.Pow(1 + y, s.CouponsRemaining - 1 + q) + sum;
    }

    /// <summary>Yield to maturity (annual %) from a quoted price in PV.</summary>
    public static Real Ytm(Real quotedPrice, Real annualCouponPercent,
        Real settlementValue, Real maturityValue, bool dmyMode)
    {
        var s = BuildSchedule(settlementValue, maturityValue, dmyMode);
        double c = annualCouponPercent.ToDouble() / 2.0;
        double target = quotedPrice.ToDouble();
        double accruedFraction = s.AccruedDays / s.DaysInPeriod;

        double F(double y) => DirtyPrice(y, c, s) - c * accruedFraction - target;

        double lo = -0.499, hi = 10.0; // semiannual yield −99.8%…2000% annual
        double flo = F(lo), fhi = F(hi);
        if (double.IsNaN(flo) || double.IsNaN(fhi) || Math.Sign(flo) == Math.Sign(fhi))
            throw new CalcException(CalcError.CompoundInterest);
        double root = TvmSolver.Bisect(F, lo, hi);
        return Real.FromDouble(root * 200.0);
    }
}
