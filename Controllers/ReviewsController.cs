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
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReviewsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("{movieId}")]
        public async Task<IActionResult> GetReviews(int movieId)
        {
            var reviews = await _context.MovieReviews
                .Where(r => r.MovieId == movieId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    id = r.Id,
                    userName = r.User.UserName,
                    rating = r.Rating,
                    comment = r.Comment,
                    createdAt = r.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss")
                })
                .ToListAsync();

            var averageRating = reviews.Any() ? Math.Round(reviews.Average(r => r.rating), 1) : 0;
            
            // Tính số lượng review theo từng sao (1-5)
            var ratingCounts = new int[5];
            foreach (var r in reviews)
            {
                if (r.rating >= 1 && r.rating <= 5)
                {
                    ratingCounts[r.rating - 1]++;
                }
            }

            return Ok(new {
                reviews = reviews,
                averageRating = averageRating,
                totalReviews = reviews.Count,
                ratingCounts = ratingCounts
            });
        }

        [HttpPost("{movieId}")]
        [Authorize]
        public async Task<IActionResult> PostReview(int movieId, [FromBody] ReviewRequest request)
        {
            if (request == null || request.Rating < 1 || request.Rating > 5 || string.IsNullOrWhiteSpace(request.Comment))
            {
                return BadRequest(new { message = "Dữ liệu không hợp lệ." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // 1. Kiểm tra xem người dùng đã đánh giá phim này chưa (1 lần duy nhất)
            var existingReview = await _context.MovieReviews
                .FirstOrDefaultAsync(r => r.MovieId == movieId && r.UserId == user.Id);
                
            if (existingReview != null)
            {
                return BadRequest(new { message = "Bạn đã đánh giá bộ phim này rồi." });
            }

            // 2. Kiểm tra xem người dùng đã mua vé (đã thanh toán) cho phim này chưa
            var hasPurchased = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Showtime)
                .AnyAsync(o => o.UserId == user.Id && o.IsPaid == true && o.OrderDetails.Any(od => od.Showtime.MovieId == movieId));

            if (!hasPurchased)
            {
                return BadRequest(new { message = "Bạn cần mua vé phim này để bình luận." });
            }

            // 3. Lưu đánh giá
            var review = new MovieReview
            {
                MovieId = movieId,
                UserId = user.Id,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.Now
            };

            _context.MovieReviews.Add(review);
            
            // 4. Cộng điểm thưởng (+1đ)
            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile != null)
            {
                profile.RewardPoints += 1;
                _context.CustomerProfiles.Update(profile);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đánh giá của bạn đã được ghi nhận. Bạn được cộng 1 điểm thưởng!" });
        }
    }

    public class ReviewRequest
    {
        public int Rating { get; set; }
        public string Comment { get; set; }
    }
}
