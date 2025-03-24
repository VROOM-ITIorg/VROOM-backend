// RiderAssignment.cs 
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class RiderAssignment
    {
        [Key, Column(Order = 0)]
        public int RiderID { get; set; }
        [Key, Column(Order = 1)]
        public int BusinessID { get; set; }
        public DateTime AssignmentDate { get; set; }

        public virtual Rider Rider { get; set; }
        public virtual BusinessOwner BusinessOwner { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; } = DateTime.Now;
    }
}