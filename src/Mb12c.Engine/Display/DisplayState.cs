namespace Mb12c.Engine.Display;

/// <summary>One LCD digit position: the character shown plus trailing point/comma segments.</summary>
public readonly record struct DigitCell(char Char, bool Point, bool Comma);

/// <summary>
/// Everything the LCD shows: a plain-text rendering (used by tests), the eleven digit
/// cells (sign + ten digits) for the seven-segment UI, and the annunciator flags.
/// </summary>
public sealed record DisplayState(
    string Text,
    IReadOnlyList<DigitCell> Cells,
    Annunciators Annunciators,
    bool IsOff = false)
{
    public const int CellCount = 11;

    /// <summary>Builds the cell row from display text. Points and commas attach to the
    /// preceding digit cell; the first cell is reserved for the sign when the text
    /// starts with '-'.</summary>
    public static DisplayState FromText(string text, Annunciators annunciators, bool off = false)
    {
        var cells = new DigitCell[CellCount];
        for (int i = 0; i < CellCount; i++) cells[i] = new DigitCell(' ', false, false);

        int cell = 0;
        bool first = true;
        foreach (char ch in text)
        {
            if (ch is '.' or ',')
            {
                if (cell > 0)
                {
                    var prev = cells[cell - 1];
                    cells[cell - 1] = ch == '.' ? prev with { Point = true } : prev with { Comma = true };
                }
                continue;
            }
            if (first && ch != '-')
            {
                cell = 1; // leave the sign cell blank for positive numbers
            }
            first = false;
            if (cell >= CellCount) break;
            cells[cell++] = new DigitCell(ch, false, false);
        }
        return new DisplayState(text, cells, annunciators, off);
    }

    public static DisplayState Blank(Annunciators annunciators = Annunciators.None) =>
        FromText(string.Empty, annunciators, off: true);
}
