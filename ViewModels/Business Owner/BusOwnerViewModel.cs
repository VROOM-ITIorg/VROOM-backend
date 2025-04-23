using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using VROOM.Models;

namespace VROOM.ViewModels
{
    public class AdminCreateBusOnwerVM : UserProfile
    {
        [Required(ErrorMessage = "Owner name is required")]
        [Display(Name = "Owner Name")]
        [StringLength(100, ErrorMessage = "Owner name cannot exceed 100 characters")]
        public string OwnerName { get; set; }

        [Required(ErrorMessage = "Business name is required")]
        [Display(Name = "Business Name")]
        [StringLength(100, ErrorMessage = "Business name cannot exceed 100 characters")]
        public string BusinessName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Business type is required")]
        [Display(Name = "Business Type")]
        public int BusinessTypeId { get; set; }

        [Display(Name = "Address")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string Address { get; set; }

        [Display(Name = "Business Logo")]
        public IFormFile? ProfilePicture { get; init; }
        public string? ImagePath { get; set; }

        [Display(Name = "Subscription Type")]
        public SubscriptionTypeEnum SubscriptionType { get; set; } = SubscriptionTypeEnum.None;

        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
    }

    public record AdminEditBusOwnerVM : UserProfile
    {
        public string? UserID { get; init; }

        [Required(ErrorMessage = "Owner name is required")]
        [Display(Name = "Owner Name")]
        [StringLength(100, ErrorMessage = "Owner name cannot exceed 100 characters")]
        public string OwnerName { get; set; }

        [Required(ErrorMessage = "Business name is required")]
        [Display(Name = "Business Name")]
        [StringLength(100, ErrorMessage = "Business name cannot exceed 100 characters")]
        public string BusinessName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Business type is required")]
        [Display(Name = "Business Type")]
        public int BusinessTypeId { get; set; }

        [Display(Name = "Address")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string Address { get; set; }

        [Display(Name = "Business Logo")]
        public IFormFile? ProfilePicture { get; init; }
        public string? ImagePath { get; set; }

        // Subscription-related properties
        [Required(ErrorMessage = "Subscription Type is required")]
        [Display(Name = "Subscription Type")]
        public string SubscriptionType { get; set; }

        [Required(ErrorMessage = "Subscription Start Date is required")]
        [Display(Name = "Subscription Start Date")]
        [DataType(DataType.Date)]
        public DateTime SubscriptionStartDate { get; set; }

        [Required(ErrorMessage = "Subscription End Date is required")]
        [Display(Name = "Subscription End Date")]
        [DataType(DataType.Date)]
        public DateTime SubscriptionEndDate { get; set; }

        [Display(Name = "Subscription Status")]
        public bool IsSubscriptionActive { get; set; }
    }


    public class AdminBusOwnerDetialsVM
    {

        public string UserID { get; init; }
        public string OwnerName { get; set; }
        public string BusinessName { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string Address { get; set; }
        public string? ImagePath { get; set; }

        public string SubscriptionName { get; set; }
        public DateTime SubscriptionExpiryDate { get; set; }

    }
}
