using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewModels.User
{
    public class CustomerAddViewModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        // this will come from token
        public string BussnisOwnerId { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string Name { get; set; }

    }
}
