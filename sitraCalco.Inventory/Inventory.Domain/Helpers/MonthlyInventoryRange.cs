namespace Inventory.Domain.Helpers;

public static class MonthlyInventoryRange
{
    public static bool IsValid(int? startDay, int? endDay) =>
        (startDay is null && endDay is null) ||
        (startDay is >= 1 and <= 31 && endDay is >= 1 and <= 31 && startDay <= endDay);

    public static bool Includes(DateTime date, int? startDay, int? endDay) =>
        (startDay is null && endDay is null) ||
        (startDay is int start && endDay is int end && IsValid(start, end) &&
         date.Day >= start && date.Day <= Math.Min(end, DateTime.DaysInMonth(date.Year, date.Month)));
}
