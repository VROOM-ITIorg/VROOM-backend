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
    public class OrderRiderRepository : BaseRepository<OrderRider>
    {
        public OrderRiderRepository(VroomDbContext context) : base(context) { }
    }
}
