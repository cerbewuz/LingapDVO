using Microsoft.Extensions.Configuration;

namespace LingapDVO.Services
{
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
    }

    public class SmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;

        public SmsService(HttpClient httpClient, IConfiguration configuration, ILogger<SmsService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                var apiUrl = _configuration["SMSSettings:ApiUrl"];
                var apiKey = _configuration["SMSSettings:ApiKey"];
                var senderId = _configuration["SMSSettings:SenderId"];
                var defaultProvider = _configuration.GetValue<int>("SMSSettings:DefaultProvider", 0);

                _logger.LogInformation($"📱 SMS Service - Starting SMS send process");
                _logger.LogInformation($"   Phone: {phoneNumber}");
                _logger.LogInformation($"   API URL: {apiUrl}");
                _logger.LogInformation($"   Sender ID: {senderId}");
                _logger.LogInformation($"   Provider: {defaultProvider}");

                if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("❌ SMS configuration is missing or incomplete");
                    _logger.LogError($"   ApiUrl present: {!string.IsNullOrEmpty(apiUrl)}");
                    _logger.LogError($"   ApiKey present: {!string.IsNullOrEmpty(apiKey)}");
                    return false;
                }

                // Normalize phone number - iProg accepts: 639XXXXXXXXX or 09XXXXXXXXX
                string normalizedPhone = phoneNumber;
                if (phoneNumber.StartsWith("0"))
                {
                    normalizedPhone = "63" + phoneNumber.Substring(1);
                }
                else if (!phoneNumber.StartsWith("63"))
                {
                    normalizedPhone = "63" + phoneNumber;
                }

                _logger.LogInformation($"   Normalized phone: {normalizedPhone}");

                // Build query string according to iProg API documentation
                var url = $"{apiUrl}?api_token={apiKey}" +
                          $"&phone_number={normalizedPhone}" +
                          $"&message={Uri.EscapeDataString(message)}" +
                          $"&sms_provider={defaultProvider}";

                // Add sender_id if configured
                if (!string.IsNullOrEmpty(senderId))
                {
                    url += $"&sender_id={Uri.EscapeDataString(senderId)}";
                }

                _logger.LogInformation($"   Sending POST request to iProg API...");
                _logger.LogInformation($"   Full URL (without token): {apiUrl}?phone_number={normalizedPhone}&message=[message]&sms_provider={defaultProvider}&sender_id={senderId}");

                var response = await _httpClient.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Failed to send SMS. Status Code: {response.StatusCode}");
                    _logger.LogError($"   Response: {errorDetails}");
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"✅ SMS sent successfully to {normalizedPhone}");
                _logger.LogInformation($"   API Response: {responseContent}");

                // Check if response indicates success
                if (responseContent.Contains("\"status\":200") || responseContent.Contains("successfully"))
                {
                    return true;
                }
                else
                {
                    _logger.LogWarning($"⚠️ SMS API returned success status code but unexpected response: {responseContent}");
                    return true; 
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Exception occurred while sending SMS to {phoneNumber}");
                _logger.LogError($"   Exception Message: {ex.Message}");
                _logger.LogError($"   Stack Trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}