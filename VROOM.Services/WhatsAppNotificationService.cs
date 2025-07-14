// VROOM.Services/WhatsAppNotificationService.cs
using Microsoft.Extensions.Configuration;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using VROOM.Models;
using Microsoft.Extensions.Logging;
using System.Web;
using ViewModels.Order;

namespace VROOM.Services
{
    public class WhatsAppNotificationService : IWhatsAppNotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppNotificationService> _logger;
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromNumber;

        public WhatsAppNotificationService(IConfiguration configuration, ILogger<WhatsAppNotificationService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _accountSid = _configuration["Twilio:AccountSid"] ?? throw new ArgumentNullException("Twilio:AccountSid");
            _authToken = _configuration["Twilio:AuthToken"] ?? throw new ArgumentNullException("Twilio:AuthToken");
            _fromNumber = _configuration["Twilio:WhatsAppFromNumber"] ?? throw new ArgumentNullException("Twilio:WhatsAppFromNumber");

            // Initialize the Twilio client
            TwilioClient.Init(_accountSid, _authToken);
        }

        public async Task<bool> SendFeedbackRequestAsync(Order order)
        {
            if (order?.CustomerID == null || string.IsNullOrEmpty(order.RiderID))
            {
                _logger.LogWarning("Cannot send feedback request for Order ID {OrderId} due to missing data.", order?.Id);
                return false;
            }

            try
            {
                // Generate the secure feedback URL with JWT token
                var feedbackUrl = GenerateSecureFeedbackUrl(order.Id, order.CustomerID, order.RiderID);

                // Construct the message body
                var messageBody = $"Hi {order.Customer.User.Name}! 👋 Your order \"{order.Title}\" has been delivered. Click to provide feedback: {feedbackUrl}";

                // Create the WhatsApp message
                var messageOptions = new CreateMessageOptions(new PhoneNumber($"whatsapp:{order.Customer.User.PhoneNumber}"))
                {
                    From = new PhoneNumber(_fromNumber),
                    Body = messageBody
                };

                var message = await MessageResource.CreateAsync(messageOptions);
                _logger.LogInformation("WhatsApp feedback request sent successfully to {PhoneNumber}. Message SID: {MessageSid}", order.Customer.User.PhoneNumber, message.Sid);

                return message.Sid != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp message for Order ID {OrderId}.", order.Id);
                return false;
            }
        }

        private string GenerateSecureFeedbackUrl(int orderId, string customerId, string riderId)
        {
            // Generate JWT token
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key"));
            var issuer = _configuration["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer");
            var audience = _configuration["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience");
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "30");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, customerId),
                new Claim(ClaimTypes.Role, "Customer"),
                new Claim("orderId", orderId.ToString()),
                new Claim("riderId", riderId)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Construct the feedback URL
            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "https://your-domain.com/feedback";
            var queryParams = $"token={HttpUtility.UrlEncode(tokenString)}&orderId={HttpUtility.UrlEncode(orderId.ToString())}&customerId={HttpUtility.UrlEncode(customerId)}&riderId={HttpUtility.UrlEncode(riderId)}";
            return $"{frontendBaseUrl}?{queryParams}";
        }
    }
}