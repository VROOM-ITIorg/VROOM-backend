using Microsoft.EntityFrameworkCore;
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
        public async Task<OrderRider?> GetByOrderAndRiderAsync(int orderId, string riderId)
        {
            return await context.OrderRiders
                .FirstOrDefaultAsync(or => or.OrderID == orderId && or.RiderID == riderId && !or.IsDeleted);
        }

    }
}
