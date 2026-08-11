using System.Text;
using MBRPN.Engine.Core;
using MBRPN.Engine.Keys;

namespace MBRPN.Engine.History;

/// <summary>
/// Renders a resolved action's key sequence as the text a handbook would print:
/// "f AMORT", "g DATE", "STO + 4", "RCL .5", "FIX 4". Labels come from
/// <see cref="KeyboardMap"/> legends — never from parsing the display.
/// </summary>
public static class HistoryLabeler
{
    public static string Label(ResolvedAction action)
    {
        if (action is FixAction fix) return $"FIX {fix.Digits}";
        return Label(action.Sequence);
    }

    public static string Label(IReadOnlyList<KeyId> keys)
    {
        var sb = new StringBuilder();
        int i = 0;
        while (i < keys.Count)
        {
            if (sb.Length > 0) sb.Append(' ');
            KeyId key = keys[i];
            var def = KeyboardMap.Get(key);

            if (key == KeyId.F && i + 1 < keys.Count)
            {
                var shifted = KeyboardMap.Get(keys[i + 1]);
                sb.Append("f ").Append(shifted.FLabel ?? shifted.PrimaryLabel);
                i += 2;
                continue;
            }
            if (key == KeyId.G && i + 1 < keys.Count)
            {
                var shifted = KeyboardMap.Get(keys[i + 1]);
                sb.Append("g ").Append(shifted.GLabel ?? shifted.PrimaryLabel);
                i += 2;
                continue;
            }

            // Consecutive digits (optionally starting with the point) form one number
            // token: "12", ".5" — used by STO/RCL/GTO arguments.
            if (KeyboardMap.DigitOf(key) is not null || key == KeyId.Point)
            {
                while (i < keys.Count && (KeyboardMap.DigitOf(keys[i]) is not null || keys[i] == KeyId.Point))
                {
                    sb.Append(KeyboardMap.Get(keys[i]).PrimaryLabel);
                    i++;
                }
                continue;
            }

            sb.Append(def.PrimaryLabel);
            i++;
        }
        return sb.ToString();
    }
}
