using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using VROOM.Models;

namespace VROOM.ViewModels
{
    public interface UserProfile
    {
        [Display(Name = "Profile Picture")]
        IFormFile? ProfilePicture { get; init; }

        string? ImagePath { get; set; }
    }
    public class AdminCreateRiderVM : UserProfile
    {

        public string BusinessName { get; init; }

        public RiderStatusEnum Status { get; init; }

        public VehicleTypeEnum VehicleType { get; init; }

        public string Location { get; init; }

        public float ExperienceLevel { get; init; }

        public string UserName { get; init; }

        public string Email { get; init; }

        public string PhoneNumber { get; init; }
        public IFormFile? ProfilePicture { get; init; }
        public string? ImagePath { get; set ; }
    }

    public record AdminEditRiderVM : UserProfile
    {

        public string BusinessName { get; init; }

        public string? UserID { get; init; }

        public RiderStatusEnum Status { get; init; }

        public VehicleTypeEnum VehicleType { get; init; }

        public string Location { get; init; }

        public float ExperienceLevel { get; init; }

        public string UserName { get; init; }

        public string Email { get; init; }

        public string PhoneNumber { get; init; }

        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePicture { get; init; }

        public string? ImagePath { get; set; }
    }


    public class AdminRiderDetialsVM
    {

        public string BusinessName { get; init; }

        public string? UserID { get; init; }

        public RiderStatusEnum Status { get; init; }

        public VehicleTypeEnum VehicleType { get; init; }

        public string Location { get; init; }

        public float ExperienceLevel { get; init; }

        public string UserName { get; init; }

        public string Email { get; init; }

        public string PhoneNumber { get; init; }

        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePicture { get; init; }

        public string? ImagePath { get; set; }
    }
    public record RiderDTO
    {
        public string UserID { get; init; }

        public string BusinessID { get; init; }

        public VehicleTypeEnum VehicleType { get; init; }

        public string VehicleStatus { get; init; }

        public float ExperienceLevel { get; init; }

        public LocationDto Location { get; init; }
    }

    public record LocationDto
    {
        public double Lat { get; init; }

        public double Lang { get; init; }

        public string Area { get; init; }
    }


    public record RiderVM
    {
        public string UserID { get; init; }

        public string BusinessID { get; init; }

        public string Name { get; init; }

        public string Email { get; init; }


        public VehicleTypeEnum VehicleType { get; init; }

        public string VehicleStatus { get; init; }

        public float ExperienceLevel { get; init; }

        public LocationDto Location { get; init; }
        public RiderStatusEnum Status { get; init; }
    }

    public class RiderRegisterRequest
    {
        // Identity user info
        public string Name { get; set; }
        //public string BusinessID { get; init; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ProfilePicture { get; set; }

        // Rider-specific info
        public string BusinessID { get; set; }
        public VehicleTypeEnum VehicleType { get; set; }
        public string VehicleStatus { get; set; }
        public float ExperienceLevel { get; set; }
        public LocationDto Location { get; set; }
    }

}
