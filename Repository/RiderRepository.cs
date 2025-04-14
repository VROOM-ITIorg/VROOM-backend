using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ViewModels;
using VROOM.Data;
using VROOM.Models;
using VROOM.ViewModels;

namespace VROOM.Repositories
{
    public class RiderRepository : BaseRepository<Rider>
    {
        
        public RiderRepository(VroomDbContext options) : base(options) { }


        public PaginationViewModel<AdminRiderDetialsVM> Search( int status,
            string Name = "", string PhoneNumber = "", int pageNumber = 1, int pageSize = 4)
        {

            var builder = PredicateBuilder.New<Rider>();

            builder = builder.And(i => i.User.IsDeleted == false);

            if (!Name.IsNullOrEmpty())
                builder = builder.And(i => i.User.Name.ToLower().Contains(Name.ToLower()));

            if (!PhoneNumber.IsNullOrEmpty())
                builder = builder.And(i => i.User.PhoneNumber.Contains(PhoneNumber));
            if (status >= 0)
                builder = builder.And(i => i.Status == (RiderStatusEnum) status);




            var count = base.GetList(builder).Count();

            var resultAfterPagination = base.Get(
                 filter: builder,
                 pageSize: pageSize,
                 pageNumber: pageNumber)
                 .Include(r => r.User)
                 .ToList()
                 .Select(p => p.ToShowVModel())
                 .ToList();

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


        public async Task<List<Rider>> GetRidersForBusinessOwnerAsync(string businessOwnerId)
        {
            return await context.Riders
                .Include(r => r.User)
                .Where(r => r.BusinessID == businessOwnerId && !r.User.IsDeleted)
                .ToListAsync();
        }
    }
}
