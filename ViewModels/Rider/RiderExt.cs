
using VROOM.Models;
using VROOM.ViewModels;

namespace ViewModels
{
    public static class RiderExt
    {
        public static Rider ToModel(this RiderViewModel rider)
        {

            return new Rider
            {
                Location = rider.Location,
                Rating =  rider.Rating,
                ExperienceLevel = rider.ExperienceLevel,
                BusinessID = rider.BusinessID,
                UserID = rider.UserID,
                Status = rider.Status,
                Vehicle = rider.Vehicle
                
                
            };
        }

        public static RiderViewModel ToDetailsVModel(this Rider rider)
        {
            if (rider == null || rider.User == null)
                return null;

            return new RiderViewModel
            {
                Id = rider.Id,
                UserName = rider.User?.Name ?? "Unknown",
                Email = rider.User?.Email ?? "No Email",
                ImagePath = rider.User.ProfilePicture,
                PhoneNumber = rider.User?.PhoneNumber ?? "No Phone Number"
            };
        }

    }
}
