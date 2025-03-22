using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
{
    public class BusinessOwner
    {
        [Key]
        public int BusinessID { get; set; }
        public int UserID { get; set; }
        public string BankAccount { get; set; }
        public string BusinessType { get; set; }

        public User User { get; set; }
        public ICollection<Rider> Riders { get; set; }
        public ICollection<RiderAssignment> RiderAssignments { get; set; }
    }
}