using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnDatVeXemPhim.Data;
using DoAnDatVeXemPhim.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace DoAnDatVeXemPhim.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CustomerProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CustomerProfilesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //  HÀM TÍNH TỔNG TIỀN VÉ + THĂNG HẠNG TỰ ĐỘNG + TÍNH THANH TIẾN TRÌNH CHUẨN ĐÓNG 5 HẠNG 
        public static async Task<MembershipInfoResult> UpdateAndGetMembershipAsync(ApplicationDbContext context, string userId)
        {
            if (string.IsNullOrEmpty(userId)) return new MembershipInfoResult();

            // 1. Tính tổng tiền từ các đơn đặt vé thành công
            decimal totalSpent = await context.Orders
                .Where(o => o.UserId == userId && o.IsPaid == true)
                .SumAsync(o => o.TotalAmount);

            // Bốc toàn bộ danh sách hạng từ DB lên để tính toán
            var allLevels = await context.MembershipLevels.ToListAsync();

            // 2. Tìm hạng thành viên thỏa mãn điều kiện chi tiêu tối thiểu
            var matchedLevel = allLevels
                .Where(m => totalSpent >= m.MinSpending)
                .OrderByDescending(m => m.MinSpending)
                .FirstOrDefault();

            // Lấy hạng thấp nhất trong DB để làm hạng mặc định (Hạng Đồng - 0đ) nếu chưa đạt mốc nào
            if (matchedLevel == null)
            {
                matchedLevel = allLevels
                    .OrderBy(m => m.MinSpending)
                    .FirstOrDefault();
            }

            // 3. Tự động cập nhật hạng mới vào bảng CustomerProfiles ngầm
            var profile = await context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile != null && matchedLevel != null && profile.MembershipLevelId != matchedLevel.Id)
            {
                profile.MembershipLevelId = matchedLevel.Id;
                context.CustomerProfiles.Update(profile);
                await context.SaveChangesAsync();
            }

            // 4. Thuật toán gán mã Voucher động tương ứng với 5 phân hạng thành viên mới nạp
            string uuDaiText = "Hãy tích lũy thêm tiền vé để nhận đặc quyền giảm giá hấp dẫn nha! 🥰";
            string codeVoucher = "";

            if (matchedLevel != null)
            {
                // Mặc định khởi tạo phân hạng thấp nhất
                string searchCode = "DONG";
                if (matchedLevel.LevelName.ToUpper().Contains("BẠC") || matchedLevel.LevelName.ToLower().Contains("silver")) searchCode = "BAC";
                else if (matchedLevel.LevelName.ToUpper().Contains("VÀNG") || matchedLevel.LevelName.ToLower().Contains("gold")) searchCode = "VANG";
                else if (matchedLevel.LevelName.ToUpper().Contains("BẠCH KIM") || matchedLevel.LevelName.ToLower().Contains("platinum")) searchCode = "BACHKIM";
                else if (matchedLevel.LevelName.ToUpper().Contains("KIM CƯƠNG") || matchedLevel.LevelName.ToLower().Contains("diamond")) searchCode = "KIMCUONG";

                // Tìm mã voucher tương ứng còn hạn sử dụng trong DB
                var promo = await context.Promotions
                    .FirstOrDefaultAsync(p => p.VoucherCode == searchCode && p.EndDate >= DateTime.Now);

                if (promo != null)
                {
                    codeVoucher = promo.VoucherCode;
                    uuDaiText = promo.Description;
                }
                else if (matchedLevel.DiscountRate > 0)
                {
                    uuDaiText = $"Nhận ngay đặc quyền giảm giá {matchedLevel.DiscountRate * 100}% tổng hóa đơn đặt vé!";
                }
            }

            // 5. THUẬT TOÁN TÍNH TOÁN TIẾN TRÌNH THĂNG HẠNG ĐỘNG
            decimal nextLevelTarget = 0;
            decimal amountMissing = 0;
            int progressPercentage = 0;

            // Tìm phân hạng kế tiếp cao hơn hạng hiện tại của người dùng
            var nextLevel = allLevels
                .Where(l => l.MinSpending > totalSpent)
                .OrderBy(l => l.MinSpending)
                .FirstOrDefault();

            if (nextLevel != null)
            {
                nextLevelTarget = nextLevel.MinSpending;
                amountMissing = nextLevelTarget - totalSpent;

                // Tính toán % làm đầy thanh dựa trên khoảng cách giữa hạng hiện tại và hạng tiếp theo
                decimal currentLevelMin = matchedLevel?.MinSpending ?? 0;
                decimal range = nextLevelTarget - currentLevelMin;

                if (range > 0)
                {
                    decimal progress = ((totalSpent - currentLevelMin) / range) * 100;
                    progressPercentage = (int)Math.Round(progress);
                }
            }
            else
            {
                progressPercentage = 100;
            }

            // Đảm bảo số phần trăm luôn nằm trong ngưỡng an toàn tuyệt đối từ 0 -> 100%
            if (progressPercentage < 0) progressPercentage = 0;
            if (progressPercentage > 100) progressPercentage = 100;

            return new MembershipInfoResult
            {
                TotalSpent = totalSpent,
                LevelName = matchedLevel?.LevelName ?? "Hạng Đồng",
                Description = uuDaiText,
                VoucherCode = codeVoucher,
                NextLevelTarget = nextLevelTarget,
                AmountMissing = amountMissing,
                ProgressPercentage = progressPercentage
            };
        }

        // 1. DANH SÁCH THÀNH VIÊN + SEARCH 
        public async Task<IActionResult> Index(string searchString)
        {
            
            var query = _context.CustomerProfiles
                .Include(c => c.User)
                .Include(c => c.MembershipLevel)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string s = searchString.ToLower();
                query = query.Where(p =>
                    p.FullName.ToLower().Contains(s) ||
                    p.City.ToLower().Contains(s) ||
                    p.District.ToLower().Contains(s) ||
                    p.Address.ToLower().Contains(s) ||
                    (p.User != null && p.User.Email.ToLower().Contains(s))
                );
            }

            ViewData["CurrentFilter"] = searchString;

            // Sắp xếp danh sách hồ sơ mới nhất lên trên đầu để Admin tiện quản lý
            var list = await query.OrderByDescending(p => p.Id).ToListAsync();
            return View(list);
        }

        // 2. CHỈNH SỬA (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var customerProfile = await _context.CustomerProfiles
                .Include(c => c.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (customerProfile == null) return NotFound();

            return View(customerProfile);
        }

        // 2. CHỈNH SỬA (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerProfile customerProfile, string NewPassword, string NewEmail)
        {
            if (id != customerProfile.Id) return NotFound();

            ModelState.Remove("User");
            ModelState.Remove("NewEmail");
            ModelState.Remove("NewPassword");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(customerProfile);
                    await _context.SaveChangesAsync();

                    var user = await _userManager.FindByIdAsync(customerProfile.UserId);
                    if (user != null)
                    {
                        if (!string.IsNullOrEmpty(NewEmail) && NewEmail != user.Email)
                        {
                            user.Email = NewEmail;
                            user.UserName = NewEmail;
                            await _userManager.UpdateAsync(user);
                        }

                        if (!string.IsNullOrEmpty(NewPassword))
                        {
                            await _userManager.RemovePasswordAsync(user);
                            await _userManager.AddPasswordAsync(user, NewPassword);
                        }
                    }

                    TempData["Success"] = "Đã cập nhật thông tin thành công!";
                    customerProfile.User = await _userManager.FindByIdAsync(customerProfile.UserId);
                    return View(customerProfile);
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
                }
            }
            customerProfile.User = await _userManager.FindByIdAsync(customerProfile.UserId);
            return View(customerProfile);
        }

        // --- 3. XÓA (BƯỚC 1: HIỆN TRANG XÁC NHẬN - GET) ---
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var customerProfile = await _context.CustomerProfiles
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (customerProfile == null) return NotFound();

            return View(customerProfile);
        }

        // --- 3. XÓA (BƯỚC 2: THỰC HIỆN XÓA THẬT - POST) ---
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var profile = await _context.CustomerProfiles
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (profile == null) return RedirectToAction(nameof(Index));

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && currentUser.Id == profile.UserId)
                {
                    TempData["Error"] = "Bạn không thể tự xóa tài khoản đang đăng nhập của mình!";
                    return RedirectToAction(nameof(Index));
                }

                var identityUser = profile.User;

                _context.CustomerProfiles.Remove(profile);
                await _context.SaveChangesAsync();

                if (identityUser != null)
                {
                    var roles = await _userManager.GetRolesAsync(identityUser);
                    foreach (var role in roles)
                    {
                        await _userManager.RemoveFromRoleAsync(identityUser, role);
                    }

                    var result = await _userManager.DeleteAsync(identityUser);
                    if (!result.Succeeded)
                    {
                        TempData["Error"] = "Đã xóa Profile nhưng gặp lỗi khi xóa tài khoản Identity: " +
                                             string.Join(", ", result.Errors.Select(e => e.Description));
                        return RedirectToAction(nameof(Index));
                    }
                }

                TempData["Success"] = "Đã xóa sạch sẽ cả thông tin Profile và tài khoản đăng nhập!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi hệ thống: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }

    public class MembershipInfoResult
    {
        public decimal TotalSpent { get; set; } = 0;
        public string LevelName { get; set; } = "Hạng Đồng";
        public string Description { get; set; } = string.Empty;
        public string VoucherCode { get; set; } = string.Empty;

        public decimal NextLevelTarget { get; set; } = 0;
        public decimal AmountMissing { get; set; } = 0;
        public int ProgressPercentage { get; set; } = 0;
    }
}