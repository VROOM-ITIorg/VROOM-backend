using Microsoft.AspNetCore.Mvc;
using VROOM.Services;

namespace Delivery_System.Controllers
{
    public class OrderController : Controller
    {
        private readonly OrderService orderServices;

        public OrderController(OrderService _orderServices)
        {
            orderServices = _orderServices;
        }
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