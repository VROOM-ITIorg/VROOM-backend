using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewModels.Route
{
    public class RouteResponseVM
    {
        public double DistanceMeters { get; set; }
        public double TimeSeconds { get; set; }
        public double[][] Coordinates { get; set; }
    }
}
