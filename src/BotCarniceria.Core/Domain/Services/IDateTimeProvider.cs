namespace BotCarniceria.Core.Domain.Services;

/// <summary>
/// Provides date and time operations with timezone awareness.
/// This is a Domain Service that abstracts time operations to support
/// timezone configuration and facilitate testing.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current UTC time.
    /// Always use this for storing dates in the database.
    /// </summary>
    DateTime UtcNow { get; }
    
    /// <summary>
    /// Gets the current time in the configured business timezone.
    /// Use this for display purposes and business logic that depends on local time.
    /// </summary>
    DateTime Now { get; }
    
    /// <summary>
    /// Converts UTC time to business local time.
    /// </summary>
    /// <param name="utcDateTime">The UTC datetime to convert</param>
    /// <returns>The datetime in the business timezone</returns>
    DateTime ToLocalTime(DateTime utcDateTime);
    
    /// <summary>
    /// Converts business local time to UTC.
    /// </summary>
    /// <param name="localDateTime">The local datetime to convert</param>
    /// <returns>The datetime in UTC</returns>
    DateTime ToUtcTime(DateTime localDateTime);
    
    /// <summary>
    /// Gets the current time of day in the business timezone.
    /// Useful for comparing against business hours.
    /// </summary>
    TimeSpan LocalTimeOfDay { get; }
    
    /// <summary>
    /// Gets today's date (midnight) in the business timezone.
    /// Useful for date comparisons.
    /// </summary>
    DateTime LocalToday { get; }
}
