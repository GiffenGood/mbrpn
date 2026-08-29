namespace MBRPN.Maui.Theme;

/// <summary>
/// The MB-RPN color palette, a 1:1 port of the :root custom properties in the web app's
/// mbrpn.css. Single source of truth for XAML (via x:Static) and drawing code.
/// </summary>
public static class Palette
{
    public static readonly Color BgDeep = Color.FromArgb("#0c0f13");
    public static readonly Color Case = Color.FromArgb("#23272e");
    public static readonly Color CaseEdge = Color.FromArgb("#2f353d");
    public static readonly Color Face = Color.FromArgb("#1d2127");
    public static readonly Color Panel = Color.FromArgb("#1a1e24");
    public static readonly Color PanelBorder = Color.FromArgb("#333a43");
    public static readonly Color KeyHi = Color.FromArgb("#2c323a");
    public static readonly Color KeyLo = Color.FromArgb("#252a31");
    public static readonly Color KeyBorder = Color.FromArgb("#3a4149");
    public static readonly Color KeyText = Color.FromArgb("#e8ecf1");
    public static readonly Color Teal = Color.FromArgb("#4fd1c5");
    public static readonly Color TealDim = Color.FromArgb("#37978e");
    public static readonly Color Violet = Color.FromArgb("#b18cff");
    public static readonly Color Glow = Color.FromArgb("#3ef0b8");
    public static readonly Color LcdBg = Color.FromArgb("#0a0e0d");
    public static readonly Color Muted = Color.FromArgb("#7d848c");

    // Header band / bezel chrome
    public static readonly Color HeaderBand = Color.FromArgb("#14171c");
    public static readonly Color LcdBezelBorder = Color.FromArgb("#262d29");
    public static readonly Color WordmarkMb = Color.FromArgb("#eef2f6");
    public static readonly Color WordmarkDim = Color.FromArgb("#566068");
    public static readonly Color FooterText = Color.FromArgb("#59626c");
    public static readonly Color FooterLine = Color.FromArgb("#3a424b");

    // f (gold→teal here) and g (violet) prefix keys
    public static readonly Color KeyFTop = Color.FromArgb("#3ec9ba");
    public static readonly Color KeyFBottom = Color.FromArgb("#2ea99c");
    public static readonly Color KeyFBorder = Color.FromArgb("#57e0d2");
    public static readonly Color KeyFText = Color.FromArgb("#0c1f1c");
    public static readonly Color KeyGTop = Color.FromArgb("#8f6cf0");
    public static readonly Color KeyGBottom = Color.FromArgb("#7452d8");
    public static readonly Color KeyGBorder = Color.FromArgb("#a68cf6");
    public static readonly Color KeyGText = Color.FromArgb("#16102b");

    // Pressed key cap (web .key:active gradient)
    public static readonly Color KeyPressedHi = Color.FromArgb("#232830");
    public static readonly Color KeyPressedLo = Color.FromArgb("#1e2229");

    /// <summary>Unlit "ghost" segment: rgba(120, 220, 190, 0.05).</summary>
    public static readonly Color SegGhost = Color.FromRgba(120, 220, 190, 13);

    // History panel
    public static readonly Color HistoryText = Color.FromArgb("#dfe5ec");
    public static readonly Color HistoryResult = Color.FromArgb("#6ee7d0");
}
