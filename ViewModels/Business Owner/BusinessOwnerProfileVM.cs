using Microsoft.AspNetCore.Http;

namespace VROOM.ViewModels
{
    public class BusinessOwnerProfileVM
    {
        public string UserID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfilePicture { get; set; }
        public IFormFile ProfilePictureFile { get; set; }
        public string BankAccount { get; set; }
        public string BusinessType { get; set; }
        public BusinessLocationDto BusinessLocation { get; set; }
        public int TotalRiders { get; set; }
        public int TotalOrders { get; set; }
    }

    public class BusinessLocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string AreaName { get; set; }
    }
}