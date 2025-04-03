using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace VROOM.ViewModels
{
    public class RiderViewModel
    {
        public int Id { get; set; }

        [Required]
        public int BusinessID { get; set; }

        [Required]
        public string UserID { get; set; }

        [Required]
        [Display(Name = "Rider Status")]
        public string Status { get; set; }

        [Required]
        [Display(Name = "Rider Type")]
        public string Type { get; set; }

        [Required]
        [Display(Name = "Vehicle Type")]
        public string Vehicle { get; set; }

        [Required]
        [Display(Name = "Current Location")]
        public string Location { get; set; }

        [Required]
        [Display(Name = "Experience Level")]
        public string ExperienceLevel { get; set; }

        [Range(0, 5)]
        [Display(Name = "Rating")]
        public int Rating { get; set; }

        [Required]
        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePicture { get; set; } 

        public string? ImagePath { get; set; }
    }
}
