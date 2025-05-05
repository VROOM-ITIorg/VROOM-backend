using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using VROOM.Models;
using VROOM.Models.Map;
using VROOM.Data;
using Microsoft.EntityFrameworkCore;
//using Microsoft.AspNetCore.Routing;

namespace VROOM.Repositories
{
    public class MapRepository : IMapRepository
    {
        private readonly string _tomtomGeocodeUrl = "https://api.tomtom.com/search/2/geocode/{0}.json?key={1}";
        private readonly string _tomtomRoutingUrl = "https://api.tomtom.com/routing/1/calculateRoute/{0}/json?key={1}";
        private readonly string _apiKey = "4FPrDSd5MMo8AUYzSjjEbG609IWL9lY5";
        private readonly HttpClient _httpClient;
        private readonly VroomDbContext _dbContext;

        public MapRepository(HttpClient httpClient, VroomDbContext dbContext)
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
                string requestUrl = string.Format(_tomtomGeocodeUrl, Uri.EscapeDataString(locationName), _apiKey);
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
                Console.WriteLine($"Geocode Request URL: {requestUrl}");
                Console.WriteLine($"Geocode Response Status Code: {response.StatusCode}");
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Geocode Response Body: {jsonResponse}");
                var tomtomResponse = JsonConvert.DeserializeObject<TomTomGeocodeResponse>(jsonResponse);

                if (tomtomResponse?.Results == null || tomtomResponse.Results.Length == 0)
                {
                    throw new Exception("No coordinates found for the given location.");
                }

                var result = tomtomResponse.Results
                    .Where(r => r.EntityType == "Municipality" || r.EntityType == "MunicipalitySubdivision")
                    .OrderByDescending(r => r.MatchConfidence.Score)
                    .FirstOrDefault()
                    ?? tomtomResponse.Results.OrderByDescending(r => r.MatchConfidence.Score).First();

                map.Latitude = result.Position.Lat;
                map.Longitude = result.Position.Lon;

                Console.WriteLine($"Selected Result: Type={result.Type}, EntityType={result.EntityType}, Lat={result.Position.Lat}, Lon={result.Position.Lon}, Address={result.Address.FreeformAddress}");

