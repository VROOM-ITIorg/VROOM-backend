using API.Myhubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
        private readonly BusinessOwnerService _businessOwnerService;
        private readonly IHubContext<AcceptOrderHub> orderHub;

        public OrderController(OrderService _orderService, BusinessOwnerService businessOwnerService)
        public OrderController(OrderService _orderService,IHubContext<AcceptOrderHub> _orderHub)
        {
            orderService = _orderService;
            _businessOwnerService = businessOwnerService;
            orderHub = _orderHub;
        }

        [HttpPost]
        [Authorize(Roles = "BusinessOwner")]
        [Route("create")]
        public async Task<IActionResult> CreateOrder([FromBody] ICollection<OrderCreateViewModel> model)
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
            foreach (var item in model)
            {
                await orderService.CreateOrder(item, BussinsId);
            }
             // new method we'll define below

            return CreatedAtAction(nameof(GetOrderById), new { Message = "The order is created" });
        }
        [Authorize(Roles = "BusinessOwner")]
        [HttpGet("getOrder/{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {

            var order = await orderService.GetOrderByIdAsync(id) ?? NotFound() ;

            return Ok(order);
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAllOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string state = null,
            [FromQuery] string customerName = null,
            [FromQuery] string riderName = null,
            [FromQuery] string priority = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var filter = new OrderFilter
            {
                State = state,
                CustomerName = customerName,
                RiderName = riderName,
                Priority = priority,
                StartDate = startDate,
                EndDate = endDate
            };

            var orders = await orderService.GetAllOrders(filter, pageNumber, pageSize);
            var totalCount = await orderService.GetTotalOrders(filter); // Assume this method exists
            return Ok(new { data = orders, totalItems = totalCount });
        }

        //// update order status
        [Authorize(Roles = "Rider")]
        [HttpPost("updateOrder")]
        public async Task<IActionResult> AccOrRejOrder([FromQuery] int id, [FromBody] OrderStateEnum orderState, [FromQuery] string RiderId, [FromQuery] string BusinessId)
        {
            // There are 5 events can we update the state of the order this now
            var order = await orderService.UpdateOrderState(id, orderState, RiderId, BusinessId);
            await orderHub.Clients.All.SendAsync("AcceptOrRejectOrder", id, orderState.ToString());
            // if the order is accepted we will retrun a good massege to the customer if not 
            return Ok(new { Message = "order is updated" });
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
