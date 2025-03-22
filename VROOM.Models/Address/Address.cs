using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
{
    public class Address
    {
        [Key, ForeignKey("User")]
        public int UserID { get; set; }
        public string Lang { get; set; }
        public float Lat { get; set; }
        public string Area { get; set; }

        public User User { get; set; }
    }
}
