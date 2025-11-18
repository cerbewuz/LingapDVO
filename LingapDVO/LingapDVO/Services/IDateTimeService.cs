namespace LingapDVO.Services
{
    /// <summary>
    /// Service for providing date and time operations in Philippine timezone (Asia/Manila, UTC+8)
    /// </summary>
    public interface IDateTimeService
    {
        /// <summary>
        /// Gets the current date and time in Philippine timezone
        /// </summary>
        DateTime Now { get; }

        /// <summary>
        /// Gets the current UTC date and time
        /// </summary>
        DateTime UtcNow { get; }

        /// <summary>
        /// Gets the current date (without time) in Philippine timezone
        /// </summary>
        DateTime Today { get; }

        /// <summary>
        /// Converts a UTC DateTime to Philippine timezone
        /// </summary>
        DateTime ConvertToPhilippineTime(DateTime utcDateTime);

        /// <summary>
        /// Converts a Philippine DateTime to UTC
        /// </summary>
        DateTime ConvertToUtc(DateTime philippineDateTime);

        /// <summary>
        /// Gets the Philippine TimeZoneInfo
        /// </summary>
        TimeZoneInfo PhilippineTimeZone { get; }
    }
}
