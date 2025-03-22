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

        public Rider Rider { get; set; }
    }
}