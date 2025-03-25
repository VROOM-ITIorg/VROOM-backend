using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int OrderID { get; set; }
        public string Method { get; set; }
        public decimal Amount { get; set; }

        public virtual Order Order { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; } = DateTime.Now;
    }
}