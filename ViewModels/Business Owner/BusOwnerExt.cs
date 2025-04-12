
//using VROOM.Models;
//using VROOM.ViewModels;

//namespace ViewModels
//{
//    public static class BusOwnerExt
//    {
//        public static BusinessOwner ToModel(this AdminCreateBusOwnerVM rider)
//        {

//            return new BusinessOwner
//            {
//                Rating = 0,
//                ExperienceLevel = rider.ExperienceLevel,
//                BusinessID = rider.BusinessID,
//                Status = rider.Status,
//                VehicleType = rider.VehicleType,
//                Area = rider.Location,
//            };
//        }

//        public static AdminCreateRiderVM ToDetailsVModel(this Rider rider)
//        {
//            if (rider == null || rider.User == null)
//                return null;

//            return new AdminCreateRiderVM
//            {
//                //UserID = rider.UserID,
//                UserName = rider.User?.Name ?? "Unknown",
//                Email = rider.User?.Email ?? "No Email",
//                ImagePath = rider.User.ProfilePicture,
//                PhoneNumber = rider.User?.PhoneNumber ?? "No Phone Number",
//                Status = rider.Status
             
//            };
//        }

//    }
//}
