using System;
using System.Threading.Tasks;
using VROOM.Models;
using VROOM.Models.Map;
using VROOM.Repositories;

namespace VROOM.Services
{
    public class MapService
    {
        private readonly IMapRepository _mapRepository;

        public MapService(IMapRepository mapRepository)
        {
            _mapRepository = mapRepository ?? throw new ArgumentNullException(nameof(mapRepository));
        }

        public async Task<MapModel> FetchCoordinatesAsync(string locationName)
        {
            if (string.IsNullOrWhiteSpace(locationName))
            {
                throw new ArgumentException("Location name cannot be empty.", nameof(locationName));
            }

            try
            {
                return await _mapRepository.GetCoordinatesAsync(locationName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in MapService: {ex.Message}", ex);
            }
        }
    }
}