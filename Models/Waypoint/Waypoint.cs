using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VROOM.Models
{
    public class Waypoint
    {
        public int Id { get; set; }
        public double Lang { get; set; }
        public double Lat { get; set; }
        public string Area { get; set; }
        public int ShipmentID { get; set; }
        public virtual Shipment Shipment { get; set; }

        public int orderId { get; set; }
        public virtual Order Order { get; set; }
    }
}
