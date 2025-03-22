// Order.cs 
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public int CustomerID { get; set; }
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

        public Customer Customer { get; set; }
        public Rider Rider { get; set; }
        public Payment Payment { get; set; }
        public OrderRoute OrderRoute { get; set; }
        public OrderRider OrderRider { get; set; }
    }
}