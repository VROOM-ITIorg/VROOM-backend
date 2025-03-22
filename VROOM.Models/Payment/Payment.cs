using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }
        public int OrderID { get; set; }
        public string Method { get; set; }
        public decimal Amount { get; set; }

        public Order Order { get; set; }
    }
}