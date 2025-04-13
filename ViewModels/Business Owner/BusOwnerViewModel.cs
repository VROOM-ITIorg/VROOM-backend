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
        public string? ImagePath { get ; set ; }

        //[Display(Name = "Active")]
        //public bool IsActive { get; set; } = true;


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

        //[Display(Name = "Active")]
        //public bool IsActive { get; set; } = true;

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
    }
}
