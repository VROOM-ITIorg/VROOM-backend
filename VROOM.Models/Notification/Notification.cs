using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        public int UserID { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }

        public virtual User User { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; } = DateTime.Now;
    }
}