using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Data;
using VROOM.Models;

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


    }
}

