using Microsoft.EntityFrameworkCore;
using VROOM.Data;
using VROOM.Models;

namespace VROOM.Repositories
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
