
using VROOM.Data;
using VROOM.Models;

namespace VROOM.Repositories
{
    public class OrderRepository : BaseRepository<Order>
    {

        public OrderRepository(MyDbContext context) : base(context) {}

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
        public List<Order> GetOrdersByCustomerId(string customerId)
        {
            return _context.Orders.Where(o => o.CustomerID == customerId).ToList();
        }

        public List<Order> GetOrdersByRiderId(int riderId)
        {
            return _context.Orders.Where(o => o.RiderID == riderId).ToList();
        }

        // Add the getOrderWithBussinessOwnerID

        // Filter by Date Range
        public List<Order> GetOrdersByDateRange(DateTime startDate, DateTime endDate)
        {
            return _context.Orders
                .Where(o => o.Date >= startDate && o.Date <= endDate)
                .ToList();
        }

        // We need to AssignOrderToRider with two diffrenet way (Manually and Automatically)

        // Assign an Order to a Rider
        // In service section
        public void AssignOrderToRider(int orderId, int riderId)
        {
            var order = _context.Orders.Find(orderId);
            if (order != null)
            {
                order.RiderID = riderId;
                _context.SaveChanges();
            }
        }

        // Calculate Total Revenue
        public decimal CalculateTotalRevenue()
        {
            return _context.Orders.Sum(o => o.OrderPrice + o.DeliveryPrice);
        }

        // Check Order Status
        public string GetOrderState(int orderId)
        {
            var order = _context.Orders.Find(orderId);
            return order?.State ?? "Order not found";
        }
    }
}
