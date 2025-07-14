using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;
using ViewModels.Feedback;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Web;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly FeedbackRepository _feedbackRepository;
        private readonly IConfiguration _configuration;
        private readonly VroomDbContext _context;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(
            FeedbackRepository feedbackRepository,
            IConfiguration configuration,
            VroomDbContext context,
            ILogger<CustomerController> logger)
        {
            _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("login")]
        [AllowAnonymous] // Allow token-based login without prior auth
        public IActionResult AutoLogin([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { error = "Token is required." });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = System.Text.Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key"));

            try
            {
                var principal = tokenHandler.ValidateToken(token, new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer"),
                    ValidAudience = _configuration["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience"),
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key)
                }, out _);

                var identity = (ClaimsIdentity)principal.Identity;
                var customerId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = identity.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(customerId) || role != "Customer")
                {
                    return Unauthorized(new { error = "Invalid token claims." });
                }

                // Generate a new JWT token instead of using cookies
                var newTokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, customerId),
                        new Claim(ClaimTypes.Role, role)
                    }),
                    Expires = DateTime.UtcNow.AddHours(1), // Token valid for 1 hour
                    Issuer = _configuration["Jwt:Issuer"] ?? "VROOM",
                    Audience = _configuration["Jwt:Audience"] ?? "VROOM",
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256)
                };
                var newToken = tokenHandler.CreateToken(newTokenDescriptor);
                var newTokenString = tokenHandler.WriteToken(newToken);

                return Ok(new { message = "Logged in successfully", customerId, token = newTokenString });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-login failed for token validation");
                return Unauthorized(new { error = $"Invalid token: {ex.Message}" });
            }
        }

        [HttpPost("feedback")]
        [Authorize(Roles = "Customer")] // Requires customer to be logged in
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackRequest feedbackRequest)
        {
            if (feedbackRequest == null || string.IsNullOrEmpty(feedbackRequest.RiderId))
            {
                return BadRequest(new { error = "Feedback request or RiderId is required." });
            }

            var customerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerId))
            {
                return Unauthorized(new { error = "Customer ID not found in token." });
            }

            // Validate that the logged-in customer matches the feedback request
            //if (customerId != feedbackRequest.CustomerId)
            //{
            //    return Unauthorized(new { error = "You are not authorized to submit feedback for this customer." });
            //}

            var success = await _feedbackRepository.AddFeedbackAsync(customerId, feedbackRequest);
            if (!success)
            {
                return BadRequest(new { error = "Feedback already exists for this rider or failed to save." });
            }

            return Ok(new { message = "Feedback submitted successfully." });
        }

        [HttpGet("order/{id}")]
        //[Authorize(Roles = "Customer")] // Restricts access to authenticated customers
        public async Task<IActionResult> GetOrderById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { error = "Invalid order ID." });
                }

                var order = await _context.Orders
                    .Include(o => o.Customer) // Include Customer to access Name
                    .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

                if (order == null)
                {
                    return NotFound(new { error = "Order not found." });
                }

                // Check if the order is in a state eligible for feedback (e.g., Delivered)
                if (order.State != OrderStateEnum.Delivered)
                {
                    return BadRequest(new { error = "Order is not eligible for feedback." });
                }

                // Validate that the logged-in customer matches the order's customer
                //var customerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                //if (customerId != order.CustomerID)
                //{
                //    return Unauthorized(new { error = "You are not authorized to view this order." });
                //}

                // Response with necessary information for feedback frontend, including customer name
                var response = new
                {
                    orderId = order.Id,
                    customerId = order.CustomerID,
                    customerName = order.Customer?.User.Name ?? order.Customer.User.Name ?? "Unknown", // Fallback for name
                    riderId = order.RiderID,
                    title = order.Title,
                    state = order.State.ToString(),
                    orderPrice = order.OrderPrice,
                    deliveryPrice = order.DeliveryPrice,
                    orderDate = order.Date,
                    lastUpdated = order.ModifiedAt,
                    feedbackUrl = $"http://localhost:4200/customer/feedback?orderId={order.Id}&customerId={order.CustomerID}&riderId={order.RiderID}" // Base URL, token to be handled by frontend
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order with ID {OrderId}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the order.", details = ex.Message });
            }
        }
    }
}