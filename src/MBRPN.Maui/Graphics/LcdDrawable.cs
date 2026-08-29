using MBRPN.Engine.Display;
using MBRPN.Maui.Theme;

namespace MBRPN.Maui.Graphics;

/// <summary>
/// Draws the whole LCD — eleven seven-segment digit cells plus the annunciator row —
/// on one canvas. Geometry and the char→segments map are ported verbatim from the web
/// app's SevenSegDigit.razor (58×92 local space per digit, scaled into a 34×52 cell).
/// </summary>
public sealed class LcdDrawable : IDrawable
{
    public DisplayState State { get; set; } = DisplayState.Blank();

    // Cell metrics matching mbrpn.css: 34×52 digits with a 4px gap, 8px top / 14px left padding.
    private const float CellW = 34f, CellH = 52f, CellGap = 4f;
    private const float PadX = 14f, PadY = 8f;
    private const float LocalW = 58f, LocalH = 92f;

    private static readonly (char Id, PointF[] Points)[] Segments =
    [
        ('a', [new(10, 4), new(48, 4), new(42, 12), new(16, 12)]),
        ('b', [new(50, 6), new(50, 42), new(43, 38), new(43, 14)]),
        ('c', [new(50, 50), new(50, 86), new(43, 78), new(43, 54)]),
        ('d', [new(10, 88), new(48, 88), new(42, 80), new(16, 80)]),
        ('e', [new(8, 50), new(8, 86), new(15, 78), new(15, 54)]),
        ('f', [new(8, 6), new(8, 42), new(15, 38), new(15, 14)]),
        ('g', [new(12, 46), new(18, 42), new(40, 42), new(46, 46), new(40, 50), new(18, 50)]),
    ];

    private static readonly PointF[] CommaPoints =
        [new(52, 90), new(58, 90), new(54, 100), new(50, 100)];

    private static readonly Dictionary<char, string> Map = new()
    {
        ['0'] = "abcdef", ['1'] = "bc", ['2'] = "abged", ['3'] = "abgcd", ['4'] = "fgbc",
        ['5'] = "afgcd", ['6'] = "afgedc", ['7'] = "abc", ['8'] = "abcdefg", ['9'] = "abcdfg",
        ['-'] = "g", ['E'] = "afged", ['r'] = "eg", ['o'] = "cdeg", ['n'] = "ceg",
        ['u'] = "cde", ['i'] = "c", ['g'] = "abcdfg", ['P'] = "abefg", ['A'] = "abcefg",
        ['C'] = "adef", ['d'] = "bcdeg", ['L'] = "def", ['S'] = "afgcd", ['t'] = "defg",
        [' '] = "",
    };

    private static readonly (string Text, float Width)[] AnnLabels =
        [("f", 6), ("g", 6), ("BEGIN", 34), ("D.MY", 28), ("C", 8), ("PRGM", 32)];

    private const float AnnGap = 26f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var state = State;
        if (state.IsOff) return; // .lcd-off: segments fully transparent

        for (int i = 0; i < state.Cells.Count; i++)
            DrawDigit(canvas, state.Cells[i], PadX + i * (CellW + CellGap), PadY);

        DrawAnnunciators(canvas, state.Annunciators, dirtyRect);
    }

    private static void DrawDigit(ICanvas canvas, DigitCell cell, float x, float y)
    {
        canvas.SaveState();
        canvas.Translate(x, y);
        canvas.Scale(CellW / LocalW, CellH / LocalH);

        // Ghost pass: every segment faintly visible, like the web's unlit .seg fill.
        canvas.FillColor = Palette.SegGhost;
        foreach (var (_, points) in Segments)
            canvas.FillPath(ToPath(points));
        canvas.FillCircle(55, 86, 3.4f);
        canvas.FillPath(ToPath(CommaPoints));

        // Lit pass with the glow halo (drop-shadow in the web version).
        canvas.SetShadow(new SizeF(0, 0), 3, Palette.Glow.WithAlpha(0.55f));
        canvas.FillColor = Palette.Glow;
        string lit = Map.TryGetValue(cell.Char, out var segs) ? segs : "";
        foreach (var (id, points) in Segments)
        {
            if (lit.Contains(id))
                canvas.FillPath(ToPath(points));
        }
        if (cell.Point) canvas.FillCircle(55, 86, 3.4f);
        if (cell.Comma) canvas.FillPath(ToPath(CommaPoints));

        canvas.RestoreState();
    }

    private static PathF ToPath(PointF[] points)
    {
        var path = new PathF();
        path.MoveTo(points[0]);
        for (int i = 1; i < points.Length; i++)
            path.LineTo(points[i]);
        path.Close();
        return path;
    }

    private static void DrawAnnunciators(ICanvas canvas, Annunciators ann, RectF rect)
    {
        Annunciators[] flags =
            [Annunciators.F, Annunciators.G, Annunciators.Begin,
             Annunciators.Dmy, Annunciators.C, Annunciators.Prgm];

        float total = AnnLabels.Sum(l => l.Width) + AnnGap * (AnnLabels.Length - 1);
        float x = Math.Max(PadX, (rect.Width - total) / 2f);
        float y = PadY + CellH + 1f;

        canvas.FontColor = Palette.Glow.WithAlpha(0.9f);
        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
        canvas.FontSize = 8.5f;

        for (int i = 0; i < AnnLabels.Length; i++)
        {
            if (ann.HasFlag(flags[i]))
                canvas.DrawString(AnnLabels[i].Text, x, y, AnnLabels[i].Width, 12,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            x += AnnLabels[i].Width + AnnGap;
        }
    }
}
