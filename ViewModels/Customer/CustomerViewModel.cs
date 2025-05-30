using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.ViewModels;


    namespace VROOM.ViewModels
    {
    // ViewModel For Geting The Customers for Business Owner in the order creation
    public record CustomerVM
        {
            public string UserID { get; init; }
            public string Name { get; init; }
            public string Email { get; init; }

            public string PhoneNumber { get; set; }
            public LocationDto? Location { get; init; }
           

    }
    }

