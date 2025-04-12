using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VROOM.Services;

namespace Delivery_System.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("vroom-admin/{controller}")]
    public class OrderController : Controller
    {
        private readonly OrderService orderServices;


        public OrderController(OrderService _orderServices)
        {
            orderServices = _orderServices;
        }

        [Route("ActiveOrder")]
        public IActionResult ActiveOrders()
        {
            return View(orderServices.GetActiveOrder());
        }

        public IActionResult OrderPerformance(int id)
        {
            return View(orderServices.GetOrderPerformance(id));
        }
    }
}