using Mb12c.Engine.Core;

namespace Mb12c.Engine.Financial;

/// <summary>
/// f INT — simple interest on both a 360-day and a 365-day basis, from n (days),
/// i (annual rate %) and PV (principal). The 360-day figure lands in X, the principal
/// stays in Y (so + gives the total), and the 365-day figure sits in Z (view it with
/// R↓ x≷y, per the handbook).
/// </summary>
public static class SimpleInterest
{
    public sealed record Result(Real Interest360, Real Principal, Real Interest365);

    public static Result Run(FinancialRegisters fin)
    {
        Real n = fin.N;
        Real rate = fin.I / Real.FromLong(100);
        Real pv = fin.PV;
        Real i360 = -(pv * n * rate) / Real.FromLong(360);
        Real i365 = -(pv * n * rate) / Real.FromLong(365);
        return new Result(i360, pv, i365);
    }
}
