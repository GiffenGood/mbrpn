using MBRPN.Engine.Core;

namespace MBRPN.Engine.Dates;

/// <summary>
/// Calendar functions. Dates travel as calculator numbers — M.DDYYYY or D.MMYYYY
/// depending on the mode — and are valid from October 15, 1582 through November 25, 4046
/// (Error 8 outside that range or for a malformed number).
/// </summary>
public static class DateMath
{
    private static readonly DateOnly MinDate = new(1582, 10, 15);
    private static readonly DateOnly MaxDate = new(4046, 11, 25);

    public static DateOnly Parse(Real value, bool dmyMode)
    {
        if (value.IsNegative) throw new CalcException(CalcError.Calendar);
        decimal d = value.ToDecimal();
        int first = (int)decimal.Truncate(d);
        decimal frac = d - first;
        // Two digits for the second field, four for the year: frac = 0.DDYYYY / 0.MMYYYY
        decimal scaled = frac * 1_000_000m;
        int rest = (int)decimal.Round(scaled, 0, MidpointRounding.AwayFromZero);
        if (Math.Abs(scaled - rest) > 0.0001m) throw new CalcException(CalcError.Calendar);
        int second = rest / 10_000;
        int year = rest % 10_000;
        int month = dmyMode ? second : first;
        int day = dmyMode ? first : second;
        if (month is < 1 or > 12 || day < 1 || year < 1 || year > 9999)
            throw new CalcException(CalcError.Calendar);
        if (day > DateTime.DaysInMonth(year, month)) throw new CalcException(CalcError.Calendar);
        var date = new DateOnly(year, month, day);
        if (date < MinDate || date > MaxDate) throw new CalcException(CalcError.Calendar);
        return date;
    }

    public static Real ToReal(DateOnly date, bool dmyMode)
    {
        int first = dmyMode ? date.Day : date.Month;
        int second = dmyMode ? date.Month : date.Day;
        decimal encoded = first + second / 100m + date.Year / 1_000_000m;
        return Real.FromDecimal(encoded);
    }

    /// <summary>g DATE — the date a given number of days before/after another date.</summary>
    public static DateOnly AddDays(Real dateValue, Real days, bool dmyMode)
    {
        DateOnly start = Parse(dateValue, dmyMode);
        decimal offset = decimal.Truncate(days.ToDecimal());
        if (Math.Abs(offset) > 1_000_000m) throw new CalcException(CalcError.Calendar);
        DateOnly result;
        try
        {
            result = DateOnly.FromDayNumber(start.DayNumber + (int)offset);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new CalcException(CalcError.Calendar);
        }
        if (result < MinDate || result > MaxDate) throw new CalcException(CalcError.Calendar);
        return result;
    }

    /// <summary>Weekday digit as the calculator shows it: 1 = Monday … 7 = Sunday.</summary>
    public static int Weekday(DateOnly date) =>
        date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;

    /// <summary>g ΔDYS — actual days (X) and 30/360-basis days (Y) from date1 (Y) to date2 (X).</summary>
    public static (Real Actual, Real Basis360) DaysBetween(Real date1Value, Real date2Value, bool dmyMode)
    {
        DateOnly d1 = Parse(date1Value, dmyMode);
        DateOnly d2 = Parse(date2Value, dmyMode);
        int actual = d2.DayNumber - d1.DayNumber;

        int z1 = d1.Day == 31 ? 30 : d1.Day;
        int z2 = d2.Day == 31 && d1.Day >= 30 ? 30 : d2.Day;
        int basis360 = 360 * (d2.Year - d1.Year) + 30 * (d2.Month - d1.Month) + (z2 - z1);

        return (Real.FromLong(actual), Real.FromLong(basis360));
    }
}
