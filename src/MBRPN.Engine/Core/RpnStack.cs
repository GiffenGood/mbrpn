namespace MBRPN.Engine.Core;

/// <summary>
/// The four-register RPN stack (X, Y, Z, T) plus LAST X, with the HP-12C's stack-lift
/// discipline: ENTER, CLx and Σ+/Σ− disable lift so the next number entered overwrites X;
/// almost everything else enables it so the next number pushes the stack up.
/// </summary>
public sealed class RpnStack
{
    public Real X { get; set; }
    public Real Y { get; set; }
    public Real Z { get; set; }
    public Real T { get; set; }
    public Real LastX { get; set; }

    /// <summary>When false, the next value placed in X overwrites it instead of lifting.</summary>
    public bool LiftEnabled { get; set; }

    /// <summary>Places a new value in X, lifting the stack first if lift is enabled.</summary>
    public void Place(Real value)
    {
        if (LiftEnabled) Lift();
        X = value;
    }

    /// <summary>Lifts the stack: T is lost, Z→T, Y→Z, X→Y.</summary>
    public void Lift()
    {
        T = Z;
        Z = Y;
        Y = X;
    }

    /// <summary>Drops the stack after a two-operand function: Z→Y, T→Z, T replicates.</summary>
    public void Drop()
    {
        Y = Z;
        Z = T;
    }

    public void RollDown()
    {
        (X, Y, Z, T) = (Y, Z, T, X);
    }

    public void SwapXY()
    {
        (X, Y) = (Y, X);
    }

    public void Clear()
    {
        X = Y = Z = T = Real.Zero;
    }
}
