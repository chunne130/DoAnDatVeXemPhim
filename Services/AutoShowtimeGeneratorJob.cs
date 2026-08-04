using DoAnDatVeXemPhim.Data;
using DoAnDatVeXemPhim.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DoAnDatVeXemPhim.Services
{
    public class AutoShowtimeGeneratorJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutoShowtimeGeneratorJob> _logger;

        public AutoShowtimeGeneratorJob(IServiceProvider serviceProvider, ILogger<AutoShowtimeGeneratorJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoShowtimeGeneratorJob đang khởi động...");

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    bool isEnabled = config.GetValue<bool>("AutoGenerateShowtimes", false);

                    if (isEnabled)
                    {
                        try
                        {
                            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                            await CleanupOldShowtimesAsync(context);
                            await GenerateShowtimesAsync(context);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi khi chạy AutoShowtimeGeneratorJob.");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("AutoShowtimeGeneratorJob đã bị tắt trong cấu hình.");
                    }
                }

                // Tính toán chờ đến 01:00 AM ngày mai để dọn dẹp và tạo cho ngày mốt
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1).AddHours(1); // 01:00 AM ngày mai
                var delay = nextRun - now;
                _logger.LogInformation($"AutoShowtimeGeneratorJob sẽ chờ {delay.TotalHours:F2} giờ, chạy lần tới vào {nextRun:dd/MM/yyyy HH:mm}");
                
                await Task.Delay(delay, stoppingToken);
            }
        }

        private async Task CleanupOldShowtimesAsync(ApplicationDbContext context)
        {
            var today = DateTime.Today;
            
            // Tìm các suất chiếu trong quá khứ (của các ngày trước)
            var pastShowtimes = await context.Showtimes
                .Include(s => s.OrderDetails)
                .Where(s => s.StartTime < today)
                .ToListAsync();

            // Những suất chiếu không có ai mua vé (OrderDetails rỗng)
            var showtimesToDelete = pastShowtimes.Where(s => s.OrderDetails == null || !s.OrderDetails.Any()).ToList();
            
            if (showtimesToDelete.Any())
            {
                context.Showtimes.RemoveRange(showtimesToDelete);
                await context.SaveChangesAsync();
                _logger.LogInformation($"[AutoShowtime] Đã dọn dẹp {showtimesToDelete.Count} suất chiếu rác (không ai đặt vé) trong quá khứ.");
            }
        }

        private async Task GenerateShowtimesAsync(ApplicationDbContext context)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var datesToCheck = new[] { today, tomorrow };

            var activeMovies = await context.Movies.ToListAsync();
            var cinemaHalls = await context.CinemaHalls.ToListAsync();

            if (!activeMovies.Any() || !cinemaHalls.Any())
            {
                _logger.LogWarning("[AutoShowtime] Không có phim hoặc phòng chiếu nào trong hệ thống, bỏ qua tự tạo suất chiếu.");
                return;
            }

            // Các khung giờ chiếu mặc định trong ngày (chỉnh về chiều/tối để HR test ban ngày không bị lố giờ)
            var timeSlots = new[] { new TimeSpan(17, 0, 0), new TimeSpan(19, 30, 0), new TimeSpan(21, 45, 0), new TimeSpan(23, 30, 0) };
            
            foreach (var date in datesToCheck)
            {
                // Kiểm tra xem ngày này đã có suất chiếu nào chưa
                var hasShowtime = await context.Showtimes.AnyAsync(s => s.StartTime.Date == date);
                if (hasShowtime)
                {
                    _logger.LogInformation($"[AutoShowtime] Ngày {date:dd/MM/yyyy} đã có lịch chiếu, bỏ qua tạo mới.");
                    continue;
                }

                _logger.LogInformation($"[AutoShowtime] Bắt đầu rải lịch chiếu tự động cho ngày {date:dd/MM/yyyy}...");
                
                int movieIndex = 0;
                int totalCreated = 0;

                // Phân bổ kiểu luân phiên (Round-robin) để đảm bảo phim nào cũng có mặt
                foreach (var hall in cinemaHalls)
                {
                    foreach (var timeSlot in timeSlots)
                    {
                        var movie = activeMovies[movieIndex % activeMovies.Count];
                        movieIndex++;

                        var startTime = date.Add(timeSlot);
                        var endTime = startTime.AddMinutes(movie.Duration > 0 ? movie.Duration + 15 : 120 + 15);

                        var showtime = new Showtime
                        {
                            MovieId = movie.Id,
                            CinemaHallId = hall.Id,
                            StartTime = startTime,
                            EndTime = endTime,
                            BasePrice = 90000,
                            Format = "2D",
                            IsActive = true
                        };

                        context.Showtimes.Add(showtime);
                        totalCreated++;
                    }
                }
                
                await context.SaveChangesAsync();
                _logger.LogInformation($"[AutoShowtime] Đã rải thành công {totalCreated} suất chiếu cho ngày {date:dd/MM/yyyy}. Đảm bảo 100% phim đều có lịch.");
            }
        }
    }
}
