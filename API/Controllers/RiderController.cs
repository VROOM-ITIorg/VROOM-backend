using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VROOM.Data;
using VROOM.Models;
using VROOM.Models.Dtos;
using VROOM.Repositories;
using VROOM.Services;
using VROOM.ViewModels;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RiderController : ControllerBase
    {
        private readonly VroomDbContext _context;
        private readonly RiderRepository _riderManager;
        private readonly BusinessOwnerService _businessOwnerService;
        private readonly RiderService _riderService;
        private readonly ILogger<RiderController> _logger;

        public RiderController(
            VroomDbContext context,
            RiderRepository riderManager,
            BusinessOwnerService businessOwnerService,
            RiderService riderService,
            IRiderService rideService,
            ILogger<RiderController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _riderManager = riderManager ?? throw new ArgumentNullException(nameof(riderManager));
            _businessOwnerService = businessOwnerService ?? throw new ArgumentNullException(nameof(businessOwnerService));
            _riderService = riderService ?? throw new ArgumentNullException(nameof(riderService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("{riderId}/last-location")]
        public async Task<ActionResult<RiderLocationDto>> GetRiderLastLocation(string riderId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(riderId))
                {
                    _logger.LogWarning("GetRiderLastLocation called with empty riderId");
                    return BadRequest("Rider ID is required.");
                }

                _logger.LogInformation("Fetching last location for rider {RiderId}", riderId);

                var location = await _context.Riders
                    .Where(r => r.UserID == riderId)
                    .OrderByDescending(r => r.Lastupdated)
                    .Select(r => new RiderLocationDto
                    {
                        RiderId = r.UserID,
                        Latitude = r.Lat,
                        Longitude = r.Lang,
                        LastUpdated = r.Lastupdated
                    })
                    .FirstOrDefaultAsync();

                if (location == null)
                {
                    _logger.LogInformation("No location found for rider {RiderId}", riderId);
                    return Ok(null); // Return 200 OK with null
                }

                _logger.LogInformation("Last location found for rider {RiderId}: Lat={Latitude}, Lng={Longitude}, Updated={LastUpdated}",
                    riderId, location.Latitude, location.Longitude, location.LastUpdated);

                return Ok(location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving last location for rider {RiderId}", riderId);
                return StatusCode(500, "An error occurred while retrieving the rider's last location.");
            }
        }

        [HttpGet("assigned/{orderId}")]
        [Authorize(Roles = "Rider")]
        public async Task<IActionResult> ViewAssignedOrder(int orderId)
        {
            if (orderId <= 0)
                return BadRequest(new { message = "Invalid order ID." });

            var result = await _businessOwnerService.ViewAssignedOrderAsync(orderId);

            if (result == null)
                return NotFound(new { message = "Order not found or not assigned to you." });

            return Ok(result);
        }

        [HttpPost("start-delivery")]
        [Authorize(Roles = "Rider")]
        public async Task<IActionResult> StartDelivery([FromQuery] string riderId, [FromBody] List<int> orderIds)
        {
            if (string.IsNullOrEmpty(riderId) || orderIds == null || !orderIds.Any())
                return BadRequest(new { message = "Invalid rider ID or order IDs." });

            var result = await _riderService.StartDeliveriesAsync(riderId, orderIds);
            if (result == null)
                return BadRequest(new { message = "Unable to start delivery. Check rider status or order state." });

            return Ok(result);
        }

        [HttpPost("update-delivery-status")]
        [Authorize(Roles = "Rider")]
        public Task<Order> UpdateDeliveryStatusAsync(string riderId, int orderId, OrderStateEnum newState)
        {
            try
            {
                _logger.LogInformation("Executing UpdateDeliveryStatusAsync for OrderId={OrderId}, RiderId={RiderId}, NewState={NewState}",
                    orderId, riderId, newState);

                var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);
                if (order == null)
                {
                    _logger.LogWarning("Order not found for OrderId={OrderId}", orderId);
                    throw new InvalidOperationException($"Order with ID {orderId} not found.");
                }

                if (order.RiderID != riderId)
                {
                    _logger.LogWarning("Rider mismatch for OrderId={OrderId}, ExpectedRiderId={ExpectedRiderId}, ProvidedRiderId={ProvidedRiderId}",
                        orderId, order.RiderID, riderId);
                    throw new InvalidOperationException("Rider is not assigned to this order.");
                }

                order.State = newState;
                order.ModifiedAt = DateTime.UtcNow;

                _logger.LogInformation("Saving changes for OrderId={OrderId}", orderId);
                _context.SaveChanges();

                _logger.LogInformation("Successfully updated OrderId={OrderId} to State={NewState}", orderId, newState);
                return Task.FromResult(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update delivery status for OrderId={OrderId}", orderId);
                throw;
            }
        }

        [HttpGet("AllRiders")]
        [Authorize(Roles = "Admin,BusinessOwner")]
        public IActionResult GetAllRidersWithFilter(
              [FromQuery] int status = -1,
              [FromQuery] string name = "",
              [FromQuery] string phoneNumber = "",
              [FromQuery] int pageNumber = 1,
              [FromQuery] int pageSize = 4,
              [FromQuery] string sort = "name_asc",
              [FromQuery] string owner = "All")
        {
            try
            {
                var riders = _riderManager.Search(status, name, phoneNumber, pageNumber, pageSize, sort, owner);

                return Ok(riders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve riders");
                return StatusCode(500, new { message = "An error occurred while retrieving riders.", error = ex.Message });
            }
        }
        [HttpGet("avaliableRiders")]
        [Authorize(Roles = "Admin,BusinessOwner")]
        public async Task<IActionResult> GetAllAvaliableRiders()
        {
            try
            {
                var authorizationHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "Invalid or missing Authorization header." });
                }

                var token = authorizationHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Extract the business ID from the nameidentifier claim
                var businessId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(businessId))
                {
                    return BadRequest(new { message = "Business ID not found in token." });
                }

                // Get available riders from the repository
                var riders = await _riderManager.GetAvaliableRiders(businessId);

                // Map riders to RiderDto to control the output
                var riderDtos = riders.Select(r => new
                {
                    Id = r.User.Id,
                    Name = r.User.Name,
                    Email = r.User.Email,
                    ProfilePicture = r.User.ProfilePicture
                    // Add Status = r.Status if available in the Rider model
                }).ToList();

                return Ok(riderDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving riders.", error = ex.Message });
            }
        }

        [HttpGet("riderShipment")]
        [Authorize(Roles = "Rider")]
        public async Task<IActionResult> GetRiderShipments()
        {
            try
            {
                var authorizationHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "Invalid or missing Authorization header." });
                }

                var token = authorizationHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Extract the business ID from the nameidentifier claim
                var riderId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(riderId))
                {
                    return BadRequest(new { message = "Business ID not found in token." });
                }

                var ridersShipments = await _riderManager.GetRiderShipments(riderId);

                return Ok(ridersShipments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving riders.", error = ex.Message });
            }
        }

    }
}
