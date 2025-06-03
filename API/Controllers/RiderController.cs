using System;
using System.Collections.Generic;
using System.Linq;
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
        public async Task<IActionResult> UpdateDeliveryStatus(
            [FromQuery] string riderId,
            [FromQuery] int orderId,
            [FromQuery] OrderStateEnum newState)
        {
            if (string.IsNullOrEmpty(riderId) || orderId <= 0)
                return BadRequest(new { message = "Invalid rider ID or order ID." });

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
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update delivery status for OrderId={OrderId}", orderId);
                return StatusCode(500, new { message = "An error occurred while updating delivery status.", error = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,BusinessOwner")]
        public IActionResult Index(
            [FromQuery] string name = "",
            [FromQuery] string phoneNumber = "",
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 4)
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
    }
}
