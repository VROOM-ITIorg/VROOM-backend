using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class OrderRider
    {
        [Key]
        public int OrderRiderID { get; set; }
        public int OrderID { get; set; }
        public int RiderID { get; set; }
        public int BusinessID { get; set; }
        public int UserID { get; set; }
        public string ItemsType { get; set; }
        public string Title { get; set; }
        public bool IsBreakable { get; set; }
        public string Notes { get; set; }
        public float Weight { get; set; }
        public string Priority { get; set; }
        public string State { get; set; }
        public decimal OrderPrice { get; set; }
        public decimal DeliveryPrice { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Vehicle { get; set; }
        public string Location { get; set; }
        public string ExperienceLevel { get; set; }
        public float Rating { get; set; }

        public Order Order { get; set; }
        public Rider Rider { get; set; }
    }
}