using System.Linq;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ViewModels;
using ViewModels.Shipment;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repository;
using VROOM.ViewModels;

namespace VROOM.Repositories
{
    public class RiderRepository : BaseRepository<Rider>
    {
        
        public RiderRepository(VroomDbContext options) : base(options) { }


        public PaginationViewModel<AdminRiderDetialsVM> Search(
            int status = -1,
            string name = "",
            string phoneNumber = "",
            int pageNumber = 1,
            int pageSize = 4,
            string sort = "name_asc",
            string owner = "All")
        {
            Console.WriteLine($"Search params: status={status}, name={name}, phoneNumber={phoneNumber}, owner={owner}, pageNumber={pageNumber}, pageSize={pageSize}, sort={sort}");

            // Input validation
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 4;

            var builder = PredicateBuilder.New<Rider>(true);
            builder = builder.And(i => !i.User.IsDeleted);

            if (!string.IsNullOrWhiteSpace(name))
                builder = builder.And(i => i.User.Name.ToLower().Contains(name.ToLower()));

            if (!string.IsNullOrWhiteSpace(phoneNumber))
                builder = builder.And(i => i.User.PhoneNumber.Contains(phoneNumber));

            if (status >= 0)
                builder = builder.And(i => i.Status == (RiderStatusEnum)status);

            if (owner != "All" && !string.IsNullOrWhiteSpace(owner))
                builder = builder.And(i => i.BusinessOwner != null && i.BusinessOwner.User.Name == owner);

            // Count total records
            var count = base.GetList(builder).Count();
            Console.WriteLine($"Total records: {count}");

            // Sorting logic
            IQueryable<Rider> query = base.Get(filter: builder)
                .Include(r => r.User)
                .Include(r => r.BusinessOwner).ThenInclude(bo => bo.User);

            query = sort.ToLower() switch
            {
                "name_desc" => query.OrderByDescending(r => r.User.Name),
                "phone_asc" => query.OrderBy(r => r.User.PhoneNumber),
                "phone_desc" => query.OrderByDescending(r => r.User.PhoneNumber),
                "status_asc" => query.OrderBy(r => r.Status),
                "status_desc" => query.OrderByDescending(r => r.Status),
                _ => query.OrderBy(r => r.User.Name) // Default: name_asc
            };

            // Apply pagination
            var offset = (pageNumber - 1) * pageSize;
            var resultAfterPagination = query
                .Skip(offset)
                .Take(pageSize)
                .Select(p => p.ToShowVModel())
                .ToList();
            Console.WriteLine($"Results for page {pageNumber}: {Newtonsoft.Json.JsonConvert.SerializeObject(resultAfterPagination)}");

            return new PaginationViewModel<AdminRiderDetialsVM>
            {
                Data = resultAfterPagination,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Total = count
            };
        }

        public Rider GetBusinessOwnerByRiderId(string id)
        {
            return context.Riders.Where(i => i.UserID == id).FirstOrDefault();

        }


        public async Task<List<Rider>> GetAvaliableRiders(string businessOwnerId)
        {
            return await context.Riders.Where(r => r.BusinessID == businessOwnerId && r.Status == RiderStatusEnum.Available).ToListAsync();
        }

        public async Task<List<ShowShipment>> GetRiderShipments(string riderId)
        {
            return await context.Shipments.Where(r => r.RiderID == riderId && r.ShipmentState == ShipmentStateEnum.Assigned).Select(s=> new ShowShipment { 
                zone=s.zone,
                BeginningArea = s.BeginningArea,
                EndArea = s.EndArea,
                MaxConsecutiveDeliveries = s.MaxConsecutiveDeliveries
            }).ToListAsync();
        } 

        public async Task<List<Rider>> GetRidersForBusinessOwnerAsync(string businessOwnerId)
        {
            return await context.Riders
                .Include(r => r.User)
                .Where(r => r.BusinessID == businessOwnerId && !r.User.IsDeleted)
                .ToListAsync();
        }
        public async Task<Rider> GetRiderByIdAsync(string riderId)
        {
            return await context.Riders
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.UserID == riderId);
        }
    }
}




