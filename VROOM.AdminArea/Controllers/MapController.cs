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

        // POST: /Map/Search
        [HttpPost]
        public async Task<IActionResult> Search(string locationName)
        {
            try
            {
                var mapModel = await _mapService.FetchCoordinatesAsync(locationName);
                return View("Index", mapModel);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("Index", new MapModel());
            }
        }
    }
}