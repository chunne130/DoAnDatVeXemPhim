namespace DoAnDatVeXemPhim.Models
{
    // Quản lý từng phiên chat của khách hàng
    public class ChatSession
    {
        public int Id { get; set; }
        public string UserId { get; set; } // Khóa ngoại link sang AspNetUsers
        public DateTime StartedAt { get; set; } = DateTime.Now;

        // --- THÊM DÒNG NÀY ĐỂ KÉO DÂY SANG ASPNETUSERS ---
        public Microsoft.AspNetCore.Identity.IdentityUser User { get; set; }

        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        
    }
}
