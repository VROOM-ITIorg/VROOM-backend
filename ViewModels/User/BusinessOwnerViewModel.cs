using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Models;

namespace ViewModels.User
{
    public class BusinessOwnerViewModel
    {
        public string UserID { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public IFormFile? ProfilePicture { get; init; }

        public string? ImagePath { get; init; }
        public Address? Address { get; set; }
        public string? BankAccount { get; set; }
        public string? BusinessType { get; set; }


    }
}
