namespace MBRPN.Engine.Keys;

/// <summary>
/// Definition of one physical key: position, program keycode, printed legends, and the
/// functions it performs unshifted / f-shifted (gold) / g-shifted (blue).
/// This single record drives UI rendering, program keycode display, and dispatch.
/// </summary>
/// <param name="Keycode">Program-memory keycode text. Digit keys use their digit value
/// ("1".."9", "0"); all other keys use row+column ("11".."45"), where column 10 is 0
/// (e.g. ÷ is "10", + is "40"). The decimal point key is "48".</param>
public sealed record KeyDef(
    KeyId Id,
    int Row,
    int Col,
    string Keycode,
    string PrimaryLabel,
    string? FLabel,
    string? GLabel,
    Fn Primary,
    Fn? FShifted,
    Fn? GShifted);
