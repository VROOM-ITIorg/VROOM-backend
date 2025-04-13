using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Models;
using VROOM.ViewModels;

namespace ViewModels.Rider
{
    public record RiderVM
    {
        public string UserID { get; init; }
        public string Name { get; init; }
        public string Email { get; init; }
        public string BusinessID { get; init; }
        public VehicleTypeEnum VehicleType { get; init; }
        public string VehicleStatus { get; init; }
        public float ExperienceLevel { get; init; }
        public LocationDto Location { get; init; }
        public RiderStatusEnum Status { get; init; }
    }
}
