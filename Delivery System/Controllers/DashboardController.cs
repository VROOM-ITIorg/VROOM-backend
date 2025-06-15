using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ViewModels;
using VROOM.Data;
using VROOM.Models;

namespace Delivery_System.Controllers
{
    [Route("{controller}")]
    //[Authorize (Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly VroomDbContext _context;

        public DashboardController(VroomDbContext context)
        {
            _context = context;
        }

        [Route("index")]
        public async Task<IActionResult> Dashboard(int days = 30)
        {
            try
            {
                var startDate = DateTime.Now.AddDays(-days);

                // Order Statistics
                var totalOrders = await _context.Orders.CountAsync(o => !o.IsDeleted);

                var orderStatusCounts = await _context.Orders
                    .Where(o => !o.IsDeleted)
                    .GroupBy(o => o.State)
                    .Select(g => new OrderStatusCount
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                var ordersByDate = await _context.Orders
                    .Where(o => !o.IsDeleted && o.Date != null && o.Date >= startDate)
                    .GroupBy(o => o.Date.Date)
                    .Select(g => new OrderByDate
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                var totalRevenue = await _context.Orders
                    .Where(o => !o.IsDeleted)
                    .SumAsync(o => (decimal?)(o.OrderPrice + o.DeliveryPrice) ?? 0);

                // Shipment Statistics
                var totalShipments = await _context.Shipments.CountAsync(s => !s.IsDeleted);

                double? avgDeliveryTime = null;
                var deliveryTimes = await _context.Shipments
                    .Where(s => !s.IsDeleted && s.RealEndTime.HasValue && s.startTime != null)
                    .Select(s => EF.Functions.DateDiffHour(s.startTime, s.RealEndTime.Value))
                    .ToListAsync();
                if (deliveryTimes.Any())
                {
                    avgDeliveryTime = deliveryTimes.Average();
                }

                // Rider Statistics
                var activeRiders = await _context.Riders
                    .CountAsync(r => r.Status == RiderStatusEnum.Available);
                var riderRatings = await _context.Riders
                    .Where(r => r.Rating != null)
                    .Select(r => r.Rating)
                    .ToListAsync();

                // Customer Statistics
                var totalCustomers = await _context.Customers.CountAsync();

                // Existing Statistics
                var topRiders = await _context.Riders
                    .Where(r => r.OrdersHandled != null && !r.OrdersHandled.Any(o => o.IsDeleted))
                    .Select(r => new TopRider
                    {
                        RiderId = r.UserID,
                        RiderName = r.User != null ? r.User.Name : r.UserID, // افترض حقل Name أو استخدم UserID
                        OrdersDelivered = r.OrdersHandled.Count,
                        Rating = r.Rating
                    })
                    .OrderByDescending(r => r.OrdersDelivered)
                    .Take(5)
                    .ToListAsync();

                var onTimeDeliveries = await _context.Shipments
                    .Where(s => !s.IsDeleted && s.RealEndTime.HasValue && s.ExpectedEndTime.HasValue)
                    .CountAsync(s => s.RealEndTime <= s.ExpectedEndTime);
                var totalCompletedShipments = await _context.Shipments
                    .Where(s => !s.IsDeleted && s.RealEndTime.HasValue)
                    .CountAsync();
                var onTimeDeliveryRate = totalCompletedShipments > 0 ? (double)onTimeDeliveries / totalCompletedShipments * 100 : 0;

                var topZones = await _context.Orders
                    .Where(o => !o.IsDeleted)
                    .GroupBy(o => o.zone)
                    .Select(g => new ZoneOrderCount
                    {
                        Zone = g.Key,
                        OrderCount = g.Count()
                    })
                    .OrderByDescending(z => z.OrderCount)
                    .Take(5)
                    .ToListAsync();

                var newCustomers = await _context.Orders
                    .Where(o => !o.IsDeleted && o.Date >= startDate && o.CustomerPriority == CustomerPriorityEnum.FirstTime)
                    .Select(o => o.CustomerID)
                    .Distinct()
                    .CountAsync();

                var totalIssues = await _context.Issues
                    .Where(i => !i.IsDeleted && i.Date >= startDate)
                    .CountAsync();

                // New Statistics
                var orderPriorityCounts = await _context.Orders
                    .Where(o => !o.IsDeleted)
                    .GroupBy(o => o.OrderPriority)
                    .Select(g => new OrderPriorityCount
                    {
                        Priority = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                var cancelledOrders = await _context.Orders
                    .Where(o => !o.IsDeleted && o.State == OrderStateEnum.Cancelled)
                    .CountAsync();
                var cancellationRate = totalOrders > 0 ? (double)cancelledOrders / totalOrders * 100 : 0;

                var businessOwnerPerformances = await _context.Riders
                    .Where(r => !r.OrdersHandled.Any(o => o.IsDeleted))
                    .GroupBy(r => r.BusinessID)
                    .Select(g => new BusinessOwnerPerformance
                    {
                        BusinessOwnerId = g.Key,
                        BusinessName = _context.BusinessOwners
                            .Where(bo => bo.UserID == g.Key)
                            .Select(bo => bo.User != null ? bo.User.Name : g.Key)
                            .FirstOrDefault() ?? g.Key,
                        TotalOrders = g.Sum(r => r.OrdersHandled.Count)
                    })
                    .OrderByDescending(b => b.TotalOrders)
                    .Take(5)
                    .ToListAsync();

                double? averageOrderWeight = null;
                var orderWeights = await _context.Orders
                    .Where(o => !o.IsDeleted && o.Weight != null)
                    .Select(o => o.Weight)
                    .ToListAsync();
                if (orderWeights.Any())
                {
                    averageOrderWeight = orderWeights.Average();
                }

                var viewModel = new AdminDashboardViewModel
                {
                    TotalOrders = totalOrders,
                    OrderStatusCounts = orderStatusCounts ?? new List<OrderStatusCount>(),
                    OrdersByDate = ordersByDate ?? new List<OrderByDate>(),
                    TotalRevenue = totalRevenue,
                    TotalShipments = totalShipments,
                    AvgDeliveryTimeHours = avgDeliveryTime,
                    ActiveRiders = activeRiders,
                    RiderRatings = riderRatings ?? new List<float>(),
                    TotalCustomers = totalCustomers,
                    TopRiders = topRiders ?? new List<TopRider>(),
                    OnTimeDeliveryRate = onTimeDeliveryRate,
                    TopZones = topZones ?? new List<ZoneOrderCount>(),
                    NewCustomers = newCustomers,
                    TotalIssues = totalIssues,
                    OrderPriorityCounts = orderPriorityCounts ?? new List<OrderPriorityCount>(),
                    CancellationRate = cancellationRate,
                    BusinessOwnerPerformances = businessOwnerPerformances ?? new List<BusinessOwnerPerformance>(),
                    AverageOrderWeight = averageOrderWeight
                };

                return View(viewModel);
            }
            catch (InvalidOperationException ex) when (ex.Message == "Sequence contains no elements")
            {
                var emptyViewModel = new AdminDashboardViewModel
                {
                    TotalOrders = 0,
                    OrderStatusCounts = new List<OrderStatusCount>(),
                    OrdersByDate = new List<OrderByDate>(),
                    TotalRevenue = 0,
                    TotalShipments = 0,
                    AvgDeliveryTimeHours = null,
                    ActiveRiders = 0,
                    RiderRatings = new List<float>(),
                    TotalCustomers = 0,
                    TopRiders = new List<TopRider>(),
                    OnTimeDeliveryRate = 0,
                    TopZones = new List<ZoneOrderCount>(),
                    NewCustomers = 0,
                    TotalIssues = 0,
                    OrderPriorityCounts = new List<OrderPriorityCount>(),
                    CancellationRate = 0,
                    BusinessOwnerPerformances = new List<BusinessOwnerPerformance>(),
                    AverageOrderWeight = null
                };
                return View(emptyViewModel);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}