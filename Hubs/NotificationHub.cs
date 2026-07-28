using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace DoAnDatVeXemPhim.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // Hub trống, sử dụng Identity mặc định để map UserId với ConnectionId
        // Các client kết nối sẽ tự động được gán vào group theo UserId của họ.
    }
}
