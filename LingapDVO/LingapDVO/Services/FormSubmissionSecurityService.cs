using LingapDVO.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace LingapDVO.Services
{
    /// <summary>
    /// Security service to prevent form cloning, duplication, and unauthorized manipulation
    /// Uses FormSubmissionToken and FormSubmissionAuditLog for comprehensive protection
    /// </summary>
    public class FormSubmissionSecurityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FormSubmissionSecurityService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Generate a secure token for form submission
        /// Token is valid for 1 hour and can only be used once
        /// </summary>
        public async Task<FormSubmissionToken> GenerateSubmissionTokenAsync(int userId, string formType)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                throw new InvalidOperationException("HTTP context is not available");

            // Generate cryptographically secure token
            var token = GenerateSecureToken();

            var submissionToken = new FormSubmissionToken
            {
                Token = token,
                FormType = formType, // "HospitalBill", "MedicalLab", "Funeral"
                UserId = userId,
                IpAddress = GetClientIpAddress(httpContext),
                UserAgent = GetUserAgent(httpContext),
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddHours(1),
                IsUsed = false,
                IsRevoked = false
            };

            _context.FormSubmissionTokens.Add(submissionToken);
            await _context.SaveChangesAsync();

            return submissionToken;
        }

        /// <summary>
        /// Validate submission token before allowing form submission
        /// Prevents replay attacks and unauthorized submissions
        /// </summary>
        public async Task<(bool IsValid, string? Reason)> ValidateSubmissionTokenAsync(string token, int userId, string formType)
        {
            var submissionToken = await _context.FormSubmissionTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.UserId == userId && t.FormType == formType);

            if (submissionToken == null)
                return (false, "Invalid or missing submission token");

            if (submissionToken.IsRevoked)
                return (false, "Token has been revoked");

            if (submissionToken.IsUsed)
                return (false, "Token has already been used (possible duplication attempt)");

            if (DateTime.Now > submissionToken.ExpiresAt)
                return (false, "Token has expired - please refresh the form");

            return (true, null);
        }

        /// <summary>
        /// Mark token as used after successful submission
        /// </summary>
        public async Task MarkTokenAsUsedAsync(string token, int submittedFormId)
        {
            var submissionToken = await _context.FormSubmissionTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (submissionToken != null)
            {
                submissionToken.IsUsed = true;
                submissionToken.UsedAt = DateTime.Now;
                submissionToken.SubmittedFormId = submittedFormId;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Log form submission attempt for security audit
        /// Tracks all attempts (successful and failed) for forensic analysis
        /// </summary>
        public async Task LogSubmissionAttemptAsync(
            string formType,
            int userId,
            string patientName,
            string requestorName,
            string action, // "ATTEMPT", "SUCCESS", "BLOCKED", "FAILED", "DUPLICATE"
            string? submissionToken = null,
            bool hasValidToken = false,
            bool suspiciousActivity = false,
            string? suspiciousReasons = null,
            string? reason = null,
            int? submittedFormId = null,
            bool isDuplicate = false,
            string? duplicateDetails = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            var auditLog = new FormSubmissionAuditLog
            {
                FormType = formType,
                UserId = userId,
                IpAddress = GetClientIpAddress(httpContext),
                UserAgent = GetUserAgent(httpContext),
                PatientName = patientName,
                RequestorName = requestorName,
                Action = action,
                Source = "WEB_FORM",
                Reason = reason,
                SubmissionToken = submissionToken,
                HasValidToken = hasValidToken,
                SuspiciousActivity = suspiciousActivity,
                SuspiciousReasons = suspiciousReasons,
                AttemptedAt = DateTime.Now,
                SubmittedFormId = submittedFormId,
                IsDuplicate = isDuplicate,
                DuplicateDetails = duplicateDetails
            };

            _context.FormSubmissionAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Check for duplicate submissions based on form data hash
        /// Prevents users from submitting identical forms multiple times
        /// </summary>
        public async Task<(bool IsDuplicate, string? Details)> CheckForDuplicateSubmissionAsync(
            int userId,
            string formType,
            string patientName,
            string requestorName,
            DateTime dateOfBirth)
        {
            // Create a hash of the key form data
            var formDataHash = GenerateFormDataHash(userId, formType, patientName, requestorName, dateOfBirth);

            // Check if this exact form data was submitted recently (within last 24 hours)
            var recentDuplicate = await _context.FormSubmissionAuditLogs
                .Where(log =>
                    log.UserId == userId &&
                    log.FormType == formType &&
                    log.FormDataHash == formDataHash &&
                    log.Action == "SUCCESS" &&
                    log.AttemptedAt >= DateTime.Now.AddHours(-24))
                .OrderByDescending(log => log.AttemptedAt)
                .FirstOrDefaultAsync();

            if (recentDuplicate != null)
            {
                var details = $"Duplicate submission detected. Identical form was submitted on {recentDuplicate.AttemptedAt:yyyy-MM-dd HH:mm:ss}";
                return (true, details);
            }

            return (false, null);
        }

        /// <summary>
        /// Generate secure cryptographic token
        /// </summary>
        private string GenerateSecureToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var tokenBytes = new byte[64];
                rng.GetBytes(tokenBytes);
                return Convert.ToBase64String(tokenBytes);
            }
        }

        /// <summary>
        /// Generate SHA-256 hash of form data for duplicate detection
        /// </summary>
        private string GenerateFormDataHash(int userId, string formType, string patientName, string requestorName, DateTime dateOfBirth)
        {
            var dataToHash = $"{userId}|{formType}|{patientName}|{requestorName}|{dateOfBirth:yyyyMMdd}";
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// Get client IP address (handles proxies and load balancers)
        /// </summary>
        private string GetClientIpAddress(HttpContext context)
        {
            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "UNKNOWN";
            }
            return ipAddress.Length > 100 ? ipAddress.Substring(0, 100) : ipAddress;
        }

        /// <summary>
        /// Get user agent string
        /// </summary>
        private string GetUserAgent(HttpContext context)
        {
            var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "UNKNOWN";
            return userAgent.Length > 500 ? userAgent.Substring(0, 500) : userAgent;
        }

        /// <summary>
        /// Revoke all unused tokens for a user (useful when user logs out or session expires)
        /// </summary>
        public async Task RevokeUnusedTokensForUserAsync(int userId)
        {
            var unusedTokens = await _context.FormSubmissionTokens
                .Where(t => t.UserId == userId && !t.IsUsed && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in unusedTokens)
            {
                token.IsRevoked = true;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Clean up expired tokens (run periodically via background job)
        /// </summary>
        public async Task CleanupExpiredTokensAsync()
        {
            var expiredTokens = await _context.FormSubmissionTokens
                .Where(t => t.ExpiresAt < DateTime.Now && !t.IsUsed)
                .ToListAsync();

            _context.FormSubmissionTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }
    }
}
