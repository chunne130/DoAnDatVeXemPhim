using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DoAnDatVeXemPhim.Data;
using DoAnDatVeXemPhim.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DoAnDatVeXemPhim.Services
{
    public class EmailMarketingJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailMarketingJob> _logger;

        public EmailMarketingJob(IServiceProvider serviceProvider, ILogger<EmailMarketingJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Marketing Job đang chạy.");

            while (!stoppingToken.IsCancellationRequested)
            {
                // 1. Tính toán thời gian ngủ cho đến 00:00 ngày mai
                var now = DateTime.Now;
                var nextMidnight = now.Date.AddDays(1); // 00:00 ngày mai
                var delay = nextMidnight - now;

                _logger.LogInformation($"Email Marketing Job sẽ chờ {delay.TotalHours:F2} giờ cho đến {nextMidnight:dd/MM/yyyy HH:mm:ss} để chạy lượt tiếp theo.");

                // Đợi đến 00:00
                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested) break;

                // 2. Chạy tác vụ gửi Email (tạo Scope mới vì BackgroundService là Singleton)
                using (var scope = _serviceProvider.CreateScope())
                {
                    try
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                        await ProcessEmailMarketingAsync(context, emailSender);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Có lỗi xảy ra khi chạy ProcessEmailMarketingAsync.");
                    }
                }
            }
        }

        private async Task ProcessEmailMarketingAsync(ApplicationDbContext context, IEmailSender emailSender)
        {
            _logger.LogInformation("Bắt đầu quy trình quét và gửi Email Marketing lúc: {time}", DateTime.Now);

            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

            // TÌm những User KHÔNG có order nào trong vòng 30 ngày qua (IsPaid = true)
            // Cần lấy danh sách tất cả user, sau đó lọc ra những người không nằm trong danh sách có đặt vé gần đây.
            
            var activeUserIds = await context.Orders
                .Where(o => o.OrderDate >= thirtyDaysAgo && o.IsPaid == true)
                .Select(o => o.UserId)
                .Distinct()
                .ToListAsync();

            // Những user chưa từng đặt vé HOẶC có đặt nhưng trước 30 ngày. 
            // Ở đây ta lấy danh sách user từ bảng CustomerProfiles (để lấy Email).
            // Nếu dùng IdentityUser thì truy vấn qua User.
            var usersToEmail = await context.Users
                .Where(u => !activeUserIds.Contains(u.Id))
                .ToListAsync();

            if (!usersToEmail.Any())
            {
                _logger.LogInformation("Không tìm thấy khách hàng nào bỏ lỡ 30 ngày.");
                return;
            }

            // Đảm bảo Voucher COMEBACK20 tồn tại trong hệ thống
            var promoCode = "COMEBACK20";
            var existingPromo = await context.Promotions.FirstOrDefaultAsync(p => p.VoucherCode == promoCode);
            if (existingPromo == null)
            {
                existingPromo = new Promotion
                {
                    VoucherCode = promoCode,
                    Description = "Voucher đặc biệt tri ân khách hàng quay lại",
                    DiscountValue = 20000, // Giảm 20.000đ
                    IsPercentage = false,
                    MinOrderValue = 50000,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddYears(1), // Hạn dài
                    UsageLimit = 9999,
                    UsedCount = 0,
                    PointsRequired = 0 // Không cần đổi điểm
                };
                context.Promotions.Add(existingPromo);
                await context.SaveChangesAsync();
            }

            // Gửi email cho từng người
            int sendCount = 0;
            foreach (var user in usersToEmail)
            {
                if (string.IsNullOrEmpty(user.Email)) continue;

                string subject = "🎬 CINEMA HUB nhớ bạn! Nhận ngay ưu đãi đặc biệt 🍿";
                string message = $@"
                <div style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; background-color: #0b1014; color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.5);'>
                    <div style='background: linear-gradient(135deg, #00ff87, #00c3ff); padding: 30px; text-align: center;'>
                        <h1 style='margin: 0; color: #111; font-size: 28px; text-transform: uppercase; font-weight: 900; letter-spacing: 1px;'>CINEMA HUB</h1>
                        <p style='margin: 10px 0 0; color: #111; font-weight: 600; font-size: 16px;'>Đã lâu rồi chúng ta chưa gặp nhau...</p>
                    </div>
                    
                    <div style='padding: 30px 40px;'>
                        <p style='font-size: 16px; line-height: 1.6; color: #e0e0e0;'>Xin chào <b>{user.UserName}</b>,</p>
                        <p style='font-size: 16px; line-height: 1.6; color: #e0e0e0;'>Đã hơn 1 tháng kể từ lần cuối bạn ghé thăm rạp của chúng mình. Không biết dạo này bạn có bận rộn quá không?</p>
                        <p style='font-size: 16px; line-height: 1.6; color: #e0e0e0;'>Tháng này rạp đang có rất nhiều bom tấn phòng vé cực kỳ hấp dẫn đang chờ bạn khám phá. Để làm bạn vui hơn, Cinema Hub xin gửi tặng bạn một món quà nhỏ:</p>
                        
                        <div style='margin: 30px 0; padding: 25px; background: rgba(0, 255, 135, 0.05); border: 1px dashed #00ff87; border-radius: 10px; text-align: center;'>
                            <p style='margin: 0; font-size: 14px; color: #00ff87; text-transform: uppercase; letter-spacing: 2px;'>Mã Voucher 20.000Đ</p>
                            <h2 style='margin: 10px 0; font-size: 32px; color: #ffffff; letter-spacing: 3px;'>{promoCode}</h2>
                            <p style='margin: 0; font-size: 12px; color: #888;'>Áp dụng cho mọi phim, mọi rạp.</p>
                        </div>
                        
                        <div style='text-align: center; margin-top: 30px;'>
                            <a href='https://localhost:13015' style='display: inline-block; background-color: #00ff87; color: #111; padding: 14px 30px; font-weight: bold; text-decoration: none; border-radius: 30px; font-size: 16px; transition: transform 0.2s;'>🎬 XEM PHIM NGAY</a>
                        </div>
                    </div>
                    
                    <div style='background-color: #070a0c; padding: 20px; text-align: center; font-size: 12px; color: #666;'>
                        <p style='margin: 0;'>© 2026 Cinema Hub. All rights reserved.</p>
                        <p style='margin: 5px 0 0;'>Email này được gửi tự động từ hệ thống vì bạn là thành viên của rạp.</p>
                    </div>
                </div>";

                try
                {
                    await emailSender.SendEmailAsync(user.Email, subject, message);
                    sendCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Không thể gửi email cho {user.Email}: {ex.Message}");
                }
            }

            _logger.LogInformation($"Hoàn tất quét Email Marketing. Đã gửi ưu đãi cho {sendCount} khách hàng.");
        }
    }
}
