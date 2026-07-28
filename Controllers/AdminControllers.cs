using DoAnDatVeXemPhim.Data;
using DoAnDatVeXemPhim.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using DoAnDatVeXemPhim.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnDatVeXemPhim.Controllers
{
    public class AdminControllers : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly AprioriService _aprioriService;
        private readonly DoAnDatVeXemPhim.Services.NotificationService _notificationService;

        public AdminControllers(ApplicationDbContext context, IEmailSender emailSender, AprioriService aprioriService, DoAnDatVeXemPhim.Services.NotificationService notificationService)
        {
            _context = context;
            _emailSender = emailSender;
            _aprioriService = aprioriService;
            _notificationService = notificationService;
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            return View("~/Views/AdminControllers/Index.cshtml");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Settings()
        {
            return View("~/Views/AdminControllers/Settings.cshtml");
        }

        // ═══════════════════════════════════════════
        // TÍNH NĂNG NHÂN VIÊN: SOÁT VÉ (SCAN TICKET)
        // ═══════════════════════════════════════════

        [Authorize(Roles = "Admin,Staff")]
        public IActionResult ScanTicket()
        {
            return View("~/Views/AdminControllers/ScanTicket.cshtml");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> VerifyTicket(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails).ThenInclude(od => od.Showtime).ThenInclude(s => s.Movie)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Lỗi: Không tìm thấy đơn hàng trong hệ thống!" });
                }

                if (order.Status == "CHECKED_IN")
                {
                    return Json(new { success = false, message = "Cảnh báo: Vé này đã được sử dụng (check-in) trước đó!" });
                }

                if (order.Status == "PENDING" || order.Status == "WAITING_CONFIRM")
                {
                    return Json(new { success = false, message = "Lỗi: Đơn hàng này chưa được thanh toán hoàn tất!" });
                }

                if (order.Status == "CANCELLED")
                {
                    return Json(new { success = false, message = "Lỗi: Đơn hàng này đã bị hủy!" });
                }

                if (order.Status == "PAID")
                {
                    order.Status = "CHECKED_IN";
                    _context.Orders.Update(order);
                    await _context.SaveChangesAsync();

                    // Thu thập thông tin phim để hiển thị lời chào
                    string movieTitles = "";
                    if (order.OrderDetails.Any())
                    {
                        var titles = order.OrderDetails.Select(od => od.Showtime.Movie.Title).Distinct();
                        movieTitles = string.Join(", ", titles);
                    }

                    return Json(new { 
                        success = true, 
                        message = $"Xác nhận thành công! Vé hợp lệ.",
                        customerName = order.User?.UserName ?? "Khách vãng lai",
                        movieName = movieTitles
                    });
                }

                return Json(new { success = false, message = "Trạng thái đơn hàng không hợp lệ." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> ManageOrders(string searchString)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Seat)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Showtime)
                .Include(o => o.Promotion)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string s = searchString.ToLower();
                string sNumber = new string(s.Where(char.IsDigit).ToArray());

                query = query.Where(o =>
                    o.Id.ToString() == sNumber ||
                    (o.User != null && o.User.Email.ToLower().Contains(s)) ||
                    (o.User != null && o.User.PhoneNumber != null && o.User.PhoneNumber.Contains(s)) ||
                    o.Status.ToLower().Contains(s)
                );
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            
            ViewData["CurrentFilter"] = searchString;

            if (IsAjaxRequest()) return PartialView(orders);
            return View(orders);
        }

        [HttpGet]
        [Route("AdminControllers/SyncOldOrdersPoints")]
        public async Task<IActionResult> SyncOldOrdersPoints()
        {
            var profiles = await _context.CustomerProfiles.ToListAsync();
            int updatedCount = 0;
            var debugInfo = new List<object>();

            foreach (var p in profiles)
            {
                decimal totalSpent = await _context.Orders.Where(o => o.UserId == p.UserId && o.IsPaid == true).SumAsync(o => o.TotalAmount);
                int expectedPoints = (int)(totalSpent / 10000);

                debugInfo.Add(new { UserId = p.UserId, TotalSpent = totalSpent, CurrentPoints = p.RewardPoints, Expected = expectedPoints });

                if (p.RewardPoints < expectedPoints)
                {
                    p.RewardPoints = expectedPoints;
                    p.TotalSpent = totalSpent;
                    _context.CustomerProfiles.Update(p);
                    updatedCount++;
                }
            }
            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }
            
            return Json(new { success = true, message = $"Đã đồng bộ {updatedCount} tài khoản!", details = debugInfo });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.OrderCombos)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order != null)
            {
                _context.OrderDetails.RemoveRange(order.OrderDetails);
                _context.OrderCombos.RemoveRange(order.OrderCombos);
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa vĩnh viễn đơn hàng #" + id;
            }

            if (IsAjaxRequest())
            {
                var orders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails).ThenInclude(od => od.Seat)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
                return PartialView("ManageOrders", orders);
            }

            return RedirectToAction(nameof(ManageOrders));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmPayment(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Showtime).ThenInclude(s => s.Movie)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Seat)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            order.IsPaid = true;
            order.Status = "PAID"; 
            
            // --- TÍCH ĐIỂM THƯỞNG KHI ADMIN DUYỆT ĐƠN ---
            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == order.UserId);
            if (profile != null)
            {
                int earnedPoints = (int)(order.TotalAmount / 10000);
                profile.RewardPoints += earnedPoints;
                _context.CustomerProfiles.Update(profile);
            }

            await _context.SaveChangesAsync();

            // Bắn thông báo Real-time cho khách khi Admin duyệt vé
            await _notificationService.SendNotificationAsync(order.UserId, "✅ Đơn hàng đã được duyệt", $"Đơn hàng {order.Id} thanh toán qua chuyển khoản đã được Admin xác nhận. Bạn đã có vé!", $"/User/OrderHistory");

            string qrContent = $"TICKET-{order.Id}-{order.OrderDate:yyyyMMdd}";
            string qrImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={qrContent}";

            var movie = order.OrderDetails.FirstOrDefault()?.Showtime.Movie;
            var showtime = order.OrderDetails.FirstOrDefault()?.Showtime;
            var seats = string.Join(", ", order.OrderDetails.Select(od => od.Seat.SeatNumber));

            string subject = $"[CINEMA HUB] VÉ XEM PHIM THÀNH CÔNG - #{order.Id}";
            string message = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #ddd; border-radius: 15px; overflow: hidden;'>
                    <div style='background: #dc3545; color: white; padding: 30px; text-align: center;'>
                        <h1 style='margin: 0; font-size: 28px;'>CINEMA HUB</h1>
                        <p style='margin: 5px 0 0; opacity: 0.9;'>Cảm ơn bạn đã tin tưởng đặt vé!</p>
                    </div>
                    <div style='padding: 25px; background: #fff;'>
                        <h2 style='color: #333; border-bottom: 2px solid #f8f9fa; padding-bottom: 10px;'>THÔNG TIN VÉ XEM PHIM</h2>
                        <p style='font-size: 16px;'>🎬 <b>Phim:</b> <span style='color: #dc3545; font-weight: bold;'>{movie?.Title}</span></p>
                        <p style='font-size: 16px;'>📅 <b>Suất chiếu:</b> {showtime?.StartTime:HH:mm} - Ngày {showtime?.StartTime:dd/MM/yyyy}</p>
                        <p style='font-size: 16px;'>💺 <b>Ghế:</b> <span style='background: #28a745; color: white; padding: 3px 10px; border-radius: 5px; font-weight: bold;'>{seats}</span></p>
                        <p style='font-size: 16px;'>💰 <b>Tổng tiền:</b> <span style='color: #dc3545; font-weight: bold;'>{order.TotalAmount:N0}đ</span></p>
                        
                        <div style='text-align: center; margin: 30px 0; padding: 20px; background: #fdfdfd; border: 1px dashed #ccc; border-radius: 10px;'>
                            <p style='margin-bottom: 15px; font-weight: bold; color: #555;'>MÃ QR VÀO CỔNG</p>
                            <img src='{qrImageUrl}' alt='Mã vé QR' style='width: 200px; height: 200px; display: block; margin: auto;' />
                            <p style='font-size: 22px; font-weight: bold; color: #dc3545; margin-top: 15px;'>MÃ ĐƠN: #{order.Id}</p>
                        </div>
                    </div>
                    <div style='background: #f8f9fa; padding: 20px; text-align: center; font-size: 13px; color: #666;'>
                        Bạn vui lòng có mặt trước <b>15 phút</b> để nhận bắp nước nhe!<br>
                        Hệ thống Cinema Hub - Đồ án IT HUFLIT
                    </div>
                </div>";

            try
            {
                await _emailSender.SendEmailAsync(order.User.Email, subject, message);
                TempData["Success"] = $"Đã duyệt đơn #{orderId} và gửi vé thành công!";
            }
            catch (Exception ex)
            {
                TempData["Success"] = $"Đã duyệt đơn #{orderId} nhưng không gửi được Email. Lỗi: {ex.Message}";
            }

            if (IsAjaxRequest())
            {
                var orders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails).ThenInclude(od => od.Seat)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
                return PartialView("ManageOrders", orders);
            }

            return RedirectToAction(nameof(ManageOrders));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RevenueReport(string filter = "7", string type = "total")
        {
            var paidOrders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Showtime)
                        .ThenInclude(s => s.Movie)
                .Include(o => o.OrderCombos)
                .Where(o => o.IsPaid == true)
                .ToListAsync();

            int days = int.Parse(filter);
            DateTime startDate = DateTime.Today.AddDays(-(days - 1));

            var filteredOrders = paidOrders
                .Where(o => o.OrderDate >= startDate)
                .ToList();

            ViewBag.Type = type;

            double comboRevenue = filteredOrders.Sum(o => o.OrderCombos.Sum(c => (double)(c.Price * c.Quantity)));
            double movieRevenue = filteredOrders.Sum(o => o.OrderDetails.Sum(od => (double)od.PriceAtBooking));

            ViewBag.ComboRevenue = comboRevenue;
            ViewBag.MovieRevenue = movieRevenue;
            ViewBag.TotalRevenue = movieRevenue + comboRevenue;

            List<string> labels = new();
            List<double> data = new();
            List<double> movieRevenueData = new();

            if (type == "total" || type == "movie" || type == "combo")
            {
                for (int i = days - 1; i >= 0; i--)
                {
                    var date = DateTime.Today.AddDays(-i);
                    labels.Add(date.ToString("dd/MM"));

                    double value = 0;

                    if (type == "movie")
                    {
                        value = filteredOrders.Where(o => o.OrderDate.Date == date)
                                              .Sum(o => o.OrderDetails.Sum(od => (double)od.PriceAtBooking));
                    }
                    else if (type == "combo")
                    {
                        value = filteredOrders.Where(o => o.OrderDate.Date == date)
                                              .Sum(o => o.OrderCombos.Sum(c => (double)(c.Price * c.Quantity)));
                    }
                    else if (type == "total")
                    {
                        double dayMovie = filteredOrders.Where(o => o.OrderDate.Date == date).Sum(o => o.OrderDetails.Sum(od => (double)od.PriceAtBooking));
                        double dayCombo = filteredOrders.Where(o => o.OrderDate.Date == date).Sum(o => o.OrderCombos.Sum(c => (double)(c.Price * c.Quantity)));
                        value = dayMovie + dayCombo;
                    }

                    data.Add(value);
                }
            }
            else if (type == "topmovie")
            {
                var top = filteredOrders
                    .SelectMany(o => o.OrderDetails)
                    .GroupBy(x => x.Showtime.Movie.Title)
                    .Select(g => new {
                        g.Key,
                        TicketCount = g.Count(),
                        MoneyEarned = g.Sum(od => (double)od.PriceAtBooking)
                    })
                    .OrderByDescending(x => x.TicketCount)
                    .Take(10)
                    .ToList();

                labels = top.Select(x => x.Key).ToList();
                data = top.Select(x => (double)x.TicketCount).ToList();
                movieRevenueData = top.Select(x => x.MoneyEarned).ToList();
            }
            else if (type == "showtime")
            {
                var top = filteredOrders
                    .SelectMany(o => o.OrderDetails)
                    .GroupBy(x => x.Showtime.StartTime)
                    .Select(g => new { g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToList();

                labels = top.Select(x => x.Key.ToString("dd/MM HH:mm")).ToList();
                data = top.Select(x => (double)x.Count).ToList();
            }
            // --- ĐÃ THÊM: Tính năng Behavior Tracking (Báo cáo Lượt xem phim) ---
            else if (type == "topviews")
            {
                var topViews = await _context.Movies
                    .OrderByDescending(m => m.ViewCount)
                    .Take(10)
                    .ToListAsync();

                labels = topViews.Select(x => x.Title).ToList();
                data = topViews.Select(x => (double)x.ViewCount).ToList();
            }

            ViewBag.Labels = labels;
            ViewBag.Data = data;
            ViewBag.MovieRevenueData = movieRevenueData;

            ViewBag.Title = type switch
            {
                "total" => "Doanh thu tổng",
                "movie" => "Doanh thu phim",
                "combo" => "Doanh thu bắp nước",
                "topmovie" => "Top 10 phim",
                "showtime" => "Top suất chiếu",
                _ => ""
            };

            if (IsAjaxRequest()) return PartialView();
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AprioriAnalysis(double minSupport = 0.05, double minConfidence = 0.3, bool runAnalysis = false)
        {
            var combosDict = await _context.Combos.ToDictionaryAsync(c => c.Id, c => c.Name);
            ViewBag.CombosDict = combosDict;
            ViewBag.MinSupport = minSupport;
            ViewBag.MinConfidence = minConfidence;

            var savedRules = await _context.AssociationRules.OrderByDescending(r => r.Confidence).ToListAsync();
            ViewBag.SavedRules = savedRules;

            List<AprioriRule<int>> newRules = new List<AprioriRule<int>>();
            int orderCount = await _context.Orders.CountAsync(o => o.IsPaid && o.OrderCombos.Any());
            ViewBag.OrderCount = orderCount;

            if (runAnalysis)
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderCombos)
                    .Where(o => o.IsPaid && o.OrderCombos.Any())
                    .ToListAsync();

                var transactions = orders.Select(o => o.OrderCombos.Select(oc => oc.ComboId).ToList()).ToList();
                newRules = _aprioriService.Run<int>(transactions, minSupport, minConfidence);
                ViewBag.RunAnalysis = true;
            }

            ViewBag.NewRules = newRules;

            if (IsAjaxRequest()) return PartialView("~/Views/AdminControllers/AprioriAnalysis.cshtml");
            return View("~/Views/AdminControllers/AprioriAnalysis.cshtml");
        }

        // Thuật toán Apriori để tìm các luật kết hợp Combo và lưu vào cơ sở dữ liệu
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SaveAprioriRules(double minSupport, double minConfidence)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderCombos)
                .Where(o => o.IsPaid && o.OrderCombos.Any())
                .ToListAsync();

            var transactions = orders.Select(o => o.OrderCombos.Select(oc => oc.ComboId).ToList()).ToList();
            var rules = _aprioriService.Run<int>(transactions, minSupport, minConfidence);

            using (var dbTransaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Xóa các luật kết hợp Combo cũ
                    var oldRules = await _context.AssociationRules.Where(r => r.RuleType == "Combo").ToListAsync();
                    _context.AssociationRules.RemoveRange(oldRules);

                    // Thêm các luật mới sinh ra
                    foreach (var r in rules)
                    {
                        var ruleEntity = new AssociationRule
                        {
                            RuleType = "Combo",
                            Antecedent = string.Join(",", r.Antecedent),
                            Consequent = string.Join(",", r.Consequent),
                            Support = r.Support,
                            Confidence = r.Confidence,
                            Lift = r.Lift,
                            CreatedAt = DateTime.Now
                        };
                        _context.AssociationRules.Add(ruleEntity);
                    }

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    TempData["Success"] = $"Đã áp dụng và lưu thành công {rules.Count} luật kết hợp vào cơ sở dữ liệu!";
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    TempData["Error"] = "Lỗi khi lưu luật kết hợp: " + ex.Message;
                }
            }

            return RedirectToAction("AprioriAnalysis", new { minSupport = minSupport, minConfidence = minConfidence });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult SeedAprioriTestData()
        {
            try
            {
                SeedData.GenerateMockTransactions(_context);
                TempData["Success"] = "Đã giả lập thành công 60 hóa đơn giao dịch có quy luật vào hệ thống!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi giả lập dữ liệu: " + ex.Message;
            }
            return RedirectToAction("AprioriAnalysis");
        }
    }
}