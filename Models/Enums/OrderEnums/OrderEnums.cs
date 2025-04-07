using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VROOM.Models
{
    public enum OrderStateEnum
    {
        Created,
        Pending,
        Confirmed,
        Shipped,
        Delivered,
        Cancelled
    }


    public enum OrderPriorityEnum
    {
        Standard,
        Express,
        Urgent
    }



    public enum CustomerPriorityEnum
    {
        FirstTime = 1,  // New customers making their first purchase
        Regular = 2,    // Returning customers with standard benefits
        VIP = 3         // High-value customers with premium privileges
    }


}
