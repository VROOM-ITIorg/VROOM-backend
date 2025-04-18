using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModels.Route;
using ViewModels.Shipment;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Repository;

namespace VROOM.Services
{
    public class ShipmentServices
    {
        public readonly ShipmentRepository shipmentRepository;
        public readonly RouteRepository routeRepository;

        public ShipmentServices(ShipmentRepository _shipmentRepository, RouteRepository _routeRepository) {
        
            shipmentRepository = _shipmentRepository;
            routeRepository = _routeRepository;
        }

        public async Task CreateShipment(AddShipmentVM addShipmentVM, Route selectedRoute)
        {
            var shipment = new Shipment
            {
               startTime = DateTime.Now,
               EndTime = DateTime.Now,
               RiderID = addShipmentVM.RiderID,
               BeginningLang = addShipmentVM.BeginningLang,
               BeginningLat = addShipmentVM.BeginningLat,
               BeginningArea = addShipmentVM.BeginningArea,
               EndLang = addShipmentVM.EndLang,
               EndLat = addShipmentVM.EndLat,
               EndArea = addShipmentVM.EndArea,
               MaxConsecutiveDeliveries = addShipmentVM.MaxConsecutiveDeliveries,
            };

            shipmentRepository.Add(shipment);
            shipmentRepository.CustomSaveChanges();

            selectedRoute.ShipmentID = shipment.Id;
            routeRepository.Update(selectedRoute);
            routeRepository.CustomSaveChanges();
        }
    }
}
//public DateTime startTime { get; set; }
//public DateTime EndTime { get; set; }
//public string RiderID { get; set; }

//public double BeginningLang { get; set; }
//public double BeginningLat { get; set; }
//public string BeginningArea { get; set; }

//public double EndLang { get; set; }
//public double EndLat { get; set; }
//public string EndArea { get; set; }

//public int MaxConsecutiveDeliveries { get; set; }