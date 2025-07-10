
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ViewModels.Business_Owner
{
    // تعريف جديد خاص بموقع العمل
    public class BusinessLocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string AreaName { get; set; }
    }

    public class BusinessOwnerProfileVM
    {
        public string UserID { get; set; }

        [Required]
        public string Name { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [Display(Name = "Profile Picture")]
        public string? ProfilePicture { get; set; }
        public IFormFile? ProfilePictureFile { get; set; }
        public string? BankAccount { get; set; }
        public string? BusinessType { get; set; }

        // استخدام النوع الجديد بدلاً من LocationDto
        public BusinessLocationDto? BusinessLocation { get; set; }

        public int TotalRiders { get; set; }
        public int TotalOrders { get; set; } // إضافة هذا السطر
    }
}
