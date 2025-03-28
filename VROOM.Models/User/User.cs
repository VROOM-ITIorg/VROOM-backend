using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace VROOM.Models
{
    public class User : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Url] 
        public string? ProfilePicture { get; set; }

        public virtual Address Address { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        public virtual Customer? Customer { get; set; }
        public virtual BusinessOwner? BusinessOwner { get; set; }
        public virtual Rider? Rider { get; set; }

        // Soft delete flag
        public bool IsDeleted { get; set; } = false;

        // Audit properties
        public string? ModifiedBy { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? ModifiedAt { get; set; } = DateTime.UtcNow;

    }
}