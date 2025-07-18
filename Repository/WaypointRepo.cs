using VROOM.Data;
using VROOM.Models;

namespace VROOM.Repositories
{
    internal class WaypointRepo : BaseRepository<Waypoint>
    {
        public WaypointRepo(VroomDbContext context) : base(context) { }

        
    }
}
