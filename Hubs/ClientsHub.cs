using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
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
}
