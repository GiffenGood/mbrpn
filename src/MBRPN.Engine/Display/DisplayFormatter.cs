using System.Globalization;
using System.Text;
using MBRPN.Engine.Core;

namespace MBRPN.Engine.Display;

/// <summary>
/// Renders numbers the way the HP-12C LCD does: FIX 0–9 with thousands separators and a
/// trailing decimal point, scientific notation with a seven-digit mantissa and two-digit
/// exponent, and the special date / program-line / error texts. This class is also the
/// rounding authority that RND and AMORT use ("round to the displayed value").
/// </summary>
public sealed class DisplayFormatter
{
    /// <summary>Decimal places in FIX mode (0–9). Ignored when <see cref="Scientific"/>.</summary>
    public int FixDigits { get; set; } = 2;

    /// <summary>True after f-. (scientific display mode).</summary>
    public bool Scientific { get; set; }

    public string Format(Real x) => Scientific ? FormatScientific(x) : FormatFix(x, FixDigits);

    /// <summary>The value the display shows, as a number — what f RND turns X into.</summary>
    public Real RoundToDisplay(Real x)
    {
        if (Scientific)
        {
            decimal m = Math.Round(Math.Abs(x.M), 6, MidpointRounding.AwayFromZero);
            return Real.FromParts(x.IsNegative ? -m : m, x.E);
        }
        if (WouldUseScientific(x, FixDigits)) return x;
        return RoundToFix(x, FixDigits);
    }

    /// <summary>Rounds a value to n decimal places (the AMORT hook when n = FixDigits).</summary>
    public static Real RoundToFix(Real x, int decimals)
    {
        decimal d = Math.Round(x.ToDecimal(), decimals, MidpointRounding.AwayFromZero);
        return Real.FromDecimal(d);
    }

    private static bool WouldUseScientific(Real x, int fixDigits)
    {
        if (x.IsZero) return false;
        return x.E >= 10; // integer portion needs more than ten digits
    }

    public string FormatFix(Real x, int fixDigits)
    {
        if (WouldUseScientific(x, fixDigits)) return FormatScientific(x);

        int decimals = fixDigits;
        decimal value = Math.Round(x.ToDecimal(), decimals, MidpointRounding.AwayFromZero);

        // Ten digits total: if the integer part is wide, fewer decimal places fit.
        int integerDigits = IntegerDigitCount(value);
        if (integerDigits + decimals > 10)
        {
            decimals = Math.Max(0, 10 - integerDigits);
            value = Math.Round(x.ToDecimal(), decimals, MidpointRounding.AwayFromZero);
            if (IntegerDigitCount(value) > 10) return FormatScientific(x); // rounding carried into an 11th digit
        }

        bool negative = value < 0m;
        decimal abs = Math.Abs(value);
        decimal intPart = decimal.Truncate(abs);
        string intText = intPart.ToString("#,##0", CultureInfo.InvariantCulture);

        var sb = new StringBuilder();
        if (negative) sb.Append('-');
        sb.Append(intText);
        sb.Append('.');
        if (decimals > 0)
        {
            decimal frac = abs - intPart;
            string fracText = frac.ToString("F" + decimals, CultureInfo.InvariantCulture)[2..];
            sb.Append(fracText);
        }
        return sb.ToString();
    }

    public string FormatScientific(Real x)
    {
        decimal m = Math.Round(Math.Abs(x.M), 6, MidpointRounding.AwayFromZero);
        int e = x.E;
        if (m >= 10m) { m /= 10m; e++; }
        if (e > 99) { m = 9.999999m; e = 99; }
        var sb = new StringBuilder();
        if (x.IsNegative) sb.Append('-');
        sb.Append(m.ToString("0.000000", CultureInfo.InvariantCulture));
        sb.Append(e < 0 ? '-' : ' ');
        sb.Append(Math.Abs(e).ToString("00", CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    private static int IntegerDigitCount(decimal value)
    {
        decimal abs = Math.Abs(decimal.Truncate(value));
        if (abs < 1m) return 1;
        int count = 0;
        while (abs >= 1m) { abs /= 10m; count++; }
        return count;
    }

    /// <summary>Number entry in progress: digits as typed, thousands separators, EEX exponent.</summary>
    public static string FormatEntry(EntryBuffer entry)
    {
        var (mantissa, negative, exponent, exponentNegative) = entry.DisplayParts();
        int pointIdx = mantissa.IndexOf('.');
        string intPart = pointIdx >= 0 ? mantissa[..pointIdx] : mantissa;
        string fracPart = pointIdx >= 0 ? mantissa[(pointIdx + 1)..] : string.Empty;

        var sb = new StringBuilder();
        if (negative) sb.Append('-');
        if (intPart.Length == 0) sb.Append('0');
        else
        {
            for (int i = 0; i < intPart.Length; i++)
            {
                sb.Append(intPart[i]);
                int remaining = intPart.Length - 1 - i;
                if (remaining > 0 && remaining % 3 == 0) sb.Append(',');
            }
        }
        sb.Append('.');
        sb.Append(fracPart);
        if (exponent is not null)
        {
            sb.Append(exponentNegative ? '-' : ' ');
            sb.Append(exponent);
        }
        return sb.ToString();
    }

    /// <summary>Date result: month/day/year (order per mode) with the weekday digit at far right.</summary>
    public static string FormatDate(int month, int day, int year, int weekday, bool dmyMode)
    {
        return dmyMode
            ? string.Create(CultureInfo.InvariantCulture, $"{day},{month:00},{year:0000} {weekday}")
            : string.Create(CultureInfo.InvariantCulture, $"{month},{day:00},{year:0000} {weekday}");
    }

    public static string FormatError(CalcError error) => $"Error {(int)error}";
}
