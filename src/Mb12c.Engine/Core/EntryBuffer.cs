using System.Globalization;

namespace Mb12c.Engine.Core;

/// <summary>
/// Digit-by-digit number entry: up to 10 mantissa digits, an optional decimal point,
/// and an optional two-digit EEX exponent. CHS during entry negates the mantissa, or
/// the exponent once EEX has been pressed. Extra digits beyond capacity are ignored,
/// exactly as on the real keyboard.
/// </summary>
public sealed class EntryBuffer
{
    private readonly List<char> _digits = new(10);
    private int _pointAfter = -1;          // digit count before the decimal point; -1 = none
    private bool _negative;
    private bool _hasExponent;
    private int _exponent;                 // 0..99 magnitude, two digits shift-in
    private bool _exponentNegative;

    public bool IsEntering { get; private set; }

    public void Begin()
    {
        _digits.Clear();
        _pointAfter = -1;
        _negative = false;
        _hasExponent = false;
        _exponent = 0;
        _exponentNegative = false;
        IsEntering = true;
    }

    public void End() => IsEntering = false;

    public void AddDigit(int d)
    {
        if (_hasExponent)
        {
            _exponent = (_exponent * 10 + d) % 100;
            return;
        }
        if (_digits.Count >= 10) return;
        if (_digits.Count == 1 && _digits[0] == '0' && _pointAfter < 0)
        {
            // A lone leading zero is replaced by the next digit ("0" then "7" shows "7.")
            _digits[0] = (char)('0' + d);
            return;
        }
        _digits.Add((char)('0' + d));
    }

    public void AddPoint()
    {
        if (_hasExponent || _pointAfter >= 0) return;
        _pointAfter = _digits.Count;
    }

    public void AddEex()
    {
        if (_hasExponent) return;
        if (_digits.Count == 0)
        {
            // EEX with no mantissa starts "1" (real-calculator behavior: 1 × 10^x)
            _digits.Add('1');
        }
        _hasExponent = true;
        _exponent = 0;
        _exponentNegative = false;
    }

    public void ChangeSign()
    {
        if (_hasExponent) _exponentNegative = !_exponentNegative;
        else _negative = !_negative;
    }

    public Real ToReal()
    {
        if (_digits.Count == 0) return Real.Zero;
        string mantissa = new(_digits.ToArray());
        int pointAt = _pointAfter >= 0 ? _pointAfter : _digits.Count;
        decimal value = decimal.Parse(mantissa, CultureInfo.InvariantCulture);
        int scale = _digits.Count - pointAt;
        Real r = Real.FromParts(_negative ? -value : value, -scale);
        if (_hasExponent)
        {
            int e = _exponentNegative ? -_exponent : _exponent;
            r = Real.FromParts(r.M, r.E + e);
        }
        return r;
    }

    /// <summary>Raw entry text for the display: mantissa with point, minus sign, exponent digits.</summary>
    public (string Mantissa, bool Negative, string? Exponent, bool ExponentNegative) DisplayParts()
    {
        string mantissa = new(_digits.ToArray());
        if (_pointAfter >= 0)
            mantissa = mantissa.Insert(_pointAfter, ".");
        else
            mantissa += ".";
        if (_digits.Count == 0) mantissa = _pointAfter >= 0 ? "0." : ".";
        string? exp = _hasExponent ? _exponent.ToString("00", CultureInfo.InvariantCulture) : null;
        return (mantissa, _negative, exp, _exponentNegative);
    }
}
