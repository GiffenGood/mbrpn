using MBRPN.Engine.Core;
using MBRPN.Engine.Dates;
using MBRPN.Engine.Display;
using MBRPN.Engine.Financial;
using MBRPN.Engine.Keys;

namespace MBRPN.Engine;

public sealed partial class CalculatorEngine
{
    private void ExecuteAction(ResolvedAction action)
    {
        switch (action)
        {
            case FixAction fix:
                CommitEntry();
                Formatter.FixDigits = fix.Digits;
                Formatter.Scientific = false;
                break;

            case StoAction sto:
                CommitEntry();
                if (sto.Op is Fn op) Registers.StoreArithmetic(sto.Register, op, Stack.X);
                else Registers[sto.Register] = Stack.X;
                Stack.LiftEnabled = true;
                break;

            case RclAction rcl:
                CommitEntry();
                PlaceNumber(Registers[rcl.Register]);
                break;

            case RclFinAction rclFin:
                CommitEntry();
                PlaceNumber(rclFin.Register switch
                {
                    Fn.NKey => Fin.N,
                    Fn.IKey => Fin.I,
                    Fn.PvKey => Fin.PV,
                    Fn.PmtKey => Fin.PMT,
                    _ => Fin.FV,
                });
                break;

            case RclCashFlowAction rclCf:
                CommitEntry();
                PlaceNumber(rclCf.Amount ? Cash.RecallCurrentAmount() : Cash.RecallCurrentRepeat());
                break;

            case GtoAction gto:
                CommitEntry();
                Program.GoTo(gto.Line <= Program.Count ? gto.Line : throw new CalcException(CalcError.Memory));
                break;

            case GtoDotAction gtoDot:
                Program.GoTo(gtoDot.Line <= Program.Count ? gtoDot.Line : throw new CalcException(CalcError.Memory));
                break;

            case ToggleOddPeriodAction:
                CommitEntry();
                Fin.OddPeriodCompound = !Fin.OddPeriodCompound;
                break;

            case FnAction fn:
                ExecuteFn(fn.Fn);
                break;
        }
    }

