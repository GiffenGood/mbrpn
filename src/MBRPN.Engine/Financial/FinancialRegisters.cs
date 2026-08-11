using MBRPN.Engine.Core;

namespace MBRPN.Engine.Financial;

/// <summary>
/// The five financial registers plus the payment-timing and odd-period-compounding modes.
/// </summary>
public sealed class FinancialRegisters
{
    /// <summary>Number of compounding periods (may be fractional for odd-period calculations).</summary>
    public Real N { get; set; }

    /// <summary>Periodic interest rate, as a percentage.</summary>
    public Real I { get; set; }

    public Real PV { get; set; }
    public Real PMT { get; set; }
    public Real FV { get; set; }

    /// <summary>True = payments at beginning of period (BEGIN annunciator); false = END.</summary>
    public bool BeginMode { get; set; }

    /// <summary>True = compound interest during an odd first period (C annunciator, STO EEX).</summary>
    public bool OddPeriodCompound { get; set; }

    public void Clear()
    {
        N = I = PV = PMT = FV = Real.Zero;
    }
}
