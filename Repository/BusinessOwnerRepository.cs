using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
using System;
using System.Linq;
using System.Threading.Tasks;

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
            public async Task<Result> AssignOrderAutomaticallyAsync(string businessOwnerId, int orderId)
        {
            // Retrieve the business owner
            var businessOwner = await context.BusinessOwners.FindAsync(businessOwnerId);
            if (businessOwner == null)
                return Result.Failure("Business owner not found.");

            // Retrieve the order
            var order = await context.Orders.FindAsync(orderId);
            // Get available riders for the business owner
            var riders = await context.Riders
                .Where(r => r.BusinessID == businessOwnerId && r.Status == RiderStatusEnum.Available)
                .ToListAsync();

            // Filter riders based on vehicle status and weight capacity
            var filteredRiders = riders
                .Where(r => r.VehicleStatus == "Good")
                .ToList();

            if (!filteredRiders.Any())
                return Result.Failure("No available riders who can handle this order.");

            // Calculate distances and scores
            var distances = filteredRiders
                .Select(r => Haversine(35.5, 28.9, r.Lat, r.Lang))
                .ToList();

            var dMin = distances.Min();
            var dMax = distances.Max();

            var scoredRiders = filteredRiders
                .Select(r =>
                {
                    var distance = Haversine(35.5, 25.9, r.Lat, r.Lang);
                    var scoreDistance = dMax == dMin ? 100 : 100 * (dMax - distance) / (dMax - dMin);
                    var scoreExperience = GetExperienceScore(r.ExperienceLevel);
                    var scoreRating = r.Rating * 20;
                    var totalScore = scoreDistance + scoreExperience + scoreRating;
                    return new { Rider = r, TotalScore = totalScore };
                })
                .ToList();

            // Select the best rider
            var bestRider = scoredRiders.OrderByDescending(x => x.TotalScore).FirstOrDefault();
            if (bestRider == null)
                return Result.Failure("No suitable rider found.");

            // Assign the order to the best rider
            order.RiderID = bestRider.Rider.UserID;
            order.State = OrderStateEnum.Confirmed;
            context.Orders.Update(order);
            await context.SaveChangesAsync();

            return Result.Success("Order assigned successfully.");
        }

        private int GetMaxWeight(VehicleTypeEnum type)
        {
            switch (type)
            {
                case VehicleTypeEnum.Motorcycle: return 50;
                case VehicleTypeEnum.Car: return 100;
                case VehicleTypeEnum.Van: return 200;
                default: return 0;
            }
        }

        private double GetExperienceScore(float experienceLevel)
        {
            if (experienceLevel < 10) return 25; // Rookie
            if (experienceLevel < 20) return 50; // Experienced
            if (experienceLevel < 30) return 75; // Delivery Master
            return 100; // Leader
        }

        private double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c;
            return d;
        }

        private double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }
        public BusinessOwner GetBusinessDetails(string businessOwnerUserName)
        {
            return context.BusinessOwners.Where(i => i.User.Name == businessOwnerUserName).FirstOrDefault();

        }
            public PaginationViewModel<AdminBusOwnerDetialsVM> Search(int status = -1, string Name = "", string PhoneNumber = "", int pageNumber = 1, int pageSize = 4, string sort = "name_asc")
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
                     .ToList();

            var query = resultAfterPagination.Select(p => p.ToDetailsVModel()).ToList();
            if (sort == "name_desc")
                query = resultAfterPagination.OrderByDescending(u => u.User.Name).Select(p => p.ToDetailsVModel()).ToList();

            return new PaginationViewModel<AdminBusOwnerDetialsVM>
            {
                Data = query,
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

