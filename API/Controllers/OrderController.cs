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
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateViewModel model)
        {
            int id;

            if (!ModelState.IsValid) return BadRequest(ModelState); 

            orderService.CreateOrder(model, out id); // new method we'll define below

            return CreatedAtAction(nameof(GetOrderById), new { Message = "The order is created", Id = id });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await orderService.GetOrderByIdAsync(id) ?? NotFound() ;

            return Ok(order);
        }
    }

}
