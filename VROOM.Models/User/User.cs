// User.cs 
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }

        public Address Address { get; set; }
        public ICollection<Notification> Notifications { get; set; }
        public Customer Customer { get; set; }
        public BusinessOwner BusinessOwner { get; set; }
        public Rider Rider { get; set; }
        public ICollection<UserRole> UserRoles { get; set; }
    }
}