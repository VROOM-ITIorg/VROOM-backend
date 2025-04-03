using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VROOM.Models
{
    public enum OrderStateEnum
    {
        // Active Request Statuses

        Pending,            // The order is awaiting approval
        Confirmed,          // The order has been confirmed by the store or sender
        Assigned,           // The order has been assigned to a rider
        PickedUp,           // The order has been received from the sender
        InTransit,          // The order is on its way to the customer
        Delivered,          // The order has been successfully delivered


        // Inactive order statuses

        Cancelled,          // The order was canceled by the customer or store
        Failed,             // Delivery failed (e.g., the customer did not respond)
        Returned,           // The order was returned to the sender
        Archived,           // The order was archived after completion
        Deleted,            // The order was permanently deleted
    }
}
