using Microsoft.EntityFrameworkCore;
using VROOM.Data;
using VROOM.Models;

namespace VROOM.Repositories
{
    public class ShipmentRepository : BaseRepository<Shipment>
    {
        public ShipmentRepository(VroomDbContext context) : base(context) { }

        public async Task<Shipment> GetShipmentByIdAsync(int shipmentId)
        {
            return await context.Shipments
                .FirstOrDefaultAsync(s => s.Id == shipmentId && !s.IsDeleted);
        }
    }




        //// Update Shipment Status

        //public void UpdateStatus(int shipmentId, ShipmentStatus newStatus)
        //{
        //    var shipment = _context.Shipments.Find(shipmentId);
        //    if (shipment != null)
        //    {
        //        shipment.Status = newStatus;
        //        _context.SaveChanges();
        //    }
        //}

        //// Bring Shipments for a specific Rider
        //public List<Shipment> GetByRider(int riderId)
        //{
        //    return _context.Shipments
        //        .Where(s => s.RiderID == riderId)
        //        .ToList();
        //}

        //// Bring Shipments according to Status
        //public List<Shipment> GetByStatus(ShipmentStatus status)
        //{
        //    return _context.Shipments
        //        .Where(s => s.Status == status)
        //        .ToList();
        //}

        //// Track a specific Shipment
        //public Shipment TrackShipment(int shipmentId)
        //{
        //    return _context.Shipments.Include(s => s.Rider).Include(s => s.Route).FirstOrDefault(s => s.Id == shipmentId);
        //}


        //// Cancel Shipment (soft delete)
        //public void CancelShipment(int shipmentId)
        //{
        //    var shipment = _context.Shipments.Find(shipmentId);
        //    if (shipment != null)
        //    {
        //        shipment.Status = ShipmentStatus.Cancelled;
        //        _context.SaveChanges();
        //    }
        //}

        //// Shipments related to a specific business
        //public List<Shipment> GetByBusiness(int businessId)
        //{
        //    return _context.Shipments.Include(s => s.Rider)
        //        .Where(s => s.Rider.BusinessID == businessId)
        //        .ToList();
        //}



        //// Reset the charge for a new Rider
        //public void ReassignShipment(int shipmentId, int newRiderId)
        //{
        //    var shipment = _context.Shipments
        //        .Include(s => s.Rider)
        //        .FirstOrDefault(s => s.Id == shipmentId);

        //    if (shipment != null)
        //    {
        //        shipment.RiderID = newRiderId;
        //        shipment.ModifiedAt = DateTime.Now;
        //        _context.SaveChanges();
        //    }
        //}
    }


