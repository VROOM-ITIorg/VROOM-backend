using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViewModels.Order;
using VROOM.Services;

namespace Delivery_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly OrderService orderServices;
        public OrderController(OrderService _orderServices)
        {
            orderServices = _orderServices;
        }

        [HttpGet]
        [Route("ActiveOrder")]
        public async Task<IActionResult> ActiveOrders(
            string priority = null,
            string state = null,
            string customer = null,
            string rider = null,
            bool? isBreakable = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string search = null,
            int pageNumber = 1,
            int pageSize = 4,
            string sort = "title_asc")
        {
            try
            {
             var viewModel = await orderServices.GetActiveOrdersAsync(
                    priority, state, customer, rider, isBreakable,
                    dateFrom, dateTo, minPrice, maxPrice, search,
                    pageNumber, pageSize, sort);
                return View(viewModel);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(new ActiveOrdersViewModel { Orders = new List<OrderDetailsViewModel>() });
            }
        }

        [HttpPost]
        [Route("createOrder")]
        public IActionResult CreateOrders([FromBody] OrderCreateViewModel model)
        {

            //orderServices.CreateOrder(model);
            return View(orderServices.GetActiveOrder());
        }


        public IActionResult OrderPerformance(int id)
        {
            return View(orderServices.GetOrderPerformance(id));
        }
    }
}