using BotCarniceria.Core.Domain.Services;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Constants;

namespace BotCarniceria.Infrastructure.Services;

/// <summary>
/// Implementation of IDateTimeProvider that provides timezone-aware date and time operations.
/// Uses configuration from the database to determine the business timezone.
/// Implements caching to avoid excessive database queries.
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    private readonly IUnitOfWork _unitOfWork;
    private TimeZoneInfo? _cachedTimeZone;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private readonly object _lock = new object();

    public DateTimeProvider(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime Now => ToLocalTime(UtcNow);

    public TimeSpan LocalTimeOfDay => Now.TimeOfDay;

    public DateTime LocalToday => Now.Date;

    public DateTime ToLocalTime(DateTime utcDateTime)
    {
        var timeZone = GetTimeZone();
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
    }

    public DateTime ToUtcTime(DateTime localDateTime)
    {
        var timeZone = GetTimeZone();
        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
    }

    private TimeZoneInfo GetTimeZone()
    {
        // Check cache (double-check locking pattern)
        if (_cachedTimeZone != null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedTimeZone;
        }

        lock (_lock)
        {
            // Check again inside lock
            if (_cachedTimeZone != null && DateTime.UtcNow < _cacheExpiry)
            {
                return _cachedTimeZone;
            }

            // Load from configuration
            var timeZoneId = _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.System.TimeZoneId)
                .GetAwaiter().GetResult();

            if (string.IsNullOrEmpty(timeZoneId))
            {
                // Default to Central Standard Time (Mexico)
                timeZoneId = "Central Standard Time (Mexico)";
            }

            try
            {
                _cachedTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch
            {
                // Fallback to UTC if timezone not found
                _cachedTimeZone = TimeZoneInfo.Utc;
            }

            _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
            return _cachedTimeZone;
        }
    }
}
