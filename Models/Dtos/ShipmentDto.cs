namespace VROOM.Models.Dtos
{
    public class ShipmentDto
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public string? RiderID { get; set; }
        public double BeginningLat { get; set; }
        public double BeginningLang { get; set; }
        public string BeginningArea { get; set; }
        public double EndLat { get; set; }
        public double EndLang { get; set; }
        public string EndArea { get; set; }
        public ZoneEnum Zone { get; set; }
        public int MaxConsecutiveDeliveries { get; set; }
        public DateTime? InTransiteBeginTime { get; set; }
        public DateTime? RealEndTime { get; set; }
        public ShipmentStateEnum ShipmentState { get; set; }
        public List<WaypointDto> Waypoints { get; set; }
        public List<TheRouteDto> Routes { get; set; }
    }

    public class TheRouteDto
    {
        public int Id { get; set; }
        public double OriginLat { get; set; }
        public double OriginLang { get; set; }
        public string OriginArea { get; set; }
        public double DestinationLat { get; set; }
        public double DestinationLang { get; set; }
        public string DestinationArea { get; set; }
        public DateTime Start { get; set; }
        public DateTime DateTime { get; set; }
        public float SafetyIndex { get; set; }
        public int? ShipmentID { get; set; }
        public List<int> OrderIds { get; set; }
    }

    public class WaypointDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Area { get; set; }
    }
}