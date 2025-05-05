using System.Threading.Tasks;
using VROOM.Models;
using VROOM.Models.Map;

namespace VROOM.Repositories
{
    public interface IMapRepository
    {
        Task<MapModel> GetCoordinatesAsync(string locationName);
        Task<Route> GetOptimizedRouteAsync(int shipmentId);
    }
}