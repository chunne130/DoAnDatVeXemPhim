namespace DoAnDatVeXemPhim.Models
{
    // Lưu chi tiết từng câu chat trong phiên đó
    public class ChatMessage
    {
        public int Id { get; set; }
        public int ChatSessionId { get; set; }
        public ChatSession ChatSession { get; set; }

        public string Sender { get; set; } // Lưu "User" hoặc "Bot" để phân biệt khi render UI
        public string MessageText { get; set; }
        public DateTime SentAt { get; set; } = DateTime.Now;

       
    }
}
