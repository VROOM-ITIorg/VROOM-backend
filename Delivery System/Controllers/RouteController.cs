using Microsoft.AspNetCore.Mvc;

namespace Delivery_System.Controllers
{
    public class RouteController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
