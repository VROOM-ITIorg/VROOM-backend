
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using VROOM.Data;
using VROOM.Models;
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
        public Order GetOrderById(int orderId)
        {
            return context.Orders.Find(orderId);
        }

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
         
        public List<Order> GetOrderPerformance(int id)
        {
            var orderReports = context.Orders
               .Where(o => o.Id == id)
               .Include(o => o.Rider)
               .ToList();

            return orderReports;
        }



        public async Task<List<Order>> GetActiveOrdersAsync(
            string priority = null,
            string state = null,
            string customer = null,
            string rider = null,
            bool? isBreakable = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string search = null)
        {
            IQueryable<Order> query = context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Rider)
                .Where(o => !o.IsDeleted);

            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<OrderPriorityEnum>(priority, out var priorityEnum))
                query = query.Where(o => o.OrderPriority == priorityEnum);

            if (!string.IsNullOrEmpty(state) && Enum.TryParse<OrderStateEnum>(state, out var stateEnum))
                query = query.Where(o => o.State == stateEnum);

            if (!string.IsNullOrEmpty(customer))
                query = query.Where(o => o.Customer.User.Name.Contains(customer, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(rider))
                query = query.Where(o => o.Rider.User.Name.Contains(rider, StringComparison.OrdinalIgnoreCase));

            if (isBreakable.HasValue)
                query = query.Where(o => o.IsBreakable == isBreakable.Value);

            if (dateFrom.HasValue)
                query = query.Where(o => o.Date >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(o => o.Date <= dateTo.Value);

            if (minPrice.HasValue)
                query = query.Where(o => o.OrderPrice >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(o => o.OrderPrice <= maxPrice.Value);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(o => o.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || o.Details.Contains(search, StringComparison.OrdinalIgnoreCase));

            return await query.ToListAsync();
        }

        public async Task<(decimal MinPrice, decimal MaxPrice)> GetPriceRangeAsync(
            string priority = null,
            string state = null,
            string customer = null,
            string rider = null,
            bool? isBreakable = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string search = null)
        {
            IQueryable<Order> query = context.Orders
                .Where(o => !o.IsDeleted);

            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<OrderPriorityEnum>(priority, out var priorityEnum))
                query = query.Where(o => o.OrderPriority == priorityEnum);

            if (!string.IsNullOrEmpty(state) && Enum.TryParse<OrderStateEnum>(state, out var stateEnum))
                query = query.Where(o => o.State == stateEnum);

            if (!string.IsNullOrEmpty(customer))
                query = query.Where(o => o.Customer.User.Name.Contains(customer, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(rider))
                query = query.Where(o => o.Rider.User.Name.Contains(rider, StringComparison.OrdinalIgnoreCase));

            if (isBreakable.HasValue)
                query = query.Where(o => o.IsBreakable == isBreakable.Value);

            if (dateFrom.HasValue)
                query = query.Where(o => o.Date >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(o => o.Date <= dateTo.Value);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(o => o.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || o.Details.Contains(search, StringComparison.OrdinalIgnoreCase));

            var priceRange = await query
                .Select(o => new { o.OrderPrice })
                .AggregateAsync(
                    new { MinPrice = (decimal?)null, MaxPrice = (decimal?)null },
                    (acc, curr) => new
                    {
                        MinPrice = acc.MinPrice == null || curr.OrderPrice < acc.MinPrice ? curr.OrderPrice : acc.MinPrice,
                        MaxPrice = acc.MaxPrice == null || curr.OrderPrice > acc.MaxPrice ? curr.OrderPrice : acc.MaxPrice
                    });

            return (priceRange.MinPrice ?? 0, priceRange.MaxPrice ?? 5000);
        }

        public async Task<OrderRoute> GetOrderRouteByOrderIdAsync(int orderId)
        {
            return await context.OrderRoutes
                .Include(or => or.Route)
                .ThenInclude(r => r.Shipment)
                .FirstOrDefaultAsync(or => or.OrderID == orderId && !or.IsDeleted);
        }

        public async Task<List<Order>> GetOrdersByShipmentIdAsync(int shipmentId)
        {
            return await context.OrderRoutes
                .Where(or => or.Route.ShipmentID == shipmentId && !or.IsDeleted)
                .Include(or => or.Order)
                .Include(or => or.Route)
                .Where(or => or.Route != null && !or.Route.IsDeleted)
                .Select(or => or.Order)
                .Where(o => o != null && !o.IsDeleted)
                .ToListAsync();
        }

        public async Task<(List<Order> Data, int TotalCount)> GetPaginatedOrdersAsync(
            Expression<Func<Order, bool>> filter = null,
            int pageSize = 4,
            int pageNumber = 1,
            string sort = "title_asc")
        {
            IQueryable<Order> query = context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Rider)
                .Where(o => !o.IsDeleted);

            if (filter != null)
                query = query.Where(filter);

            int totalCount = await query.CountAsync();

            // Pagination
            if (pageSize < 0)
                pageSize = 4;

            if (pageNumber < 0)
                pageNumber = 1;

            if (totalCount < pageSize)
            {
                pageSize = totalCount;
                pageNumber = 1;
            }

            int toSkip = (pageNumber - 1) * pageSize;

            // Sorting
            switch (sort.ToLower())
            {
                case "title_desc":
                    query = query.OrderByDescending(o => o.Title);
                    break;
                case "price_asc":
                    query = query.OrderBy(o => o.OrderPrice);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(o => o.OrderPrice);
                    break;
                case "date_asc":
                    query = query.OrderBy(o => o.Date);
                    break;
                case "date_desc":
                    query = query.OrderByDescending(o => o.Date);
                    break;
                default: // title_asc
                    query = query.OrderBy(o => o.Title);
                    break;
            }

            query = query.Skip(toSkip).Take(pageSize);

            var data = await query.ToListAsync();

            return (data, totalCount);
        }

        public float GetMaxWeight(int shipmentId)
        {
           return context.Database.SqlQuery<float>($"SELECT MAX(o.Weight) AS Value FROM Waypoint AS w LEFT JOIN Orders AS o ON w.orderId = o.Id WHERE ShipmentID = {shipmentId} GROUP BY ShipmentID").FirstOrDefault();
        }

    }
}


public static class QueryableExtensions
{
    public static async Task<TAccumulate> AggregateAsync<TSource, TAccumulate>(
        this IQueryable<TSource> source,
        TAccumulate seed,
        Expression<Func<TAccumulate, TSource, TAccumulate>> func)
    {
        var compiledFunc = func.Compile();
        var result = seed;
        var items = await source.ToListAsync();
        foreach (var item in items)
        {
            result = compiledFunc(result, item);
        }
        return result;
    }
}
