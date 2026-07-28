using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnDatVeXemPhim.Data;
using DoAnDatVeXemPhim.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DoAnDatVeXemPhim.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PromotionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PromotionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        // ── ⚙️ API KIỂM TRA MÃ VOUCHER + CHẶN CHÉO PHÂN HẠNG THÀNH VIÊN REAL-TIME ──
        [HttpGet]
        [AllowAnonymous]
        [Route("Promotions/CheckVoucher")]
        public async Task<IActionResult> CheckVoucher(string code, decimal orderValue)
        {
            if (string.IsNullOrEmpty(code))
                return Json(new { success = false, message = "Mã voucher trống." });

            var promo = await _context.Promotions
                .Include(p => p.MembershipLevel)
                .FirstOrDefaultAsync(p => p.VoucherCode == code.ToUpper().Trim());

            if (promo == null)
                return Json(new { success = false, message = "Mã khuyến mãi này không tồn tại hệ thống rồi ạ!" });

            if (promo.StartDate > DateTime.Now || promo.EndDate < DateTime.Now)
                return Json(new { success = false, message = "Mã giảm giá này đã hết hạn sử dụng mất tiêu!" });

            if (promo.UsedCount >= promo.UsageLimit)
                return Json(new { success = false, message = "Mã giảm giá này đã hết lượt sử dụng rồi bạn ơi!" });

            if (orderValue < promo.MinOrderValue)
                return Json(new { success = false, message = $"Đơn hàng phải đạt tối thiểu {promo.MinOrderValue:N0}đ mới xài được mã này á." });

            // CHẶN VIỆC TỰ NHẬP MÃ CỦA VOUCHER ĐỔI BẰNG ĐIỂM
            if (promo.PointsRequired > 0)
            {
                if (User.Identity == null || !User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "Bạn phải đăng nhập để dùng voucher đổi bằng điểm." });
                }
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
                if (user != null)
                {
                    bool ownsVoucher = await _context.UserPromotions.AnyAsync(up => up.UserId == user.Id && up.PromotionId == promo.Id && !up.IsUsed);
                    if (!ownsVoucher)
                    {
                        return Json(new { success = false, message = "Bạn chưa đổi mã này trong Trung Tâm Ưu Đãi nên không thể sử dụng!" });
                    }
                }
            }

            // CHẶN TÀI KHOẢN HẠNG THẤP SỬ DỤNG LÉN VOUCHER HẠNG CAO 🛑───
            if (promo.MembershipLevelId.HasValue)
            {
                if (User.Identity == null || !User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập tài khoản để áp dụng mã giảm giá theo hạng nha!" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không xác thực được danh tính người dùng." });
                }

                var userProfile = await _context.CustomerProfiles
                    .Include(c => c.MembershipLevel)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (userProfile == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy hồ sơ cá nhân để đối chiếu hạng thành viên rồi ạ." });
                }

                decimal userCurrentMinSpend = userProfile.MembershipLevel?.MinSpending ?? 0;
                decimal voucherRequiredMinSpend = promo.MembershipLevel?.MinSpending ?? 0;

                if (userCurrentMinSpend < voucherRequiredMinSpend)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Mã này chỉ dành cho thành viên đạt mốc {promo.MembershipLevel?.LevelName} trở lên thôi ạ! Hạng hiện tại của bạn chưa đủ đặc quyền nha ❌"
                    });
                }
            }

            decimal discountAmount = 0;
            if (promo.IsPercentage)
            {
                discountAmount = orderValue * (promo.DiscountValue / 100m);
            }
            else
            {
                discountAmount = promo.DiscountValue;
            }

            if (discountAmount > orderValue) discountAmount = orderValue;

            return Json(new
            {
                success = true,
                message = promo.Description,
                discountValue = discountAmount
            });
        }

        // ==========================================
        // 1. DANH SÁCH MÃ KHUYẾN MÃI (INDEX)
        // ==========================================
        public async Task<IActionResult> Index(string searchString)
        {
            var query = _context.Promotions.Include(p => p.MembershipLevel).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string s = searchString.ToLower();
                query = query.Where(p => p.VoucherCode.ToLower().Contains(s) || p.Description.ToLower().Contains(s));
            }

            ViewData["CurrentFilter"] = searchString;
            var list = await query.OrderByDescending(p => p.StartDate).ToListAsync();

            if (IsAjaxRequest()) return PartialView(list);
            return View(list);
        }

        // ==========================================
        // 2. TẠO MỚI MÃ VOUCHER (GET)
        // ==========================================
        public IActionResult Create()
        {
            ViewData["MembershipLevelId"] = new SelectList(_context.MembershipLevels, "Id", "LevelName");
            if (IsAjaxRequest()) return PartialView();
            return View();
        }

        // 2. TẠO MỚI MÃ VOUCHER (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Promotion promotion)
        {
            if (ModelState.IsValid)
            {
                var isExist = await _context.Promotions.AnyAsync(p => p.VoucherCode.ToUpper() == promotion.VoucherCode.ToUpper());
                if (isExist)
                {
                    ModelState.AddModelError("VoucherCode", "Mã code voucher này đã tồn tại trong hệ thống rồi bạn ơi!");
                    ViewData["MembershipLevelId"] = new SelectList(_context.MembershipLevels, "Id", "LevelName", promotion.MembershipLevelId);

                    if (IsAjaxRequest()) return PartialView(promotion);
                    return View(promotion);
                }

                promotion.VoucherCode = promotion.VoucherCode.ToUpper().Trim();
                promotion.UsedCount = 0;

                _context.Promotions.Add(promotion);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Đã tạo mới mã khuyến mãi thành công!" });
                }

                TempData["Success"] = "Đã tạo mới mã khuyến mãi thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["MembershipLevelId"] = new SelectList(_context.MembershipLevels, "Id", "LevelName", promotion.MembershipLevelId);
            if (IsAjaxRequest()) return PartialView(promotion);
            return View(promotion);
        }

        // ==========================================
        // 3. CHỈNH SỬA VOUCHER (GET)
        // ==========================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) return NotFound();

            ViewData["MembershipLevelId"] = new SelectList(_context.MembershipLevels, "Id", "LevelName", promotion.MembershipLevelId);

            if (IsAjaxRequest()) return PartialView(promotion);
            return View(promotion);
        }

        // 3. CHỈNH SỬA VOUCHER (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Promotion promotion)
        {
            if (id != promotion.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    promotion.VoucherCode = promotion.VoucherCode.ToUpper().Trim();
                    _context.Update(promotion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Promotions.Any(e => e.Id == promotion.Id)) return NotFound();
                    else throw;
                }

                if (IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Cập nhật mã khuyến mãi thành công!" });
                }

                TempData["Success"] = "Cập nhật mã khuyến mãi thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["MembershipLevelId"] = new SelectList(_context.MembershipLevels, "Id", "LevelName", promotion.MembershipLevelId);
            if (IsAjaxRequest()) return PartialView(promotion);
            return View(promotion);
        }

        // ==========================================
        // 4. XÓA VOUCHER (GET)
        // ==========================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var promotion = await _context.Promotions
                .Include(p => p.MembershipLevel)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (promotion == null) return NotFound();

            if (IsAjaxRequest()) return PartialView(promotion);
            return View(promotion);
        }

        // 4. XÓA VOUCHER (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion != null)
            {
                _context.Promotions.Remove(promotion);
                await _context.SaveChangesAsync();
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true, message = "Đã xóa mã khuyến mãi thành công khỏi hệ thống!" });
            }

            TempData["Success"] = "Đã xóa mã khuyến mãi thành công khỏi hệ thống!";
            return RedirectToAction(nameof(Index));
        }
    }
}