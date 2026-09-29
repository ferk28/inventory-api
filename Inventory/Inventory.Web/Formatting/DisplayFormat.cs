using System.Globalization;
namespace Inventory.Web.Formatting;
public static class DisplayFormat
{
    private static readonly CultureInfo MoneyCulture = CultureInfo.GetCultureInfo("en-US");
    public const int LowStockThreshold = 5;
    public static string Money(decimal amount)
    {
        return amount.ToString("C", MoneyCulture);
    }
    // The API stores and returns UTC without an offset; the app runs on the user's machine,
    // so its local time zone is the user's.
    public static string LocalTime(DateTime utc)
    {
        return DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }
    public static string? LocalTime(DateTime? utc)
    {
        return utc is null ? null : LocalTime(utc.Value);
    }
}
