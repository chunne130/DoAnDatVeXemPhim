using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnDatVeXemPhim.Models;
using DoAnDatVeXemPhim.Data;

namespace DoAnDatVeXemPhim.Controllers
{
    public class ComboesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ComboesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        // GET: Comboes
        public async Task<IActionResult> Index(string searchString)
        {
            var query = _context.Combos.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string s = searchString.ToLower();
                query = query.Where(c => 
                    c.Name.ToLower().Contains(s) ||
                    (c.Description != null && c.Description.ToLower().Contains(s))
                );
            }
            
            ViewData["CurrentFilter"] = searchString;

            // Sắp xếp combo mới nhất lên đầu để dễ quản lý
            var combos = await query.OrderByDescending(c => c.Id).ToListAsync();
            
            if (IsAjaxRequest()) return PartialView(combos);
            return View(combos);
        }

        // GET: Comboes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var combo = await _context.Combos.FirstOrDefaultAsync(m => m.Id == id);
            if (combo == null) return NotFound();

            return View(combo);
        }

        // GET: Comboes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Comboes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Price,ImageUrl")] Combo combo)
        {
            if (ModelState.IsValid)
            {
                
                if (string.IsNullOrEmpty(combo.ImageUrl))
                {
                    combo.ImageUrl = "https://via.placeholder.com/300x200?text=Cinema+Popcorn";
                }

                _context.Add(combo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(combo);
        }

        // GET: Comboes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var combo = await _context.Combos.FindAsync(id);
            if (combo == null) return NotFound();

            return View(combo);
        }

        // POST: Comboes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,ImageUrl")] Combo combo)
        {
            if (id != combo.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(combo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ComboExists(combo.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(combo);
        }

        // GET: Comboes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var combo = await _context.Combos.FirstOrDefaultAsync(m => m.Id == id);
            if (combo == null) return NotFound();

            return View(combo);
        }

        // POST: Comboes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var combo = await _context.Combos.FindAsync(id);
            if (combo != null)
            {
                _context.Combos.Remove(combo);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ComboExists(int id)
        {
            return _context.Combos.Any(e => e.Id == id);
        }
    }
}