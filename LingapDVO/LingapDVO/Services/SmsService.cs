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

                if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("SMS configuration is missing or incomplete");
                    return false;
                }

                var url = $"{apiUrl}?api_token={apiKey}" +
                          $"&message={Uri.EscapeDataString(message)}" +
                          $"&phone_number={phoneNumber}" +
                          $"&sender_id={senderId}" +
                          $"&sms_provider={defaultProvider}";

                var response = await _httpClient.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to send SMS. Status Code: {response.StatusCode}, Response: {errorDetails}");
                    return false;
                }

                _logger.LogInformation($"SMS sent successfully to {phoneNumber}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while sending SMS to {phoneNumber}");
                return false;
            }
        }
    }
}