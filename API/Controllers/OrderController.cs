using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
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

        [HttpPost]
        [Authorize(Roles = "BusinessOwner")]
        [Route("create")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateViewModel model)
        {

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            // Decode the JWT token
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Access token claims (e.g., user ID, roles, etc.)
            var BussinsId = jwtToken.Claims.FirstOrDefault()?.Value;


            // Take CustomerInfo and call a func to check if the user exist or not and return id
            // and if the customer is not exsit we will create a customer
            // func here
            await orderService.CreateOrder(model, BussinsId); // new method we'll define below

            return CreatedAtAction(nameof(GetOrderById), new { Message = "The order is created" });
        }

        [HttpGet("getOrder/{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {

            var order = await orderService.GetOrderByIdAsync(id) ?? NotFound() ;

            return Ok(order);
        }

        //// update order status
        [Authorize(Roles = "Rider")]
        [HttpPost("updateOrder/{id}")]
        public async Task<IActionResult> AccOrRejOrder(int id, [FromBody] OrderStateEnum orderState,  string RiderId, string BusinessId)
        {
            // There are 5 events can we update the state of the order this now
            var order = await orderService.UpdateOrderState(id, orderState, RiderId, BusinessId);
            // if the order is accepted we will retrun a good massege to the customer if not 
            return Ok(new { Order = order, Message = "order is updated" });
        }

        // Track order 
        [HttpPost("trackOrder/{id}")]
        public async Task<IActionResult> TrackOrder(int id)
        {
            //var order = await orderService.UpdateOrderState(id);

            return Ok();
        }
    }

}
