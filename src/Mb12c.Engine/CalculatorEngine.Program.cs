using Mb12c.Engine.Core;
using Mb12c.Engine.Keys;
using Mb12c.Engine.Programming;

namespace Mb12c.Engine;

/// <summary>Outcome of one program step.</summary>
public enum ProgramStepResult
{
    Running,
    /// <summary>A PSE instruction: show X for about a second before the next step.</summary>
    Paused,
    Halted,
    Errored,
}

public sealed partial class CalculatorEngine
{
    private bool _showXWhilePaused;

    /// <summary>
    /// Executes the next program line. The UI calls this in a loop while
    /// <see cref="IsRunning"/>, delaying after a <see cref="ProgramStepResult.Paused"/>.
    /// </summary>
    public ProgramStepResult StepProgram()
    {
        if (!IsRunning) return ProgramStepResult.Halted;
        _showXWhilePaused = false;
        try
        {
            int next = Program.Pointer + 1;
            if (next > Program.Count)
            {
                Program.Pointer = 0;
                IsRunning = false;
                Changed?.Invoke();
                return ProgramStepResult.Halted;
            }
            Program.Pointer = next;
            var result = ExecuteInstruction(Program.Line(next));
            if (result is ProgramStepResult.Halted) IsRunning = false;
            if (result is ProgramStepResult.Paused) _showXWhilePaused = true;
            Changed?.Invoke();
            return result;
        }
        catch (CalcException ex)
        {
            AbortEntry();
            _error = ex.Error;
            IsRunning = false;
            Changed?.Invoke();
            return ProgramStepResult.Errored;
        }
    }

    private ProgramStepResult ExecuteInstruction(Instruction instruction)
    {
        var resolver = new PrefixStateMachine();
        ResolvedAction? action = null;
        foreach (KeyId key in instruction.Keys) action = resolver.Accept(key);
        if (action is null) return ProgramStepResult.Running; // malformed line: ignore

        switch (action)
        {
            case FnAction { Fn: Fn.RunStop }:
                CommitEntry();
                return ProgramStepResult.Halted;

            case FnAction { Fn: Fn.Pse }:
                CommitEntry();
                return ProgramStepResult.Paused;

            case FnAction { Fn: Fn.XLessOrEqualY }:
                CommitEntry();
                if (!(Stack.X <= Stack.Y)) SkipNextLine();
                return ProgramStepResult.Running;

            case FnAction { Fn: Fn.XEqualZero }:
                CommitEntry();
                if (!Stack.X.IsZero) SkipNextLine();
                return ProgramStepResult.Running;

            case GtoAction gto:
                if (gto.Line > Program.Count) throw new CalcException(CalcError.Memory);
                Program.Pointer = Math.Max(0, gto.Line - 1); // next step executes that line
                return ProgramStepResult.Running;

            default:
                ExecuteAction(action);
                return ProgramStepResult.Running;
        }
    }

    private void SkipNextLine() => Program.Pointer++;

    /// <summary>SST in run mode: executes a single program line.</summary>
    private void SingleStep()
    {
        if (Program.Count == 0) return;
        IsRunning = true;
        StepProgram();
        IsRunning = false;
    }

    /// <summary>
    /// Runs the program to completion without pause delays (used by tests and headless
    /// callers). Stops after <paramref name="maxSteps"/> steps as a runaway guard.
    /// </summary>
    public ProgramStepResult RunToHalt(int maxSteps = 2_000_000)
    {
        var result = ProgramStepResult.Running;
        for (int i = 0; i < maxSteps && IsRunning; i++)
        {
            result = StepProgram();
            if (result is ProgramStepResult.Halted or ProgramStepResult.Errored) return result;
        }
        if (IsRunning)
        {
            IsRunning = false;
            return ProgramStepResult.Halted;
        }
        return result;
    }
}
