using VROOM.Data;
using VROOM.Models;

namespace VROOM.Repositories
{
    public class OrderRouteRepository : BaseRepository<OrderRoute>
    {
        public OrderRouteRepository(VroomDbContext context) : base(context) { }

        public OrderRoute GetOrderRouteByOrderID(int orderID)
        {
            // EF.Property<string>(e, "Email") this for weak entity because we don't know the property in the compile time 
            Console.WriteLine($"dbSet Type: {dbSet.GetType().Module}");
            return  dbSet.Where<OrderRoute>(o=>o.OrderID==orderID ).FirstOrDefault();
            //return await dbSet.Where(o => !o.IsDeleted && o.OrderID == orderID).FirstOrDefaultAsync();
        }
    }
}
