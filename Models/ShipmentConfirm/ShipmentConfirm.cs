using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VROOM.Models
{
    public class ShipmentConfirmation
    {
        public int ShipmentId { get; set; }
        public string RiderId { get; set; }
        public string BusinessOwnerId { get; set; }
        public DateTime ExpiryTime { get; set; }
        public ConfirmationStatus Status { get; set; }
    }

    public enum ConfirmationStatus
    {
        Pending,
        Accepted,
        Rejected
    }
}
