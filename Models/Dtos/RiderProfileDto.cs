using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VROOM.Models.Dtos
{
    public class RiderProfileDto
    {
        public string id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string status { get; set; }
        public string vehicleType { get; set; }
        public VehicleTypeStatus? vehicleStatus { get; set; }
        public LocationDto location { get; set; }
        public float experienceLevel { get; set; }
        public double rating { get; set; }
        public ICollection feedbacks { get; set; }
        public object stats { get; set; }
        public string profilePicture { get; set; }
    }

    public class LocationDto
    {
        public double? latitude { get; set; }
        public double? longitude { get; set; }
        public string area { get; set; }
    }

    public class StatsDto
    {
        public int assignedShipments { get; set; }
        public int completedShipments { get; set; }
        public int assignedOrders { get; set; }
        public int completedOrders { get; set; }
    }
}
