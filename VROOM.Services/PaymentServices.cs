using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace VROOM.Services
{
    public class PayPalService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public PayPalService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        private async Task<string> GetAccessToken()
        {
            var clientId = _configuration["PayPal:ClientId"];
            var clientSecret = _configuration["PayPal:ClientSecret"];
            var mode = _configuration["PayPal:Mode"];

            var authString = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authString}");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _httpClient.PostAsync(
                mode == "sandbox" ? "https://api-m.sandbox.paypal.com/v1/oauth2/token" : "https://api-m.paypal.com/v1/oauth2/token",
                content);

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<JsonElement>(json);
            return token.GetProperty("access_token").GetString();
        }

        public async Task<string> CreatePayment(string amount, string currency, string description)
        {
            var accessToken = await GetAccessToken();
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var paymentData = new
            {
                intent = "sale",
                payer = new { payment_method = "paypal" },
                transactions = new[]
                {
                    new
                    {
                        amount = new { total = amount, currency = currency },
                        description = description
                    }
                },
                redirect_urls = new
                {
                    return_url = $"{_configuration["AppBaseUrl"]}/Payment/Success",
                    cancel_url = $"{_configuration["AppBaseUrl"]}/Payment/Cancel"
                }
            };

            var jsonContent = JsonSerializer.Serialize(paymentData);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                _configuration["PayPal:Mode"] == "sandbox" ? "https://api-m.sandbox.paypal.com/v1/payments/payment" : "https://api-m.paypal.com/v1/payments/payment",
                content);

            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var payment = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
            return payment.GetProperty("links").EnumerateArray()
                .FirstOrDefault(x => x.GetProperty("rel").GetString() == "approval_url").GetProperty("href").GetString();
        }

        public async Task<bool> ExecutePayment(string paymentId, string payerId)
        {
            var accessToken = await GetAccessToken();
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var executionData = new
            {
                payer_id = payerId
            };

            var jsonContent = JsonSerializer.Serialize(executionData);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                (_configuration["PayPal:Mode"] == "sandbox" ? "https://api-m.sandbox.paypal.com" : "https://api-m.paypal.com") + $"/v1/payments/payment/{paymentId}/execute",
                content);

            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var payment = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
            return payment.GetProperty("state").GetString().ToLower() == "approved";
        }
    }
}