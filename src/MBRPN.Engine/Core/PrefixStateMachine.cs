using MBRPN.Engine.Keys;

namespace MBRPN.Engine.Core;

/// <summary>A fully resolved key sequence, carrying the keys that formed it (for program recording).</summary>
public abstract record ResolvedAction(IReadOnlyList<KeyId> Sequence);

/// <summary>A plain function (possibly f- or g-shifted).</summary>
public sealed record FnAction(Fn Fn, IReadOnlyList<KeyId> Sequence) : ResolvedAction(Sequence);

/// <summary>f n — set FIX display digits.</summary>
public sealed record FixAction(int Digits, IReadOnlyList<KeyId> Sequence) : ResolvedAction(Sequence);

/// <summary>STO reg (0–9 = R0–R9, 10–19 = R.0–R.9), optionally with register arithmetic.</summary>
public sealed record StoAction(int Register, Fn? Op, IReadOnlyList<KeyId> Sequence) : ResolvedAction(Sequence);

/// <summary>RCL reg.</summary>
public sealed record RclAction(int Register, IReadOnlyList<KeyId> Sequence) : ResolvedAction(Sequence);

/// <summary>RCL n/i/PV/PMT/FV.</summary>
public sealed record RclFinAction(Fn Register, IReadOnlyList<KeyId> Sequence) : ResolvedAction(Sequence);

/// <summary>RCL g CFⱼ (amount, steps j back) or RCL g Nⱼ (repeat count).</summary>
public sealed record RclCashFlowAction(bool Amount, IReadOnlyList<KeyId> Sequence) : ResolvedAction(Sequence);

/// <summary>g GTO nn — branch target (recorded in PRGM mode, repositions in run mode).</summary>
public sealed record GtoAction(int Line, IReadOnlyList<KeyId> Sequence) : ResolvedAction(Sequence);

/// <summary>g GTO . nn — reposition without recording (never recorded).</summary>
public sealed record GtoDotAction(int Line, IReadOnlyList<KeyId> Sequence) : ResolvedAction(Sequence);

/// <summary>STO EEX — toggle odd-period compounding (the C annunciator).</summary>
public sealed record ToggleOddPeriodAction(IReadOnlyList<KeyId> Sequence) : ResolvedAction(Sequence);

/// <summary>
/// Tracks pending prefixes (f, g, STO, RCL, GTO and their argument digits) and resolves
/// complete key sequences into actions. A key that cannot complete the pending sequence
/// cancels it and is re-processed from scratch, closely matching the real keyboard.
/// </summary>
public sealed class PrefixStateMachine
{
    private enum State { None, F, G, Sto, StoDot, StoOp, StoOpDot, Rcl, RclDot, RclG, Gto, GtoDigit1, GtoDot, GtoDotDigit1 }

    private State _state = State.None;
    private Fn _pendingOp;
    private int _firstDigit;
    private readonly List<KeyId> _sequence = new();

    public bool FPending => _state == State.F;
    public bool GPending => _state == State.G;
    public bool AnyPending => _state != State.None;

    public void Reset()
    {
        _state = State.None;
        _sequence.Clear();
    }

