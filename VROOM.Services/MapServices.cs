using System;
using System.Threading.Tasks;
using VROOM.Data;
using VROOM.Models;
using VROOM.Models.Dtos;
using VROOM.Models.Map;
using VROOM.Repositories;

namespace VROOM.Services
{
    public class MapService
    {
        private readonly IMapRepository _mapRepository;
        private readonly VroomDbContext dbContext;
        public MapService(IMapRepository mapRepository, VroomDbContext _dbContext)
        {
            _mapRepository = mapRepository ?? throw new ArgumentNullException(nameof(mapRepository));
            dbContext = _dbContext;
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

        public async Task<RouteDto> FetchOptimizedRouteAsync(int shipmentId)
        {
            var shipment = dbContext.Shipments.FirstOrDefault(sh => sh.Id == shipmentId);
            if (shipment == null)
            {
                throw new ArgumentException("Shipment not found.");
            }
            var origin = shipment.BeginningArea;
            var destination = shipment.EndArea;
            if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
            {
                throw new ArgumentException("Origin and destination cannot be empty.");
            }

            var route = await _mapRepository.GetOptimizedRouteAsync(shipmentId);
            return new RouteDto
            {
                Id = route.Id,
                ShipmentID = route.Id,
                OriginArea = route.OriginArea,
                DestinationArea = route.DestinationArea,
                Waypoints = route.Waypoints,
                Start = route.Start,
                End = route.End
            };
        }
    }
}