using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ViewModels;
using ViewModels.User;
using VROOM.Data;
using VROOM.Models;
using VROOM.ViewModels;
using OrderPriorityCount = VROOM.ViewModels.OrderPriorityCount;
using OrderStatusCount = VROOM.ViewModels.OrderStatusCount;
using ZoneOrderCount = VROOM.ViewModels.ZoneOrderCount;


/*
0- CRUD 
1- Get Business Owners by Business Type
2- Get Riders for a Business Owner
3- Assign a Rider to a Business Owner 
4- Remove a Rider from a Business Owner
5- Pagination //not yet
6- edge cases //not yet
 
 */


namespace VROOM.Repositories
{
    
        public class BusinessOwnerRepository : BaseRepository<BusinessOwner>
        {
            public BusinessOwnerRepository(VroomDbContext _dbContext): base(_dbContext){}
            public List<BusinessOwner> GetBusinessOwnersByType(string businessType) => context.BusinessOwners
                        .Where(b => b.BusinessType == businessType)
                        .ToList();
            public List<Rider> GetRidersForBusinessOwner(string businessOwnerId) => context.Riders
                        .Where(r => r.BusinessID == businessOwnerId)
                        .ToList();
            public void AssignRiderToBusinessOwner(string businessOwnerId, string riderId)
            {
                context.RiderAssignments.Add(new RiderAssignment
                {
                    BusinessID = businessOwnerId,
                    RiderID = riderId,
                    AssignmentDate = DateTime.UtcNow
                });

                CustomSaveChanges();
            }
            public void RemoveRiderFromBusinessOwner(string riderId)
            {
                var rider = context.Riders.FirstOrDefault(r => r.UserID == riderId);
                if (rider != null)
                {
                    context.Riders.Remove(rider);
                    CustomSaveChanges();
                }
            }
           
