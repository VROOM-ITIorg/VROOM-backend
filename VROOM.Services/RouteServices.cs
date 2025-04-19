using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ViewModels.Route;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;

namespace VROOM.Services
{
    public class RouteServices
    {
        private RouteRepository routeRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;
        private readonly ILogger<RouteServices> _logger;
        public RouteServices(RouteRepository _routeRepository, IHttpClientFactory httpClient, IConfiguration configuration, ILogger<RouteServices> logger)
        {
            routeRepository = _routeRepository;
            _httpClientFactory = httpClient;
            _apiKey = configuration["GraphHopper:ApiKey"];
            _logger = logger;

        }
        public async Task<RouteResponseVM> GetRouteAsync(double fromLat, double fromLon, double toLat, double toLon)
        {
            string url = $"https://graphhopper.com/api/1/route?point={fromLat},{fromLon}&point={toLat},{toLon}&profile=car&locale=en&calc_points=true&key={_apiKey}";

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("GraphHopper API error: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"GraphHopper API returned {response.StatusCode}: {errorContent}");
            }

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("GraphHopper API response: {Response}", json);

            var routeData = JsonSerializer.Deserialize<JsonElement>(json);

            // Check for error message in response
            if (routeData.TryGetProperty("message", out var messageElement))
            {
                var errorMessage = messageElement.GetString();
                _logger.LogError("GraphHopper API error message: {ErrorMessage}", errorMessage);
                throw new Exception($"GraphHopper API error: {errorMessage}");
            }

            if (!routeData.TryGetProperty("paths", out var paths) || paths.GetArrayLength() == 0)
            {
                _logger.LogWarning("No routes found for the given coordinates.");
                throw new Exception("No route found.");
            }

            var path = paths[0];
            var distance = path.GetProperty("distance").GetDouble();
            var time = path.GetProperty("time").GetInt64();

            // Handle points property safely
            double[][] coordinates;
            if (path.TryGetProperty("points", out var pointsElement))
            {
                if (pointsElement.ValueKind == JsonValueKind.String)
                {
                    // Handle case where points is a string (e.g., GeoJSON)
                    _logger.LogWarning("Points property is a string, not an object. Attempting to parse as GeoJSON.");
                    coordinates = JsonSerializer.Deserialize<double[][]>(pointsElement.GetString());
                }
                else
                {
                    coordinates = JsonSerializer.Deserialize<double[][]>(pointsElement.GetProperty("coordinates").GetRawText());
                }
            }
            else
            {
                _logger.LogWarning("Points property not found in response.");
                coordinates = Array.Empty<double[]>();
            }

            return new RouteResponseVM
            {
                DistanceMeters = distance,
                TimeSeconds = time / 1000.0,
                Coordinates = coordinates
            };
        }
        public async Task<Route> CreateRoute(RouteLocation routeLocation)
        {
            var route = new Route
            {
                OriginLang = routeLocation.OriginLang,
                OriginLat = routeLocation.OriginLat,
                OriginArea = routeLocation.OriginArea,
                DestinationLang = routeLocation.DestinationLang,
                DestinationLat = routeLocation.DestinationLat,
                DestinationArea = routeLocation.DestinationArea,
                Start = DateTime.Now,
                dateTime = DateTime.Now,
            };

            routeRepository.Add(route);
            routeRepository.CustomSaveChanges();

            return route;
        }
    }
}
