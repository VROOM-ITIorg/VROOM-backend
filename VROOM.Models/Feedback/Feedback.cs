using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class Feedback
    {
        public int Id { get; set; }
        public int RiderID { get; set; }
        public int CustomerID { get; set; }
        public int Rating { get; set; }
        public string Message { get; set; }

        public virtual Rider Rider { get; set; }
        public virtual Customer Customer { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
    }
}