    private void ExecuteFn(Fn fn)
    {
        switch (fn)
        {
            // ---- Entry ----
            case >= Fn.Digit0 and <= Fn.Digit9:
                BeginEntryIfNeeded();
                Entry.AddDigit(fn - Fn.Digit0);
                Stack.X = Entry.ToReal();
                break;
            case Fn.Point:
                BeginEntryIfNeeded();
                Entry.AddPoint();
                Stack.X = Entry.ToReal();
                break;
            case Fn.Eex:
                BeginEntryIfNeeded();
                Entry.AddEex();
                Stack.X = Entry.ToReal();
                break;
            case Fn.Chs:
                if (Entry.IsEntering)
                {
                    Entry.ChangeSign();
                    Stack.X = Entry.ToReal();
                }
                else
                {
                    Stack.X = -Stack.X;
                    _numberPending = true;
                }
                break;
            case Fn.Enter:
                CommitEntry();
                Stack.Lift();
                Stack.LiftEnabled = false;
                break;
            case Fn.CLx:
                AbortEntry();
                Stack.X = Real.Zero;
                Stack.LiftEnabled = false;
                _numberPending = true;
                break;

            // ---- Stack ----
            case Fn.RollDown:
                CommitEntry();
                Stack.RollDown();
                _numberPending = true;
                break;
            case Fn.XExchangeY:
                CommitEntry();
                Stack.SwapXY();
                _numberPending = true;
                break;
            case Fn.LastX:
                CommitEntry();
                PlaceNumber(Stack.LastX);
                break;

            // ---- Arithmetic ----
            case Fn.Add: Binary(static (y, x) => y + x); break;
            case Fn.Subtract: Binary(static (y, x) => y - x); break;
            case Fn.Multiply: Binary(static (y, x) => y * x); break;
            case Fn.Divide: Binary(static (y, x) => y / x); break;
            case Fn.YtoX: Binary(MathX.Pow); break;
            case Fn.Reciprocal: Unary(static x => Real.One / x); break;
            case Fn.Sqrt: Unary(MathX.Sqrt); break;
            case Fn.Exp: Unary(MathX.Exp); break;
            case Fn.Ln: Unary(MathX.Ln); break;
            case Fn.Factorial: Unary(MathX.Factorial); break;
            case Fn.Frac: Unary(static x => x.FractionalPart()); break;
            case Fn.Intg: Unary(static x => x.IntegerPart()); break;
            case Fn.Round: Unary(Formatter.RoundToDisplay); break;

            // ---- Percentages (Y is preserved) ----
            case Fn.Percent:
                PercentFamily(static (y, x) => y * x / Real.FromLong(100));
                break;
            case Fn.DeltaPercent:
                PercentFamily(static (y, x) => y.IsZero
                    ? throw new CalcException(CalcError.Math)
                    : (x - y) / y * Real.FromLong(100));
                break;
            case Fn.PercentT:
                PercentFamily(static (y, x) => y.IsZero
                    ? throw new CalcException(CalcError.Math)
                    : x / y * Real.FromLong(100));
                break;

            // ---- Display modes ----
            case Fn.Scientific:
                CommitEntry();
                Formatter.Scientific = true;
                break;
            case Fn.Mantissa: // (unused: f ENTER resolves to ClearPrefix)
            case Fn.ClearPrefix:
                CommitEntry();
                _transient = MantissaText();
                break;

            // ---- Financial: the five TVM keys ----
            case Fn.NKey: TvmKey(fn); break;
            case Fn.IKey: TvmKey(fn); break;
            case Fn.PvKey: TvmKey(fn); break;
            case Fn.PmtKey: TvmKey(fn); break;
            case Fn.FvKey: TvmKey(fn); break;

            case Fn.Times12:
                CommitEntry();
                Stack.X *= Real.FromLong(12);
                Fin.N = Stack.X;
                FinishFinancial();
                break;
            case Fn.Divide12:
                CommitEntry();
                Stack.X /= Real.FromLong(12);
                Fin.I = Stack.X;
                FinishFinancial();
                break;

            case Fn.BeginMode: CommitEntry(); Fin.BeginMode = true; break;
            case Fn.EndMode: CommitEntry(); Fin.BeginMode = false; break;

            case Fn.Amort:
            {
                CommitEntry();
                var result = Amortization.Run(Fin, Stack.X, Formatter.FixDigits);
                Stack.LastX = Stack.X;
                Fin.PV = result.NewBalance;
                Fin.N = result.NewN;
                Stack.Z = Stack.X;
                Stack.Y = result.TotalPrincipal;
                Stack.X = result.TotalInterest;
                FinishFinancial();
                break;
            }

            case Fn.SimpleInterest:
            {
                CommitEntry();
                var result = Financial.SimpleInterest.Run(Fin);
                Stack.LastX = Stack.X;
                Stack.Z = result.Interest365;
                Stack.Y = result.Principal;
                Stack.X = result.Interest360;
                FinishFinancial();
                break;
            }

            case Fn.CashFlow0:
                CommitEntry();
                Cash.StoreInitial(Stack.X);
                FinishFinancial();
                break;
            case Fn.CashFlowJ:
                CommitEntry();
                Cash.StoreNext(Stack.X);
                FinishFinancial();
                break;
            case Fn.CashFlowNj:
                CommitEntry();
                Cash.StoreRepeat(Stack.X);
                FinishFinancial();
                break;
            case Fn.Npv:
                CommitEntry();
                Fin.PV = Cash.Npv();
                Stack.X = Fin.PV;
                FinishFinancial();
                break;
            case Fn.Irr:
                CommitEntry();
                Fin.I = Cash.Irr();
                Stack.X = Fin.I;
                FinishFinancial();
                break;

            case Fn.Price:
            {
                CommitEntry();
                var (price, accrued) = Bonds.Price(Fin.I, Fin.PMT, Stack.Y, Stack.X, _dmyMode);
                Stack.LastX = Stack.X;
                Fin.PV = price;
                Stack.Y = accrued;
                Stack.X = price;
                FinishFinancial();
                break;
            }
            case Fn.Ytm:
            {
                CommitEntry();
                Real ytm = Bonds.Ytm(Fin.PV, Fin.PMT, Stack.Y, Stack.X, _dmyMode);
                Stack.LastX = Stack.X;
                Fin.I = ytm;
                Stack.X = ytm;
                FinishFinancial();
                break;
            }

            case Fn.DepreciationSl: Depreciate(Depreciation.StraightLine); break;
            case Fn.DepreciationSoyd: Depreciate(Depreciation.SumOfYearsDigits); break;
            case Fn.DepreciationDb: Depreciate(Depreciation.DecliningBalance); break;

            case Fn.ClearFin:
                CommitEntry();
                Fin.Clear();
                break;

            // ---- Dates ----
            case Fn.SetDmy: CommitEntry(); _dmyMode = true; break;
            case Fn.SetMdy: CommitEntry(); _dmyMode = false; break;
            case Fn.Date:
            {
                CommitEntry();
                DateOnly result = DateMath.AddDays(Stack.Y, Stack.X, _dmyMode);
                Stack.LastX = Stack.X;
                Stack.Drop();
                Stack.X = DateMath.ToReal(result, _dmyMode);
                Stack.LiftEnabled = true;
                _numberPending = true;
                _transient = DisplayFormatter.FormatDate(result.Month, result.Day, result.Year,
                    DateMath.Weekday(result), _dmyMode);
                break;
            }
            case Fn.DeltaDays:
            {
                CommitEntry();
                var (actual, basis360) = DateMath.DaysBetween(Stack.Y, Stack.X, _dmyMode);
                Stack.LastX = Stack.X;
                Stack.Y = basis360;
                Stack.X = actual;
                Stack.LiftEnabled = true;
                _numberPending = true;
                break;
            }

            // ---- Statistics ----
            case Fn.SigmaPlus:
            case Fn.SigmaMinus:
                CommitEntry();
                Stack.LastX = Stack.X;
                Stack.X = Stats.Accumulate(Stack.X, Stack.Y, fn == Fn.SigmaMinus);
                Stack.LiftEnabled = false;
                _numberPending = true;
                break;
            case Fn.Mean:
            {
                CommitEntry();
                var (mx, my) = Stats.Mean();
                PlacePair(mx, my);
                break;
            }
            case Fn.StdDev:
            {
                CommitEntry();
                var (sx, sy) = Stats.SampleStdDev();
                PlacePair(sx, sy);
                break;
            }
            case Fn.WeightedMean:
                CommitEntry();
                PlaceNumber(Stats.WeightedMean());
                break;
            case Fn.EstimateY:
            {
                CommitEntry();
                var (estimate, r) = Stats.EstimateY(Stack.X);
                Stack.LastX = Stack.X;
                ReplaceXWithPair(estimate, r);
                break;
            }
            case Fn.EstimateX:
            {
                CommitEntry();
                var (estimate, r) = Stats.EstimateX(Stack.X);
                Stack.LastX = Stack.X;
                ReplaceXWithPair(estimate, r);
                break;
            }
            case Fn.ClearSigma:
                AbortEntry();
                Stats.Clear();
                Stack.Clear();
                Stack.LiftEnabled = false;
                break;

            // ---- Clearing ----
            case Fn.ClearReg:
                AbortEntry();
                Registers.ClearAll();
                Fin.Clear();
                Stack.Clear();
                Stack.LastX = Real.Zero;
                Stack.LiftEnabled = false;
                break;
            case Fn.ClearPrgm:
                if (_prgmMode) Program.Clear();
                else Program.Pointer = 0;
                break;

            // ---- Programming ----
            case Fn.ProgramRun:
                AbortEntry();
                _prgmMode = !_prgmMode;
                Program.Pointer = 0;
                break;
            case Fn.Mem:
                CommitEntry();
                _transient = Program.MemText();
                break;
            case Fn.Sst:
                if (_prgmMode)
                {
                    Program.Pointer = Program.Pointer >= Program.Count ? 0 : Program.Pointer + 1;
                }
                else
                {
                    CommitEntry();
                    SingleStep();
                }
                break;
            case Fn.Bst:
                if (!_prgmMode) CommitEntry();
                Program.Pointer = Program.Pointer <= 0 ? Program.Count : Program.Pointer - 1;
                break;
            case Fn.RunStop:
                CommitEntry();
                if (Program.Count > 0) IsRunning = true;
                break;
            case Fn.Pse:
            case Fn.XLessOrEqualY:
            case Fn.XEqualZero:
                CommitEntry(); // manual presses of program-only keys have no other effect
                break;

            case Fn.OnOff:
                _off = true;
                break;

            case Fn.PrefixF:
            case Fn.PrefixG:
            case Fn.Sto:
            case Fn.Rcl:
            case Fn.Gto:
                break; // consumed by the prefix machine; never reach execution
        }
    }

