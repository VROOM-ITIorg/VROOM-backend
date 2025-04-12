using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using VROOM.Models; // For Route
using VROOM.Models.Map;
using VROOM.Data; // For MyDbContext

namespace VROOM.Repositories
{
    public class MapRepository : IMapRepository
    {
        private readonly string _radarGeocodeUrl = "https://api.radar.io/v1/geocode/forward?query={0}";
        private readonly string _radarRouteOptimizeUrl = "https://api.radar.io/v1/route/optimize?locations={0}&mode=car&units=imperial";
        private readonly HttpClient _httpClient;
        private readonly MyDbContext _dbContext; // Use MyDbContext instead of VroomDbContext

        public MapRepository(HttpClient httpClient, MyDbContext dbContext)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<MapModel> GetCoordinatesAsync(string locationName)
        {
            if (string.IsNullOrWhiteSpace(locationName))
            {
                throw new ArgumentException("Location name cannot be empty.", nameof(locationName));
            }

            var map = new MapModel { LocationName = locationName };

            try
            {
                string requestUrl = string.Format(_radarGeocodeUrl, Uri.EscapeDataString(locationName));
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
                Console.WriteLine($"Request URL: {requestUrl}");
                Console.WriteLine($"Authorization Header: {_httpClient.DefaultRequestHeaders.Authorization}");
                Console.WriteLine($"Response Status Code: {response.StatusCode}");
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Body: {jsonResponse}");
                var radarResponse = JsonConvert.DeserializeObject<RadarGeocodeResponse>(jsonResponse);

                if (radarResponse?.Addresses == null || radarResponse.Addresses.Length == 0)
                {
                    throw new Exception("No coordinates found for the given location.");
                }

                var address = radarResponse.Addresses[0];
                map.Latitude = address.Latitude;
                map.Longitude = address.Longitude;

                return map;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching coordinates from Radar API: {ex.Message}", ex);
            }
        }

        public async Task<Route> GetOptimizedRouteAsync(string origin, string destination, int shipmentId)
        {
            if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
            {
                throw new ArgumentException("Origin and destination cannot be empty.");
            }

            // Step 1: Get coordinates for origin and destination
            var originMap = await GetCoordinatesAsync(origin);
            var destMap = await GetCoordinatesAsync(destination);

            // Step 2: Format locations for the API
            string locations = $"{originMap.Latitude},{originMap.Longitude}|{destMap.Latitude},{destMap.Longitude}";
            string requestUrl = string.Format(_radarRouteOptimizeUrl, Uri.EscapeDataString(locations));

            try
            {
                // Step 3: Call the route optimization API
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
                Console.WriteLine($"Route Request URL: {requestUrl}");
                Console.WriteLine($"Authorization Header: {_httpClient.DefaultRequestHeaders.Authorization}");
                Console.WriteLine($"Response Status Code: {response.StatusCode}");
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Route Response Body: {jsonResponse}");
                var routeResponse = JsonConvert.DeserializeObject<RadarRouteResponse>(jsonResponse);

                // Step 4: Map API response to Route entity
                var route = new Route
                {
                    ShipmentID = shipmentId,
                    Origin = origin,
                    Destination = destination,
                    Waypoints = routeResponse.Route.Legs[0].Geometry.Polyline, // Polyline as waypoints
                    Start = DateTime.Now,
                    End = DateTime.Now.AddMinutes(routeResponse.Route.Duration.Value), // Duration in minutes
                    SafetyIndex = 0.0f, // Placeholder
                    DateTime = DateTime.Now,
                    ModifiedBy = "System", // Example; adjust based on auth context
                    ModifiedAt = DateTime.Now
                };

                // Step 5: Save to database using MyDbContext
                _dbContext.Routes.Add(route);
                await _dbContext.SaveChangesAsync();

                return route;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching optimized route from Radar API: {ex.Message}", ex);
            }
        }

        // Internal classes for geocoding response
        internal class RadarGeocodeResponse
        {
            [JsonProperty("addresses")]
            public RadarAddress[] Addresses { get; set; }
        }

        internal class RadarAddress
        {
            [JsonProperty("latitude")]
            public double Latitude { get; set; }

            [JsonProperty("longitude")]
            public double Longitude { get; set; }

            [JsonProperty("formattedAddress")]
            public string FormattedAddress { get; set; }
        }

        // Internal classes for route response
        internal class RadarRouteResponse
        {
            [JsonProperty("meta")]
            public Meta Meta { get; set; }

            [JsonProperty("route")]
            public RouteData Route { get; set; }
        }

        internal class Meta
        {
            [JsonProperty("code")]
            public int Code { get; set; }
        }

        internal class RouteData
        {
            [JsonProperty("distance")]
            public Distance Distance { get; set; }

            [JsonProperty("duration")]
            public Duration Duration { get; set; }

            [JsonProperty("legs")]
            public Leg[] Legs { get; set; }
        }

        internal class Distance
        {
            [JsonProperty("value")]
            public double Value { get; set; }

            [JsonProperty("text")]
            public string Text { get; set; }
        }

        internal class Duration
        {
            [JsonProperty("value")]
            public double Value { get; set; }

            [JsonProperty("text")]
            public string Text { get; set; }
        }

        internal class Leg
        {
            [JsonProperty("geometry")]
            public Geometry Geometry { get; set; }
        }

        internal class Geometry
        {
            [JsonProperty("polyline")]
            public string Polyline { get; set; }
        }
    }
}