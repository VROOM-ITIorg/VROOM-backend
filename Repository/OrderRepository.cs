
using Microsoft.EntityFrameworkCore;
using VROOM.Data;
using VROOM.Models;

namespace VROOM.Repositories
{
    public class OrderRepository : BaseManager<Order>
    {
        public OrderRepository(VroomDbContext _context) : base (_context) { }

        // Create Order
        public void CreateOrder(Order order)
        {
            dbcontext.Orders.Add(order);
            dbcontext.SaveChanges();
        }

        // Read Order by ID
        public Order GetOrderById(int orderId)
        {
            return dbcontext.Orders.Find(orderId);
        }

        // Update Order
        public void UpdateOrder(Order updatedOrder)
        {
            dbcontext.Orders.Update(updatedOrder);
            dbcontext.SaveChanges();
        }

        // Delete Order
        public void DeleteOrder(int orderId)
        {
            var order = dbcontext.Orders.Find(orderId);
            if (order != null)
            {
                dbcontext.Orders.Remove(order);
                dbcontext.SaveChanges();
            }
        }

        // Get All Orders
        public List<Order> GetAllOrders()
        {
            return dbcontext.Orders.ToList();
        }

        // Find Orders by Customer or Rider
        public List<Order> GetOrdersByCustomerId(string customerId)
        {
            return dbcontext.Orders.Where(o => o.CustomerID == customerId).ToList();
        }

        public List<Order> GetOrdersByRiderId(int riderId)
        {
            return dbcontext.Orders.Where(o => o.RiderID == riderId).ToList();
        }
        // Filter by Date Range
        public List<Order> GetOrdersByDateRange(DateTime startDate, DateTime endDate)
        {
            return dbcontext.Orders
                .Where(o => o.Date >= startDate && o.Date <= endDate)
                .ToList();
        }

        // We need to AssignOrderToRider with two diffrenet way (Manually and Automatically)

        // Assign an Order to a Rider
        public void AssignOrderToRider(int orderId, int riderId)
        {
            var order = dbcontext.Orders.Find(orderId);
            if (order != null)
            {
                order.RiderID = riderId;
                dbcontext.SaveChanges();
            }
        }

        // Calculate Total Revenue
        public decimal CalculateTotalRevenue()
        {
            return dbcontext.Orders.Sum(o => o.OrderPrice + o.DeliveryPrice);
        }

        // Check Order Status
        public OrderStateEnum GetOrderState(int orderId)
        {
            var order = dbcontext.Orders.Find(orderId);
            return order.State ;
        }

        public List<Order> GetActiveOrder()
        {
           var ActOrder = dbcontext.Orders
                .Where(o => o.IsActive == true)
                .Include(o => o.Customer)
                .Include(o => o.Rider)
                .Include(o => o.OrderRider)
                .Include(o => o.OrderRoute)
                .ToList();
            return ActOrder;
        }
         
        public List<Order> GetOrderPerformance(int id)
        {
            var orderReports = dbcontext.Orders
               .Where(o => o.Id == id)
               .Include(o => o.Rider)
               .ToList();

            return orderReports;
        }
    }
}