    // ---- Helpers -----------------------------------------------------------------

    private void Binary(Func<Real, Real, Real> f)
    {
        CommitEntry();
        Stack.LastX = Stack.X;
        Real result = f(Stack.Y, Stack.X);
        Stack.Drop();
        Stack.X = result;
        Stack.LiftEnabled = true;
        _numberPending = true;
    }

    private void Unary(Func<Real, Real> f)
    {
        CommitEntry();
        Stack.LastX = Stack.X;
        Stack.X = f(Stack.X);
        Stack.LiftEnabled = true;
        _numberPending = true;
    }

    private void PercentFamily(Func<Real, Real, Real> f)
    {
        CommitEntry();
        Stack.LastX = Stack.X;
        Stack.X = f(Stack.Y, Stack.X);
        Stack.LiftEnabled = true;
        _numberPending = true;
    }

    /// <summary>Puts a recalled/derived number in X, honoring stack lift.</summary>
    private void PlaceNumber(Real value)
    {
        Stack.Place(value);
        Stack.LiftEnabled = true;
        _numberPending = true;
    }

    /// <summary>x̄ / s pattern: X gets the x-statistic, Y the y-statistic, old X saved in Z.</summary>
    private void PlacePair(Real xValue, Real yValue)
    {
        Stack.Place(yValue);
        Stack.Lift();
        Stack.X = xValue;
        Stack.LiftEnabled = true;
        _numberPending = true;
    }

