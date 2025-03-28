using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Data;
using VROOM.Models;

namespace VROOM.Repositories
{
    public class ShipmentRepository
    {
        private readonly MyDbContext _context;

        public ShipmentRepository(MyDbContext context)
        {
            _context = context;
        }

        // Create New Shipment
        public Shipment CreateShipment(int orderId)
        {
            var shipment = new Shipment
            {
                Id = orderId,
                Status = ShipmentStatus.Pending,
                Beginning = DateTime.Now
            };

            _context.Shipments.Add(shipment);
            _context.SaveChanges();

            return shipment;
        }

        // Update Shipment Status
        public void UpdateStatus(int shipmentId, ShipmentStatus newStatus)
        {
            var shipment = _context.Shipments.Find(shipmentId);
            if (shipment != null)
            {
                shipment.Status = newStatus;
                _context.SaveChanges();
            }
        }

        // Bring Shipments for a specific Rider
        public List<Shipment> GetByRider(int riderId) => _context.Shipments
                .Where(s => s.RiderID == riderId)
                .ToList();

        // Bring Shipments according to Status
        public List<Shipment> GetByStatus(ShipmentStatus status) => _context.Shipments
                .Where(s => s.Status == status)
                .ToList();

        // Track a specific Shipment
        //public Shipment TrackShipment(int shipmentId)
        //{
        //    return _context.Shipments
        //        .Include(s => s.)
        //            .ThenInclude(o => o.Assignment)
        //        .Include(s => s.Order.DeliveryProof)
        //        .FirstOrDefault(s => s.Id == shipmentId);
        //}



        // ✅ متوسط مدة التوصيل بالدقائق
        public double CalculateAverageDeliveryTime() => _context.Shipments
                .Where(s => s.Status == ShipmentStatus.Delivered)
                .Select(s => EF.Functions.DateDiffMinute(s.Beginning, s.End))
                .Average();

        // ✅ شحنات اليوم فقط
        public List<Shipment> GetTodayShipments()
        {
            var today = DateTime.Today;
            return _context.Shipments
                .Where(s => s.Beginning == today)
                .ToList();
        }

        // Cancel Shipment (soft delete)
        public void CancelShipment(int shipmentId)
        {
            var shipment = _context.Shipments.Find(shipmentId);
            if (shipment != null)
            {
                shipment.Status = ShipmentStatus.Cancelled;
                _context.SaveChanges();
            }
        }

        // ✅ شحنات مرتبطة بـ Business معين
        //public List<Shipment> GetByBusiness(int businessId)
        //{
        //    return _context.Shipments
        //        .Where(s => s. == businessId)
        //        .ToList();
        //}

        // ✅ Late shipments (more than 24 hours and not delivered)
        public List<Shipment> GetDelayedShipments() => _context.Shipments
                .Where(s => s.Status != ShipmentStatus.Delivered && EF.Functions.DateDiffHour(s.Beginning, DateTime.Now) > 24)
                .ToList();

        // ✅ إعادة تعيين الشحنة لـ Rider جديد
        //public void ReassignShipment(int shipmentId, int newRiderId)
        //{
        //    var shipment = _context.Shipments
        //        .Include(s => s.Order)
        //        .ThenInclude(o => o.Assignment)
        //        .FirstOrDefault(s => s.Id == shipmentId);

        //    if (shipment?.Order?.Assignment != null)
        //    {
        //        shipment.Order.Assignment.RiderId = newRiderId;
        //        _context.SaveChanges();
        //    }
        //}
    }

}
