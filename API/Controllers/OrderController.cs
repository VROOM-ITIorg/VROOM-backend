using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using VROOM.Models;
using VROOM.Services;
using VROOM.ViewModels;

namespace API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly OrderService orderService;

        public OrderController(OrderService _orderService)
        {
            orderService = _orderService;
        }

        [HttpPost("create")]
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var businessId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            // Modified to receive the created order
            var createdOrder = await orderService.CreateOrder(model, businessId);

            // Return with the correct route values
            return CreatedAtAction(
                actionName: nameof(GetOrderById),
                routeValues: new { id = createdOrder.Id },
                value: new { Order = createdOrder, Message = "Order created successfully" }
            );
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();

            return Ok(order);
        }

        [Authorize(Roles = "Rider")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStateUpdateRequest request)
        {
            var updatedOrder = await orderService.UpdateOrderState(
                id,
                request.OrderState,
                request.RiderId,
                request.BusinessId);

            return Ok(new { Order = updatedOrder, Message = "Order status updated" });
        }

        // Track order 
        [HttpPost("trackOrder/{id}")]
        public async Task<IActionResult> TrackOrder(int id)
        {
            //var order = await orderService.UpdateOrderState(id);

            return Ok();
        }
    }
    public class OrderStateUpdateRequest
    {
        public OrderStateEnum OrderState { get; set; }
        public string RiderId { get; set; }
        public string BusinessId { get; set; }
    }
}



