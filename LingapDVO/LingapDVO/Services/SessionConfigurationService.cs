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
        /// Gets the idle timeout in minutes from configuration
        /// </summary>
        int IdleTimeoutMinutes { get; }

        /// <summary>
        /// Gets the warning timeout in minutes (default: IdleTimeout - 1)
        /// </summary>
        int WarningTimeoutMinutes { get; }

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

        public int IdleTimeoutMinutes => _configuration.GetValue<int>("Session:IdleTimeoutMinutes", 10);

        public int WarningTimeoutMinutes => _configuration.GetValue<int>("Session:WarningTimeoutMinutes", IdleTimeoutMinutes - 1);

        public string CookieName => _configuration.GetValue<string>("Session:CookieName", ".LingapDVO.Session");

        public object GetClientConfiguration()
        {
            return new
            {
                idleTimeoutMinutes = IdleTimeoutMinutes,
                warningTimeoutMinutes = WarningTimeoutMinutes,
                idleTimeoutMs = IdleTimeoutMinutes * 60 * 1000,
                warningTimeoutMs = WarningTimeoutMinutes * 60 * 1000,
                checkIntervalMs = 30 * 1000, // 30 seconds
                countdownIntervalMs = 1000 // 1 second
            };
        }
    }
}
