using DoAnDatVeXemPhim.Data;
using DoAnDatVeXemPhim.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnDatVeXemPhim.Controllers
{
    [Authorize] // Bắt buộc đăng nhập mới thả tim được
    public class FavoritesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public FavoritesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Helper kiểm tra request gọi ngầm bằng AJAX chống chớp trang của ní
        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        // 🚀 API Xử lý Thả tim / Bỏ tim bằng AJAX
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int movieId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            // Kiểm tra xem phim này đã được user thả tim chưa
            var favorite = await _context.MovieFavorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.MovieId == movieId);

            if (favorite != null)
            {
                // Nếu đã có -> Tiến hành BỎ TIM (Xóa khỏi DB)
                _context.MovieFavorites.Remove(favorite);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = false, message = "Đã xóa khỏi danh sách yêu thích!" });
            }
            else
            {
                // Nếu chưa có -> Tiến hành THẢ TIM (Thêm vào DB)
                var newFav = new MovieFavorite
                {
                    UserId = userId,
                    MovieId = movieId,
                    CreatedAt = DateTime.Now
                };
                _context.MovieFavorites.Add(newFav);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = true, message = "Đã thêm vào danh sách yêu thích!" });
            }
        }

        // 🚀 Trang hiển thị toàn bộ Phim yêu thích của User
        public async Task<IActionResult> MyFavorites()
        {
            var userId = _userManager.GetUserId(User);

            // 🚀 ĐÃ SỬA: Thêm điều kiện f.Movie != null để tránh dính bản ghi rác dưới Database
            var favoriteMovies = await _context.MovieFavorites
                .Include(f => f.Movie)
                .Where(f => f.UserId == userId && f.Movie != null)
                .Select(f => f.Movie)
                .ToListAsync();

            // 🚀 ĐÃ SỬA: Đồng bộ cơ chế SPA tải trang không chớp màn hình của dự án ní
            if (IsAjaxRequest()) return PartialView(favoriteMovies);

            return View(favoriteMovies);
        }
    }
}