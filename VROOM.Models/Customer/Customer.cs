using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
{
    public class Customer
    {
        public int UserID { get; set; }
        public int OrderID { get; set; } 
        public int FeedbackID { get; set; } 

        public virtual User User { get; set; }
        public virtual Order Order { get; set; }
        public virtual Feedback Feedback { get; set; }
        public virtual ICollection<Feedback> FeedbacksProvided { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
    }
}