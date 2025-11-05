using LingapDVO.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LingapDVO.Services
{
    /// <summary>
    /// Service for handling Form Submission Audit Logs and Token Validation
    /// Detects cloning, duplication, and suspicious form submission activity
    /// </summary>
    public class FormSubmissionAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FormSubmissionAuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Generate a new form submission token
        /// </summary>
        public string GenerateFormSubmissionToken(int userId, string formType)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return string.Empty;

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();

            var submissionToken = new FormSubmissionToken
            {
                Token = token,
                FormType = formType,
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(2), // 2 hour expiry
                IsUsed = false,
                IsRevoked = false
            };

            _context.FormSubmissionTokens.Add(submissionToken);
            _context.SaveChanges();

            return token;
        }

        /// <summary>
        /// Validate form submission token and mark as used
        /// </summary>
        public (bool IsValid, string Reason) ValidateAndConsumeFormToken(
            string token,
            int userId,
            string formType,
            int? submittedFormId = null)
        {
            if (string.IsNullOrEmpty(token))
            {
                LogFormSubmissionAttempt(formType, userId, "BLOCKED", "WEB_FORM", null, null,
                    "Missing form submission token", null, false, true, "No token provided");
                return (false, "Missing form submission token");
            }

            var tokenRecord = _context.FormSubmissionTokens
                .FirstOrDefault(t => t.Token == token && t.UserId == userId && t.FormType == formType);

            if (tokenRecord == null)
            {
                LogFormSubmissionAttempt(formType, userId, "BLOCKED", "UNKNOWN", null, null,
                    "Invalid form submission token", token, false, true, "Token not found or mismatch");
                return (false, "Invalid form submission token");
            }

            if (tokenRecord.IsUsed)
            {
                LogFormSubmissionAttempt(formType, userId, "BLOCKED", "CLONED", null, null,
                    "Token already used - possible form cloning", token, false, true, "Token reuse detected");
                return (false, "This form has already been submitted");
            }

            if (tokenRecord.IsRevoked)
            {
                LogFormSubmissionAttempt(formType, userId, "BLOCKED", "UNKNOWN", null, null,
                    "Token has been revoked", token, false, true, "Revoked token used");
                return (false, "This form is no longer valid");
            }

            if (tokenRecord.ExpiresAt < DateTime.UtcNow)
            {
                LogFormSubmissionAttempt(formType, userId, "BLOCKED", "WEB_FORM", null, null,
                    "Token expired", token, false, true, "Token expired");
                return (false, "Form submission window has expired. Please refresh the page.");
            }

            // Mark token as used
            tokenRecord.IsUsed = true;
            tokenRecord.UsedAt = DateTime.UtcNow;
            tokenRecord.SubmittedFormId = submittedFormId;
            _context.SaveChanges();

            return (true, "Token valid");
        }

        /// <summary>
        /// Log a form submission attempt
        /// </summary>
        public void LogFormSubmissionAttempt(
            string formType,
            int userId,
            string action,
            string source,
            string? patientName = null,
            string? requestorName = null,
            string? reason = null,
            string? submissionToken = null,
            bool hasValidToken = false,
            bool suspiciousActivity = false,
            string? suspiciousReasons = null,
            int? submittedFormId = null,
            bool isDuplicate = false,
            string? duplicateDetails = null,
            string? formDataHash = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            var auditLog = new FormSubmissionAuditLog
            {
                FormType = formType,
                UserId = userId,
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
                PatientName = patientName,
                RequestorName = requestorName,
                Action = action,
                Source = source,
                Reason = reason,
                SubmissionToken = submissionToken,
                HasValidToken = hasValidToken,
                SuspiciousActivity = suspiciousActivity,
                SuspiciousReasons = suspiciousReasons,
                AttemptedAt = DateTime.UtcNow,
                SubmittedFormId = submittedFormId,
                IsDuplicate = isDuplicate,
                DuplicateDetails = duplicateDetails,
                FormDataHash = formDataHash
            };

            _context.FormSubmissionAuditLogs.Add(auditLog);
            _context.SaveChanges();
        }

        /// <summary>
        /// Generate hash of form data for duplicate detection
        /// </summary>
        public string GenerateFormDataHash(object formData)
        {
            var json = JsonSerializer.Serialize(formData);
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(json);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Check for duplicate form submissions
        /// </summary>
        public (bool IsDuplicate, string Details) CheckDuplicateSubmission(
            string formType,
            int userId,
            string formDataHash,
            int minutesWindow = 30)
        {
            // Check if the same form data was submitted recently
            var recentSubmission = _context.FormSubmissionAuditLogs
                .Where(l => l.FormType == formType &&
                           l.UserId == userId &&
                           l.FormDataHash == formDataHash &&
                           l.Action == "SUCCESS" &&
                           l.AttemptedAt > DateTime.UtcNow.AddMinutes(-minutesWindow))
                .OrderByDescending(l => l.AttemptedAt)
                .FirstOrDefault();

            if (recentSubmission != null)
            {
                var details = $"Duplicate form submitted within {minutesWindow} minutes. " +
                             $"Original submission at {recentSubmission.AttemptedAt:yyyy-MM-dd HH:mm:ss}";
                return (true, details);
            }

            return (false, string.Empty);
        }

        /// <summary>
        /// Check for suspicious form submission patterns
        /// </summary>
        public (bool IsSuspicious, string Reasons) DetectSuspiciousSubmission(
            string formType,
            int userId,
            string? token)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return (false, string.Empty);

            var reasons = new List<string>();

            // Check 1: Missing token
            if (string.IsNullOrEmpty(token))
            {
                reasons.Add("Missing submission token (possible direct API call)");
            }

            // Check 2: Rapid submissions from same user
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var recentSubmissions = _context.FormSubmissionAuditLogs
                .Where(l => l.UserId == userId &&
                           l.FormType == formType &&
                           l.AttemptedAt > DateTime.UtcNow.AddMinutes(-5))
                .Count();

            if (recentSubmissions > 3)
            {
                reasons.Add($"Rapid submission attempts: {recentSubmissions} in 5 minutes");
            }

            // Check 3: Check for failed attempts
            var failedAttempts = _context.FormSubmissionAuditLogs
                .Where(l => l.IpAddress == ipAddress &&
                           l.Action == "FAILED" &&
                           l.AttemptedAt > DateTime.UtcNow.AddMinutes(-10))
                .Count();

            if (failedAttempts > 5)
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

        /// <summary>
        /// Revoke a form submission token (e.g., if user navigates away)
        /// </summary>
        public void RevokeToken(string token)
        {
            var tokenRecord = _context.FormSubmissionTokens.FirstOrDefault(t => t.Token == token);
            if (tokenRecord != null && !tokenRecord.IsUsed)
            {
                tokenRecord.IsRevoked = true;
                _context.SaveChanges();
            }
        }
    }
}
