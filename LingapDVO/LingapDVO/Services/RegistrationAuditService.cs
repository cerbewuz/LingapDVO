using LingapDVO.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LingapDVO.Services
{
    /// <summary>
    /// Service for handling Registration Audit Logs and Token Validation
    /// Detects suspicious activity, backend manipulation, and duplicate registrations
    /// </summary>
    public class RegistrationAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDateTimeService _dateTimeService;

        public RegistrationAuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, IDateTimeService dateTimeService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _dateTimeService = dateTimeService;
        }

        /// <summary>
        /// Generate a new registration token
        /// </summary>
        public string GenerateRegistrationToken()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return string.Empty;

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();

            var registrationToken = new RegistrationToken
            {
                Token = token,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = _dateTimeService.Now,
                ExpiresAt = _dateTimeService.Now.AddMinutes(30), // 30 minute expiry
                IsUsed = false,
                IsRevoked = false
            };

            _context.RegistrationTokens.Add(registrationToken);
            _context.SaveChanges();

            return token;
        }

        /// <summary>
        /// Validate registration token and mark as used
        /// </summary>
        public (bool IsValid, string Reason) ValidateAndConsumeToken(string token, string email)
        {
            if (string.IsNullOrEmpty(token))
            {
                LogRegistrationAttempt(null, email, "BLOCKED", "WEB_FORM", "Missing registration token", false, true, "No token provided");
                return (false, "Missing registration token");
            }

            var tokenRecord = _context.RegistrationTokens.FirstOrDefault(t => t.Token == token);

            if (tokenRecord == null)
            {
                LogRegistrationAttempt(token, email, "BLOCKED", "UNKNOWN", "Invalid registration token", false, true, "Token not found in database");
                return (false, "Invalid registration token");
            }

            if (tokenRecord.IsUsed)
            {
                LogRegistrationAttempt(token, email, "BLOCKED", "CLONED", "Token already used - possible cloning attempt", false, true, "Token reuse detected");
                return (false, "This registration form has already been used");
            }

            if (tokenRecord.IsRevoked)
            {
                LogRegistrationAttempt(token, email, "BLOCKED", "UNKNOWN", "Token has been revoked", false, true, "Revoked token used");
                return (false, "This registration form is no longer valid");
            }

            if (tokenRecord.ExpiresAt < _dateTimeService.Now)
            {
                LogRegistrationAttempt(token, email, "BLOCKED", "WEB_FORM", "Token expired", false, true, "Token expired");
                return (false, "Registration form has expired. Please refresh the page.");
            }

            // Mark token as used
            tokenRecord.IsUsed = true;
            tokenRecord.UsedAt = _dateTimeService.Now;
            tokenRecord.UsedByEmail = email;
            _context.SaveChanges();

            return (true, "Token valid");
        }

        /// <summary>
        /// Log a registration attempt
        /// </summary>
        public void LogRegistrationAttempt(
            string? registrationToken,
            string? email,
            string action,
            string source,
            string? reason = null,
            bool hasValidToken = false,
            bool suspiciousActivity = false,
            string? suspiciousReasons = null,
            int? registeredUserId = null,
            string? username = null,
            string? fullName = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            var auditLog = new RegistrationAuditLog
            {
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
                Email = email,
                Username = username,
                FullName = fullName,
                Action = action,
                Source = source,
                Reason = reason,
                RegistrationToken = registrationToken,
                HasValidToken = hasValidToken,
                SuspiciousActivity = suspiciousActivity,
                SuspiciousReasons = suspiciousReasons,
                AttemptedAt = _dateTimeService.Now,
                RegisteredUserId = registeredUserId
            };

            _context.RegistrationAuditLogs.Add(auditLog);
            _context.SaveChanges();
        }

        /// <summary>
        /// Check for duplicate registration attempts (same email, IP, etc.)
        /// </summary>
        public (bool IsDuplicate, string Reason) CheckDuplicateRegistration(string email, string username)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return (false, string.Empty);

            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // Check if email already exists in UserAccount
            var existingEmail = _context.UserAccount.Any(r => r.Email.ToLower() == email.ToLower());
            if (existingEmail)
            {
                return (true, "Email already registered");
            }

            // Check if username already exists
            var existingUsername = _context.UserAccount.Any(r => r.Username.ToLower() == username.ToLower());
            if (existingUsername)
            {
                return (true, "Username already taken");
            }

            // Check for suspicious rapid attempts from same IP (more than 5 in last 5 minutes)
            var recentAttempts = _context.RegistrationAuditLogs
                .Where(l => l.IpAddress == ipAddress &&
                           l.AttemptedAt > _dateTimeService.Now.AddMinutes(-5))
                .Count();

            if (recentAttempts > 5)
            {
                LogRegistrationAttempt(null, email, "BLOCKED", "WEB_FORM",
                    "Too many registration attempts", false, true,
                    $"Rapid attempts from IP: {recentAttempts} in 5 minutes",
                    null, username, null);
                return (true, "Too many registration attempts. Please try again later.");
            }

            return (false, string.Empty);
        }

        /// <summary>
        /// Check for suspicious registration patterns
        /// </summary>
        public (bool IsSuspicious, string Reasons) DetectSuspiciousRegistration(
            string email,
            string username,
            string? token)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return (false, string.Empty);

            var reasons = new List<string>();

            // Check 1: Missing token
            if (string.IsNullOrEmpty(token))
            {
                reasons.Add("Missing registration token (possible direct API call)");
            }

            // Check 2: Check for bot-like usernames/emails
            if (username.Contains("test") || username.Contains("admin") ||
                email.Contains("test@") || email.Contains("admin@"))
            {
                reasons.Add("Suspicious username/email pattern");
            }

            // Check 3: Check recent failed attempts from same IP
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var failedAttempts = _context.RegistrationAuditLogs
                .Where(l => l.IpAddress == ipAddress &&
                           l.Action == "FAILED" &&
                           l.AttemptedAt > _dateTimeService.Now.AddMinutes(-10))
                .Count();

            if (failedAttempts > 3)
            {
                reasons.Add($"Multiple failed attempts from IP: {failedAttempts}");
            }

            // Check 4: UserAgent check
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            if (string.IsNullOrEmpty(userAgent) || userAgent.Length < 20)
            {
                reasons.Add("Missing or suspicious User-Agent");
            }

            return (reasons.Any(), string.Join("; ", reasons));
        }
    }
}
