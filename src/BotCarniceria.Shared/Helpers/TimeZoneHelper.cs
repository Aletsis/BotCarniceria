namespace BotCarniceria.Shared.Helpers;

/// <summary>
/// Static helper for timezone conversions
/// This is in Shared layer as it's a cross-cutting concern used by multiple presentation layers
/// </summary>
public static class TimeZoneHelper
{
    // Default timezone - could be made configurable via environment variable
    private static readonly string DefaultTimeZoneId = 
        Environment.GetEnvironmentVariable("TIMEZONE_ID") ?? "Central Standard Time (Mexico)";
    
    private static readonly Lazy<TimeZoneInfo> TimeZone = new Lazy<TimeZoneInfo>(() =>
        TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId));

    /// <summary>
    /// Converts UTC DateTime to configured local time
    /// </summary>
    public static DateTime ToLocalTime(DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
        {
            // If not UTC, assume it is and convert
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TimeZone.Value);
    }

    /// <summary>
    /// Gets current local time
    /// </summary>
    public static DateTime Now => ToLocalTime(DateTime.UtcNow);

    /// <summary>
    /// Gets current local date
    /// </summary>
    public static DateTime Today => Now.Date;

    /// <summary>
    /// Converts local DateTime to UTC
    /// </summary>
    public static DateTime ToUtcTime(DateTime localDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, TimeZone.Value);
    }
}
