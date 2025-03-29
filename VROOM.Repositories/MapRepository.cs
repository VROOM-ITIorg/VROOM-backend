using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using VROOM.Models;
using VROOM.Models.Map;

namespace VROOM.Repositories
{
    public class MapRepository : IMapRepository
    {
        private readonly string _radarGeocodeUrl = "https://api.radar.io/v1/geocode/forward?query={0}";
        private readonly HttpClient _httpClient;

        public MapRepository(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
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
                Console.WriteLine($"Request URL: {requestUrl}");
                Console.WriteLine($"Authorization Header: {_httpClient.DefaultRequestHeaders.Authorization}");
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
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
    }
}