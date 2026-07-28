using DoAnDatVeXemPhim.Data;
using DoAnDatVeXemPhim.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnDatVeXemPhim.Controllers
{
    [Authorize]
    public class RewardsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly DoAnDatVeXemPhim.Services.NotificationService _notificationService;

        public RewardsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, DoAnDatVeXemPhim.Services.NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var profile = await _context.CustomerProfiles
                .Include(p => p.MembershipLevel)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                TempData["ErrorMessage"] = "Vui lòng cập nhật đầy đủ thông tin 'Dữ liệu cá nhân' trong phần Cài Đặt Tài Khoản để tham gia Trung Tâm Ưu Đãi!";
                return RedirectToAction("Index", "Home");
            }

            var now = DateTime.Now;

            // Danh sách Voucher có thể đổi bằng điểm
            var availablePromotions = await _context.Promotions
                .Include(p => p.MembershipLevel)
                .Where(p => p.PointsRequired > 0 
                         && p.StartDate <= now 
                         && p.EndDate >= now 
                         && p.UsedCount < p.UsageLimit)
                .OrderBy(p => p.PointsRequired)
                .ToListAsync();

            // Danh sách Voucher người dùng đang sở hữu
            var userPromotions = await _context.UserPromotions
                .Include(up => up.Promotion)
                .Where(up => up.UserId == userId && up.Promotion.EndDate >= now)
                .OrderByDescending(up => up.AcquiredDate)
                .ToListAsync();

            ViewBag.Profile = profile;
            ViewBag.AvailablePromotions = availablePromotions;
            ViewBag.UserPromotions = userPromotions;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Exchange(int promotionId)
        {
            var userId = _userManager.GetUserId(User);
            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            
            if (profile == null) return Json(new { success = false, message = "Không tìm thấy hồ sơ người dùng." });

            var promo = await _context.Promotions.FindAsync(promotionId);
            if (promo == null || promo.PointsRequired <= 0) 
                return Json(new { success = false, message = "Voucher không hợp lệ hoặc không cho phép đổi bằng điểm." });

            if (promo.EndDate < DateTime.Now || promo.UsedCount >= promo.UsageLimit)
                return Json(new { success = false, message = "Voucher này đã hết hạn hoặc hết lượt đổi." });

            if (profile.RewardPoints < promo.PointsRequired)
                return Json(new { success = false, message = $"Bạn không đủ điểm. Cần {promo.PointsRequired} điểm." });

            // Kiểm tra xem user đã sở hữu voucher này chưa và chưa xài
            var alreadyOwns = await _context.UserPromotions.AnyAsync(up => up.UserId == userId && up.PromotionId == promotionId && !up.IsUsed);
            if (alreadyOwns)
                return Json(new { success = false, message = "Bạn đã đổi voucher này và chưa sử dụng rồi!" });

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    profile.RewardPoints -= promo.PointsRequired;
                    _context.CustomerProfiles.Update(profile);

                    var userPromo = new UserPromotion
                    {
                        UserId = userId,
                        PromotionId = promo.Id,
                        AcquiredDate = DateTime.Now,
                        IsUsed = false
                    };
                    _context.UserPromotions.Add(userPromo);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Bắn thông báo Real-time
                    await _notificationService.SendNotificationAsync(userId, "🎁 Đổi mã giảm giá thành công!", $"Bạn vừa đổi {promo.PointsRequired} điểm để nhận Voucher {promo.VoucherCode} (giảm {promo.DiscountValue.ToString("N0")}đ).", "/Rewards");

                    return Json(new { success = true, message = $"Đổi thành công {promo.VoucherCode}! Trừ {promo.PointsRequired} điểm." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Lỗi hệ thống khi đổi điểm: " + ex.Message });
                }
            }
        }
    }
}
