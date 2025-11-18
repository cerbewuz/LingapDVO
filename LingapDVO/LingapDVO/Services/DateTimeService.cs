namespace LingapDVO.Services
{
    /// <summary>
    /// Implementation of IDateTimeService that provides date and time operations
    /// strictly in Philippine timezone (Asia/Manila, UTC+8)
    /// </summary>
    public class DateTimeService : IDateTimeService
    {
        private readonly TimeZoneInfo _philippineTimeZone;

        public DateTimeService()
        {
            // Initialize Philippine timezone (Asia/Manila)
            // This works cross-platform (Windows, Linux, macOS)
            try
            {
                // Try Linux/macOS timezone ID first
                _philippineTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    // Fallback to Windows timezone ID
                    _philippineTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
                }
                catch (TimeZoneNotFoundException)
                {
                    // Last resort: create custom timezone for UTC+8
                    _philippineTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                        "Philippine Standard Time",
                        TimeSpan.FromHours(8),
                        "Philippine Standard Time",
                        "Philippine Standard Time"
                    );
                }
            }
        }

        /// <summary>
        /// Gets the current date and time in Philippine timezone
        /// </summary>
        public DateTime Now
        {
            get
            {
                var utcNow = DateTime.UtcNow;
                return TimeZoneInfo.ConvertTimeFromUtc(utcNow, _philippineTimeZone);
            }
        }

        /// <summary>
        /// Gets the current UTC date and time
        /// </summary>
        public DateTime UtcNow => DateTime.UtcNow;

        /// <summary>
        /// Gets the current date (without time) in Philippine timezone
        /// </summary>
        public DateTime Today => Now.Date;

        /// <summary>
        /// Gets the Philippine TimeZoneInfo
        /// </summary>
        public TimeZoneInfo PhilippineTimeZone => _philippineTimeZone;

        /// <summary>
        /// Converts a UTC DateTime to Philippine timezone
        /// </summary>
        public DateTime ConvertToPhilippineTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Unspecified)
            {
                // Assume it's UTC if unspecified
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            else if (utcDateTime.Kind == DateTimeKind.Local)
            {
                // Convert to UTC first
                utcDateTime = utcDateTime.ToUniversalTime();
            }

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _philippineTimeZone);
        }

        /// <summary>
        /// Converts a Philippine DateTime to UTC
        /// </summary>
        public DateTime ConvertToUtc(DateTime philippineDateTime)
        {
            if (philippineDateTime.Kind == DateTimeKind.Utc)
            {
                return philippineDateTime;
            }

            // Treat the datetime as Philippine time and convert to UTC
            return TimeZoneInfo.ConvertTimeToUtc(philippineDateTime, _philippineTimeZone);
        }
    }
}
