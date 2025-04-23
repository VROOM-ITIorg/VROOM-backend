using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;

namespace VROOM.Repository
{
    public class OrderRouteRepository : BaseRepository<OrderRoute>
    {
        public OrderRouteRepository(VroomDbContext context) : base(context) { }

        public async Task<OrderRoute> GetOrderRouteByOrderID(int orderID)
        {
            // EF.Property<string>(e, "Email") this for weak entity because we don't know the property in the compile time 
            Console.WriteLine($"dbSet Type: {dbSet.GetType().Module}");
            return await dbSet.FindAsync(orderID, orderID);
            //return await GetList().Where(o => o.OrderID == orderID).FirstOrDefaultAsync();
        }
    }
}
