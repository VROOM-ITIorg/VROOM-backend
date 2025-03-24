using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
{
    public class BusinessOwner
    {
        public int BusinessID { get; set; }
        public int UserID { get; set; }
        public string BankAccount { get; set; }
        public string BusinessType { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<Rider> Riders { get; set; }
        public virtual ICollection<RiderAssignment> RiderAssignments { get; set; }
    }
}