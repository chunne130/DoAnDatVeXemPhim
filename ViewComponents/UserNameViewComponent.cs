using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnDatVeXemPhim.Data; 
using System.Threading.Tasks;

namespace DoAnDatVeXemPhim.ViewComponents
{
    public class UserNameViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UserNameViewComponent(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // 1. Lấy Id của người đang đăng nhập
            var userId = _userManager.GetUserId(HttpContext.User);

            if (userId != null)
            {
                // 2. Vào bảng CustomerProfile để tìm FullName
                var profile = await _context.CustomerProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile != null && !string.IsNullOrEmpty(profile.FullName))
                {
                    // Trả về FullName nếu tìm thấy
                    return View("Default", profile.FullName);
                }
            }

            // Nếu không tìm thấy FullName, trả về Email mặc định cho đỡ lỗi
            return View("Default", User.Identity.Name);
        }
    }
}