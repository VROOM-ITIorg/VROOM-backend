using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
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
        private readonly ShipmentServices _shipmentService;
        private readonly RiderService _riderService;
        private readonly ILogger<RiderController> _logger;

        public RiderController(
            VroomDbContext context,
            RiderRepository riderManager,
            BusinessOwnerService businessOwnerService,
            RiderService riderService,
            ShipmentServices shipmentService,
            IRiderService rideService,
            ILogger<RiderController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _riderManager = riderManager ?? throw new ArgumentNullException(nameof(riderManager));
            _businessOwnerService = businessOwnerService ?? throw new ArgumentNullException(nameof(businessOwnerService));
            _shipmentService = shipmentService ?? throw new ArgumentNullException(nameof(shipmentService));
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


        // Retrieve rider profile data
        [HttpGet("{riderId}")]
        [Authorize(Roles = "Rider")]
        public async Task<IActionResult> GetRiderProfile(string riderId)
        {
            try
            {
                // Verify that riderId matches the authenticated user
                var currentRiderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (currentRiderId != riderId)
                    return Unauthorized(new { error = "You are not authorized to access this rider's data." });

                // Fetch rider data
                var rider = await _riderService.GetRiderProfileAsync(riderId);

                // Fetch shipment and order statistics
                var shipments = await _context.Shipments
                    .Where(s => s.RiderID == riderId && !s.IsDeleted)
                    .ToListAsync();
                var orders = await _context.Orders
                    .Where(o => o.RiderID == riderId && !o.IsDeleted)
                    .ToListAsync();

                // Fetch the latest 3 feedbacks
                var feedbacks = await _context.Feedbacks
                    .Where(f => f.RiderID == riderId && !f.IsDeleted)
                    .OrderByDescending(f => f.ModifiedAt)
                    .Take(3)
                    .Select(f => new
                    {
                        Comment = f.Message,
                        f.Rating,
                        f.ModifiedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    id = rider.UserID,
                    name = rider.User?.Name,
                    email = rider.User?.Email,
                    phone = rider.User?.PhoneNumber,
                    status = rider.Status.ToString(),
                    vehicleType = rider.VehicleType.ToString(),
                    vehicleStatus = rider.VehicleStatus,
                    location = new
                    {
                        latitude = rider.Lat,
                        longitude = rider.Lang, // Using Lang
                        area = rider.Area
                    },
                    experienceLevel = rider.ExperienceLevel,
                    rating = rider.Rating,
                    feedbacks,
                    stats = new
                    {
                        assignedShipments = shipments.Count(s => s.ShipmentState == ShipmentStateEnum.Assigned),
                        completedShipments = shipments?.Count(s => s.ShipmentState == ShipmentStateEnum.Delivered) ?? 0,
                        assignedOrders = orders?.Count(o => o.State == OrderStateEnum.Pending || o.State == OrderStateEnum.Confirmed) ?? 0,
                        completedOrders = orders?.Count(o => o.State == OrderStateEnum.Delivered) ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving rider data.", details = ex.Message });
            }
        }

        // Update rider profile data
        [HttpPut("{riderId}")]
        [Authorize(Roles = "Rider")]
        public async Task<IActionResult> UpdateRiderProfile(string riderId, [FromBody] UpdateRiderDto dto)
        {
            try
            {
                // Verify that riderId matches the authenticated user
                var currentRiderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (currentRiderId != riderId)
                    return Unauthorized(new { error = "You are not authorized to update this rider's data." });

                // Retrieve rider with user data
                var rider = await _context.Riders
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.UserID == riderId);

                if (rider == null)
                    return NotFound(new { error = "The rider does not exist." });

                // Validate the data
                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
                {
                    var errors = validationResults.Select(vr => vr.ErrorMessage).ToList();
                    return BadRequest(new { error = "Invalid data.", errors });
                }

                // Update fields
                var modifiedAt = DateTime.UtcNow;
                bool isModified = false;

                if (!string.IsNullOrEmpty(dto.Name))
                {
                    rider.User.Name = dto.Name;
                    rider.User.ModifiedBy = riderId;
                    rider.User.ModifiedAt = modifiedAt;
                    isModified = true;
                }
                if (!string.IsNullOrEmpty(dto.Email))
                {
                    rider.User.Email = dto.Email;
                    rider.User.ModifiedBy = riderId;
                    rider.User.ModifiedAt = modifiedAt;
                    isModified = true;
                }
                if (!string.IsNullOrEmpty(dto.Phone))
                {
                    rider.User.PhoneNumber = dto.Phone;
                    rider.User.ModifiedBy = riderId;
                    rider.User.ModifiedAt = modifiedAt;
                    isModified = true;
                }
                if (dto.Lat.HasValue)
                {
                    rider.Lat = dto.Lat.Value;
                    isModified = true;
                }
                if (dto.Lang.HasValue)
                {
                    rider.Lang = dto.Lang.Value; // Using Lang
                    isModified = true;
                }
                if (!string.IsNullOrEmpty(dto.Area))
                {
                    rider.Area = dto.Area;
                    isModified = true;
                }

                // Save changes if any field was modified
                if (isModified)
                {
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "Rider data updated successfully." });
                }

                return Ok(new { message = "No changes were made." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating rider data.", details = ex.Message });
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
        public async Task<IActionResult> StartDelivery([FromQuery] int shipmentId)
        {
            if (shipmentId == null)
                return BadRequest(new { error = "Invalid rider ID or order IDs." });

            var result = await _shipmentService.StartShipment(shipmentId);

            if (result == null)
                return BadRequest(new { error = "Unable to start SHIPMENT. Check rider status or order state." });

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
