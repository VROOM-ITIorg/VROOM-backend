using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using VROOM.Models;

namespace VROOM.ViewModels
{
    public record AdminCreateRiderVM
    {

        public string BusinessName { get; init; }

        //public string UserID { get; init; }

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
}
