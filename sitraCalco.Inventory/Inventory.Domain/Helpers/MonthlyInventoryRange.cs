namespace Inventory.Domain.Helpers;

public static class MonthlyInventoryRange
{
    public static bool IsValid(int startDay, int endDay) =>
        startDay is >= 1 and <= 31 && endDay is >= 1 and <= 31 && startDay <= endDay;

    public static bool Includes(DateTime date, int startDay, int endDay) =>
        IsValid(startDay, endDay) && date.Day >= startDay &&
        date.Day <= Math.Min(endDay, DateTime.DaysInMonth(date.Year, date.Month));
}
