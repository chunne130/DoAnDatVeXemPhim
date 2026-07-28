using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DoAnDatVeXemPhim.Models;
using DoAnDatVeXemPhim.Data;

namespace DoAnDatVeXemPhim.Controllers
{
    public class ShowtimesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShowtimesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🟢 CẢM BIẾN KIỂM TRA AJAX BỌC THÉP CHO SPA
        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        // GET: Showtimes
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.CinemaHall)
                .ThenInclude(ch => ch.Cinema);

            var showtimes = await applicationDbContext.ToListAsync();

            // Nếu AJAX gọi tới -> Chỉ trả PartialView
            if (IsAjaxRequest()) return PartialView(showtimes);
            return View(showtimes);
        }

        // GET: Showtimes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var showtime = await _context.Showtimes
                .Include(s => s.CinemaHall)
                .ThenInclude(ch => ch.Cinema)
                .Include(s => s.Movie)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (showtime == null) return NotFound();

            if (IsAjaxRequest()) return PartialView(showtime);
            return View(showtime);
        }

        // GET: Showtimes/Create
        public IActionResult Create()
        {
            LoadDropdownData();

            if (IsAjaxRequest()) return PartialView();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,StartTime,BasePrice,Format,IsActive,MovieId,CinemaHallId")] Showtime showtime)
        {
            if (ModelState.IsValid)
            {
                // Lấy thông tin phim để tính Duration
                var movie = await _context.Movies.FindAsync(showtime.MovieId);
                if (movie != null)
                {
                    showtime.EndTime = showtime.StartTime.AddMinutes(movie.Duration + 15);
                    _context.Add(showtime);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            LoadDropdownData(showtime);

            if (IsAjaxRequest()) return PartialView(showtime);
            return View(showtime);
        }

        // GET: Showtimes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var showtime = await _context.Showtimes.FindAsync(id);
            if (showtime == null) return NotFound();

            LoadDropdownData(showtime);

            if (IsAjaxRequest()) return PartialView(showtime);
            return View(showtime);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StartTime,BasePrice,Format,IsActive,MovieId,CinemaHallId")] Showtime showtime)
        {
            if (id != showtime.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var movie = await _context.Movies.FindAsync(showtime.MovieId);
                    if (movie != null)
                    {
                        showtime.EndTime = showtime.StartTime.AddMinutes(movie.Duration + 15);
                        _context.Update(showtime);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShowtimeExists(showtime.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            LoadDropdownData(showtime);

            if (IsAjaxRequest()) return PartialView(showtime);
            return View(showtime);
        }

        // Hàm dùng chung để load dữ liệu Dropdown 
        private void LoadDropdownData(Showtime showtime = null)
        {
            ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Title", showtime?.MovieId);

            var halls = _context.CinemaHalls.Include(h => h.Cinema).Select(h => new
            {
                Id = h.Id,
                DisplayName = h.Name + " - " + (h.Cinema != null ? h.Cinema.Name : "Rạp lẻ")
            }).ToList();

            ViewData["CinemaHallId"] = new SelectList(halls, "Id", "DisplayName", showtime?.CinemaHallId);
        }

        // GET: Showtimes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var showtime = await _context.Showtimes
                .Include(s => s.CinemaHall)
                .Include(s => s.Movie)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (showtime == null) return NotFound();

            if (IsAjaxRequest()) return PartialView(showtime);
            return View(showtime);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var showtime = await _context.Showtimes.FindAsync(id);
            if (showtime != null)
            {
                _context.Showtimes.Remove(showtime);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ShowtimeExists(int id)
        {
            return _context.Showtimes.Any(e => e.Id == id);
        }
    }
}