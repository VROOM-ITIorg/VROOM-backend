using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace VROOM.Services
{
    public class MapModel
    {
        // Properties for the map data
        public string LocationName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Radar API configuration
        private readonly string _radarApiKey;
        private readonly string _radarGeocodeUrl = "https://api.radar.io/v1/geocode/forward?query={0}";
        private readonly HttpClient _httpClient;

        // Constructor
        public MapModel(string locationName, string radarApiKey, HttpClient httpClient)
        {
            LocationName = locationName ?? throw new ArgumentNullException(nameof(locationName));
            _radarApiKey = radarApiKey ?? throw new ArgumentNullException(nameof(radarApiKey));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            // Set the Authorization header for Radar API
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_radarApiKey}");
        }

        // Method to fetch coordinates using Radar's geocoding API
        public async Task FetchCoordinatesAsync()
        {
            try
            {
                // Construct the request URL
                string requestUrl = string.Format(_radarGeocodeUrl, Uri.EscapeDataString(LocationName));

                // Make the API call
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                // Read and parse the response
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var radarResponse = JsonConvert.DeserializeObject<RadarGeocodeResponse>(jsonResponse);

                // Check if we got any results
                if (radarResponse?.Addresses == null || radarResponse.Addresses.Length == 0)
                {
                    throw new Exception("No coordinates found for the given location.");
                }

                // Extract coordinates from the first result
                var address = radarResponse.Addresses[0];
                Latitude = address.Latitude;
                Longitude = address.Longitude;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching coordinates from Radar API: {ex.Message}", ex);
            }
        }
    }

    // Helper class to deserialize Radar API response
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