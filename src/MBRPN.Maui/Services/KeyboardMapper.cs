using MBRPN.Engine.Keys;

namespace MBRPN.Maui.Services;

/// <summary>Maps keyboard keys to calculator keys. Same table as the web app's
/// KeyboardMapper so the README shortcut list applies to both.</summary>
public static class KeyboardMapper
{
    public static KeyId? Map(string key) => key switch
    {
        "0" => KeyId.D0, "1" => KeyId.D1, "2" => KeyId.D2, "3" => KeyId.D3, "4" => KeyId.D4,
        "5" => KeyId.D5, "6" => KeyId.D6, "7" => KeyId.D7, "8" => KeyId.D8, "9" => KeyId.D9,
        "." or "," => KeyId.Point,
        "Enter" => KeyId.Enter,
        "+" => KeyId.Add,
        "-" => KeyId.Subtract,
        "*" => KeyId.Multiply,
        "/" => KeyId.Divide,
        "%" => KeyId.Percent,
        "Backspace" or "Delete" => KeyId.CLx,
        "Escape" => KeyId.On,
        " " => KeyId.RunStop,
        "ArrowDown" => KeyId.SST,
        "ArrowUp" => KeyId.SST,          // use g-BST on screen for stepping backwards
        "f" or "F" => KeyId.F,
        "g" or "G" => KeyId.G,
        "s" or "S" => KeyId.STO,
        "r" or "R" => KeyId.RCL,
        "n" or "N" => KeyId.N,
        "i" or "I" => KeyId.I,
        "p" or "P" => KeyId.PV,
        "m" or "M" => KeyId.PMT,
        "v" or "V" => KeyId.FV,
        "c" or "C" => KeyId.CHS,
        "e" or "E" => KeyId.EEX,
        "y" or "Y" => KeyId.YtoX,
        "x" or "X" => KeyId.XExchangeY,
        "d" or "D" => KeyId.RollDown,
        "q" or "Q" => KeyId.Reciprocal,
        "l" or "L" => KeyId.PercentT,
        _ => null,
    };
}
