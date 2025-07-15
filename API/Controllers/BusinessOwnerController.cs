using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using ViewModels.Order;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Services;
using VROOM.ViewModels;
using Microsoft.AspNetCore.Identity;
using ViewModels.User;
using static VROOM.Services.BusinessOwnerService;


namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusinessOwnerController : Controller
    {
        private readonly BusinessOwnerService _businessOwnerService;
        private readonly BusinessOwnerRepository _businessOwnerRepository;
        private readonly UserService _userService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BusinessOwnerController(
            BusinessOwnerService businessOwnerService,
            BusinessOwnerRepository businessOwnerRepository,
            UserService userService,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _businessOwnerService = businessOwnerService;
            _businessOwnerRepository = businessOwnerRepository;
            _userService = userService;
        }

        // GET: api/BusinessOwner/Profile
        [Authorize(Roles = "BusinessOwner")]
        [HttpGet("Profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var currentOwnerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(currentOwnerId))
                {
                    return Unauthorized(new { error = "You are not authorized to access this profile." });
                }

                var result = await _businessOwnerService.GetProfileAsync(currentOwnerId);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Error });
                }

                return Ok(result.Value);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An unexpected error occurred while retrieving the profile." });
            }
        }
        // PUT: api/BusinessOwner/Profile
[Authorize(Roles = "BusinessOwner")]
[HttpPut("Profile")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> UpdateProfile([FromForm] BusinessOwnerProfileVM model)
{
    try
    {
        var currentOwnerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(currentOwnerId))
        {
            return Unauthorized(new { error = "You are not authorized to update this profile." });
        }

        if (model == null)
        {
            return BadRequest(new { error = "Profile data is required." });
        }

        var result = await _businessOwnerService.UpdateProfileAsync(currentOwnerId, model);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { message = "Profile updated successfully." });
    }
    catch (Exception)
    {
        return StatusCode(500, new { error = "An unexpected error occurred while updating the profile." });
    }
}
        [HttpPost("register")]
        public async Task<IActionResult> RegisterBusinessOwner([FromBody] BusinessOwnerRegisterRequest request)
        {
            var result = await _businessOwnerService.CreateBusinessOwnerAsync(request);
            if (!result.IsSuccess)
                return BadRequest();

            return Ok(result.Value);
        }

        [HttpPost("changePassword")]
        public async Task<IActionResult> ChangeRiderPassword(string riderId, string newPassword)
        {
            var result = await _businessOwnerService.ChangeRiderPasswordAsync(riderId, newPassword);
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }
            return Ok(result.Value);
        }

        [HttpPost("registerRider")]
        public async Task<IActionResult> RegisterRider([FromBody] RiderRegisterRequest rider)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            // Decode the JWT token
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Access token claims (e.g., user ID, roles, etc.)
            var BussinsId = jwtToken.Claims.FirstOrDefault()?.Value;

            var result = await _businessOwnerService.CreateRiderAsync(rider, BussinsId);
            return Ok(result);
        }

        [HttpPut("updateRider/{riderUserId}")]
        public async Task<IActionResult> UpdateRider(string riderUserId, [FromBody] RiderUpdateRequest rider)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                return Unauthorized("Invalid or missing authorization token.");
            }

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var businessId = jwtToken.Claims.FirstOrDefault()?.Value;
            if (string.IsNullOrEmpty(businessId))
            {
                return Unauthorized("Invalid token: Business ID not found.");
            }

            var result = await _businessOwnerService.UpdateRiderAsync(rider, businessId, riderUserId);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }

        [Authorize(Roles = "BusinessOwner")]
        [HttpPost("assignOrderManually")]
        public async Task<IActionResult> AssignOrderToRider([FromBody] AssignOrderToRiderRequest request)
        {
            if (request == null || request.OrderId <= 0 || string.IsNullOrEmpty(request.RiderId))
            {
                return BadRequest(new { message = "Invalid order or rider details." });
            }

            var success = await _businessOwnerService.AssignShipmentToRiderAsync(request.OrderId, request.RiderId);
            if (!success)
            {
                return Ok(new { message = "Unable to assign the order to the rider. Please check the details." });
            }

            return Ok(new { message = "Order successfully assigned to the rider." });
        }

        [Authorize(Roles = "BusinessOwner")]
        [HttpPost("assignOrderAutomatically")]
        public async Task<IActionResult> AssignOrderAutomatically([FromBody] OrderCreateViewModel model)
        {
            var result = await _businessOwnerService.PrepareOrder(model);
            if (!result)
            {
                return BadRequest(new { message = $"Error Occured While Assigning the Rider with Id {model.RiderID}" });
            }

            return Ok(new { message = "Rider Assigned Successfully! " });
        }

        [HttpGet("riders")]
        public async Task<IActionResult> GetRiders()
        {
            var result = await _businessOwnerService.GetRiders();
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }
            return Ok(result);
        }

        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
        {
            var result = await _businessOwnerService.GetCustomers();
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }
            return Ok(result.Value);
        }

        [HttpPost("create-customer")]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerRegisterRequest request)
        {
            var result = await _businessOwnerService.CreateCustomerAsync(request);
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }
            return Ok(result.Value);
        }

        [HttpGet("all-customers")]
        public async Task<IActionResult> GetAllCustomers()
        {
            var result = await _businessOwnerService.GetAllCustomers();
            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }
            return Ok(result.Value);
        }

        [HttpPost("create-order-with-assignment")]
        public async Task<IActionResult> CreateOrderAndAssign([FromBody] CreateOrderWithAssignmentRequest request)
        {
            if (request == null || request.Order == null)
            {
                return BadRequest(new { error = "Request body is required and must include order details." });
            }

            var result = await _businessOwnerService.CreateOrderAndAssignAsync(request);
            if (!result.IsSuccess)
            {
                return Ok(new { error = result.Error });
            }

            return Ok(new { message = result.Value });
        }
        public class AssignOrderToRiderRequest
        {
            public int OrderId { get; set; }
            public string RiderId { get; set; }
        }

        public class RespondToOrderRequest
        {
            public int OrderId { get; set; }
            public string RiderId { get; set; }
            public bool IsAccepted { get; set; }
        }

        public class AssignOrderAutomaticallyRequest
        {
            public string BusinessOwnerId { get; set; }
            public int OrderId { get; set; }
        }


        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
        {
            try
            {
                // Extract the authenticated user's ID from JWT claims
                var ownerUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(ownerUserId))
                {
                    return Unauthorized(new { Message = "User not authenticated." });
                }

                var stats = await _businessOwnerService.GetDashboardStatsAsync(ownerUserId);
                return Ok(stats);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while fetching dashboard stats.", Error = ex.Message });
            }
        }
    }
}