using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace DoAnDatVeXemPhim.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            if (Context.User.IsInRole("Admin") || Context.User.IsInRole("Staff"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "StaffGroup");
            }
            await base.OnConnectedAsync();
        }
    }
}
