using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VROOM.Models;
using VROOM.Services;
namespace VROOM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RiderController : ControllerBase
    {
        private readonly RiderService _riderService;

        public RiderController(RiderService riderService)
        {
            _riderService = riderService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterRider([FromBody] Rider rider)
        {
            var registeredRider = await _riderService.RegisterRiderAsync(rider);
            return Ok(registeredRider);
        }

        [HttpGet("{riderId}/profile")]
        public async Task<IActionResult> GetRiderProfile(int riderId)
        {
            var rider = await _riderService.GetRiderProfileAsync(riderId);
            return Ok(rider);
        }

        [HttpGet("{riderId}/orders")]
        public async Task<IActionResult> GetAssignedOrders(int riderId)
        {
            var orders = await _riderService.GetAssignedOrdersAsync(riderId);
            return Ok(orders);
        }

        [HttpPut("{riderId}/orders/{orderId}/accept")]
        public async Task<IActionResult> AcceptOrder(int riderId, int orderId)
        {
            var order = await _riderService.AcceptOrderAsync(riderId, orderId);
            return Ok(order);
        }

        [HttpPut("{riderId}/orders/{orderId}/reject")]
        public async Task<IActionResult> RejectOrder(int riderId, int orderId)
        {
            var order = await _riderService.RejectOrderAsync(riderId, orderId);
            return Ok(order);
        }

        [HttpPut("{riderId}/orders/{orderId}/status")]
        public async Task<IActionResult> UpdateDeliveryStatus(int riderId, int orderId, [FromBody] OrderState newState)
        {
            var order = await _riderService.UpdateDeliveryStatusAsync(riderId, orderId, newState);
            return Ok(order);
        }
    }
}