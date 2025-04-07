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
        public string  RiderID { get; set; }
        public string  BusinessID { get; set; }
        public DateTime AssignmentDate { get; set; }

        public virtual Rider Rider { get; set; }
        public virtual BusinessOwner BusinessOwner { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; } = DateTime.Now;
    }
}