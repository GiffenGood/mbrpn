using MBRPN.Maui.Theme;

namespace MBRPN.Maui.Graphics;

/// <summary>
/// The CLEAR group rule drawn over the f-legends of SST…ENTER (row 3, columns 2–6):
/// a three-sided teal bracket, open at the bottom, with "CLEAR" breaking the top line.
/// Port of the web app's .clear-bracket.
/// </summary>
public sealed class ClearBracketDrawable : IDrawable
{
    public void Draw(ICanvas canvas, RectF rect)
    {
        const float top = 11f, radius = 5f, inset = 0.5f;
        float right = rect.Width - inset;
        float bottom = rect.Height;

        var path = new PathF();
        path.MoveTo(inset, bottom);
        path.LineTo(inset, top + radius);
        path.QuadTo(inset, top, inset + radius, top);
        path.LineTo(right - radius, top);
        path.QuadTo(right, top, right, top + radius);
        path.LineTo(right, bottom);

        canvas.StrokeColor = Palette.TealDim.WithAlpha(0.85f);
        canvas.StrokeSize = 1f;
        canvas.DrawPath(path);

        // "CLEAR" floats just above the top line, on a face-colored plate that
        // interrupts it (the web span's top: -7 / background: var(--face) effect).
        const string label = "C L E A R";
        const float plateW = 64f, plateH = 12f;
        float cx = rect.Width / 2f;
        canvas.FillColor = Palette.Face;
        canvas.FillRectangle(cx - plateW / 2f, 0, plateW, plateH);
        canvas.FontColor = Palette.Teal;
        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
        canvas.FontSize = 8f;
        canvas.DrawString(label, cx - plateW / 2f, 0, plateW, plateH,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
