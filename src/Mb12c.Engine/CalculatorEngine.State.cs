using Mb12c.Engine.Keys;
using Mb12c.Engine.Programming;
using Mb12c.Engine.State;

namespace Mb12c.Engine;

public sealed partial class CalculatorEngine
{
    public CalculatorState Snapshot()
    {
        var state = new CalculatorState
        {
            X = StateSerializer.EncodeReal(Stack.X),
            Y = StateSerializer.EncodeReal(Stack.Y),
            Z = StateSerializer.EncodeReal(Stack.Z),
            T = StateSerializer.EncodeReal(Stack.T),
            LastX = StateSerializer.EncodeReal(Stack.LastX),
            Registers = Registers.Snapshot().Select(StateSerializer.EncodeReal).ToList(),
            FinN = StateSerializer.EncodeReal(Fin.N),
            FinI = StateSerializer.EncodeReal(Fin.I),
            FinPv = StateSerializer.EncodeReal(Fin.PV),
            FinPmt = StateSerializer.EncodeReal(Fin.PMT),
            FinFv = StateSerializer.EncodeReal(Fin.FV),
            BeginMode = Fin.BeginMode,
            OddPeriodCompound = Fin.OddPeriodCompound,
            CashFlowRepeats = Cash.RepeatCounts.ToList(),
            FixDigits = Formatter.FixDigits,
            Scientific = Formatter.Scientific,
            DmyMode = _dmyMode,
            ProgramMode = _prgmMode,
            ProgramLines = Program.Lines
                .Select(line => line.Keys.Select(k => k.ToString()).ToList())
                .ToList(),
            ConvertedRegisters = Registers.ConvertedToProgram,
            ProgramPointer = Program.Pointer,
        };
        return state;
    }

    public void Restore(CalculatorState state)
    {
        Stack.X = StateSerializer.DecodeReal(state.X);
        Stack.Y = StateSerializer.DecodeReal(state.Y);
        Stack.Z = StateSerializer.DecodeReal(state.Z);
        Stack.T = StateSerializer.DecodeReal(state.T);
        Stack.LastX = StateSerializer.DecodeReal(state.LastX);
        Stack.LiftEnabled = true;
        Registers.Restore(state.Registers.Select(StateSerializer.DecodeReal).ToList());
        Fin.N = StateSerializer.DecodeReal(state.FinN);
        Fin.I = StateSerializer.DecodeReal(state.FinI);
        Fin.PV = StateSerializer.DecodeReal(state.FinPv);
        Fin.PMT = StateSerializer.DecodeReal(state.FinPmt);
        Fin.FV = StateSerializer.DecodeReal(state.FinFv);
        Fin.BeginMode = state.BeginMode;
        Fin.OddPeriodCompound = state.OddPeriodCompound;
        for (int i = 0; i < Cash.RepeatCounts.Length && i < state.CashFlowRepeats.Count; i++)
            Cash.RepeatCounts[i] = state.CashFlowRepeats[i];
        Formatter.FixDigits = state.FixDigits;
        Formatter.Scientific = state.Scientific;
        _dmyMode = state.DmyMode;
        _prgmMode = state.ProgramMode;
        var lines = state.ProgramLines
            .Select(line => new Instruction(line
                .Select(StateSerializer.ParseKey)
                .Where(k => k is not null)
                .Select(k => k!.Value)
                .ToList()))
            .Where(instr => instr.Keys.Count > 0);
        Program.Restore(lines, state.ConvertedRegisters);
        Program.Pointer = Math.Min(state.ProgramPointer, Program.Count);
        Changed?.Invoke();
    }
}
