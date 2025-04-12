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


        public PaginationViewModel<AdminCreateRiderVM> Search( int status ,
            string Name = "", string PhoneNumber = "", int pageNumber = 1, int pageSize = 4 )
        {

            var builder = PredicateBuilder.New<Rider>();

            var old = builder;

            if (!Name.IsNullOrEmpty())
                builder = builder.And(i => i.User.Name.ToLower().Contains(Name.ToLower()));

            if (!PhoneNumber.IsNullOrEmpty())
                builder = builder.And(i => i.User.PhoneNumber.Contains(PhoneNumber));
            if (status >= 0)
                builder = builder.And(i => i.Status == (RiderStatusEnum) status);


            if (old == builder)
                builder = null;



            var count = base.GetList(builder).Count();

            var resultAfterPagination = base.Get(
                 filter: builder,
                 pageSize: pageSize,
                 pageNumber: pageNumber)
                 .Include(r => r.User)
                 .ToList()
                 .Select(p => p.ToDetailsVModel())
                 .ToList();

            return new PaginationViewModel<AdminCreateRiderVM>
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
    }
}
