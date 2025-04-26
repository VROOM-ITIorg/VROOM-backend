using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VROOM.Models.Dtos
{
    public class RouteDto
    {
        public int Id { get; set; }
        public int ShipmentID { get; set; }
        public string OriginArea { get; set; }
        public string DestinationArea { get; set; }
        public string Waypoints { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
    }
}
