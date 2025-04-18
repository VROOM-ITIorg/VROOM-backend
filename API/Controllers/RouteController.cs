using Microsoft.AspNetCore.Mvc;
using ViewModels.Route;
using VROOM.Services;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RouteController : Controller
    {
        private readonly RouteServices routeService;

        public RouteController(RouteServices _routeService)
        {
            routeService = _routeService;
        }
        [HttpGet("getRoute")]
        public async Task<IActionResult> GetRoute([FromBody] RouteGraphHopperVM graphHopperVM)
        {
            try
            {
                var route = await routeService.GetRouteAsync(graphHopperVM.FromLat, graphHopperVM.FromLon, graphHopperVM.ToLat, graphHopperVM.ToLon);
                return Ok(route);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving route: {ex.Message}");
            }
        }
    }
}
