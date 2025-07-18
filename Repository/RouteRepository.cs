using VROOM.Data;
using VROOM.Models;

namespace VROOM.Repositories
{
    public class RouteRepository : BaseRepository<Route>
    {
        public RouteRepository(VroomDbContext context) : base(context) { }

    }
}
