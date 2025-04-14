
using VROOM.Data;
using VROOM.Models;
using VROOM.ViewModels;

namespace ViewModels
{
    public static class RiderExt
    {
        
        public static Rider ToModel(this AdminCreateRiderVM rider)
        {

            return new Rider
            {
                Rating = 0,
                ExperienceLevel = rider.ExperienceLevel,
                Status = rider.Status,
                VehicleType = rider.VehicleType,
                Area = rider.Location,
            };
        }

        public static AdminRiderDetialsVM ToShowVModel(this Rider rider)
        {
            if (rider == null || rider.User == null)
                return null;

            return new AdminRiderDetialsVM
            {
                UserID = rider.UserID,
                UserName = rider.User?.Name ?? "Unknown",
                Email = rider.User?.Email ?? "No Email",
                ImagePath = rider.User.ProfilePicture,
                PhoneNumber = rider.User?.PhoneNumber ?? "No Phone Number",
                Status = rider.Status,
                BusinessName = rider.BusinessOwner.User.Name                
            };
        }

    }
}
