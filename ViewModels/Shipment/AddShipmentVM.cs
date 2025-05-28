using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Models;

namespace ViewModels.Shipment
{
    public class AddShipmentVM
    {
        public DateTime startTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime? InTransiteBeginTime { get; set; }
        public string? RiderID { get; set; }

        public double BeginningLang { get; set; }
        public double BeginningLat { get; set; }
        public string BeginningArea { get; set; }
        public List<int> OrderIds { get; set; }

        public double EndLang { get; set; }
        public double EndLat { get; set; }
        public string EndArea { get; set; }

        public ZoneEnum zone { get; set; }

        public int MaxConsecutiveDeliveries { get; set; }
    }
}
