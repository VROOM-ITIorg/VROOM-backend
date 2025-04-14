using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VROOM.Models
{
    public enum OrderState
    {
        Created,
        Pending,
        Confirmed,
        Shipped,
        Delivered,
        Cancelled
    }


    public enum OrderPriority
    {
        Standard,
        Express,
        Urgent
    }



    public enum CustomerPriority
    {
        FirstTime = 1,  // New customers making their first purchase
        Regular = 2,    // Returning customers with standard benefits
        VIP = 3         // High-value customers with premium privileges
    }


}