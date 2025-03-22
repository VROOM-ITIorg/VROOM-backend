using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class Feedback
    {
        [Key]
        public int Id { get; set; }
        public int RiderID { get; set; }
        public int CustomerID { get; set; }
        public int Rating { get; set; }
        public string Message { get; set; }

        public Rider Rider { get; set; }
        public Customer Customer { get; set; }
    }
}
