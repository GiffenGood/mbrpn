using System.Globalization;
using Mb12c.Engine.Core;

namespace Mb12c.Engine.Programming;

/// <summary>
/// Program memory: 8 lines to start, growing to at most 99 by converting storage
/// registers (R.9 downward, 7 lines per register — the real machine's memory map).
/// Line numbers are 1-based; line 00 is the empty "home" position. Recording writes to
/// the line after the pointer, overwriting whatever was there (the HP-12C has no insert).
/// </summary>
public sealed class ProgramMemory(RegisterFile registers)
{
    public const int BaseLines = 8;
    public const int MaxLines = 99;
    public const int LinesPerRegister = 7;

    private readonly List<Instruction> _lines = new();

    /// <summary>Current program pointer: 0 = line 00, otherwise a 1-based line number.</summary>
    public int Pointer { get; set; }

    public int Count => _lines.Count;

    public int Capacity => BaseLines + registers.ConvertedToProgram * LinesPerRegister;

    public Instruction Line(int number)
    {
        if (number < 1 || number > _lines.Count) throw new CalcException(CalcError.Memory);
        return _lines[number - 1];
    }

    /// <summary>Records an instruction at the line after the pointer and advances.</summary>
    public void Record(Instruction instruction)
    {
        int line = Pointer + 1;
        if (line > MaxLines) throw new CalcException(CalcError.Memory);
        if (line > _lines.Count)
        {
            while (line > Capacity)
            {
                if (registers.ConvertedToProgram >= 13 || Capacity >= MaxLines)
                    throw new CalcException(CalcError.Memory);
                registers.ConvertedToProgram++;
            }
            _lines.Add(instruction);
        }
        else
        {
            _lines[line - 1] = instruction;
        }
        Pointer = line;
    }

    public void Clear()
    {
        _lines.Clear();
        Pointer = 0;
        registers.ConvertedToProgram = 0;
    }

    public void GoTo(int line)
    {
        if (line < 0 || line > _lines.Count) throw new CalcException(CalcError.Memory);
        Pointer = line;
    }

    /// <summary>g MEM text: program lines allocated and data registers still available.</summary>
    public string MemText() => string.Create(CultureInfo.InvariantCulture,
        $"P-{Capacity:00} r-{registers.AvailableCount:00}");

    /// <summary>PRGM-mode display for the line at the pointer.</summary>
    public string DisplayText()
    {
        if (Pointer == 0) return "00-";
        return string.Create(CultureInfo.InvariantCulture,
            $"{Pointer:00}-{Line(Pointer).KeycodeText(),8}");
    }

    public IReadOnlyList<Instruction> Lines => _lines;

    public void Restore(IEnumerable<Instruction> lines, int converted)
    {
        _lines.Clear();
        _lines.AddRange(lines);
        registers.ConvertedToProgram = converted;
        Pointer = 0;
    }
}