    /// <summary>
    /// Feeds one key. Returns the resolved action, or null when the key was consumed
    /// as part of a still-incomplete sequence.
    /// </summary>
    public ResolvedAction? Accept(KeyId key)
    {
        int? digit = KeyboardMap.DigitOf(key);
        switch (_state)
        {
            case State.None:
                return AcceptFromIdle(key, digit);

            case State.F:
                return AcceptShifted(key, digit, isF: true);

            case State.G:
                return AcceptShifted(key, digit, isF: false);

            case State.Sto:
                if (digit is int sd) return Complete(key, new StoAction(sd, null, Seq(key)));
                if (key == KeyId.Point) { _state = State.StoDot; _sequence.Add(key); return null; }
                if (key is KeyId.Add or KeyId.Subtract or KeyId.Multiply or KeyId.Divide)
                {
                    _pendingOp = KeyboardMap.Get(key).Primary;
                    _state = State.StoOp;
                    _sequence.Add(key);
                    return null;
                }
                if (key == KeyId.EEX) return Complete(key, new ToggleOddPeriodAction(Seq(key)));
                return CancelAndRedispatch(key);

            case State.StoDot:
                if (digit is int sdd) return Complete(key, new StoAction(10 + sdd, null, Seq(key)));
                return CancelAndRedispatch(key);

            case State.StoOp:
                if (digit is int sod) return Complete(key, new StoAction(sod, _pendingOp, Seq(key)));
                if (key == KeyId.Point) { _state = State.StoOpDot; _sequence.Add(key); return null; }
                return CancelAndRedispatch(key);

            case State.StoOpDot:
                if (digit is int sodd) return Complete(key, new StoAction(10 + sodd, _pendingOp, Seq(key)));
                return CancelAndRedispatch(key);

            case State.Rcl:
                if (digit is int rd) return Complete(key, new RclAction(rd, Seq(key)));
                if (key == KeyId.Point) { _state = State.RclDot; _sequence.Add(key); return null; }
                if (key is KeyId.N or KeyId.I or KeyId.PV or KeyId.PMT or KeyId.FV)
                    return Complete(key, new RclFinAction(KeyboardMap.Get(key).Primary, Seq(key)));
                if (key == KeyId.G) { _state = State.RclG; _sequence.Add(key); return null; }
                return CancelAndRedispatch(key);

            case State.RclDot:
                if (digit is int rdd) return Complete(key, new RclAction(10 + rdd, Seq(key)));
                return CancelAndRedispatch(key);

            case State.RclG:
                if (key == KeyId.PMT) return Complete(key, new RclCashFlowAction(Amount: true, Seq(key)));
                if (key == KeyId.FV) return Complete(key, new RclCashFlowAction(Amount: false, Seq(key)));
                if (key == KeyId.PV) return Complete(key, new RclAction(0, Seq(key)));
                return CancelAndRedispatch(key);

            case State.Gto:
                if (digit is int gd) { _firstDigit = gd; _state = State.GtoDigit1; _sequence.Add(key); return null; }
                if (key == KeyId.Point) { _state = State.GtoDot; _sequence.Add(key); return null; }
                return CancelAndRedispatch(key);

            case State.GtoDigit1:
                if (digit is int gd2) return Complete(key, new GtoAction(_firstDigit * 10 + gd2, Seq(key)));
                return CancelAndRedispatch(key);

            case State.GtoDot:
                if (digit is int gdd) { _firstDigit = gdd; _state = State.GtoDotDigit1; _sequence.Add(key); return null; }
                return CancelAndRedispatch(key);

            case State.GtoDotDigit1:
                if (digit is int gdd2) return Complete(key, new GtoDotAction(_firstDigit * 10 + gdd2, Seq(key)));
                return CancelAndRedispatch(key);

            default:
                return CancelAndRedispatch(key);
        }
    }

    private ResolvedAction? AcceptFromIdle(KeyId key, int? digit)
    {
        _ = digit;
        var def = KeyboardMap.Get(key);
        switch (def.Primary)
        {
            case Fn.PrefixF: _state = State.F; _sequence.Clear(); _sequence.Add(key); return null;
            case Fn.PrefixG: _state = State.G; _sequence.Clear(); _sequence.Add(key); return null;
            case Fn.Sto: _state = State.Sto; _sequence.Clear(); _sequence.Add(key); return null;
            case Fn.Rcl: _state = State.Rcl; _sequence.Clear(); _sequence.Add(key); return null;
            default:
                return new FnAction(def.Primary, new[] { key });
        }
    }

    private ResolvedAction? AcceptShifted(KeyId key, int? digit, bool isF)
    {
        var def = KeyboardMap.Get(key);
        // Pressing the other shift key just switches the pending prefix.
        if (def.Primary == Fn.PrefixF) { _state = State.F; _sequence.Clear(); _sequence.Add(key); return null; }
        if (def.Primary == Fn.PrefixG) { _state = State.G; _sequence.Clear(); _sequence.Add(key); return null; }

        if (isF)
        {
            if (digit is int d) return Complete(key, new FixAction(d, Seq(key)));
            if (def.FShifted is Fn fShifted)
            {
                if (fShifted == Fn.Sto || fShifted == Fn.Rcl) return CancelAndRedispatch(key);
                return Complete(key, new FnAction(fShifted, Seq(key)));
            }
            return CancelAndRedispatch(key); // no gold meaning: prefix dropped, key acts alone
        }

        if (def.GShifted is Fn gShifted)
        {
            if (gShifted == Fn.Gto)
            {
                _state = State.Gto;
                _sequence.Add(key);
                return null;
            }
            return Complete(key, new FnAction(gShifted, Seq(key)));
        }
        return CancelAndRedispatch(key);
    }

    private ResolvedAction Complete(KeyId key, ResolvedAction action)
    {
        _state = State.None;
        _sequence.Clear();
        return action;
    }

    private IReadOnlyList<KeyId> Seq(KeyId last)
    {
        var seq = new List<KeyId>(_sequence) { last };
        return seq;
    }

    private ResolvedAction? CancelAndRedispatch(KeyId key)
    {
        _state = State.None;
        _sequence.Clear();
        return Accept(key);
    }
}
