using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
{
    public class BusinessOwner
    {

        public string UserID { get; set; }
        public string BankAccount { get; set; } = "df";
        public string BusinessType { get; set; }

        public SubscriptionTypeEnum SubscriptionType { get; set; } = SubscriptionTypeEnum.None;
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<Rider> Riders { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<RiderAssignment> RiderAssignments { get; set; }
    }
}