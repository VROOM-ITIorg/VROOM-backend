using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
{

    //public class Location
    //{
    //    public int Id { get; set; }
    //    public double Lang { get; set; }
    //    public double Lat { get; set; }
    //    public string Area { get; set; }


    public class Rider
    {

        public string BusinessID { get; set; }
        public string UserID { get; set; }
        public RiderStatusEnum Status { get; set; } = RiderStatusEnum.Unavailable;
        public VehicleTypeEnum VehicleType { get; set; }
        public VehicleTypeStatus? VehicleStatus { get; set; } = VehicleTypeStatus.Good;
        public double Lang { get; set; }
        public double Lat { get; set; }
        public string Area { get; set; }

        public float ExperienceLevel { get; set; }
        public float Rating { get; set; }

        public DateTime ? Lastupdated { get; set; }

        public virtual BusinessOwner BusinessOwner { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<Feedback> Feedbacks { get; set; }
        public virtual ICollection<Shipment> Shipments { get; set; }
        public virtual ICollection<Issues> Issues { get; set; }
        public virtual ICollection<RiderAssignment> RiderAssignments { get; set; }
        public virtual ICollection<Order> OrdersHandled { get; set; }
        public virtual ICollection<OrderRider> OrderRiders { get; set; }
        public virtual ICollection<RiderRouteIssue> RiderRouteIssues { get; set; }
    }
}