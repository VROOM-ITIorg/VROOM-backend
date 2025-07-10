using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;

namespace VROOM.Repository
{
    internal class WaypointRepo : BaseRepository<Waypoint>
    {
        public WaypointRepo(VroomDbContext context) : base(context) { }

        
    }
}
