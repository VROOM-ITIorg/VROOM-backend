// Order.cs 
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string CustomerID { get; set; }
        public int RiderID { get; set; }
        public string ItemsType { get; set; }
        public string Title { get; set; }
        public bool IsBreakable { get; set; }
        public string Notes { get; set; }
        public string Details { get; set; }
        public float Weight { get; set; }
        public string Priority { get; set; }
        public string State { get; set; }
        public decimal OrderPrice { get; set; }
        public decimal DeliveryPrice { get; set; }
        public DateTime Date { get; set; }

        public virtual Customer Customer { get; set; }
        public virtual Rider Rider { get; set; }
        public virtual Payment Payment { get; set; }
        public virtual OrderRoute OrderRoute { get; set; }
        public virtual OrderRider OrderRider { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; } = DateTime.Now;
    }
}