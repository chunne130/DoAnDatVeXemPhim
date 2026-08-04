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
    public class CinemaHallsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CinemaHallsController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        // GET: CinemaHalls 
        public async Task<IActionResult> Index(string searchString)
        {
            var query = _context.CinemaHalls.Include(c => c.Cinema).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string s = searchString.ToLower();
                query = query.Where(c => 
                    c.Name.ToLower().Contains(s) ||
                    (c.Cinema != null && c.Cinema.Name.ToLower().Contains(s))
                );
            }
            
            ViewData["CurrentFilter"] = searchString;
            var result = await query.ToListAsync();

            if (IsAjaxRequest()) return PartialView(result);
            return View(result);
        }

        // GET: CinemaHalls/Details/5 
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var cinemaHall = await _context.CinemaHalls
                .Include(c => c.Cinema)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cinemaHall == null) return NotFound();

            return PartialView(cinemaHall); 
        }

        // GET: CinemaHalls/Create 
        public IActionResult Create()
        {
            ViewBag.CinemaId = new SelectList(_context.Cinemas, "Id", "Name");
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,TotalSeats,CinemaId")] CinemaHall cinemaHall)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cinemaHall);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CinemaId = new SelectList(_context.Cinemas, "Id", "Name", cinemaHall.CinemaId);
            return PartialView(cinemaHall);
        }

        // GET: CinemaHalls/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var cinemaHall = await _context.CinemaHalls.FindAsync(id);
            if (cinemaHall == null) return NotFound();

            ViewBag.CinemaId = new SelectList(_context.Cinemas, "Id", "Name", cinemaHall.CinemaId);
            return PartialView(cinemaHall);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,TotalSeats,CinemaId")] CinemaHall cinemaHall)
        {
            if (id != cinemaHall.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cinemaHall);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CinemaHallExists(cinemaHall.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CinemaId = new SelectList(_context.Cinemas, "Id", "Name", cinemaHall.CinemaId);
            return PartialView(cinemaHall);
        }

        // GET: CinemaHalls/Delete/5 
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var cinemaHall = await _context.CinemaHalls
                .Include(c => c.Cinema)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cinemaHall == null) return NotFound();

            return PartialView(cinemaHall);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cinemaHall = await _context.CinemaHalls.FindAsync(id);
            if (cinemaHall != null)
            {
                _context.CinemaHalls.Remove(cinemaHall);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CinemaHallExists(int id)
        {
            return _context.CinemaHalls.Any(e => e.Id == id);
        }
    }
}