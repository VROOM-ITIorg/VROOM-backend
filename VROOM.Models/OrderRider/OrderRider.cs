using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class OrderRider
    {
        public int Id { get; set; }
        public int OrderID { get; set; }
        public int RiderID { get; set; }
       

        public virtual Order Order { get; set; }
        public virtual Rider Rider { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; } = DateTime.Now;
    }
}