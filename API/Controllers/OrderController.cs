using Microsoft.AspNetCore.Mvc;
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
        [Route("create")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateViewModel model)
        {

            if (!ModelState.IsValid) return BadRequest(ModelState); 

            await orderService.CreateOrder(model); // new method we'll define below

            return CreatedAtAction(nameof(GetOrderById), new { Message = "The order is created" });
        }

        [HttpGet("getOrder/{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await orderService.GetOrderByIdAsync(id) ?? NotFound() ;

            return Ok(order);
        }
    }

}
