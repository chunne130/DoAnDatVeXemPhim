using DoAnDatVeXemPhim.Data;
using DoAnDatVeXemPhim.Models;
using DoAnDatVeXemPhim.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace DoAnDatVeXemPhim.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ThanhToanService _thanhToanService;
        private readonly IEmailSender _emailSender;
        private readonly NotificationService _notificationService;

        public BookingController(ApplicationDbContext context,
                                 UserManager<IdentityUser> userManager,
                                 ThanhToanService thanhToanService,
                                 IEmailSender emailSender,
                                 NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _thanhToanService = thanhToanService;
            _emailSender = emailSender;
            _notificationService = notificationService;
        }

        // CÁC HÀM SELECT VÉ 
        [Authorize]
        public async Task<IActionResult> SelectShowtime(int movieId, DateTime? date)
        {
            ClearBookingSession();

            var selectDate = date ?? DateTime.Today;
            var movie = await _context.Movies.Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == movieId);
            if (movie == null) return NotFound();

            // Tính năng Behavior Tracking 
            movie.ViewCount++;
            await _context.SaveChangesAsync();

            var now = DateTime.Now;
            var showtimes = await _context.Showtimes
                .Include(s => s.CinemaHall).ThenInclude(h => h.Cinema)
                .Where(s => s.MovieId == movieId && s.IsActive && s.StartTime.Date == selectDate.Date && s.StartTime > now)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            ViewBag.Movie = movie;
            ViewBag.SelectDate = selectDate;
            return View(showtimes);
        }

        public async Task<IActionResult> SelectSeat(int showtimeId)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.CinemaHall).ThenInclude(h => h.Cinema)
                .Include(s => s.CinemaHall).ThenInclude(h => h.Seats)
                .FirstOrDefaultAsync(s => s.Id == showtimeId);

            if (showtime == null) return NotFound();

            // THUẬT TOÁN: Bỏ qua các đơn đã bị HỦY (CANCELLED) để giải phóng ghế
            var now = DateTime.Now;
            var bookedSeatIds = await _context.OrderDetails
                .Include(od => od.Order)
                .Where(od => od.ShowtimeId == showtimeId
                          && od.Order.Status != "CANCELLED" // KHÔNG KHÓA GHẾ NẾU ĐƠN ĐÃ HỦY
                          && (
                                od.Order.IsPaid == true
                                || od.Order.Status == "WAITING_CONFIRM"
                                || (od.Order.Status == "PENDING" && od.Order.OrderDate > now.AddMinutes(-10) && showtime.StartTime > now)
                             )
                       )
                .Select(od => od.SeatId)
                .ToListAsync();

            ViewBag.BookedSeatIds = bookedSeatIds;
            return View(showtime);
        }

        [HttpPost]
        public IActionResult ConfirmSeats(int showtimeId, string selectedSeatsJson, int adultQty, int studentQty, int childQty)
        {
            HttpContext.Session.SetString("SelectedSeats", selectedSeatsJson);
            HttpContext.Session.SetInt32("ShowtimeId", showtimeId);
            HttpContext.Session.SetInt32("AdultQty", adultQty);
            HttpContext.Session.SetInt32("StudentQty", studentQty);
            HttpContext.Session.SetInt32("ChildQty", childQty);
            return RedirectToAction("SelectCombo");
        }

        public async Task<IActionResult> SelectCombo()
        {
            var showtimeId = HttpContext.Session.GetInt32("ShowtimeId");
            if (showtimeId == null) return RedirectToAction("Index", "Home");

            var showtime = await _context.Showtimes.FirstOrDefaultAsync(s => s.Id == showtimeId);
            decimal basePrice = showtime?.BasePrice ?? 0;

            var seatsJson = HttpContext.Session.GetString("SelectedSeats");
            var selectedSeats = !string.IsNullOrEmpty(seatsJson)
                ? JsonConvert.DeserializeObject<List<SelectedSeatDto>>(seatsJson)
                : new List<SelectedSeatDto>();

            int adultQty = HttpContext.Session.GetInt32("AdultQty") ?? 0;
            int studentQty = HttpContext.Session.GetInt32("StudentQty") ?? 0;
            int childQty = HttpContext.Session.GetInt32("ChildQty") ?? 0;

            decimal ticketTotal = 0;
            foreach (var s in selectedSeats)
            {
                if (adultQty > 0)
                {
                    ticketTotal += s.price;
                    adultQty--;
                }
                else if (studentQty > 0)
                {
                    ticketTotal += s.price - (basePrice * 0.20m);
                    studentQty--;
                }
                else if (childQty > 0)
                {
                    ticketTotal += s.price - (basePrice * 0.30m);
                    childQty--;
                }
                else
                {
                    ticketTotal += s.price;
                }
            }

            var combos = await _context.Combos.ToListAsync();
            ViewBag.SelectedSeats = seatsJson;
            ViewBag.TicketTotal = ticketTotal;
            
            return View(combos);
        }

        [HttpPost]
        public async Task<IActionResult> GetComboRecommendations([FromBody] List<int> selectedComboIds)
        {
            if (selectedComboIds == null || selectedComboIds.Count == 0)
            {
                return Json(new List<object>());
            }

            var rules = await _context.AssociationRules
                .Where(r => r.RuleType == "Combo")
                .ToListAsync();

            var selectedSet = new HashSet<int>(selectedComboIds);
            var recommendedIds = new List<int>();

            foreach (var rule in rules)
            {
                try
                {
                    var antIds = rule.Antecedent.Split(',').Select(int.Parse).ToList();
                    if (antIds.All(id => selectedSet.Contains(id)))
                    {
                        var consIds = rule.Consequent.Split(',').Select(int.Parse);
                        foreach (var cid in consIds)
                        {
                            if (!selectedSet.Contains(cid) && !recommendedIds.Contains(cid))
                            {
                                recommendedIds.Add(cid);
                            }
                        }
                    }
                }
                catch
                {
                    // Tránh lỗi nếu định dạng chuỗi ID bị hỏng
                }
            }

            var recommendedCombos = await _context.Combos
                .Where(c => recommendedIds.Contains(c.Id))
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.Price,
                    c.ImageUrl
                })
                .ToListAsync();

            return Json(recommendedCombos);
        }

        [HttpPost]
        public IActionResult ConfirmCombo(string selectedCombosJson, decimal finalTotal)
        {
            HttpContext.Session.SetString("SelectedCombos", selectedCombosJson);
            HttpContext.Session.SetString("FinalTotal", finalTotal.ToString());
            return RedirectToAction("Checkout");
        }

        // TỰ ĐỘNG KHẤU TRỪ THEO HẠNG THÀNH VIÊN REAL-TIME 
        public async Task<IActionResult> Checkout()
        {
            var showtimeId = HttpContext.Session.GetInt32("ShowtimeId");
            var finalTotalStr = HttpContext.Session.GetString("FinalTotal");
            if (showtimeId == null || string.IsNullOrEmpty(finalTotalStr)) return RedirectToAction("Index", "Home");

            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.CinemaHall).ThenInclude(h => h.Cinema)
                .FirstOrDefaultAsync(s => s.Id == showtimeId);

            decimal totalAmount = decimal.Parse(finalTotalStr);
            decimal vipDiscountAmount = 0;
            string vipLevelName = "Hạng Đồng";
            decimal discountRate = 0;

            var userId = _userManager.GetUserId(User);
            var userProfile = await _context.CustomerProfiles
                .Include(c => c.MembershipLevel)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (userProfile != null && userProfile.MembershipLevel != null)
            {
                vipLevelName = userProfile.MembershipLevel.LevelName;
                discountRate = (decimal)userProfile.MembershipLevel.DiscountRate;
                vipDiscountAmount = totalAmount * discountRate;
            }

            //  BỔ SUNG TRUY VẤN LẤY VÍ NỘI BỘ
            var userWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            decimal walletBalance = userWallet != null ? userWallet.Balance : 0;

            ViewBag.SelectedSeats = HttpContext.Session.GetString("SelectedSeats");
            ViewBag.SelectedCombos = HttpContext.Session.GetString("SelectedCombos");
            ViewBag.OriginalTotal = totalAmount;
            ViewBag.VipLevelName = vipLevelName;
            ViewBag.VipDiscountRate = (int)(discountRate * 100);
            ViewBag.VipDiscountAmount = vipDiscountAmount;
            ViewBag.FinalTotal = totalAmount - vipDiscountAmount;

            ViewBag.AdultQty = HttpContext.Session.GetInt32("AdultQty") ?? 0;
            ViewBag.StudentQty = HttpContext.Session.GetInt32("StudentQty") ?? 0;
            ViewBag.ChildQty = HttpContext.Session.GetInt32("ChildQty") ?? 0;

            //  BỔ SUNG TRUY VẤN LẤY VÍ VOUCHER KHẢ DỤNG
            var now = DateTime.Now;
            var availableVouchers = await _context.UserPromotions
                .Include(up => up.Promotion)
                .Where(up => up.UserId == userId && !up.IsUsed && up.Promotion.EndDate >= now && up.Promotion.StartDate <= now)
                .ToListAsync();

            ViewBag.UserPromotions = availableVouchers;
            ViewBag.WalletBalance = walletBalance;

            return View(showtime);
        }

        // --- HÀM TẠO ĐƠN ---
        [HttpPost]
        public async Task<IActionResult> CreateOrder(string paymentMethod, string voucherCode)
        {
            var showtimeId = HttpContext.Session.GetInt32("ShowtimeId");
            var finalTotalStr = HttpContext.Session.GetString("FinalTotal");
            var seatsJson = HttpContext.Session.GetString("SelectedSeats");
            var combosJson = HttpContext.Session.GetString("SelectedCombos");

            if (showtimeId == null || string.IsNullOrEmpty(finalTotalStr) || string.IsNullOrEmpty(seatsJson))
            {
                bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
                if (isAjax) return Json(new { success = false, message = "Đơn hàng của bạn đã được khởi tạo thành công trước đó." });
                return RedirectToAction("Index", "Home");
            }

            var userId = _userManager.GetUserId(User);

            // Giải nén dữ liệu danh sách ghế và combo được chọn
            var selectedSeats = JsonConvert.DeserializeObject<List<SelectedSeatDto>>(seatsJson);
            var selectedCombos = !string.IsNullOrEmpty(combosJson)
                ? JsonConvert.DeserializeObject<List<SelectedComboDto>>(combosJson)
                : new List<SelectedComboDto>();

            var showtime = await _context.Showtimes.FirstOrDefaultAsync(s => s.Id == showtimeId);
            decimal basePrice = showtime?.BasePrice ?? 0;

            decimal finalTotal = decimal.Parse(finalTotalStr);

            decimal vipDiscountAmount = 0;
            string vipLevelName = "";

            var userProfile = await _context.CustomerProfiles
                .Include(c => c.MembershipLevel)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (userProfile != null && userProfile.MembershipLevel != null)
            {
                decimal vipRate = (decimal)userProfile.MembershipLevel.DiscountRate;
                vipDiscountAmount = finalTotal * vipRate;
                vipLevelName = userProfile.MembershipLevel.LevelName;
                finalTotal -= vipDiscountAmount;
            }

            int? promotionId = null;

            // Áp dụng thêm mã Voucher giảm giá (nếu có nhập)
            if (!string.IsNullOrEmpty(voucherCode))
            {
                var promo = await _context.Promotions
                    .Include(p => p.MembershipLevel)
                    .FirstOrDefaultAsync(p => p.VoucherCode == voucherCode.ToUpper().Trim());

                if (promo != null && promo.StartDate <= DateTime.Now && promo.EndDate >= DateTime.Now && promo.UsedCount < promo.UsageLimit)
                {
                    bool isEligibleForVoucher = true;

                    // Nếu voucher yêu cầu hạng thành viên, kiểm tra điều kiện
                    if (promo.MembershipLevelId.HasValue)
                    {
                        decimal userMinSpend = userProfile?.MembershipLevel?.MinSpending ?? 0;
                        decimal voucherMinSpend = promo.MembershipLevel?.MinSpending ?? 0;
                        if (userMinSpend < voucherMinSpend)
                        {
                            isEligibleForVoucher = false;
                        }
                    }

                    // Nếu voucher là loại đổi điểm, user phải sở hữu nó trong ví
                    if (promo.PointsRequired > 0)
                    {
                        bool ownsVoucher = await _context.UserPromotions.AnyAsync(up => up.UserId == userId && up.PromotionId == promo.Id && !up.IsUsed);
                        if (!ownsVoucher)
                        {
                            isEligibleForVoucher = false;
                        }
                    }

                    if (isEligibleForVoucher && finalTotal >= promo.MinOrderValue)
                    {
                        decimal discountAmount = 0;
                        if (promo.IsPercentage) discountAmount = finalTotal * (promo.DiscountValue / 100m);
                        else discountAmount = promo.DiscountValue;

                        finalTotal -= discountAmount;
                        if (finalTotal < 0) finalTotal = 0;

                        promo.UsedCount += 1;
                        _context.Promotions.Update(promo);
                        promotionId = promo.Id;
                        
                        // Đánh dấu Voucher trong ví của user đã được sử dụng
                        var userPromo = await _context.UserPromotions.FirstOrDefaultAsync(up => up.UserId == userId && up.PromotionId == promo.Id && !up.IsUsed);
                        if (userPromo != null)
                        {
                            userPromo.IsUsed = true;
                            userPromo.UsedDate = DateTime.Now;
                            _context.UserPromotions.Update(userPromo);
                        }
                    }
                }
            }

            int newOrderId = 0;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var order = new Order
                    {
                        OrderDate = DateTime.Now,
                        TotalAmount = finalTotal,
                        PaymentMethod = paymentMethod,
                        IsPaid = false,
                        UserId = userId,
                        Status = "PENDING",
                        VipDiscount = vipDiscountAmount,
                        VipLevel = vipLevelName,
                        PromotionId = promotionId
                    };
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    int adultQty = HttpContext.Session.GetInt32("AdultQty") ?? 0;
                    int studentQty = HttpContext.Session.GetInt32("StudentQty") ?? 0;
                    int childQty = HttpContext.Session.GetInt32("ChildQty") ?? 0;

                    foreach (var s in selectedSeats)
                    {
                        decimal seatPrice = s.price; // Giá này đã bao gồm Phụ thu VIP nếu có từ trang chọn ghế
                        string ticketType = "Người lớn";
                        
                        // Áp dụng giá theo loại vé (tính trên giá gốc)
                        if (adultQty > 0)
                        {
                            // Vé người lớn: Giữ nguyên s.price
                            adultQty--;
                        }
                        else if (studentQty > 0)
                        {
                            // Vé HSSV: S.price - 20% giá gốc
                            seatPrice = s.price - (basePrice * 0.20m);
                            ticketType = "Học sinh";
                            studentQty--;
                        }
                        else if (childQty > 0)
                        {
                            // Vé Trẻ em: S.price - 30% giá gốc
                            seatPrice = s.price - (basePrice * 0.30m);
                            ticketType = "Trẻ em";
                            childQty--;
                        }

                        _context.OrderDetails.Add(new OrderDetail
                        {
                            OrderId = order.Id,
                            ShowtimeId = showtimeId.Value,
                            SeatId = s.id,
                            PriceAtBooking = seatPrice,
                            TicketType = ticketType
                        });
                    }

                    foreach (var c in selectedCombos.Where(x => x.qty > 0))
                    {
                        _context.OrderCombos.Add(new OrderCombo
                        {
                            OrderId = order.Id,
                            ComboId = c.id,
                            Quantity = c.qty,
                            Price = c.price
                        });
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    await _notificationService.SendOrderUpdateAsync();
                    newOrderId = order.Id;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Content("Lỗi lưu Database: " + ex.Message);
                }
            }

            //  XỬ LÝ THANH TOÁN BẰNG VÍ NỘI BỘ
            if (paymentMethod == "Wallet")
            {
                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);

                if (wallet == null || wallet.Balance < finalTotal)
                {
                    TempData["Error"] = "Số dư ví không đủ để thanh toán vé này!";
                    return RedirectToAction("Checkout");
                }

                // Trừ tiền ví
                wallet.Balance -= finalTotal;
                wallet.UpdatedAt = DateTime.Now;
                _context.Wallets.Update(wallet);

                // Ghi lịch sử giao dịch (Dòng trừ tiền)
                _context.WalletTransactions.Add(new WalletTransaction
                {
                    WalletId = wallet.Id,
                    Amount = finalTotal,
                    Type = "PAYMENT",
                    Description = $"Thanh toán vé xem phim đơn hàng #{newOrderId}",
                    CreatedAt = DateTime.Now
                });

                // Đánh dấu đơn hàng là đã thanh toán 
                var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == newOrderId);
                order.IsPaid = true;
                order.Status = "PAID";
                _context.Orders.Update(order);

                // --- TÍCH ĐIỂM THƯỞNG (10.000đ = 1 điểm) ---
                var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == order.UserId);
                if (profile != null)
                {
                    int earnedPoints = (int)(order.TotalAmount / 10000);
                    profile.RewardPoints += earnedPoints;
                    _context.CustomerProfiles.Update(profile);
                }

                await _context.SaveChangesAsync();
                await _notificationService.SendOrderUpdateAsync();
                ClearBookingSession();

                // Gửi mail xác nhận 
                await SendConfirmationEmail(order);
                // Bắn thông báo Real-time
                await _notificationService.SendNotificationAsync(order.UserId, "🎉 Thanh toán thành công!", $"Đơn hàng {order.Id} trị giá {order.TotalAmount.ToString("N0")}đ đã được thanh toán bằng Ví nội bộ.", $"/User/OrderHistory");

                bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
                if (isAjax)
                {
                    return Json(new { success = true, method = "Wallet", redirectUrl = $"/Booking/PaymentSuccess?orderId={newOrderId}&status=PAID" });
                }
                return RedirectToAction("PaymentSuccess", new { orderId = newOrderId, status = "PAID" });
            }

            if (paymentMethod == "PayOS")
            {
                try
                {
                    var (checkoutUrl, orderCode) = await _thanhToanService.CreatePaymentLink(newOrderId, finalTotal);
                    ClearBookingSession();
                    
                    bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
                    if (isAjax)
                    {
                        return Json(new { success = true, method = "PayOS", checkoutUrl = checkoutUrl, orderCode = orderCode, orderId = newOrderId });
                    }
                    
                    return Redirect(checkoutUrl);
                }
                catch (Exception ex)
                {
                    bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
                    if (isAjax) return Json(new { success = false, message = "Lỗi gọi PayOS: " + ex.Message });
                    return Content("Lỗi gọi PayOS: " + ex.Message);
                }
            }
            
            return BadRequest("Phương thức thanh toán không hợp lệ.");
        }

        // --- THANH TOÁN LẠI ĐƠN HÀNG ĐÃ TỒN TẠI (TỪ TRANG VÉ CỦA TÔI) ---
        [Authorize]
        public async Task<IActionResult> PayExistingOrder(int orderId, string paymentMethod)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (order == null)
            {
                if (isAjax) return Json(new { success = false, message = $"Không tìm thấy đơn hàng ID {orderId} trong DB." });
                return RedirectToAction("MyTickets");
            }

            if (order.UserId != userId)
            {
                if (isAjax) return Json(new { success = false, message = $"Bạn không có quyền thanh toán đơn hàng này. (Đơn của {order.UserId}, bạn là {userId})" });
                return RedirectToAction("MyTickets");
            }

            if (order.IsPaid)
            {
                if (isAjax) return Json(new { success = false, message = "Đơn hàng này đã được thanh toán rồi!" });
                return RedirectToAction("MyTickets");
            }

            if (order.Status != "PENDING")
            {
                if (isAjax) return Json(new { success = false, message = $"Đơn hàng không ở trạng thái chờ thanh toán. (Trạng thái hiện tại: {order.Status})" });
                return RedirectToAction("MyTickets");
            }

            var firstDetail = await _context.OrderDetails
                .Include(od => od.Showtime)
                .FirstOrDefaultAsync(od => od.OrderId == orderId);
                
            if (firstDetail != null && firstDetail.Showtime.StartTime < DateTime.Now)
            {
                if (isAjax) return Json(new { success = false, message = "Suất chiếu của vé này đã bắt đầu hoặc kết thúc, không thể thanh toán nữa!" });
                TempData["Error"] = "Suất chiếu của vé này đã bắt đầu hoặc kết thúc, không thể thanh toán nữa!";
                return RedirectToAction("MyTickets");
            }

            if (paymentMethod == "Wallet")
            {
                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
                if (wallet == null || wallet.Balance < order.TotalAmount)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = "Số dư ví không đủ để thanh toán vé này!" });
                    }
                    TempData["Error"] = "Số dư ví không đủ để thanh toán vé này!";
                    return RedirectToAction("MyTickets"); 
                }

                // Trừ tiền ví
                wallet.Balance -= order.TotalAmount;
                wallet.UpdatedAt = DateTime.Now;
                _context.Wallets.Update(wallet);

                // Ghi lịch sử giao dịch
                _context.WalletTransactions.Add(new WalletTransaction
                {
                    WalletId = wallet.Id,
                    Amount = order.TotalAmount,
                    Type = "PAYMENT",
                    Description = $"Thanh toán vé xem phim đơn hàng #{order.Id}",
                    CreatedAt = DateTime.Now
                });

                // Đánh dấu đơn hàng là đã thanh toán 
                order.IsPaid = true;
                order.Status = "PAID";
                _context.Orders.Update(order);

                // --- TÍCH ĐIỂM THƯỞNG ---
                var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile != null)
                {
                    int earnedPoints = (int)(order.TotalAmount / 10000);
                    profile.RewardPoints += earnedPoints;
                    _context.CustomerProfiles.Update(profile);
                }

                await _context.SaveChangesAsync();
                await _notificationService.SendOrderUpdateAsync(order.Id, $"Đơn hàng #{order.Id} vừa thanh toán {order.TotalAmount.ToString("N0")}đ");
                // Gửi mail xác nhận & thông báo
                await SendConfirmationEmail(order);
                await _notificationService.SendNotificationAsync(order.UserId, "🎉 Thanh toán thành công!", $"Đơn hàng {order.Id} trị giá {order.TotalAmount.ToString("N0")}đ đã được thanh toán bằng Ví nội bộ.", $"/User/OrderHistory");

                if (isAjax)
                {
                    return Json(new { success = true, method = "Wallet", redirectUrl = $"/Booking/PaymentSuccess?orderId={order.Id}&status=PAID" });
                }

                return RedirectToAction("PaymentSuccess", new { orderId = order.Id, status = "PAID" });
            }
            else if (paymentMethod == "PayOS")
            {
                try
                {
                    string customCancelUrl = $"{Request.Scheme}://{Request.Host}/Booking/MyTickets";
                    var (checkoutUrl, orderCode) = await _thanhToanService.CreatePaymentLink(order.Id, order.TotalAmount, customCancelUrl);
                    
                    if (isAjax)
                    {
                        return Json(new { success = true, method = "PayOS", checkoutUrl = checkoutUrl, orderCode = orderCode, orderId = order.Id });
                    }

                    return Redirect(checkoutUrl);
                }
                catch (Exception ex)
                {
                    if (isAjax) return Json(new { success = false, message = "Lỗi gọi PayOS: " + ex.Message });
                    return Content("Lỗi gọi PayOS: " + ex.Message);
                }
            }

            return BadRequest("Phương thức thanh toán không hợp lệ.");
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPaid(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order != null && order.Status == "PENDING")
            {
                order.Status = "WAITING_CONFIRM";
                await _context.SaveChangesAsync();
                await _notificationService.SendOrderUpdateAsync(order.Id, $"Đơn hàng #{order.Id} vừa thanh toán {order.TotalAmount.ToString("N0")}đ");
            }
            ClearBookingSession();
            return RedirectToAction("PaymentSuccess", new { orderId = orderId, status = "WAITING" });
        }

        public async Task<IActionResult> PaymentSuccess(int orderId)
        {
            var status = Request.Query["status"].ToString();
            if (string.IsNullOrEmpty(status)) status = "WAITING";

            ViewBag.OrderId = orderId;

            var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == orderId);

            if (status == "PAID")
            {
                if (order != null && !order.IsPaid)
                {
                    bool isLegit = true;
                    
                    // NẾU LÀ PAYOS -> BẮT BUỘC KIỂM TRA API ĐỂ CHỐNG HACK GIẢ MẠO LINK
                    if (order.PaymentMethod == "PayOS")
                    {
                        string orderCodeStr = Request.Query["orderCode"];
                        if (long.TryParse(orderCodeStr, out long orderCode))
                        {
                            var paymentInfo = await _thanhToanService.GetPaymentInfo(orderCode);
                            
                            // Phải đúng trạng thái PAID và số tiền phải khớp với database mới được duyệt
                            if (paymentInfo == null || 
                                paymentInfo["status"]?.ToString() != "PAID" ||
                                (decimal)paymentInfo["amount"] != order.TotalAmount)
                            {
                                isLegit = false;
                            }
                        }
                        else
                        {
                            isLegit = false;
                        }
                    }

                    if (isLegit)
                    {
                        order.IsPaid = true;
                        order.Status = "PAID";
                        
                        // --- TÍCH ĐIỂM THƯỞNG (10.000đ = 1 điểm) ---
                        var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == order.UserId);
                        if (profile != null)
                        {
                            int earnedPoints = (int)(order.TotalAmount / 10000);
                            profile.RewardPoints += earnedPoints;
                            _context.CustomerProfiles.Update(profile);
                        }

                        await _context.SaveChangesAsync();
                        await _notificationService.SendOrderUpdateAsync(order.Id, $"Đơn hàng #{order.Id} vừa thanh toán {order.TotalAmount.ToString("N0")}đ");
                        await SendConfirmationEmail(order);

                        // Bắn thông báo Real-time
                        await _notificationService.SendNotificationAsync(order.UserId, "🎉 Thanh toán thành công!", $"Đơn hàng {order.Id} trị giá {order.TotalAmount.ToString("N0")}đ đã được thanh toán.", $"/User/OrderHistory");
                        
                        ViewBag.Status = "Thành công";
                    }
                    else
                    {
                        ViewBag.Status = "Gian lận hoặc lỗi xác thực thanh toán";
                        status = "ERROR";
                    }
                }
                else
                {
                    ViewBag.Status = "Thành công";
                }
            }
            else if (status == "WAITING")
            {
                ViewBag.Status = "Đang chờ duyệt";
            }
            else
            {
                ViewBag.Status = "Thất bại hoặc bị hủy";
            }

            return View(order);
        }

        // ---  HÀM HỦY VÉ HOÀN TIỀN VÀO VÍ NỘI BỘ BẬC THANG MỚI  ---
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Showtime)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null || order.Status == "CANCELLED")
                return Json(new { success = false, message = "Đơn hàng không hợp lệ hoặc đã bị hủy trước đó!" });

            // 1. Nếu đơn hàng chưa thanh toán (PENDING) -> Hủy ngay không hoàn tiền
            if (!order.IsPaid && order.Status == "PENDING")
            {
                order.Status = "CANCELLED";
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                await _notificationService.SendOrderUpdateAsync(order.Id, $"Đơn hàng #{order.Id} vừa bị hủy");
                return Json(new { success = true, message = "Đã hủy đơn hàng chưa thanh toán và giải phóng ghế thành công!" });
            }

            // 2. Chặn khách tự hủy đơn hàng đang chờ duyệt WAITING_CONFIRM
            if (order.Status == "WAITING_CONFIRM")
            {
                return Json(new { success = false, message = "Đơn hàng đang chờ Admin đối soát thanh toán, bạn không thể tự hủy!" });
            }

            // 3. Nếu đơn hàng đã thanh toán (PAID) -> Tính toán hoàn tiền bậc thang
            var firstDetail = order.OrderDetails.FirstOrDefault();
            if (firstDetail == null) return NotFound();

            DateTime showtime = firstDetail.Showtime.StartTime;
            DateTime now = DateTime.Now;

            TimeSpan timeDifference = showtime - now;
            double hoursBeforeShow = timeDifference.TotalHours;

            TimeSpan timeSinceBooking = now - order.OrderDate;
            double minutesSinceBooking = timeSinceBooking.TotalMinutes;

            decimal refundPercentage = 0;
            string refundReason = "";

            // Áp dụng luật mới linh hoạt
            if (minutesSinceBooking <= 10 && hoursBeforeShow > 0)
            {
                refundPercentage = 1.0m;
                refundReason = $"Hủy trong 10 phút đầu sau khi đặt (Miễn phí hủy)";
            }
            else
            {
                if (hoursBeforeShow >= 12)
                {
                    refundPercentage = 1.0m;
                    refundReason = "Hủy trước giờ chiếu trên 12 tiếng (Hoàn 100%)";
                }
                else if (hoursBeforeShow >= 4)
                {
                    refundPercentage = 0.75m;
                    refundReason = "Hủy trước giờ chiếu từ 4 đến 12 tiếng (Hoàn 75%)";
                }
                else if (hoursBeforeShow >= 1)
                {
                    refundPercentage = 0.50m;
                    refundReason = "Hủy trước giờ chiếu từ 1 đến 4 tiếng (Hoàn 50%)";
                }
                else
                {
                    return Json(new { success = false, message = "Phim sắp chiếu (dưới 1 tiếng), rạp không hỗ trợ hủy nữa bạn ơi!" });
                }
            }

            decimal refundAmount = order.TotalAmount * refundPercentage;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    order.IsPaid = false;
                    order.Status = "CANCELLED";
                    _context.Orders.Update(order);

                    // --- TRỪ ĐIỂM THƯỞNG KHI HỦY ĐƠN ---
                    var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == order.UserId);
                    if (profile != null)
                    {
                        int earnedPoints = (int)(order.TotalAmount / 10000);
                        profile.RewardPoints -= earnedPoints;
                        if (profile.RewardPoints < 0) profile.RewardPoints = 0; // Tránh âm điểm
                        _context.CustomerProfiles.Update(profile);
                    }

                    var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
                    if (wallet == null)
                    {
                        wallet = new Wallet { UserId = userId, Balance = 0, UpdatedAt = DateTime.Now };
                        _context.Wallets.Add(wallet);
                        await _context.SaveChangesAsync();
                    }

                    wallet.Balance += refundAmount;
                    wallet.UpdatedAt = DateTime.Now;
                    _context.Wallets.Update(wallet);

                    var walletTx = new WalletTransaction
                    {
                        WalletId = wallet.Id,
                        Amount = refundAmount,
                        Type = "REFUND",
                        Description = $"Hoàn tiền {(refundPercentage * 100):F0}% hủy vé #{order.Id} ({refundReason})",
                        CreatedAt = DateTime.Now
                    };
                    _context.WalletTransactions.Add(walletTx);

                    // Khôi phục Voucher sử dụng (nếu có)
                    if (order.PromotionId.HasValue)
                    {
                        var promo = await _context.Promotions.FindAsync(order.PromotionId.Value);
                        if (promo != null && promo.UsedCount > 0)
                        {
                            promo.UsedCount--;
                            _context.Promotions.Update(promo);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    await _notificationService.SendOrderUpdateAsync(order.Id, $"Đơn hàng #{order.Id} vừa được hoàn {refundAmount.ToString("N0")}đ");

                    // Gửi Email thông báo hoàn tiền thành công
                    await SendRefundEmail(order, refundAmount, refundPercentage, refundReason, wallet.Balance);

                    // Bắn thông báo Real-time
                    await _notificationService.SendNotificationAsync(userId, "❌ Đơn hàng đã hủy", $"Bạn đã hủy thành công đơn hàng {order.Id}. Tiền hoàn lại {refundAmount.ToString("N0")}đ đã được cộng vào Ví nội bộ.", $"/User/OrderHistory");

                    return Json(new { success = true, message = $"Hủy vé thành công! Đã hoàn {refundAmount:N0}đ ({(refundPercentage * 100):F0}%) vào ví nội bộ do: {refundReason}." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Có lỗi xảy ra khi hoàn tiền: " + ex.Message });
                }
            }
        }

        // --- HÀM TÍNH TOÁN ƯỚC LƯỢNG HOÀN TIỀN TRƯỚC KHI HỦY ---
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetRefundEstimate(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Showtime)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null) return Json(new { success = false, message = "Đơn hàng không tồn tại!" });

            if (order.Status == "WAITING_CONFIRM")
            {
                return Json(new {
                    success = true,
                    isPaid = order.IsPaid,
                    status = order.Status,
                    totalAmount = order.TotalAmount,
                    refundPercentage = 0,
                    refundAmount = 0,
                    policy = "Đơn hàng đang chờ Admin xác nhận thanh toán, không thể tự hủy",
                    canCancel = false
                });
            }

            var firstDetail = order.OrderDetails.FirstOrDefault();
            if (firstDetail == null) return Json(new { success = false, message = "Đơn hàng không có thông tin vé!" });

            DateTime showtime = firstDetail.Showtime.StartTime;
            DateTime now = DateTime.Now;

            double hoursBeforeShow = (showtime - now).TotalHours;
            double minutesSinceBooking = (now - order.OrderDate).TotalMinutes;

            decimal refundPercentage = 0;
            string policyDescription = "";

            if (!order.IsPaid)
            {
                refundPercentage = 0;
                policyDescription = "Hủy đơn hàng chưa thanh toán (Không hoàn tiền)";
            }
            else if (minutesSinceBooking <= 10 && hoursBeforeShow > 0)
            {
                refundPercentage = 1.0m;
                policyDescription = $"Đặt dưới 10 phút - Hoàn 100% (Đã đặt {minutesSinceBooking:F0} phút). Sau 10 phút sẽ áp dụng quy định theo giờ chiếu.";
            }
            else if (hoursBeforeShow >= 12)
            {
                refundPercentage = 1.0m;
                policyDescription = "Hủy trước giờ chiếu trên 12 tiếng (Hoàn 100%)";
            }
            else if (hoursBeforeShow >= 4)
            {
                refundPercentage = 0.75m;
                policyDescription = "Hủy trước giờ chiếu từ 4 đến 12 tiếng (Hoàn 75%)";
            }
            else if (hoursBeforeShow >= 1)
            {
                refundPercentage = 0.50m;
                policyDescription = "Hủy trước giờ chiếu từ 1 đến 4 tiếng (Hoàn 50%)";
            }
            else
            {
                refundPercentage = 0;
                policyDescription = "Không hỗ trợ hủy vé dưới 1 tiếng trước giờ chiếu (Hoàn 0%)";
            }

            return Json(new {
                success = true,
                isPaid = order.IsPaid,
                status = order.Status,
                totalAmount = order.TotalAmount,
                refundPercentage = (int)(refundPercentage * 100),
                refundAmount = order.TotalAmount * refundPercentage,
                policy = policyDescription,
                canCancel = order.Status == "PENDING" || (order.IsPaid && (hoursBeforeShow >= 1 || (minutesSinceBooking <= 10 && hoursBeforeShow > 0)))
            });
        }

        // --- GỬI EMAIL THÔNG BÁO HOÀN TIỀN VÉ THÀNH CÔNG ---
        private async Task SendRefundEmail(Order order, decimal refundAmount, decimal refundPercentage, string refundReason, decimal currentBalance)
        {
            try
            {
                var orderDetails = await _context.OrderDetails
                    .Include(od => od.Showtime).ThenInclude(s => s.Movie)
                    .Include(od => od.Showtime).ThenInclude(s => s.CinemaHall).ThenInclude(ch => ch.Cinema)
                    .Include(od => od.Seat)
                    .Where(od => od.OrderId == order.Id)
                    .ToListAsync();

                var firstDetail = orderDetails.FirstOrDefault();
                var movie = firstDetail?.Showtime?.Movie;
                var showtime = firstDetail?.Showtime;
                var hall = showtime?.CinemaHall;
                var cinema = hall?.Cinema;
                var seatRows = string.Join("", orderDetails.Select(od =>
                    $"<tr><td style='padding:6px 12px;border-bottom:1px solid #f0f0f0;'>" +
                    $"<span style='background:#1a1a2e;color:#ef4444;padding:3px 10px;border-radius:4px;font-weight:700;'>{od.Seat.SeatNumber}</span></td>" +
                    $"<td style='padding:6px 12px;border-bottom:1px solid #f0f0f0;color:#555;'>{(string.IsNullOrEmpty(od.TicketType) ? "Ng\u01b0\u1eddi l\u1edbn" : od.TicketType)}</td>" +
                    $"</tr>"));

                string subject = $"[CINEMA HUB] XÁC NHẬN HOÀN TIỀN VÉ THÀNH CÔNG - #{order.Id}";

                string message = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; border-radius: 15px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08);'>

                <div style='background: linear-gradient(135deg, #064e3b 0%, #065f46 100%); color: white; padding: 30px; text-align: center;'>
                    <div style='font-size: 13px; letter-spacing: 3px; opacity: 0.7; margin-bottom: 6px;'>&#128176; HOÀN TIỀN VÉ</div>
                    <h1 style='margin: 0; font-size: 28px; color: #00ff87;'>CINEMA HUB</h1>
                    <p style='margin: 8px 0 0; opacity: 0.8; font-size: 14px;'>Tiền hoàn đã được cộng vào ví nội bộ của bạn!</p>
                </div>

                <div style='background: #fff; padding: 28px;'>

                    <h2 style='color: #1a1a2e; font-size: 18px; margin-bottom: 16px; border-left: 4px solid #ef4444; padding-left: 12px;'>VÉ ĐÃ HỦY</h2>

                    <table style='width:100%; border-collapse:collapse; margin-bottom:20px;'>
                        <tr>
                            <td style='width:40%; color:#888; padding:8px 0; font-size:14px;'>🎬 Phim:</td>
                            <td style='font-weight:700; color:#1a1a2e;'>{movie?.Title}</td>
                        </tr>
                        {(cinema != null ? $"<tr><td style='color:#888;padding:8px 0;font-size:14px;'>📍 Rạp:</td><td style='font-weight:600;color:#1a1a2e;'>{cinema.Name}</td></tr>" : "")}
                        {(hall != null ? $"<tr><td style='color:#888;padding:8px 0;font-size:14px;'>🚪 Phòng:</td><td style='font-weight:600;color:#1a1a2e;'>{hall.Name}</td></tr>" : "")}
                        <tr>
                            <td style='color:#888; padding:8px 0; font-size:14px;'>&#128197; Ngày chiếu:</td>
                            <td style='font-weight:600;'>{showtime?.StartTime:dd/MM/yyyy}</td>
                        </tr>
                        <tr>
                            <td style='color:#888; padding:8px 0; font-size:14px;'>&#9200; Suất chiếu:</td>
                            <td style='font-weight:700; font-size:18px; color:#e53e3e;'>{showtime?.StartTime:HH:mm}</td>
                        </tr>
                    </table>

                    <h3 style='color:#1a1a2e; font-size:15px; margin-bottom:10px; border-left:4px solid #ef4444; padding-left:12px;'>GHẾ ĐÃ GIẢI PHÓNG</h3>
                    <table style='width:100%; border-collapse:collapse; margin-bottom:24px; background:#fafafa; border-radius:8px; overflow:hidden;'>
                        <thead>
                            <tr style='background:#1a1a2e; color:#fff; font-size:13px;'>
                                <th style='padding:8px 12px; text-align:left;'>Số ghế</th>
                                <th style='padding:8px 12px; text-align:left;'>Loại vé</th>
                            </tr>
                        </thead>
                        <tbody>{seatRows}</tbody>
                    </table>

                    <h2 style='color: #1a1a2e; font-size: 18px; margin-bottom: 16px; border-left: 4px solid #00ff87; padding-left: 12px;'>THÔNG TIN HOÀN TIỀN</h2>
                    <table style='width:100%; border-collapse:collapse; margin-bottom:20px;'>
                        <tr>
                            <td style='color:#888; padding:8px 0; font-size:14px;'>Giá trị vé gốc:</td>
                            <td style='font-weight:600; text-align:right;'>{order.TotalAmount:N0}đ</td>
                        </tr>
                        <tr>
                            <td style='color:#888; padding:8px 0; font-size:14px;'>Tỷ lệ hoàn trả:</td>
                            <td style='font-weight:700; color:#059669; text-align:right;'>{(refundPercentage * 100):F0}%</td>
                        </tr>
                        <tr style='border-top:2px solid #e0e0e0;'>
                            <td style='padding:12px 0; font-size:16px; font-weight:700;'>Số tiền hoàn vào ví:</td>
                            <td style='font-size:22px; font-weight:700; color:#059669; text-align:right;'>{refundAmount:N0}đ</td>
                        </tr>
                        <tr>
                            <td style='color:#888; padding:8px 0; font-size:14px;'>Số dư ví sau hoàn:</td>
                            <td style='font-weight:600; text-align:right;'>{currentBalance:N0}đ</td>
                        </tr>
                    </table>

                    <div style='background:#fef3c7; border:1px solid #fde68a; border-radius:8px; padding:14px; font-size:13px; color:#92400e;'>
                        &#8505;&#65039; <b>Lý do:</b> {refundReason}
                    </div>

                </div>

                <div style='background:#f8f9fa; padding:18px; text-align:center; font-size:13px; color:#888; border-top:1px solid #eee;'>
                    Tiền hoàn đã được cộng trực tiếp vào Ví nội bộ Cinema Hub của bạn.<br>
                    <span style='color:#bbb;'>Cinema Hub &mdash; Đồ án IT HUFLIT</span>
                </div>

            </div>";

                await _emailSender.SendEmailAsync(order.User.Email, subject, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi Mail Hoàn Tiền: " + ex.Message);
            }
        }

        // --- CÁC HÀM TRỢ GIÚP (HELPERS) ---
        private void ClearBookingSession()
        {
            HttpContext.Session.Remove("ShowtimeId");
            HttpContext.Session.Remove("SelectedSeats");
            HttpContext.Session.Remove("SelectedCombos");
            HttpContext.Session.Remove("FinalTotal");
        }

        private async Task SendConfirmationEmail(Order order)
        {
            try
            {
                var orderDetails = await _context.OrderDetails
                    .Include(od => od.Showtime).ThenInclude(s => s.Movie)
                    .Include(od => od.Showtime).ThenInclude(s => s.CinemaHall).ThenInclude(ch => ch.Cinema)
                    .Include(od => od.Seat)
                    .Where(od => od.OrderId == order.Id)
                    .ToListAsync();

                var firstDetail = orderDetails.FirstOrDefault();
                var movie = firstDetail?.Showtime?.Movie;
                var showtime = firstDetail?.Showtime;
                var hall = showtime?.CinemaHall;
                var cinema = hall?.Cinema;

                string qrContent = $"TICKET-{order.Id}-{order.OrderDate:yyyyMMdd}";
                string qrImageUrl = $"https://quickchart.io/qr?text={qrContent}&size=200&dark=050505&light=f8f8f8";

                // Build từng dòng ghế + loại vé
                var seatRows = string.Join("", orderDetails.Select(od =>
                    $"<tr><td style='padding:6px 12px;border-bottom:1px solid #f0f0f0;'>" +
                    $"<span style='background:#1a1a2e;color:#00ff87;padding:3px 10px;border-radius:4px;font-weight:700;'>{od.Seat.SeatNumber}</span></td>" +
                    $"<td style='padding:6px 12px;border-bottom:1px solid #f0f0f0;color:#555;'>{(string.IsNullOrEmpty(od.TicketType) ? "Ng\u01b0\u1eddi l\u1edbn" : od.TicketType)}</td>" +
                    $"<td style='padding:6px 12px;border-bottom:1px solid #f0f0f0;font-weight:600;'>{od.PriceAtBooking:N0}\u0111</td>" +
                    $"</tr>"));

                string subject = $"[CINEMA HUB] V\u00c9 XEM PHIM TH\u00c0NH C\u00d4NG - #{order.Id}";

                string message = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; border-radius: 15px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08);'>

                <div style='background: linear-gradient(135deg, #0a1628 0%, #0d2137 100%); color: white; padding: 30px; text-align: center;'>
                    <div style='font-size: 13px; letter-spacing: 3px; opacity: 0.7; margin-bottom: 6px;'>&#127916; VÉ XEM PHIM</div>
                    <h1 style='margin: 0; font-size: 30px; color: #00ff87; letter-spacing: -1px;'>CINEMA HUB</h1>
                    <p style='margin: 8px 0 0; opacity: 0.8; font-size: 14px;'>Cảm ơn bạn đã đặt vé. Chúc bạn xem phim vui!</p>
                </div>

                <div style='background: #fff; padding: 28px;'>

                    <h2 style='color: #1a1a2e; font-size: 18px; margin-bottom: 16px; border-left: 4px solid #00ff87; padding-left: 12px;'>THÔNG TIN VÉ</h2>

                    <table style='width:100%; border-collapse:collapse; margin-bottom:20px;'>
                        <tr>
                            <td style='width:40%; color:#888; padding:8px 0; font-size:14px;'>🎬 Phim:</td>
                            <td style='font-weight:700; color:#1a1a2e; font-size:15px;'>{movie?.Title}</td>
                        </tr>
                        {(cinema != null ? $"<tr><td style='color:#888;padding:8px 0;font-size:14px;'>📍 Rạp:</td><td style='font-weight:600;color:#1a1a2e;'>{cinema.Name}</td></tr>" : "")}
                        {(hall != null ? $"<tr><td style='color:#888;padding:8px 0;font-size:14px;'>🚪 Phòng chiếu:</td><td style='font-weight:600;color:#1a1a2e;'>{hall.Name}</td></tr>" : "")}
                        <tr>
                            <td style='color:#888; padding:8px 0; font-size:14px;'>&#128197; Ngày chiếu:</td>
                            <td style='font-weight:600; color:#1a1a2e;'>{showtime?.StartTime:dd/MM/yyyy}</td>
                        </tr>
                        <tr>
                            <td style='color:#888; padding:8px 0; font-size:14px;'>&#9200; Suất chiếu:</td>
                            <td style='font-weight:700; font-size:18px; color:#e53e3e;'>{showtime?.StartTime:HH:mm}</td>
                        </tr>
                        {(!string.IsNullOrEmpty(showtime?.Format) ? $"<tr><td style='color:#888;padding:8px 0;font-size:14px;'>&#127909; Định dạng:</td><td><span style='background:#6366f1;color:#fff;padding:2px 10px;border-radius:4px;font-weight:700;font-size:13px;'>{showtime.Format}</span></td></tr>" : "")}
                    </table>

                    <h3 style='color:#1a1a2e; font-size:15px; margin-bottom:10px; border-left:4px solid #e53e3e; padding-left:12px;'>DANH SÁCH GHẾ</h3>
                    <table style='width:100%; border-collapse:collapse; margin-bottom:24px; background:#fafafa; border-radius:8px; overflow:hidden;'>
                        <thead>
                            <tr style='background:#1a1a2e; color:#fff; font-size:13px;'>
                                <th style='padding:8px 12px; text-align:left;'>Số ghế</th>
                                <th style='padding:8px 12px; text-align:left;'>Loại vé</th>
                                <th style='padding:8px 12px; text-align:left;'>Giá</th>
                            </tr>
                        </thead>
                        <tbody>{seatRows}</tbody>
                    </table>

                    <div style='background:#fafafa; border:1px solid #e0e0e0; border-radius:10px; padding:16px; margin-bottom:24px; display:flex; justify-content:space-between; align-items:center;'>
                        <span style='color:#555; font-size:15px;'>💰 Tổng tiền thanh toán:</span>
                        <span style='font-size:22px; font-weight:700; color:#e53e3e;'>{order.TotalAmount:N0}đ</span>
                    </div>

                    <div style='text-align:center; margin:24px 0; padding:24px; background:#fafafa; border:2px dashed #e0e0e0; border-radius:12px;'>
                        <p style='margin-bottom:12px; font-weight:700; color:#555; font-size:14px; letter-spacing:1px;'>MÃ QR VÀO CỔNG</p>
                        <img src='{qrImageUrl}' alt='Mã QR' style='width:180px; height:180px; display:block; margin:0 auto; border:6px solid #fff; border-radius:8px; box-shadow:0 2px 10px rgba(0,0,0,0.15);' />
                        <p style='font-size:20px; font-weight:700; color:#1a1a2e; margin-top:14px; font-family:monospace;'>#{order.Id.ToString().PadLeft(6, '0')}</p>
                        <p style='color:#888; font-size:12px; margin:0;'>Xuất trình mã này tại cổng vào rạp</p>
                    </div>

                </div>

                <div style='background:#f8f9fa; padding:18px; text-align:center; font-size:13px; color:#888; border-top:1px solid #eee;'>
                    Vui lòng có mặt trước <b>15 phút</b> để check-in và nhận bắp nước nhé! 🍿<br>
                    <span style='color:#bbb;'>Cinema Hub &mdash; Đồ án IT HUFLIT</span>
                </div>

            </div>";

                await _emailSender.SendEmailAsync(order.User.Email, subject, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi Mail: " + ex.Message);
            }
        }

        [Authorize]
        public IActionResult MyTickets()
        {
            var userId = _userManager.GetUserId(User);
            var tickets = _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Showtime).ThenInclude(s => s.Movie)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            return View(tickets);
        }
    }

    public class SelectedSeatDto { public int id { get; set; } public string name { get; set; } public decimal price { get; set; } }
    public class SelectedComboDto { public int id { get; set; } public string name { get; set; } public decimal price { get; set; } public int qty { get; set; } }
}