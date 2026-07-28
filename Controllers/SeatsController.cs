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
    public class SeatsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SeatsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. TRANG CHÍNH: HIỂN THỊ SƠ ĐỒ GHẾ THEO PHÒNG
        public async Task<IActionResult> Index(int? hallId)
        {
            if (hallId == null)
            {
                return RedirectToAction("Index", "CinemaHalls");
            }

            var seats = _context.Seats
                .Include(s => s.CinemaHall)
                .Where(s => s.CinemaHallId == hallId)
                .OrderBy(s => s.SeatNumber);

            var hall = await _context.CinemaHalls.Include(h => h.Cinema).FirstOrDefaultAsync(h => h.Id == hallId);
            ViewBag.HallId = hallId;
            ViewBag.HallName = hall != null ? $"{hall.Name} - {hall.Cinema?.Name}" : "N/A";
            ViewBag.TotalSeats = hall?.TotalSeats ?? 0;

            return View(await seats.ToListAsync());
        }

        // 2. HÀM TỰ ĐỘNG TẠO GHẾ (AUTO GENERATE)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoGenerate(int hallId, int numRows, int numCols)
        {
            var hall = await _context.CinemaHalls.FindAsync(hallId);
            if (hall == null) return NotFound();

            // Xóa ghế cũ để làm lại sơ đồ mới
            var oldSeats = _context.Seats.Where(s => s.CinemaHallId == hallId);
            _context.Seats.RemoveRange(oldSeats);

            for (int i = 0; i < numRows; i++)
            {
                char rowLabel = (char)('A' + i);
                for (int j = 1; j <= numCols; j++)
                {
                    var seat = new Seat
                    {
                        CinemaHallId = hallId,
                        SeatNumber = $"{rowLabel}{j}",
                        // Logic phân loại: 2 hàng đầu Normal, gần cuối VIP, cuối Sweetbox
                        SeatType = (i < 2) ? "Normal" : (i < numRows - 1 ? "VIP" : "Sweetbox")
                    };
                    _context.Seats.Add(seat);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { hallId = hallId });
        }

        // 3. TẠO LẺ (CREATE)
        public IActionResult Create(int? hallId)
        {
            if (hallId != null)
                ViewData["CinemaHallId"] = new SelectList(_context.CinemaHalls.Where(h => h.Id == hallId), "Id", "Name");
            else
                ViewData["CinemaHallId"] = new SelectList(_context.CinemaHalls, "Id", "Name");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SeatNumber,SeatType,CinemaHallId")] Seat seat)
        {
            if (ModelState.IsValid)
            {
                _context.Add(seat);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { hallId = seat.CinemaHallId });
            }
            ViewData["CinemaHallId"] = new SelectList(_context.CinemaHalls, "Id", "Name", seat.CinemaHallId);
            return View(seat);
        }

        // 4. CHỈNH SỬA (EDIT) 
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var seat = await _context.Seats.FindAsync(id);
            if (seat == null) return NotFound();

            ViewData["CinemaHallId"] = new SelectList(_context.CinemaHalls, "Id", "Name", seat.CinemaHallId);
            return View(seat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SeatNumber,SeatType,CinemaHallId")] Seat seat)
        {
            if (id != seat.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(seat);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SeatExists(seat.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index), new { hallId = seat.CinemaHallId });
            }
            ViewData["CinemaHallId"] = new SelectList(_context.CinemaHalls, "Id", "Name", seat.CinemaHallId);
            return View(seat);
        }

        // 5. XÓA (DELETE) 
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var seat = await _context.Seats
                .Include(s => s.CinemaHall)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (seat == null) return NotFound();

            return View(seat);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var seat = await _context.Seats.FindAsync(id);
            int? hallId = seat?.CinemaHallId;

            if (seat != null)
            {
                _context.Seats.Remove(seat);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index), new { hallId = hallId });
        }

        private bool SeatExists(int id)
        {
            return _context.Seats.Any(e => e.Id == id);
        }
    }
}