        public BusinessOwner GetBusinessDetails(string businessOwnerUserName)
        {
            return context.BusinessOwners.Where(i => i.User.Name == businessOwnerUserName).FirstOrDefault();

        }
        public PaginationViewModel<AdminBusOwnerDetialsVM> Search(int status = -1, string Name = "", string PhoneNumber = "", int pageNumber = 1, int pageSize = 4, string sort = "name_asc")
        {
            var builder = PredicateBuilder.New<BusinessOwner>();

            builder = builder.And(i => i.User.IsDeleted == false);

            if (!string.IsNullOrEmpty(Name))
            {
                builder = builder.And(i => i.User.Name.ToLower().Contains(Name.ToLower()));
            }

            if (!string.IsNullOrEmpty(PhoneNumber))
            {
                builder = builder.And(i => i.User.PhoneNumber.Contains(PhoneNumber));
            }

            var resultAfterPagination = base.Get(
                filter: builder,
                pageSize: pageSize,
                pageNumber: pageNumber)
                .Include(r => r.User)
                .ToList();

            var count = resultAfterPagination.Count();

            var query = resultAfterPagination.Select(p => p.ToDetailsVModel()).ToList();

            if (sort == "name_desc")
            {
                query = query.OrderByDescending(u => u.OwnerName).ToList();
            }
            else if (sort == "name_asc")
            {
                query = query.OrderBy(u => u.OwnerName).ToList();
            }

            return new PaginationViewModel<AdminBusOwnerDetialsVM>
            {
                Data = query,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Total = count
            };
        }
        public async Task<DashboardStatsDto> GetDashboardStatsAsync(string ownerUserId)
        {
            var stats = new DashboardStatsDto
            {
                // 1. Total Orders by Status
                OrdersByStatus = await context.Orders
                    .Where(o => o.BusinessID == ownerUserId && !o.IsDeleted)
                    .GroupBy(o => o.State)
                    .Select(g => new OrderStatusCount
                    {
                        Status = g.Key.ToString(),
                        OrderCount = g.Count()
                    })
                    .ToListAsync(),

                // 2. Revenue from Orders (Monthly)
                MonthlyRevenues = await context.Orders
                    .Where(o => o.BusinessID == ownerUserId && !o.IsDeleted)
                    .GroupBy(o => new { o.Date.Year, o.Date.Month })
                    .Select(g => new MonthlyRevenue
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        TotalRevenue = g.Sum(o => o.OrderPrice + o.DeliveryPrice)
                    })
                    .OrderBy(r => r.Year).ThenBy(r => r.Month)
                    .ToListAsync(),

                // 3. Rider Performance
                RiderPerformances = await context.Riders
                    .Where(r => r.BusinessID == ownerUserId && !r.User.IsDeleted)
                    .GroupJoin(context.Orders,
                        r => r.UserID,
                        o => o.RiderID,
                        (r, orders) => new RiderPerformance
                        {
                            RiderName = r.User.Name,
                            AverageRating = r.Rating,
                            OrdersHandled = orders.Count()
                        })
                    .ToListAsync(),

                // 4. Order Priority Distribution
                OrderPriorities = await context.Orders
                    .Where(o => o.BusinessID == ownerUserId && !o.IsDeleted)
                    .GroupBy(o => o.OrderPriority)
                    .Select(g => new OrderPriorityCount
                    {
                        Priority = g.Key.ToString(),
                        OrderCount = g.Count()
                    })
                    .ToListAsync(),

                // 5. Top Zones by Order Count
                TopZones = await context.Orders
                    .Where(o => o.BusinessID == ownerUserId && !o.IsDeleted)
                    .GroupBy(o => o.zone)
                    .Select(g => new ZoneOrderCount
                    {
                        Zone = g.Key.ToString(),
                        OrderCount = g.Count()
                    })
                    .OrderByDescending(z => z.OrderCount)
                    .Take(5) // Limit to top 5 zones
                    .ToListAsync(),

                // 6. Shipment Status Distribution
                ShipmentStatuses = await context.Shipments
                    .Where(s => s.Rider.BusinessID == ownerUserId && !s.IsDeleted)
                    .GroupBy(s => s.ShipmentState)
                    .Select(g => new ShipmentStatusCount
                    {
                        Status = g.Key.ToString(),
                        ShipmentCount = g.Count()
                    })
                    .ToListAsync(),

                // 7. Average Shipment Duration (in hours)
                AverageShipmentDurationHours = await context.Shipments
                    .Where(s => s.Rider.BusinessID == ownerUserId && !s.IsDeleted && s.RealEndTime != null)
                    .Select(s => EF.Functions.DateDiffHour(s.startTime, s.RealEndTime))
                    .AverageAsync() ?? 0,

                // 8. Orders by Item Type
                OrdersByItemType = await context.Orders
                    .Where(o => o.BusinessID == ownerUserId && !o.IsDeleted)
                    .GroupBy(o => o.ItemsType)
                    .Select(g => new ItemTypeCount
                    {
                        ItemType = g.Key,
                        OrderCount = g.Count()
                    })
                    .ToListAsync(),

                // 9. Customer Priority Distribution
                CustomerPriorities = await context.Orders
                    .Where(o => o.BusinessID == ownerUserId && !o.IsDeleted)
                    .GroupBy(o => o.CustomerPriority)
                    .Select(g => new CustomerPriorityCount
                    {
                        Priority = g.Key.ToString(),
                        OrderCount = g.Count()
                    })
                    .ToListAsync(),

                // 10. Issues by Type
                IssuesByType = await context.Issues
                    .Where(i => i.Rider.BusinessID == ownerUserId && !i.IsDeleted)
                    .GroupBy(i => i.Type)
                    .Select(g => new IssueTypeCount
                    {
                        Type = g.Key.ToString(),
                        IssueCount = g.Count()
                    })
                    .ToListAsync()
            };

            return stats;
        }
    }
}

