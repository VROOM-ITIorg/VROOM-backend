using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class OrderRoute
    {
        [Key, Column(Order = 0)]
        public int OrderID { get; set; }
        [Key, Column(Order = 1)]
        public int RouteID { get; set; }
        public string Status { get; set; }

        public virtual Order Order { get; set; }
        public virtual Route Route { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; } = DateTime.Now;
    }
}