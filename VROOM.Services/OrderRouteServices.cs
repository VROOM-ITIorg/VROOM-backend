using VROOM.Models;
using VROOM.Repositories;

namespace VROOM.Services
{
    public class OrderRouteServices
    {
        private readonly OrderRouteRepository orderRouteRepository;

        public OrderRouteServices(OrderRouteRepository _orderRouteRepository)
        {
            orderRouteRepository = _orderRouteRepository;
        }


        public async Task CreateOrderRoute(int orderId,int routeId)
        {
            var orderRoute = new OrderRoute
            {
                OrderID = orderId,
                RouteID = routeId,
            };

            orderRouteRepository.Add(orderRoute);
            orderRouteRepository.CustomSaveChanges();
        }
    }
}