    /// <summary>ŷ,r pattern: X (the input) is replaced by the estimate, r goes into Y.</summary>
    private void ReplaceXWithPair(Real xValue, Real yValue)
    {
        Stack.T = Stack.Z;
        Stack.Z = Stack.Y;
        Stack.Y = yValue;
        Stack.X = xValue;
        Stack.LiftEnabled = true;
        _numberPending = true;
    }

    private void TvmKey(Fn fn)
    {
        CommitEntry();
        if (_numberPending)
        {
            switch (fn)
            {
                case Fn.NKey: Fin.N = Stack.X; break;
                case Fn.IKey: Fin.I = Stack.X; break;
                case Fn.PvKey: Fin.PV = Stack.X; break;
                case Fn.PmtKey: Fin.PMT = Stack.X; break;
                case Fn.FvKey: Fin.FV = Stack.X; break;
            }
            FinishFinancial();
            return;
        }
        Real result = fn switch
        {
            Fn.NKey => TvmSolver.SolveN(Fin),
            Fn.IKey => TvmSolver.SolveI(Fin),
            Fn.PvKey => TvmSolver.SolvePv(Fin),
            Fn.PmtKey => TvmSolver.SolvePmt(Fin),
            _ => TvmSolver.SolveFv(Fin),
        };
        switch (fn)
        {
            case Fn.NKey: Fin.N = result; break;
            case Fn.IKey: Fin.I = result; break;
            case Fn.PvKey: Fin.PV = result; break;
            case Fn.PmtKey: Fin.PMT = result; break;
            default: Fin.FV = result; break;
        }
        Stack.X = result;
        FinishFinancial();
    }

    private void Depreciate(Func<FinancialRegisters, Real, Depreciation.Result> method)
    {
        CommitEntry();
        var result = method(Fin, Stack.X);
        Stack.LastX = Stack.X;
        Stack.Y = result.Remaining;
        Stack.X = result.Amount;
        FinishFinancial();
    }

    /// <summary>Common tail for financial operations: lift re-enabled, no number pending.</summary>
    private void FinishFinancial()
    {
        Stack.LiftEnabled = true;
        _numberPending = false;
    }
}
