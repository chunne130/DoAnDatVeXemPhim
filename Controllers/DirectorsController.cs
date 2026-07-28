using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnDatVeXemPhim.Data;
using DoAnDatVeXemPhim.Models;

namespace DoAnDatVeXemPhim.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class DirectorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DirectorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        public async Task<IActionResult> Index()
        {
            var directors = await _context.Directors.ToListAsync();
            if (IsAjaxRequest()) return PartialView(directors);
            return View(directors);
        }

        public IActionResult Create()
        {
            if (IsAjaxRequest()) return PartialView();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,ProfilePictureUrl")] Director director)
        {
            if (ModelState.IsValid)
            {
                _context.Add(director);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            if (IsAjaxRequest()) return PartialView(director);
            return View(director);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var director = await _context.Directors.FindAsync(id);
            if (director == null) return NotFound();
            
            if (IsAjaxRequest()) return PartialView(director);
            return View(director);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ProfilePictureUrl")] Director director)
        {
            if (id != director.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(director);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DirectorExists(director.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            if (IsAjaxRequest()) return PartialView(director);
            return View(director);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var director = await _context.Directors.FindAsync(id);
            if (director != null)
            {
                _context.Directors.Remove(director);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DirectorExists(int id)
        {
            return _context.Directors.Any(e => e.Id == id);
        }
    }
}
