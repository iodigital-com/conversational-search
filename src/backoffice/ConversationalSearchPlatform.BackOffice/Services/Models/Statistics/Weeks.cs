using System.Globalization;

namespace ConversationalSearchPlatform.BackOffice.Services.Models.Statistics;

public static class Weeks
{
    public static int AmountOfWeeksInCurrentYear() => ISOWeek.GetWeeksInYear(DateTimeOffset.UtcNow.Year);

    public static IEnumerable<int> GetWeekIntsInCurrentYear => Enumerable.Range(1, AmountOfWeeksInCurrentYear());
}