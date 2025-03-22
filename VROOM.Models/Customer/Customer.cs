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

        public User User { get; set; }
        public Order Order { get; set; }
        public Feedback Feedback { get; set; }
        public ICollection<Feedback> FeedbacksProvided { get; set; }
        public ICollection<Order> Orders { get; set; }
    }
}