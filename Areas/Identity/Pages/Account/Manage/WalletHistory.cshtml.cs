using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoAnDatVeXemPhim.Data;
using DoAnDatVeXemPhim.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DoAnDatVeXemPhim.Areas.Identity.Pages.Account.Manage
{
    public class WalletHistoryModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public WalletHistoryModel(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public decimal CurrentBalance { get; set; }
        public List<WalletTransaction> Transactions { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == user.Id);

            if (wallet != null)
            {
                CurrentBalance = wallet.Balance;
                // Lấy lịch sử giao dịch, sắp xếp mới nhất lên đầu
                Transactions = await _context.WalletTransactions
                    .Where(t => t.WalletId == wallet.Id)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                CurrentBalance = 0;
                Transactions = new List<WalletTransaction>();
            }

            return Page();
        }
    }
}