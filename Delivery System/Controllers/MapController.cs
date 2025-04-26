using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using VROOM.Data;
using VROOM.Models;
using VROOM.Models.Map;
using VROOM.Services;

namespace VROOM.Controllers
{
    public class MapController : Controller
    {
        private readonly MapService _mapService;
        private readonly VroomDbContext _dbContext ;

        public MapController(MapService mapService, VroomDbContext dbContext )
        {
            _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
            _dbContext = dbContext;
        }

        // GET: /Map
        public IActionResult Index(int id)
        {
            var shipment = _dbContext.Shipments.FirstOrDefault(x => x.Id == id);

            return View(shipment);
        }

        // POST: /Map/Route
        [HttpPost]
        public async Task<IActionResult> Route([FromBody] RouteRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                    return BadRequest(new { errors });
                }
                var route = await _mapService.FetchOptimizedRouteAsync(request.ShipmentId);
                return Json(route);
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        public class RouteRequest
        {
            [Required(ErrorMessage = "Origin is required.")]
            public string Origin { get; set; }

            [Required(ErrorMessage = "Destination is required.")]
            public string Destination { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "Shipment ID must be a positive integer.")]
            public int ShipmentId { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> ViewRoute(int id)
        {
            try
            {
                var route = await _dbContext.Routes.FindAsync(id);
                if (route == null)
                {
                    return NotFound();
                }
                return View("ViewRoute", route);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return RedirectToAction("Index");
            }
        }
    }
}