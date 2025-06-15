using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VROOM.Data;
using VROOM.Models;
using VROOM.Models.Dtos;
using VROOM.Repositories;
using VROOM.Services;
using System.ComponentModel.DataAnnotations;

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
                return BadRequest(new { error = "Invalid order ID." });

            var result = await _businessOwnerService.ViewAssignedOrderAsync(orderId);

            if (result == null)
                return NotFound(new { error = "Order not found or not assigned to you." });

            return Ok(result);
        }

        [HttpPost("start-delivery")]
        [Authorize(Roles = "Rider")]
        public async Task<IActionResult> StartDelivery([FromQuery] string riderId, [FromBody] List<int> orderIds)
        {
            if (string.IsNullOrEmpty(riderId) || orderIds == null || !orderIds.Any())
                return BadRequest(new { error = "Invalid rider ID or order IDs." });

            var result = await _riderService.StartDeliveriesAsync(riderId, orderIds);
            if (result == null)
                return BadRequest(new { error = "Unable to start delivery. Check rider status or order state." });

            return Ok(result);
        }

        [HttpPost("update-delivery-status")]
        [Authorize(Roles = "Rider")]
        public Task<Order> UpdateDeliveryStatusAsync(string riderId, int orderId, OrderStateEnum newState)
        {
            if (string.IsNullOrEmpty(riderId) || orderId <= 0)
                return BadRequest(new { error = "Invalid rider ID or order ID." });

            try
            {
                var updatedOrder = await _riderService.UpdateDeliveryStatusAsync(riderId, orderId, newState);
                return Ok(new
                {
                    message = $"Order delivery status successfully updated to {newState}.",
                    order = updatedOrder
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating delivery status.", details = ex.Message });
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
                var riders = _riderManager.Search(
                    Name: name,
                    PhoneNumber: phoneNumber,
                    pageNumber: pageNumber,
                    pageSize: pageSize,
                    status: -1);

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
                return StatusCode(500, new { error = "An error occurred while retrieving riders.", details = ex.Message });
            }
        }
    }

    // DTO model for updating rider data
    public class UpdateRiderDto
    {
        [StringLength(100, ErrorMessage = "The name cannot exceed 100 characters.")]
        public string Name { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number.")]
        public string Phone { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double? Lat { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double? Lang { get; set; } // Using Lang

        [StringLength(200, ErrorMessage = "The area cannot exceed 200 characters.")]
        public string Area { get; set; }
    }
}
