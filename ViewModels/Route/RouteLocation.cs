using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewModels.Route
{
    public class RouteLocation
    {
        public double OriginLang { get; set; }
        public double OriginLat { get; set; }
        public string OriginArea { get; set; }
        public double DestinationLang { get; set; }
        public double DestinationLat { get; set; }
        public string DestinationArea { get; set; }
    }
}
