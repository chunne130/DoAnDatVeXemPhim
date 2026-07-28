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
            var query = _context.Movies.Include(m => m.Genres).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string s = searchString.ToLower();
                query = query.Where(m => 
                    m.Title.ToLower().Contains(s) ||
                    (m.Description != null && m.Description.ToLower().Contains(s)) ||
                    (m.Genres.Any(g => g.Name.ToLower().Contains(s)))
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
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null) return NotFound();

            if (IsAjaxRequest()) return PartialView(movie);
            return View(movie);
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
            ViewData["Genres"] = new MultiSelectList(_context.Genres, "Id", "Name");

            if (IsAjaxRequest()) return PartialView();
            return View();
        }

        // POST: Movies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,PosterUrl,Duration,ReleaseDate,TrailerUrl,AgeRestriction")] Movie movie, int[] selectedGenres)
        {
            if (ModelState.IsValid)
            {
                if (selectedGenres != null)
                {
                    var genres = await _context.Genres.Where(g => selectedGenres.Contains(g.Id)).ToListAsync();
                    movie.Genres = genres;
                    movie.GenreName = string.Join(", ", genres.Select(g => g.Name));
                }

                _context.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Genres"] = new MultiSelectList(_context.Genres, "Id", "Name", selectedGenres);

            if (IsAjaxRequest()) return PartialView(movie);
            return View(movie);
        }

        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movies.Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null) return NotFound();

            ViewData["Genres"] = new MultiSelectList(_context.Genres, "Id", "Name", movie.Genres.Select(g => g.Id));

            if (IsAjaxRequest()) return PartialView(movie);
            return View(movie);
        }

        // POST: Movies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,PosterUrl,Duration,ReleaseDate,TrailerUrl,AgeRestriction")] Movie movie, int[] selectedGenres)
        {
            if (id != movie.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var movieToUpdate = await _context.Movies.Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == id);
                    
                    movieToUpdate.Title = movie.Title;
                    movieToUpdate.Description = movie.Description;
                    movieToUpdate.PosterUrl = movie.PosterUrl;
                    movieToUpdate.Duration = movie.Duration;
                    movieToUpdate.ReleaseDate = movie.ReleaseDate;
                    movieToUpdate.TrailerUrl = movie.TrailerUrl;
                    movieToUpdate.AgeRestriction = movie.AgeRestriction;

                    movieToUpdate.Genres.Clear();
                    if (selectedGenres != null)
                    {
                        var genres = await _context.Genres.Where(g => selectedGenres.Contains(g.Id)).ToListAsync();
                        foreach (var g in genres) { movieToUpdate.Genres.Add(g); }
                        movieToUpdate.GenreName = string.Join(", ", genres.Select(g => g.Name));
                    }

                    _context.Update(movieToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(movie.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["Genres"] = new MultiSelectList(_context.Genres, "Id", "Name", selectedGenres);

            if (IsAjaxRequest()) return PartialView(movie);
            return View(movie);
        }

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movies
                .Include(m => m.Genres)
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