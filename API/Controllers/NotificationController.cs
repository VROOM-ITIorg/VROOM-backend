using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VROOM.Services;
using VROOM.ViewModels;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly NotificationService notificationService;

        public NotificationController(NotificationService _notificationService)
        {
            notificationService = _notificationService;
        }

        // Get all notifications for a user
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserNotifications(string userId)
        {
            var notifications = await notificationService.GetUserNotificationsAsync(userId);
            return Ok(notifications);
        }

        //  Send order status update notification
        //[HttpPost("order-status")]
        //public async Task<IActionResult> SendOrderStatusUpdate([FromBody] OrderStatusNotificationRequest model)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    await notificationService.SendOrderStatusUpdateAsync(model.UserId, model.Message, model.OrderId);
        //    return Ok(new { message = "تم إرسال إشعار حالة الطلب." });
        //}

        ////  Notify rider of a new order
        //[HttpPost("rider-order")]
        //public async Task<IActionResult> NotifyRiderOfNewOrder([FromBody] RiderOrderNotificationRequest model)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    await notificationService.NotifyRiderOfNewOrderAsync(model.RiderId, model.OrderTitle, model.OrderId);
        //    return Ok(new { message = "تم إشعار السائق بطلب جديد." });
        //}

        ////  Send alert to admins
        //[HttpPost("admin-alert")]
        //public async Task<IActionResult> SendAdminAlert([FromBody] AdminAlertRequest model)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    await notificationService.SendAdminAlertAsync(model.Message);
        //    return Ok(new { message = "تم إرسال تنبيه إلى الإدارة." });
        //}

        //  Mark notification as read
        [HttpPut("read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await notificationService.MarkAsReadAsync(id);
            return Ok(new { message = "تم تحديد الإشعار كمقروء." });
        }
    }

    //  DTOs

    public class OrderStatusNotificationRequest
    {
        public string UserId { get; set; }
        public string Message { get; set; }
        public string OrderId { get; set; }
    }

    public class RiderOrderNotificationRequest
    {
        public string RiderId { get; set; }
        public string OrderTitle { get; set; }
        public int OrderId { get; set; }
    }

    public class AdminAlertRequest
    {
        public string Message { get; set; }
    }
}
