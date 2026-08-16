using PayDefteri.Domain.Enums;

namespace PayDefteri.Domain.Services;

public static class ExpenseRecurrenceCalendar
{
    public static string PeriodKey(RecurrenceFrequency frequency, DateOnly date) =>
        frequency switch
        {
            RecurrenceFrequency.Weekly => $"{date.Year}-W{ISOWeek(date):00}",
            RecurrenceFrequency.Yearly => $"{date.Year}",
            _ => $"{date.Year}-{date.Month:00}",
        };

    public static DateOnly NextAfter(RecurrenceFrequency frequency, int anchorDay, DateOnly current)
    {
        return frequency switch
        {
            RecurrenceFrequency.Weekly => current.AddDays(7),
            RecurrenceFrequency.Yearly => ClampDay(current.Year + 1, current.Month, anchorDay),
            _ => NextMonthly(current, anchorDay),
        };
    }

    public static DateOnly FirstOnOrAfter(RecurrenceFrequency frequency, int anchorDay, DateOnly start)
    {
        return frequency switch
        {
            RecurrenceFrequency.Weekly => NextWeekdayOnOrAfter(start, anchorDay),
            RecurrenceFrequency.Yearly =>
                start <= ClampDay(start.Year, start.Month, anchorDay)
                    ? ClampDay(start.Year, start.Month, anchorDay)
                    : ClampDay(start.Year + 1, start.Month, anchorDay),
            _ => start.Day <= Math.Min(anchorDay, DateTime.DaysInMonth(start.Year, start.Month))
                ? ClampDay(start.Year, start.Month, anchorDay)
                : NextMonthly(ClampDay(start.Year, start.Month, Math.Min(anchorDay, 28)), anchorDay),
        };
    }

    private static DateOnly NextMonthly(DateOnly current, int anchorDay)
    {
        var y = current.Year;
        var m = current.Month + 1;
        if (m > 12)
        {
            m = 1;
            y++;
        }

        return ClampDay(y, m, anchorDay);
    }

    private static DateOnly ClampDay(int year, int month, int day)
    {
        var max = DateTime.DaysInMonth(year, month);
        var d = Math.Clamp(day <= 0 ? 1 : day, 1, Math.Min(max, 28));
        if (day > 28)
        {
            d = Math.Min(day, max);
        }

        return new DateOnly(year, month, d);
    }

    private static DateOnly NextWeekdayOnOrAfter(DateOnly start, int dayOfWeek)
    {
        var target = Math.Clamp(dayOfWeek, 0, 6);
        var current = (int)start.DayOfWeek;
        var delta = (target - current + 7) % 7;
        return start.AddDays(delta);
    }

    private static int ISOWeek(DateOnly date)
    {
        var dt = date.ToDateTime(TimeOnly.MinValue);
        return System.Globalization.ISOWeek.GetWeekOfYear(dt);
    }
}
