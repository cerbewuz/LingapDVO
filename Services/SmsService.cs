    namespace LingapDVO.Services;

    public class SmsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "https://api.iprog.com/api/v1/"; // Using the provided API token
        private readonly string _apiToken = "42f4335a0fa3dde088f8304fe2936f5a8013a2c4";

        public SmsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task SendSmsAsync(string phoneNumber, string message, int smsProvider = 0)
        {
            var url = $"{_apiUrl}?api_token={_apiToken}" +
                      $"&message={Uri.EscapeDataString(message)}" +
                      $"&phone_number={phoneNumber}" +
                      $"&sms_provider={smsProvider}";

            // Send the request
            var response = await _httpClient.PostAsync(url, null); // No body content, just query string

            if (!response.IsSuccessStatusCode)
            {
                // Log the error and response details for debugging
                var errorDetails = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to send SMS. Status Code: {response.StatusCode}, Response: {errorDetails}");
            }

            // Optionally, log success
            Console.WriteLine("SMS sent successfully.");
        }
    }