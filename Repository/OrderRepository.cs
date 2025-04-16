
using Microsoft.EntityFrameworkCore;
using VROOM.Data;
using VROOM.Models;


namespace VROOM.Repositories
{
    public class OrderRepository : BaseRepository<Order>
    {
        public OrderRepository(VroomDbContext context) : base(context) { }

        // Create Order
        //public void CreateOrder(Order order)
        //{
        //    _context.Orders.Add(order);
        //    _context.SaveChanges();
        //}

        //// Read Order by ID
        //public Order GetOrderById(int orderId)
        //{
        //    return _context.Orders.Find(orderId);
        //}

        //// Update Order
        //public void UpdateOrder(Order updatedOrder)
        //{
        //    _context.Orders.Update(updatedOrder);
        //    _context.SaveChanges();
        //}

        //// Delete Order
        //public void DeleteOrder(int orderId)
        //{
        //    var order = _context.Orders.Find(orderId);
        //    if (order != null)
        //    {
        //        _context.Orders.Remove(order);
        //        _context.SaveChanges();
        //    }
        //}

        //// Get All Orders
        //public List<Order> GetAllOrders()
        //{
        //    return _context.Orders.ToList();
        //}

        // Find Orders by Customer or Rider
        public List<Order> GetOrdersByCustomerId(string customerId) => GetList(o => o.CustomerID == customerId).ToList();

        public List<Order> GetOrdersByRiderId(string riderId)
        {
            return GetList(o => o.RiderID == riderId).ToList();
        }

        public decimal SumOrderRevenue(int orderId)
        {
            var order = GetAsync(orderId);

            return order.Result.OrderPrice + order.Result.DeliveryPrice;
        }
        public async Task<List<Order>> GetOrdersByStatusAsync(OrderStateEnum status)
        {
            return await dbSet.Where(o => o.State == status).ToListAsync();
        }

        //public string TrackOrder(int orderId)
        //{
        //    var order = GetAsync(orderId);

        //    return order?.Result.State.ToString() ?? "Order not found";
        //}

        // Add the getOrderWithBussinessOwnerID
        //public List<Order> getOrderWithBussinessOwnerID(int riderId)
        //{
        //    return GetList(o => o == riderId).ToList();
        //}

        // Filter by Date Range
        public List<Order> GetOrdersByDateRange(DateTime startDate, DateTime endDate) =>
            GetList(o => o.Date >= startDate && o.Date <= endDate).ToList();

        public List<Order> GetActiveOrder()
        {
           var ActOrder = context.Orders
                .Where(o => o.State ==  OrderStateEnum.Shipped || o.State == OrderStateEnum.Pending || o.State == OrderStateEnum.Created)
                .Include(o => o.Customer)
                .Include(o => o.Rider)
                .Include(o => o.OrderRider)
                .Include(o => o.OrderRoute)
                .ToList();
            return ActOrder;
        }



        public async Task<Order?> GetActiveConfirmedOrderByRiderIdAsync(string riderId)
        {
            return await context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Rider)
                .Include(o => o.OrderRider)
                .Include(o => o.OrderRoute)
                .Where(o => o.Rider.UserID == riderId && o.State == OrderStateEnum.Confirmed)
                .FirstOrDefaultAsync();
        }
        public List<Order> GetOrderPerformance(int id)
        {
            var orderReports = context.Orders
               .Where(o => o.Id == id)
               .Include(o => o.Rider)
               .ToList();

            return orderReports;
        }
    }
}
