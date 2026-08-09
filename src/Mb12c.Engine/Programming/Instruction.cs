using System.Text;
using Mb12c.Engine.Keys;

namespace Mb12c.Engine.Programming;

/// <summary>
/// One program line: the complete key sequence (1–3 keys) that was recorded. Execution
/// replays the sequence through the normal dispatch path; the keycode text shown in
/// PRGM mode derives from the same keys (an f/g prefix joins its key with a comma,
/// e.g. GTO 09 displays as "43,33 09").
/// </summary>
public sealed record Instruction(IReadOnlyList<KeyId> Keys)
{
    public string KeycodeText()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < Keys.Count; i++)
        {
            string code = KeyboardMap.Get(Keys[i]).Keycode;
            if (i > 0)
            {
                bool prevIsShift = Keys[i - 1] is KeyId.F or KeyId.G;
                // A branch target's two digits form one two-digit keycode (GTO 09 → "43,33 09").
                bool mergedDigits = KeyboardMap.DigitOf(Keys[i]) is not null
                    && KeyboardMap.DigitOf(Keys[i - 1]) is not null;
                if (mergedDigits) { sb.Append(code); continue; }
                sb.Append(prevIsShift ? ',' : ' ');
            }
            sb.Append(code);
        }
        return sb.ToString();
    }
}