                return map;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching coordinates from TomTom API: {ex.Message}", ex);
            }
        }

        public async Task<Route> GetOptimizedRouteAsync( int shipmentId)
        {
            var shipment = _dbContext.Shipments.FirstOrDefault(sh => sh.Id == shipmentId);
            var origin = shipment.BeginningArea;
            var destination = shipment.EndArea;
            if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
            {
                throw new ArgumentException("Origin and destination cannot be empty.");
            }

            // Step 1: Get coordinates for origin and destination (needed for Shipment)
            var originMap = await GetCoordinatesAsync(origin);
            var destMap = await GetCoordinatesAsync(destination);

            // Step 2: Validate coordinates
            if (!IsValidCoordinate(originMap.Latitude, originMap.Longitude) || !IsValidCoordinate(destMap.Latitude, destMap.Longitude))
            {
                throw new ArgumentException("Invalid coordinates for origin or destination.");
            }

            // Step 3: Validate or create Shipment
            try
            {
                var shipmentExists = await _dbContext.Shipments.AnyAsync(s => s.Id == shipmentId);
                if (!shipmentExists)
                {
                    // Create test Shipment
                    var testShipment = new Shipment
                    {
                        Id = shipmentId,
                        //StartId = 1,
                        //EndId = 2,
                        RiderID = "e5e945d2-c0a3-414d-9879-9e5e2b16b81c",
                        BeginningLang = originMap.Longitude,
                        BeginningLat = originMap.Latitude,
                        BeginningArea = origin,
                        EndLang = destMap.Longitude,
                        EndLat = destMap.Latitude,
                        EndArea = destination,
                        MaxConsecutiveDeliveries = 5,
                        ModifiedAt = DateTime.Now,
                        ModifiedBy = "System",
                        IsDeleted = false
                    };
                    Console.WriteLine($"Attempting to create shipment: ID={shipmentId}, StartId=1, EndId=2, RiderID=e5e945d2-c0a3-414d-9879-9e5e2b16b81c, BeginningArea={origin}, BeginningLat={originMap.Latitude}, BeginningLang={originMap.Longitude}, EndArea={destination}, EndLat={destMap.Latitude}, EndLang={destMap.Longitude}, MaxConsecutiveDeliveries=5");
                    _dbContext.Shipments.Add(testShipment);
                    try
                    {
                        await _dbContext.SaveChangesAsync();
                        Console.WriteLine($"Successfully created test shipment with ID: {shipmentId}");
                    }
                    catch (Exception saveEx)
                    {
                        Console.WriteLine($"Shipment creation failed: {saveEx.Message}");
                        throw new Exception($"Failed to save shipment with ID {shipmentId}: {saveEx.Message}", saveEx);
                    }
                }
                else
                {
                    Console.WriteLine($"Shipment with ID: {shipmentId} already exists.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Shipment validation failed: {ex.Message}");
                throw new Exception($"Failed to create or validate shipment with ID {shipmentId}: {ex.Message}", ex);
            }

            // Step 4: Prepare routing request
            string coordinates = $"{originMap.Latitude},{originMap.Longitude}:{destMap.Latitude},{destMap.Longitude}";
            string requestUrl = string.Format(_tomtomRoutingUrl, coordinates, _apiKey) + "&travelMode=car&traffic=live";

            try
            {
                // Step 5: Call the TomTom Routing API
                Console.WriteLine($"Route Request URL: {requestUrl}");
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
                Console.WriteLine($"Route Response Status Code: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Route Error Response Body: {errorResponse}");
                    throw new HttpRequestException($"TomTom API returned {response.StatusCode}: {errorResponse}");
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Route Response Body: {jsonResponse}");
                var routeResponse = JsonConvert.DeserializeObject<TomTomRoutingResponse>(jsonResponse);

                Console.WriteLine($"Route Response Validation: RoutesCount={(routeResponse?.Routes?.Length ?? 0)}");

                if (routeResponse == null || routeResponse.Routes == null || !routeResponse.Routes.Any())
                {
                    throw new Exception($"No valid route found in TomTom API response. Response: {jsonResponse}");
                }

                var optimizedRoute = routeResponse.Routes.FirstOrDefault();
                if (optimizedRoute == null || optimizedRoute.Legs == null || !optimizedRoute.Legs.Any())
                {
                    throw new Exception($"No route data returned from TomTom API. Response: {jsonResponse}");
                }

                // Step 6: Map API response to Route entity
                var route = new Route
                {
                    ShipmentID = shipmentId,
                    OriginArea = origin,
                    DestinationArea = destination,
                    Waypoints = optimizedRoute.Legs.First().Points != null
                        ? EncodePolyline(optimizedRoute.Legs.First().Points)
                        : string.Empty,
                    Start = DateTime.Now,
                    End = DateTime.Now.AddSeconds(optimizedRoute.Summary.TravelTimeInSeconds),
                    SafetyIndex = 0.0f,
                    dateTime = DateTime.Now,
                    ModifiedBy = "System",
                    ModifiedAt = DateTime.Now
                };

                // Step 7: Save to database
                Console.WriteLine($"Attempting to save route for ShipmentID: {shipmentId}");
                _dbContext.Routes.Add(route);
                try
                {
                    await _dbContext.SaveChangesAsync();
                    Console.WriteLine($"Successfully saved route for ShipmentID: {shipmentId}");
                }
                catch (Exception routeEx)
                {
                    Console.WriteLine($"Route save failed: {routeEx.Message}");
                    throw new Exception($"Failed to save route for ShipmentID {shipmentId}: {routeEx.Message}", routeEx);
                }

                return route;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Route processing failed: {ex.Message}");
                throw new Exception($"Error fetching optimized route from TomTom API: {ex.Message}", ex);
            }
        }

        private bool IsValidCoordinate(double latitude, double longitude)
        {
            return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
        }

        private string EncodePolyline(Point[] points)
        {
            var coords = points.Select(p => new[] { p.Latitude, p.Longitude }).ToArray();
            return JsonConvert.SerializeObject(coords);
        }

        internal class TomTomGeocodeResponse
        {
            [JsonProperty("results")]
            public TomTomResult[] Results { get; set; }
        }

        internal class TomTomResult
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("entityType")]
            public string EntityType { get; set; }

            [JsonProperty("position")]
            public TomTomPosition Position { get; set; }

            [JsonProperty("address")]
            public TomTomAddress Address { get; set; }

            [JsonProperty("matchConfidence")]
            public TomTomMatchConfidence MatchConfidence { get; set; }
        }

        internal class TomTomAddress
        {
            [JsonProperty("freeformAddress")]
            public string FreeformAddress { get; set; }
        }

        internal class TomTomMatchConfidence
        {
            [JsonProperty("score")]
            public double Score { get; set; }
        }

        internal class TomTomPosition
        {
            [JsonProperty("lat")]
            public double Lat { get; set; }

            [JsonProperty("lon")]
            public double Lon { get; set; }
        }

        internal class TomTomRoutingResponse
        {
            [JsonProperty("routes")]
            public TomTomRoute[] Routes { get; set; }
        }

        internal class TomTomRoute
        {
            [JsonProperty("summary")]
            public TomTomSummary Summary { get; set; }

            [JsonProperty("legs")]
            public TomTomLeg[] Legs { get; set; }
        }

        internal class TomTomLeg
        {
            [JsonProperty("points")]
            public Point[] Points { get; set; }
        }

        internal class TomTomSummary
        {
            [JsonProperty("travelTimeInSeconds")]
            public int TravelTimeInSeconds { get; set; }
        }

        internal class Point
        {
            [JsonProperty("latitude")]
            public double Latitude { get; set; }

            [JsonProperty("longitude")]
            public double Longitude { get; set; }
        }
    }
}