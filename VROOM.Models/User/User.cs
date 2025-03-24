// User.cs 
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Principal;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using VROOM.Models;

namespace VROOM.Models
{
    public class User : IdentityUser
    {
        public string Name { get; set; }
        public string? ProfilePicture { get; set; } // URL to profile picture




        public virtual Address Address { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual Customer? Customer { get; set; }
        public virtual BusinessOwner? BusinessOwner { get; set; }
        public virtual Rider? Rider { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; } = DateTime.Now;
    }
}