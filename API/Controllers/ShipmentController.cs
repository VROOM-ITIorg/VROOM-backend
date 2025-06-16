using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using ViewModels.Shipment;
using VROOM.Models;
using VROOM.Models.Dtos;
using VROOM.Services;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShipmentController : ControllerBase
    {
        private readonly ShipmentServices _shipmentServices;
        private readonly IRiderService _riderService;
        private readonly ILogger<ShipmentController> _logger;

        public ShipmentController(ShipmentServices shipmentServices, ILogger<ShipmentController> logger, IRiderService riderService)
        {
            _shipmentServices = shipmentServices ?? throw new ArgumentNullException(nameof(shipmentServices));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _riderService = riderService;
        }

        // GET: api/Shipment
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShipmentDto>>> GetAllShipments()
        {
            try
            {
                var shipments = await _shipmentServices.GetAllShipmentsAsync();
                return Ok(shipments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shipments.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving shipments.");
            }
        }

        // GET: api/Shipment/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ShipmentDto>> GetShipment(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
                {
                    _logger.LogWarning("Unauthorized access attempt: No user ID or role found.");
                    return Unauthorized("User not authenticated.");
                }

                var shipment = await _shipmentServices.GetShipmentByIdAsync(id);
                if (shipment == null)
                {
                    _logger.LogWarning($"Shipment {id} not found or deleted.");
                    return NotFound("Shipment not found or deleted.");
                }

                // Check if the shipment has a rider assigned
                if (string.IsNullOrEmpty(shipment.RiderID))
                {
                    _logger.LogWarning($"Shipment {id} has no rider assigned.");
                    return BadRequest("No rider assigned to this shipment.");
                }

                // Role-based authorization
                if (userRole == "Rider")
                {
                    // Riders can only access shipments assigned to them
                    if (shipment.RiderID != userId)
                    {
                        _logger.LogWarning($"Rider {userId} attempted to access shipment {id} not assigned to them.");
                        return Forbid(); // Simply return a 403 without a custom scheme
                    }
                }
                else if (userRole == "BusinessOwner")
                {
                    // Business owners can only access shipments if they manage the assigned rider
                    var riderBusinessOwnerId = await _riderService.GetBusinessOwnerByRiderIdAsync(shipment.RiderID);
                    if (riderBusinessOwnerId != userId)
                    {
                        _logger.LogWarning($"BusinessOwner {userId} not authorized to track rider {shipment.RiderID} for shipment {id}.");
                        return StatusCode(StatusCodes.Status403Forbidden, "You are not authorized to track this rider."); // Return a 403 with a custom message
                    }
                }
                // Admins have unrestricted access

                _logger.LogInformation($"Shipment {id} retrieved successfully for user {userId} ({userRole}).");
                return Ok(shipment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving shipment {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving shipment.");
            }
        }

        // GET: api/Shipment/rider/{riderId}
        [HttpGet("rider/{riderId}")]
        public async Task<ActionResult<IEnumerable<ShipmentDto>>> GetShipmentsByRiderId(string riderId)
        {
            try
            {
                if (string.IsNullOrEmpty(riderId))
                {
                    _logger.LogWarning("RiderId is null or empty.");
                    return BadRequest("RiderId is required.");
                }

                var shipments = await _shipmentServices.GetShipmentsByRiderIdAsync(riderId);
                return Ok(shipments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving shipments for rider {riderId}.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving shipments.");
            }
        }

        // POST: api/Shipment
        [HttpPost]
        public async Task<ActionResult<ShipmentDto>> CreateShipments([FromBody] List<AddShipmentVM> shipmentVMs)
        {
            try
            {
                if (shipmentVMs == null || !shipmentVMs.Any())
                {
                    _logger.LogWarning("Received empty or null shipmentVMs list.");
                    return BadRequest("Shipment data is required.");
                }

                var shipments = new List<ShipmentDto>();
                foreach (var vm in shipmentVMs)
                {
                    if (!string.IsNullOrEmpty(vm.RiderID))
                    {
                        _logger.LogWarning($"RiderID must be null during shipment creation. Received: {vm.RiderID}");
                        return BadRequest("RiderID must be null when creating a shipment.");
                    }

                    var shipment = await _shipmentServices.CreateShipment(vm);
                    shipments.Add(new ShipmentDto
                    {
                        Id = shipment.Id,
                        StartTime = shipment.startTime,
                        RiderID = shipment.RiderID,
                        BeginningLat = shipment.BeginningLat,
                        BeginningLang = shipment.BeginningLang,
                        BeginningArea = shipment.BeginningArea,
                        EndLat = shipment.EndLat,
                        EndLang = shipment.EndLang,
                        EndArea = shipment.EndArea,
                        Zone = shipment.zone,
                        MaxConsecutiveDeliveries = shipment.MaxConsecutiveDeliveries,
                        InTransiteBeginTime = shipment.InTransiteBeginTime,
                        RealEndTime = shipment.RealEndTime,
                        ShipmentState = shipment.ShipmentState,
                        Waypoints = shipment.waypoints?.Select(w => new WaypointDto
                        {
                            Latitude = w.Lat,
                            Longitude = w.Lang,
                            Area = w.Area,
                            orderId = w.orderId

                        }).ToList(),
                        Routes = shipment.Routes?.Select(r => new TheRouteDto
                        {
                            Id = r.Id,
                            OriginLat = r.OriginLat,
                            OriginLang = r.OriginLang,
                            OriginArea = r.OriginArea,
                            DestinationLat = r.DestinationLat,
                            DestinationLang = r.DestinationLang,
                            DestinationArea = r.DestinationArea,
                            Start = r.Start,
                            DateTime = r.dateTime,
                            SafetyIndex = r.SafetyIndex,
                            ShipmentID = r.ShipmentID,
                            OrderIds = r.OrderRoutes?.Select(or => or.OrderID).ToList()
                        }).ToList()
                    });
                }
                return CreatedAtAction(nameof(GetAllShipments), shipments);
            }
            catch (Exception ex) when (ex.Message.Contains("Rider not found"))
            {
                _logger.LogWarning(ex, "Invalid RiderID provided during shipment creation.");
                return BadRequest("RiderID must be null when creating a shipment.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shipments.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error creating shipments: {ex.Message}");
            }
        }

        // PUT: api/Shipment/{id}/state
        [HttpPut("{id}/state")]
        public async Task<ActionResult<Shipment>> UpdateShipmentState(int id, [FromBody] UpdateShipmentStateVM updateShipmentStateVM)
        {
            if (updateShipmentStateVM == null)
            {
                _logger.LogWarning("Received null UpdateShipmentStateVM.");
                return BadRequest("Shipment state data is required.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid ModelState for UpdateShipmentStateVM: {Errors}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                if (!string.IsNullOrEmpty(updateShipmentStateVM.RiderID))
                {
                    bool riderExists = await _shipmentServices.RiderExistsAsync(updateShipmentStateVM.RiderID);
                    if (!riderExists)
                    {
                        _logger.LogWarning($"Rider with ID {updateShipmentStateVM.RiderID} not found for shipment {id}.");
                        return BadRequest($"Rider with ID {updateShipmentStateVM.RiderID} not found.");
                    }
                }

                _logger.LogInformation($"Updating shipment {id} with state {updateShipmentStateVM.ShipmentState}, RiderID: {updateShipmentStateVM.RiderID ?? "null"}, BusinessOwnerID: {updateShipmentStateVM.BusinessOwnerID}");
                var shipment = await _shipmentServices.UpdateShipmentState(id, updateShipmentStateVM.ShipmentState, updateShipmentStateVM.RiderID, updateShipmentStateVM.BusinessOwnerID);
                if (shipment == null)
                {
                    _logger.LogWarning($"Shipment with ID {id} not found or deleted.");
                    return NotFound("Shipment not found or deleted.");
                }
                return Ok(shipment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating shipment state for ID {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error updating shipment state: {ex.Message}");
            }
        }

        // DELETE: api/Shipment/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShipment(int id)
        {
            try
            {
                var shipment = await _shipmentServices.GetShipmentByIdAsync(id);
                if (shipment == null)
                {
                    _logger.LogWarning($"Shipment with ID {id} not found or already deleted.");
                    return NotFound("Shipment not found or already deleted.");
                }

                await _shipmentServices.DeleteShipmentAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting shipment {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error deleting shipment: {ex.Message}");
            }
        }



    }

    public class UpdateShipmentStateVM
    {
        [EnumDataType(typeof(ShipmentStateEnum))]
        public ShipmentStateEnum ShipmentState { get; set; }
        public string? RiderID { get; set; }
        [Required]
        public string BusinessOwnerID { get; set; }
    }
}