using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Repositories.VROOM.Repositories;

namespace VROOM.Services
{
    public class RiderService
    {
        private readonly RiderRepository _riderRepository;
        private readonly MyDbContext _context;
        private readonly DbSet<Order> _orders;
        private readonly DbSet<OrderRider> _orderRiders;

        public RiderService(RiderRepository riderRepository, MyDbContext context)
        {
            _riderRepository = riderRepository ?? throw new ArgumentNullException(nameof(riderRepository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _orders = context.Set<Order>();
            _orderRiders = context.Set<OrderRider>();
        }

        public async Task<Rider> RegisterRiderAsync(Rider rider)
        {
            if (rider == null)
                throw new ArgumentNullException(nameof(rider));

            if (rider.User == null || string.IsNullOrEmpty(rider.UserID))
                throw new InvalidOperationException("Rider must be associated with a user profile.");

            if (rider.Status == default)
                rider.Status = RiderStatus.Available;

            return await _riderRepository.AddAsync(rider);
        }

        public async Task<Rider> GetRiderProfileAsync(int riderId)
        {
            var rider = await _riderRepository.GetByIdAsync(riderId);
            if (rider == null)
                throw new KeyNotFoundException($"Rider with ID {riderId} not found.");

            return rider;
        }

        public async Task<List<Order>> GetAssignedOrdersAsync(int riderId)
        {
            var rider = await _riderRepository.GetByIdAsync(riderId);
            if (rider == null)
                throw new KeyNotFoundException($"Rider with ID {riderId} not found.");

            var assignedOrders = await _orderRiders
                .Where(or => or.RiderID == riderId && !or.IsDeleted)
                .Join(
                    _orders.Where(o => !o.IsDeleted && o.State == OrderState.Pending),
                    or => or.OrderID,
                    o => o.Id,
                    (or, o) => o)
                .Include(o => o.Customer)
                .Include(o => o.OrderRoute)
                .ToListAsync();

            return assignedOrders;
        }

        public async Task<Order> AcceptOrderAsync(int riderId, int orderId)
        {
            var rider = await _riderRepository.GetByIdAsync(riderId);
            if (rider == null)
                throw new KeyNotFoundException($"Rider with ID {riderId} not found.");

            if (rider.Status != RiderStatus.Available)
                throw new InvalidOperationException($"Rider with ID {riderId} is not available to accept orders. Current status: {rider.Status}.");

            var orderRider = await _orderRiders
                .FirstOrDefaultAsync(or => or.OrderID == orderId && or.RiderID == riderId && !or.IsDeleted);
            if (orderRider == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found or not assigned to this rider.");

            var order = await _orders
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
            if (order == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");

            if (order.State != OrderState.Pending)
                throw new InvalidOperationException($"Order with ID {orderId} cannot be accepted. Current state: {order.State}.");

            order.State = OrderState.Confirmed;
            order.ModifiedBy = rider.UserID;
            order.ModifiedAt = DateTime.UtcNow;

            rider.Status = RiderStatus.OnDelivery;

            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order> RejectOrderAsync(int riderId, int orderId)
        {
            var rider = await _riderRepository.GetByIdAsync(riderId);
            if (rider == null)
                throw new KeyNotFoundException($"Rider with ID {riderId} not found.");

            var orderRider = await _orderRiders
                .FirstOrDefaultAsync(or => or.OrderID == orderId && or.RiderID == riderId && !or.IsDeleted);
            if (orderRider == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found or not assigned to this rider.");

            var order = await _orders
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
            if (order == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");

            if (order.State != OrderState.Pending)
                throw new InvalidOperationException($"Order with ID {orderId} cannot be rejected. Current state: {order.State}.");

            order.State = OrderState.Cancelled;
            order.ModifiedBy = rider.UserID;
            order.ModifiedAt = DateTime.UtcNow;

            orderRider.IsDeleted = true;
            orderRider.ModifiedBy = rider.UserID;
            orderRider.ModifiedAt = DateTime.UtcNow;

            rider.Status = RiderStatus.Available;

            order.RiderID = 0;

            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order> UpdateDeliveryStatusAsync(int riderId, int orderId, OrderState newState)
        {
            var rider = await _riderRepository.GetByIdAsync(riderId);
            if (rider == null)
                throw new KeyNotFoundException($"Rider with ID {riderId} not found.");

            var orderRider = await _orderRiders
                .FirstOrDefaultAsync(or => or.OrderID == orderId && or.RiderID == riderId && !or.IsDeleted);
            if (orderRider == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found or not assigned to this rider.");

            var order = await _orders
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
            if (order == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");

            if (!IsValidStateTransition(order.State, newState))
                throw new InvalidOperationException($"Cannot transition from {order.State} to {newState}.");

            order.State = newState;
            order.ModifiedBy = rider.UserID;
            order.ModifiedAt = DateTime.UtcNow;

            if (newState == OrderState.Delivered || newState == OrderState.Cancelled)
            {
                orderRider.IsDeleted = true;
                orderRider.ModifiedBy = rider.UserID;
                orderRider.ModifiedAt = DateTime.UtcNow;
                order.RiderID = 0;
                rider.Status = RiderStatus.Available;
            }

            await _context.SaveChangesAsync();
            return order;
        }

        private bool IsValidStateTransition(OrderState currentState, OrderState newState)
        {
            return (currentState, newState) switch
            {
                (OrderState.Confirmed, OrderState.Shipped) => true,
                (OrderState.Shipped, OrderState.Delivered) => true,
                (OrderState.Confirmed, OrderState.Cancelled) => true,
                (OrderState.Shipped, OrderState.Cancelled) => true,
                _ => false
            };
        }
    }
}