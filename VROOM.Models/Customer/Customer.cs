using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
{
    public class Customer
    {
        public string UserID { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<Feedback> FeedbacksProvided { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
    }
}