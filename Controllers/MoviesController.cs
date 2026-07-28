using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoAnDatVeXemPhim.Data;
using DoAnDatVeXemPhim.Models;
using Microsoft.AspNetCore.Mvc;
// Trigger Rebuild to compile CSHTML changes
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoAnDatVeXemPhim.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MoviesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🟢 HÀM KIỂM TRA AJAX BỌC THÉP
        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        // GET: Movies
        public async Task<IActionResult> Index(string searchString)
        {
            var query = _context.Movies.Include(m => m.Genre).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string s = searchString.ToLower();
                query = query.Where(m => 
                    m.Title.ToLower().Contains(s) ||
                    (m.Description != null && m.Description.ToLower().Contains(s)) ||
                    (m.Genre != null && m.Genre.Name.ToLower().Contains(s))
                );
            }

            var movies = await query.OrderByDescending(m => m.Id).ToListAsync();

            ViewData["CurrentFilter"] = searchString;

            // Nếu AJAX gọi tới -> Chỉ trả PartialView
            if (IsAjaxRequest()) return PartialView(movies);

            return View(movies);
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movies
                .Include(m => m.Genre)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null) return NotFound();

            if (IsAjaxRequest()) return PartialView(movie);
            return View(movie);
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
            ViewData["GenreId"] = new SelectList(_context.Genres, "Id", "Name");

            if (IsAjaxRequest()) return PartialView();
            return View();
        }

        // POST: Movies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 🚀 ĐÃ SỬA: Thêm AgeRestriction vào cuối danh sách Bind
        public async Task<IActionResult> Create([Bind("Id,Title,Description,PosterUrl,Duration,ReleaseDate,TrailerUrl,GenreId,AgeRestriction")] Movie movie)
        {
            if (ModelState.IsValid)
            {
                _context.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GenreId"] = new SelectList(_context.Genres, "Id", "Name", movie.GenreId);

            if (IsAjaxRequest()) return PartialView(movie);
            return View(movie);
        }

        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movies.FindAsync(id);
            if (movie == null) return NotFound();

            ViewData["GenreId"] = new SelectList(_context.Genres, "Id", "Name", movie.GenreId);

            if (IsAjaxRequest()) return PartialView(movie);
            return View(movie);
        }

        // POST: Movies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 🚀 ĐÃ SỬA: Thêm AgeRestriction vào cuối danh sách Bind
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,PosterUrl,Duration,ReleaseDate,TrailerUrl,GenreId,AgeRestriction")] Movie movie)
        {
            if (id != movie.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(movie.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["GenreId"] = new SelectList(_context.Genres, "Id", "Name", movie.GenreId);

            if (IsAjaxRequest()) return PartialView(movie);
            return View(movie);
        }

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movies
                .Include(m => m.Genre)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null) return NotFound();

            if (IsAjaxRequest()) return PartialView(movie);
            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movies.Any(e => e.Id == id);
        }
    }
}