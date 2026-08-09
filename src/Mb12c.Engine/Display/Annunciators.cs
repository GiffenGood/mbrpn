namespace Mb12c.Engine.Display;

/// <summary>Status annunciators shown along the bottom of the LCD.</summary>
[Flags]
public enum Annunciators
{
    None = 0,
    F = 1 << 0,
    G = 1 << 1,
    Begin = 1 << 2,
    Dmy = 1 << 3,
    C = 1 << 4,
    Prgm = 1 << 5,
}
