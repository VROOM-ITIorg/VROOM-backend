using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class Issues
    {
        [Key]
        public int Id { get; set; }
        public int RiderID { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }

        public virtual Rider Rider { get; set; }
        public virtual RiderRouteIssue RiderRouteIssue { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}