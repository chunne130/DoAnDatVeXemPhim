using DoAnDatVeXemPhim.Data;
using DoAnDatVeXemPhim.Hubs;
using DoAnDatVeXemPhim.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace DoAnDatVeXemPhim.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task SendNotificationAsync(string userId, string title, string message, string linkUrl = "")
        {
            // 1. Lưu thông báo vào CSDL
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                LinkUrl = linkUrl,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // 2. Gửi thông báo real-time qua SignalR tới đúng User đó
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                linkUrl = notification.LinkUrl,
                createdAt = notification.CreatedAt.ToString("o"), // ISO 8601 format
                isRead = false
            });
        }

        public async Task SendOrderUpdateAsync()
        {
            // Bắn tín hiệu RefreshOrders tới nhóm StaffGroup (gồm Admin và Staff)
            await _hubContext.Clients.Group("StaffGroup").SendAsync("RefreshOrders");
        }
    }
}
