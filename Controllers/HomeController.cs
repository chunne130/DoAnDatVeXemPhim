using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DoAnDatVeXemPhim.Models;
using DoAnDatVeXemPhim.Data;

namespace DoAnDatVeXemPhim.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public HomeController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ── HÀM BỔ TRỢ: CHUYỂN TIẾNG VIỆT CÓ DẤU THÀNH KHÔNG DẤU ──
    private string RemoveSign4Vietnamese(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        string normalizedString = text.Normalize(NormalizationForm.FormD);
        StringBuilder stringBuilder = new StringBuilder();

        foreach (char c in normalizedString)
        {
            UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        string result = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLower();

        // Sửa thủ công một số chữ đ, o, u đặc biệt sau khi gỡ tổ hợp dấu
        result = Regex.Replace(result, "đ", "d");
        return result.Trim();
    }

    // 1. TRANG CHỦ: Thanh tìm kiếm chi tiết kết hợp hệ thống gợi ý thông minh Content-Based
    public async Task<IActionResult> Index(string searchString, int? genreId, string format, DateTime? showDate)
    {
        var now = DateTime.Now;
        ViewBag.Genres = await _context.Genres.AsNoTracking().ToListAsync();
        
        // Lấy danh sách Banner đang hoạt động
        ViewBag.Banners = await _context.Banners
            .Where(b => b.IsActive && b.StartDate <= now && b.EndDate >= now)
            .OrderBy(b => b.DisplayOrder)
            .AsNoTracking()
            .ToListAsync();

        // ═════════════════════════════════════════════════════════════════
        // THUẬT TOÁN: GỢI Ý PHIM THEO GU (DATABASE DRIVEN)
        // ═════════════════════════════════════════════════════════════════
        List<Movie> recommendedMovies = new List<Movie>();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Bốc ID người dùng hiện tại

        if (!string.IsNullOrEmpty(userId))
        {
            // 1. Tìm Thể loại (GenreId) mà người dùng này mua vé nhiều nhất trong lịch sử đơn hàng đã paid
            var favoriteGenreId = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Showtime)
                    .ThenInclude(s => s.Movie)
                .Where(od => od.Order.UserId == userId && od.Order.IsPaid == true)
                .GroupBy(od => od.Showtime.Movie.GenreId)
                .OrderByDescending(g => g.Count()) // Thể loại đặt nhiều nhất đưa lên đầu
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            if (favoriteGenreId > 0)
            {
                // 2. Lấy danh sách ID các phim người dùng này ĐÃ XEM để không gợi ý trùng lặp
                var watchedMovieIds = await _context.OrderDetails
                    .Include(od => od.Order)
                    .Include(od => od.Showtime)
                    .Where(od => od.Order.UserId == userId && od.Order.IsPaid == true)
                    .Select(od => od.Showtime.Movie.Id)
                    .Distinct()
                    .ToListAsync();

                // 3. Bốc ra tối đa 4 bộ phim cùng thể loại ưa thích mà họ CHƯA XEM bao giờ
                recommendedMovies = await _context.Movies
                    .Include(m => m.Genre)
                    .Include(m => m.MovieReviews)
                    .Where(m => m.GenreId == favoriteGenreId && !watchedMovieIds.Contains(m.Id) && m.ReleaseDate <= now)
                    .Take(4)
                    .AsNoTracking()
                    .ToListAsync();
            }
        }
        ViewBag.RecommendedMovies = recommendedMovies; // Ném cục phim gợi ý ra View

        // Lấy toàn bộ danh sách phim active lên bộ nhớ tạm để xử lý chuỗi tiếng Việt nâng cao
        var moviesList = await _context.Movies
            .Include(m => m.Genre)
            .Include(m => m.Showtimes)
            .Include(m => m.MovieReviews)
            .Where(m => m.ReleaseDate.HasValue && m.ReleaseDate.Value <= now)
            .ToListAsync();

        // Chuyển danh sách sang Enumerable để sử dụng hàm Loại bỏ dấu tiếng Việt động
        var query = moviesList.AsEnumerable();

        // ── THUẬT TOÁN TÌM KIẾM THÔNG MINH BẤT CHẤP DẤU TIẾNG VIỆT ──
        if (!string.IsNullOrEmpty(searchString))
        {
            // --- ĐÃ THÊM: Tính năng Behavior Tracking ---
            var searchLog = new SearchHistory
            {
                Keyword = searchString.Trim(),
                UserId = userId,
                SearchDate = DateTime.Now
            };
            _context.SearchHistories.Add(searchLog);
            await _context.SaveChangesAsync();

            string keywordNoSign = RemoveSign4Vietnamese(searchString);

            query = query.Where(m =>
                m.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                RemoveSign4Vietnamese(m.Title).Contains(keywordNoSign)
            );
        }

        // Lọc các thông số bổ trợ khác
        if (genreId.HasValue && genreId.Value > 0)
        {
            query = query.Where(m => m.GenreId == genreId);
        }
        if (!string.IsNullOrEmpty(format))
        {
            query = query.Where(m => m.Showtimes != null && m.Showtimes.Any(s => s.Format == format && s.IsActive));
        }
        if (showDate.HasValue)
        {
            var targetDate = showDate.Value.Date;
            query = query.Where(m => m.Showtimes != null && m.Showtimes.Any(s => s.StartTime.Date == targetDate && s.IsActive));
        }

        var movies = query.OrderByDescending(m => m.ReleaseDate).ToList();

        // 🚀 BỌC THÉP: Lấy danh sách ID phim yêu thích của User hiện tại để hiển thị trạng thái nút Tim
        if (User.Identity.IsAuthenticated)
        {
            var currentUserId = _userManager.GetUserId(User);
            ViewBag.FavoriteMovieIds = await _context.MovieFavorites
                .Where(f => f.UserId == currentUserId)
                .Select(f => f.MovieId)
                .ToListAsync();
        }
        else
        {
            ViewBag.FavoriteMovieIds = new List<int>();
        }

        // --- ĐÃ THÊM: Lấy Top 5 Từ khóa tìm kiếm nhiều nhất (Behavior Tracking) ---
        var trendingSearches = await _context.SearchHistories
            .GroupBy(s => s.Keyword)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToListAsync();
        ViewBag.TrendingSearches = trendingSearches;

        // --- ĐÃ THÊM: Lấy Top 4 Phim Thịnh Hành (Trending) dựa trên ViewCount ---
        var trendingMovies = await _context.Movies
            .Include(m => m.MovieReviews)
            .Where(m => m.ReleaseDate <= now)
            .OrderByDescending(m => m.ViewCount)
            .Take(4)
            .AsNoTracking()
            .ToListAsync();
        ViewBag.TrendingMovies = trendingMovies;

        // --- ĐÃ THÊM: Tính toán Số vé bán thực tế (Tickets Sold) ---
        var ticketsSoldDict = await _context.OrderDetails
            .Include(od => od.Order)
            .Include(od => od.Showtime)
            .Where(od => od.Order.IsPaid == true)
            .GroupBy(od => od.Showtime.MovieId)
            .Select(g => new { MovieId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.MovieId, x => x.Count);
        ViewBag.TicketsSold = ticketsSoldDict;

        // KIỂM TRA ĐIỀU KIỆN AJAX: Trả về danh sách kết quả Real-time
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_MovieList", movies);
        }

        ViewBag.CurrentSearch = searchString;
        ViewBag.CurrentGenre = genreId;
        ViewBag.CurrentFormat = format;
        ViewBag.CurrentDate = showDate?.ToString("yyyy-MM-dd");

        return View(movies);
    }

    // 2. CHI TIẾT PHIM: Tích hợp thuật toán gợi ý phim tương tự cùng thể loại
    public async Task<IActionResult> MovieDetails(int? id)
    {
        if (id == null) return NotFound();

        var movie = await _context.Movies
            .Include(m => m.Genre)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null) return NotFound();

        // --- ĐÃ THÊM: Tính năng Behavior Tracking ---
        movie.ViewCount++;
        await _context.SaveChangesAsync();

        // ── THUẬT TOÁN GỢI Ý PHIM TƯƠNG TỰ CÙNG THỂ LOẠI ──
        var now = DateTime.Now;
        var relatedMovies = await _context.Movies
            .Include(m => m.Genre)
            .Include(m => m.MovieReviews)
            .Where(m => m.GenreId == movie.GenreId && m.Id != movie.Id && m.ReleaseDate <= now) // Cùng thể loại, loại trừ chính nó
            .Take(4) // Bốc tối đa 4 phim tương tự
            .AsNoTracking()
            .ToListAsync();

        // ── KIỂM TRA ĐIỀU KIỆN REVIEW ──
        bool canReview = false;
        bool hasReviewed = false;
        if (User.Identity.IsAuthenticated)
        {
            var userId = _userManager.GetUserId(User);
            hasReviewed = await _context.MovieReviews.AnyAsync(r => r.MovieId == movie.Id && r.UserId == userId);
            
            if (!hasReviewed)
            {
                canReview = await _context.Orders
                    .Include(o => o.OrderDetails).ThenInclude(od => od.Showtime)
                    .AnyAsync(o => o.UserId == userId && o.IsPaid == true && o.OrderDetails.Any(od => od.Showtime.MovieId == movie.Id));
            }
        }
        ViewBag.CanReview = canReview;
        ViewBag.HasReviewed = hasReviewed;

        // Tính tổng số vé bán thực tế cho phim này
        int ticketsSold = await _context.OrderDetails
            .Include(od => od.Order)
            .Include(od => od.Showtime)
            .Where(od => od.Order.IsPaid == true && od.Showtime.MovieId == movie.Id)
            .CountAsync();
        ViewBag.TicketsSold = ticketsSold;

        // Ném danh sách phim tương tự qua ViewBag sang View
        ViewBag.RelatedMovies = relatedMovies;

        return View(movie);
    }

    // 3. MENU PHIM
    public async Task<IActionResult> Movies()
    {
        var now = DateTime.Now;
        var movies = await _context.Movies
            .Include(m => m.Genre)
            .Include(m => m.MovieReviews)
            .Where(m => m.ReleaseDate.HasValue && m.ReleaseDate.Value <= now)
            .ToListAsync();

        var ticketsSoldDict = await _context.OrderDetails
            .Include(od => od.Order)
            .Include(od => od.Showtime)
            .Where(od => od.Order.IsPaid == true)
            .GroupBy(od => od.Showtime.MovieId)
            .Select(g => new { MovieId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.MovieId, x => x.Count);
        ViewBag.TicketsSold = ticketsSoldDict;

        return View("Index", movies);
    }

    // 4. PHIM SẮP CHIẾU
    public async Task<IActionResult> Upcoming()
    {
        var now = DateTime.Now;
        var upcomingMovies = await _context.Movies
            .Include(m => m.Genre)
            .Include(m => m.MovieReviews)
            .Where(m => m.ReleaseDate.HasValue && m.ReleaseDate.Value > now)
            .OrderBy(m => m.ReleaseDate)
            .ToListAsync();

        return View(upcomingMovies);
    }

    // 5. TRANG HỆ THỐNG RẠP 
    public async Task<IActionResult> CinemaHalls(string city, string district)
    {
        var today = DateTime.Today;
        var now = DateTime.Now;
        var query = _context.Cinemas
            .Include(c => c.CinemaHalls)
                .ThenInclude(ch => ch.Showtimes.Where(s => s.StartTime.Date == today && s.StartTime > now && s.IsActive))
                    .ThenInclude(s => s.Movie)
            .AsQueryable();

        if (!string.IsNullOrEmpty(city))
        {
            query = query.Where(h => h.City.Contains(city));
        }

        if (!string.IsNullOrEmpty(district))
        {
            query = query.Where(h => h.District.Contains(district));
        }

        var cinemas = await query.ToListAsync();

        ViewData["SelectedCity"] = city;
        ViewData["SelectedDistrict"] = district;

        return View(cinemas);
    }



    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public async Task<IActionResult> MovieDetailsComing(int? id)
    {
        if (id == null) return NotFound();

        var movie = await _context.Movies
            .Include(m => m.Genre)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null) return NotFound();

        return View(movie);
    }
    // 6. TRANG KHUYẾN MÃI (OFFERS)
    public async Task<IActionResult> Offers()
    {
        var now = DateTime.Now;
        var promotions = await _context.Promotions
            .Include(p => p.MembershipLevel)
            .Where(p => p.EndDate >= now && p.UsedCount < p.UsageLimit && p.PointsRequired <= 0)
            .OrderBy(p => p.EndDate)
            .ToListAsync();

        // ── Gamification: Đồng bộ dữ liệu chi tiêu thực tế ──
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            // Gọi chung hàm xử lý của CustomerProfilesController để đảm bảo đồng bộ 100% với trang Quản lý
            var vipInfo = await DoAnDatVeXemPhim.Controllers.CustomerProfilesController.UpdateAndGetMembershipAsync(_context, userId);
            ViewBag.VipInfo = vipInfo;
        }

        return View(promotions);
    }

    // API JSON trả về gợi ý tìm kiếm cho Desktop Search Dropdown
    [HttpGet]
    public async Task<IActionResult> SearchSuggestions(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return Json(new List<object>());
        
        string keywordNoSign = RemoveSign4Vietnamese(keyword);
        var allMovies = await _context.Movies.ToListAsync();
        var movies = allMovies
            .Where(m => m.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) || 
                        RemoveSign4Vietnamese(m.Title).Contains(keywordNoSign))
            .OrderByDescending(m => m.ReleaseDate)
            .Take(4)
            .Select(m => new {
                id = m.Id,
                title = m.Title?.Normalize(System.Text.NormalizationForm.FormC) ?? "",
                posterUrl = m.PosterUrl,
                duration = m.Duration,
                ageRestriction = string.IsNullOrEmpty(m.AgeRestriction) ? "T13" : m.AgeRestriction?.Normalize(System.Text.NormalizationForm.FormC)
            }).ToList();
            
        return Json(movies);
    }
}

