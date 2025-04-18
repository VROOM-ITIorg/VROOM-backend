using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewModels.Shipment
{
    public class AddShipmentVM
    {
        public DateTime startTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string RiderID { get; set; }

        public double BeginningLang { get; set; }
        public double BeginningLat { get; set; }
        public string BeginningArea { get; set; }

        public double EndLang { get; set; }
        public double EndLat { get; set; }
        public string EndArea { get; set; }

        public int MaxConsecutiveDeliveries { get; set; }
    }
}
