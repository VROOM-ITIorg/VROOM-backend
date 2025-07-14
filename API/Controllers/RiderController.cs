using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VROOM.Data;
using VROOM.Models;
using VROOM.Models.Dtos;
using VROOM.Repositories;
using VROOM.Services;
using ViewModels.Order;
using System.Linq;
using VROOM.ViewModels;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RiderController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly VroomDbContext _context;
        private readonly RiderRepository _riderManager;
        private readonly BusinessOwnerService _businessOwnerService;
        private readonly ShipmentServices _shipmentService;
        private readonly RiderService _riderService;
        private readonly ILogger<WhatsAppNotificationService> _logger; // Changed to match injection
        private readonly IWhatsAppNotificationService _whatsAppNotificationService; // Injected service

        public RiderController(
            VroomDbContext context,
            RiderRepository riderManager,
            BusinessOwnerService businessOwnerService,
            RiderService riderService,
            ShipmentServices shipmentService,
            IRiderService rideService,
            ILogger<WhatsAppNotificationService> logger,
            IConfiguration configuration, // Added IConfiguration parameter
            IWhatsAppNotificationService whatsAppNotificationService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _riderManager = riderManager ?? throw new ArgumentNullException(nameof(riderManager));
            _businessOwnerService = businessOwnerService ?? throw new ArgumentNullException(nameof(businessOwnerService));
            _shipmentService = shipmentService ?? throw new ArgumentNullException(nameof(shipmentService));
            _riderService = riderService ?? throw new ArgumentNullException(nameof(riderService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _whatsAppNotificationService = whatsAppNotificationService ?? throw new ArgumentNullException(nameof(whatsAppNotificationService));
        }

        [HttpGet("{riderId}")]
        [Authorize(Roles = "Rider")]
        public async Task<IActionResult> GetRiderProfile(string riderId)
        {
            try
            {
                var currentRiderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (currentRiderId != riderId)
                    return Unauthorized(new { error = "You are not authorized to access this rider's data." });

                var rider = await _riderService.GetRiderProfileAsync(riderId);
                var shipments = await _context.Shipments
                    .Where(s => s.RiderID == riderId && !s.IsDeleted)
                    .ToListAsync();
                var orders = await _context.Orders
                    .Where(o => o.RiderID == riderId && !o.IsDeleted)
                    .ToListAsync();
                var feedbacks = await _context.Feedbacks
                    .Where(f => f.RiderID == riderId && !f.IsDeleted)
                    .OrderByDescending(f => f.ModifiedAt)
                    .Take(3)
                    .Select(f => new { Comment = f.Message, f.Rating, f.ModifiedAt })
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
                    location = new { latitude = rider.Lat, longitude = rider.Lang, area = rider.Area },
                    experienceLevel = rider.ExperienceLevel,
                    rating = rider.Rating,
                    feedbacks,
                    stats = new
                    {
                        assignedShipments = shipments.Count(s => s.ShipmentState == ShipmentStateEnum.Assigned),
                        completedShipments = shipments?.Count(s => s.ShipmentState == ShipmentStateEnum.Delivered) ?? 0,
                        assignedOrders = orders?.Count(o => o.State == OrderStateEnum.Pending || o.State == OrderStateEnum.Confirmed) ?? 0,
                        completedOrders = orders?.Count(o => o.State == OrderStateEnum.Delivered) ?? 0
                    },
                    profilePicture = rider.User?.ProfilePicture
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving rider data.", details = ex.Message });
            }
        }

        [HttpPut("{riderId}")]
        [Authorize(Roles = "Rider")]
        public async Task<IActionResult> UpdateRiderProfile(string riderId, [FromForm] UpdateRiderDto dto, [FromForm] IFormFile? profilePicture)
        {
            try
            {
                var currentRiderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (currentRiderId != riderId)
                    return Unauthorized(new { error = "You are not authorized to update this rider's data." });

                var rider = await _context.Riders
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.UserID == riderId && !r.User.IsDeleted);

                if (rider == null)
                    return NotFound(new { error = "The rider does not exist." });

                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
                {
                    var errors = validationResults.Select(vr => vr.ErrorMessage).ToList();
                    return BadRequest(new { error = "Invalid data.", errors });
                }

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
                    rider.User.UserName = dto.Email; // ????? UserName ?? IdentityUser
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

                // Handle profile picture
                if (profilePicture != null)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(profilePicture.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return BadRequest(new { error = "Only JPG, JPEG, and PNG images are allowed." });
                    }

                    var maxSizeInBytes = 5 * 1024 * 1024; // 5 ????
                    if (profilePicture.Length > maxSizeInBytes)
                    {
                        return BadRequest(new { error = "Image size must be less than 5MB." });
                    }

                    var fileName = $"{riderId}_{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine("wwwroot/images", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(stream);
                    }
                    rider.User.ProfilePicture = $"/images/{fileName}";
                    rider.User.ModifiedBy = riderId;
                    rider.User.ModifiedAt = modifiedAt;
                    isModified = true;
                }

                if (isModified)
                {
                    await _context.SaveChangesAsync();
                    var riderVM = new RiderVM
                    {
                        UserID = rider.UserID,
                        Name = rider.User?.Name,
                        Email = rider.User?.Email,
                        phoneNumber = rider.User?.PhoneNumber,
                        BusinessID = rider.BusinessID,
                        VehicleType = rider.VehicleType,
                        VehicleStatus = rider.VehicleStatus,
                        ExperienceLevel = rider.ExperienceLevel,
                        Location = new LocationDto
                        {
                            Lat = rider.Lat,
                            Lang = rider.Lang,
                            Area = rider.Area
                        },
                        Status = rider.Status,
                        ProfilePicture = rider.User?.ProfilePicture
                    };
                    return Ok(new { message = "Rider data updated successfully.", data = riderVM });
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
        public async Task<Order> UpdateDeliveryStatusAsync(string riderId, int orderId, OrderStateEnum newState)
        {
            try
            {
                //_logger.LogInformation("Executing UpdateDeliveryStatusAsync for OrderId={OrderId}, RiderId={RiderId}, NewState={NewState}",
                //    orderId, riderId, newState);

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

                //_logger.LogInformation("Saving changes for OrderId={OrderId}", orderId);
                _context.SaveChanges();

                await _whatsAppNotificationService.SendFeedbackRequestAsync(order);
                //_logger.LogInformation("Successfully updated OrderId={OrderId} to State={NewState}", orderId, newState);
                return await Task.FromResult(order);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Failed to update delivery status for OrderId={OrderId}", orderId);
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
                var businessId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(businessId))
                {
                    return BadRequest(new { message = "Business ID not found in token." });
                }

                var riders = await _riderManager.GetAvaliableRiders(businessId);
                var riderDtos = riders.Select(r => new
                {
                    Id = r.User.Id,
                    Name = r.User.Name,
                    Email = r.User.Email,
                    ProfilePicture = r.User.ProfilePicture
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
        public IActionResult GetRiderShipments()
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
                var riderId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(riderId))
                {
                    return BadRequest(new { message = "Rider ID not found in token." });
                }

                var ridersShipments =  _riderManager.GetRiderShipments(riderId);

                return Ok(ridersShipments.Result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving riders.", error = ex.Message });
            }
        }

        //[HttpPost("test-whatsapp")]
        //[AllowAnonymous] // For manual testing, remove in production
        //public async Task<IActionResult> TestWhatsAppNotification()
        //{
        //    try
        //    {
        //        var order = new Order
        //        {
        //            Id = 123,
        //            CustomerID = "bc85afee-d96f-4b58-8ee8-5e1c16bd407f",
        //            RiderID = "1115f839-5aca-4aab-ad4a-630e679fb14a",
        //            Customer = new Customer
        //            {
        //                User = new User
        //                {
        //                    Name = "customer",
        //                    PhoneNumber = "+201124945557" // Replace with your test phone number  
        //                }
        //            },
        //            Title = "Test Order"
        //        };

        //        var result = await _whatsAppNotificationService.SendFeedbackRequestAsync(order);
        //        return Ok(new { Success = result, Message = "WhatsApp notification triggered" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to send WhatsApp notification");
        //        return StatusCode(500, new { error = "An error occurred while sending WhatsApp notification.", details = ex.Message });
        //    }
        //}
    }
}