using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace DoAnDatVeXemPhim.Hubs
{
    public class SeatHub : Hub
    {
        // Key: {showtimeId}_{seatId}, Value: connectionId
        private static readonly ConcurrentDictionary<string, string> _lockedSeats = new();

        public async Task JoinShowtimeGroup(string showtimeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, showtimeId);
            
            // Lấy danh sách các ghế đang bị khóa trong suất chiếu này
            var lockedSeatsInRoom = _lockedSeats
                .Where(x => x.Key.StartsWith(showtimeId + "_"))
                .Select(x => x.Key.Split('_')[1])
                .ToList();
                
            if(lockedSeatsInRoom.Any())
            {
                await Clients.Caller.SendAsync("ReceiveInitialLockedSeats", lockedSeatsInRoom);
            }
        }

        public async Task LeaveShowtimeGroup(string showtimeId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, showtimeId);
        }

        public async Task LockSeat(string showtimeId, string seatId)
        {
            var seatKey = $"{showtimeId}_{seatId}";
            
            // Cố gắng thêm vào danh sách bị khóa.
            if (_lockedSeats.TryAdd(seatKey, Context.ConnectionId))
            {
                // Báo cho MỌI NGƯỜI TRỪ NGƯỜI GỬI
                await Clients.GroupExcept(showtimeId, Context.ConnectionId).SendAsync("ReceiveSeatLock", seatId);
            }
        }

        public async Task UnlockSeat(string showtimeId, string seatId)
        {
            var seatKey = $"{showtimeId}_{seatId}";
            
            // Chỉ được mở khóa nếu mình là người đã khóa
            if (_lockedSeats.TryGetValue(seatKey, out var connId) && connId == Context.ConnectionId)
            {
                _lockedSeats.TryRemove(seatKey, out _);
                await Clients.GroupExcept(showtimeId, Context.ConnectionId).SendAsync("ReceiveSeatUnlock", seatId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Tìm tất cả ghế do connection này đang giữ
            var seatsToUnlock = _lockedSeats.Where(x => x.Value == Context.ConnectionId).ToList();
            
            foreach (var seat in seatsToUnlock)
            {
                _lockedSeats.TryRemove(seat.Key, out _);
                var parts = seat.Key.Split('_');
                if (parts.Length == 2)
                {
                    var showtimeId = parts[0];
                    var seatId = parts[1];
                    await Clients.Group(showtimeId).SendAsync("ReceiveSeatUnlock", seatId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
