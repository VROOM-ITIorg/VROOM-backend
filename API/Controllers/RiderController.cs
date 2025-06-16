using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using VROOM.Data;
using VROOM.Models;
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

        public RiderController(
            VroomDbContext context,
            RiderRepository riderManager,
            BusinessOwnerService businessOwnerService,
            RiderService riderService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _riderManager = riderManager ?? throw new ArgumentNullException(nameof(riderManager));
            _businessOwnerService = businessOwnerService ?? throw new ArgumentNullException(nameof(businessOwnerService));
            _riderService = riderService ?? throw new ArgumentNullException(nameof(riderService));
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
                return StatusCode(500, new { message = "An error occurred while updating delivery status.", error = ex.Message });
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
