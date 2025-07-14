using System.Threading.Tasks;
using ViewModels.Order;
using VROOM.Models;

namespace VROOM.Services
{
    public interface IWhatsAppNotificationService
    {
        Task<bool> SendFeedbackRequestAsync(Order order);
    }
}