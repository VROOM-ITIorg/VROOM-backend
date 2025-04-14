
using VROOM.Models;
using VROOM.ViewModels;

namespace ViewModels
{
    public static class BusOwnerExt
    {
        public static BusinessOwner ToModel(this AdminCreateBusOnwerVM owner)
        {

            return new BusinessOwner
            {
                BusinessType = owner.BusinessName,
            };
        }

        public static AdminBusOwnerDetialsVM ToDetailsVModel(this BusinessOwner owner)
        {
            if (owner == null || owner.User == null)
                return null;

            return new AdminBusOwnerDetialsVM
            {
                UserID = owner.UserID,
                OwnerName = owner.User?.Name ?? "Unknown",
                Email = owner.User?.Email ?? "No Email",
                ImagePath = owner.User.ProfilePicture,
                PhoneNumber = owner.User?.PhoneNumber ?? "No Phone Number",
                BusinessName = owner.BusinessType,
                Address = owner.User.Address?.Area,
            };
        }

    }
}
