using Microsoft.Extensions.Configuration;

namespace LingapDVO.Services
{
    /// <summary>
    /// Service to centralize session configuration management
    /// Provides consistent access to session timeout settings across the application
    /// </summary>
    public interface ISessionConfigurationService
    {
        /// <summary>
        /// Gets the idle timeout in minutes from configuration (force logout time)
        /// </summary>
        int IdleTimeoutMinutes { get; }

        /// <summary>
        /// Gets the warning timeout in minutes (when to show warning modal)
        /// </summary>
        int WarningTimeoutMinutes { get; }

        /// <summary>
        /// Gets the keep-alive interval in minutes (periodic session refresh)
        /// </summary>
        int KeepAliveIntervalMinutes { get; }

        /// <summary>
        /// Gets the cookie name from configuration
        /// </summary>
        string CookieName { get; }

        /// <summary>
        /// Gets the session configuration as a JSON-serializable object for client-side use
        /// </summary>
        object GetClientConfiguration();
    }

    public class SessionConfigurationService : ISessionConfigurationService
    {
        private readonly IConfiguration _configuration;

        public SessionConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public int IdleTimeoutMinutes => _configuration.GetValue<int>("Security:Session:IdleTimeoutMinutes", 12);

        public int WarningTimeoutMinutes => _configuration.GetValue<int>("Security:Session:WarningTimeoutMinutes", 10);

        public int KeepAliveIntervalMinutes => _configuration.GetValue<int>("Security:Session:KeepAliveIntervalMinutes", 5);

        public string CookieName => _configuration.GetValue<string>("Security:Session:CookieName", ".LingapDVO.Session");

        public object GetClientConfiguration()
        {
            var warningTime = WarningTimeoutMinutes;
            var forceLogoutTime = IdleTimeoutMinutes;
            var countdownDuration = (forceLogoutTime - warningTime) * 60; // in seconds

            return new
            {
                warningTimeMinutes = warningTime,
                forceLogoutMinutes = forceLogoutTime,
                countdownDurationSeconds = countdownDuration,
                keepAliveIntervalMinutes = KeepAliveIntervalMinutes,
                logoutUrl = "/Login/Logout",
                keepAliveUrl = "/Login/KeepAlive"
            };
        }
    }
}
