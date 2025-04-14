using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ViewModels;
using ViewModels.User;
using VROOM.Data;
using VROOM.Models;
using VROOM.ViewModels;

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



        public PaginationViewModel<AdminBusOwnerDetialsVM> Search(
         string Name = "", string PhoneNumber = "", int pageNumber = 1, int pageSize = 4)
        {

            var builder = PredicateBuilder.New<BusinessOwner>();

             builder = builder.And(i => i.User.IsDeleted == false);

            if (!Name.IsNullOrEmpty())
            {
                builder = builder.And(i => i.User.Name.ToLower().Contains(Name.ToLower()));

            }

            if (!PhoneNumber.IsNullOrEmpty())
                builder = builder.And(i => i.User.PhoneNumber.Contains(PhoneNumber));


            var count = base.GetList(builder).Count();

            var resultAfterPagination = base.Get(
                 filter: builder,
                 pageSize: pageSize,
                 pageNumber: pageNumber)
                 .Include(r => r.User)
                 .ToList()
                 .Select(p => p.ToDetailsVModel())
                 .ToList();

            return new PaginationViewModel<AdminBusOwnerDetialsVM>
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

