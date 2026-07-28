using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DoAnDatVeXemPhim.Data;
using DoAnDatVeXemPhim.Models;

namespace DoAnDatVeXemPhim.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            public string PhoneNumber { get; set; }

            public string FullName { get; set; }

            [DataType(DataType.Date)]
            public DateTime? Birthday { get; set; } // PHẢI CÓ DÒNG NÀY

            public string Gender { get; set; }
            public string City { get; set; }
            public string District { get; set; }
            public string Address { get; set; }
        }

        private async Task LoadAsync(IdentityUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                FullName = profile?.FullName ?? "Thành viên mới",
                Birthday = profile?.Birthday,
                Gender = profile?.Gender ?? "Nam",
                City = profile?.City ?? "",
                District = profile?.District ?? "",
                Address = profile?.Address ?? ""
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
            }

            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
            {
                profile = new CustomerProfile
                {
                    UserId = user.Id,
                    FullName = Input.FullName,
                    Birthday = Input.Birthday,
                    Gender = Input.Gender,
                    City = Input.City,
                    District = Input.District,
                    Address = Input.Address
                };
                _context.CustomerProfiles.Add(profile);
            }
            else
            {
                profile.FullName = Input.FullName;
                profile.Birthday = Input.Birthday;
                profile.Gender = Input.Gender;
                profile.City = Input.City;
                profile.District = Input.District;
                profile.Address = Input.Address;
                _context.CustomerProfiles.Update(profile);
            }

            await _context.SaveChangesAsync();
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Hồ sơ của bạn đã được cập nhật thành công!";
            return RedirectToPage();
        }
    }
}