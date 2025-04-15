using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VROOM.Models;
using VROOM.Models.Map;
using VROOM.Services;

namespace VROOM.Controllers
{
    public class MapController : Controller
    {
        private readonly MapService _mapService;

        public MapController(MapService mapService)
        {
            _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
        }

        // GET: /Map
        public IActionResult Index()
        {
            return View(new MapModel());
        }

        // POST: /Map/Route
        [HttpPost]
        public async Task<IActionResult> Route(string origin, string destination, int shipmentId)
        {
            try
            {
                var route = await _mapService.FetchOptimizedRouteAsync(origin, destination, shipmentId);
                return View("Route", route);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("Index", new MapModel());
            }
        }
    }
}