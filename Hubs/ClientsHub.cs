using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VROOM.Data;
using VROOM.Models;
using VROOM.Models.Dtos;
using VROOM.Models;

namespace Hubs
{
    public class RiderHub : Hub
    {
        private readonly ConcurrentDictionary<string, ShipmentConfirmation> _confirmationStore;

        public RiderHub(ConcurrentDictionary<string, ShipmentConfirmation> confirmationStore)
        {
            _confirmationStore = confirmationStore;
        }
        public override Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                Context.Items["UserId"] = userId;
            }
            return base.OnConnectedAsync();
        }
        public async Task SendShipmentRequest(string riderId, object message)
        {
            await Clients.Users(riderId).SendAsync("ReceiveShipmentRequest", message);
        }
        public async Task ReceiveRiderResponse(int shipmentId, bool isAccepted)
        {
            var riderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(riderId))
            {
                return;
            }

            if (_confirmationStore.TryGetValue(riderId, out var confirmation) && confirmation.ShipmentId == shipmentId)
            {
                if (confirmation.Status == ConfirmationStatus.Pending)
                {
                    confirmation.Status = isAccepted ? ConfirmationStatus.Accepted : ConfirmationStatus.Rejected;
                    _confirmationStore[riderId] = confirmation;

                    await Clients.User(confirmation.BusinessOwnerId).SendAsync("RiderResponseReceived", new
                    {
                        ShipmentId = shipmentId,
                        RiderId = riderId,
                        IsAccepted = isAccepted
                    });
                }

            }
        }


    }

    public class OwnerHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

    }


    public class RiderLocationHub : Hub
    {
        private readonly VroomDbContext _context;
        private readonly ILogger<RiderLocationHub> _logger;

        public RiderLocationHub(VroomDbContext context, ILogger<RiderLocationHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            _logger.LogInformation("SignalR connection established for user {UserId} with role {UserRole}", userId, userRole);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (exception != null)
            {
                _logger.LogError(exception, "SignalR connection disconnected with error for user {UserId}", userId);
            }
            else
            {
                _logger.LogInformation("SignalR connection disconnected for user {UserId}", userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinRiderGroup(string riderId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(riderId))
                {
                    throw new HubException("Rider ID cannot be empty.");
                }

                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
                {
                    throw new HubException("User not authenticated properly.");
                }

                _logger.LogInformation("User {UserId} with role {UserRole} attempting to join rider group {RiderId}", userId, userRole, riderId);

                bool canJoinGroup = false;
                string reason = "";

                switch (userRole.ToLower())
                {
                    case "rider":
                        if (userId == riderId)
                        {
                            canJoinGroup = true;
                            reason = "Rider joining own group";
                        }
                        else
                        {
                            reason = "Riders can only track their own location";
                        }
                        break;

                    case "businessowner":
                        // Check if the rider belongs to this business owner
                        var rider = await _context.Riders
                            .Include(r => r.BusinessOwner)
                            .FirstOrDefaultAsync(r => r.UserID == riderId);

                        if (rider?.BusinessOwner?.UserID == userId)
                        {
                            canJoinGroup = true;
                            reason = "Business owner managing this rider";
                        }
                        else
                        {
                            reason = "You are not authorized to track this rider";
                        }
                        break;

                    case "admin":
                        canJoinGroup = true;
                        reason = "Admin access";
                        break;

                    default:
                        reason = $"Role '{userRole}' is not authorized for rider tracking";
                        break;
                }

                if (!canJoinGroup)
                {
                    _logger.LogWarning("Access denied for user {UserId}: {Reason}", userId, reason);
                    throw new HubException(reason);
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, riderId);
                _logger.LogInformation("User {UserId} ({UserRole}) successfully joined rider group {RiderId}. Reason: {Reason}", userId, userRole, riderId, reason);
                await Clients.Caller.SendAsync("joinedgroup", new { riderId, timestamp = DateTime.UtcNow.ToString("o") });
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while joining rider group {RiderId}", riderId);
                throw new HubException("An unexpected error occurred while joining the rider group.");
            }
        }

        public async Task LeaveRiderGroup(string riderId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(riderId))
                {
                    _logger.LogWarning("Empty rider ID provided for leaving group");
                    return;
                }

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, riderId);
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("User {UserId} left rider group {RiderId}", userId, riderId);
                await Clients.Caller.SendAsync("LeftGroup", new { riderId, timestamp = DateTime.UtcNow.ToString("o") });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while leaving rider group {RiderId}", riderId);
            }
        }

        public async Task UpdateRiderLocation(string riderId, double latitude, double longitude)
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
                {
                    throw new HubException("User not authenticated properly.");
                }

                if (string.IsNullOrWhiteSpace(riderId))
                {
                    throw new HubException("Rider ID is required.");
                }

                if (!IsValidCoordinate(latitude, longitude))
                {
                    throw new HubException("Invalid coordinates provided.");
                }

                if (userRole.ToLower() != "rider" && userRole.ToLower() != "admin")
                {
                    throw new HubException("Only riders can update their location.");
                }

                if (userRole.ToLower() == "rider" && userId != riderId)
                {
                    throw new HubException("Riders can only update their own location.");
                }

                var rider = await _context.Riders.FirstOrDefaultAsync(r => r.UserID == riderId);
                var now = DateTime.UtcNow;
                if (rider == null)
                {
                    rider = new Rider
                    {
                        UserID = riderId,
                        Lat = latitude,
                        Lang = longitude,
                        Lastupdated = now
                    };
                    _context.Riders.Add(rider);
                }
                else
                {
                    rider.Lat = latitude;
                    rider.Lang = longitude;
                    rider.Lastupdated = now;
                }

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save rider location for {RiderId}", riderId);
                    throw new HubException("Failed to save rider location.");
                }

                var locationDto = new RiderLocationDto
                {
                    RiderId = riderId,
                    Latitude = latitude,
                    Longitude = longitude,
                    LastUpdated = now
                };

                _logger.LogInformation("Broadcasting location for rider {RiderId}: {LocationDto}", riderId, JsonSerializer.Serialize(locationDto));
                await Clients.Group(riderId).SendAsync("ReceiveRiderLocation", locationDto);
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating location for rider {RiderId}", riderId);
                throw new HubException("An unexpected error occurred while updating the rider location.");
            }
        }

        private static bool IsValidCoordinate(double latitude, double longitude)
        {
            return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
        }
    }
}
