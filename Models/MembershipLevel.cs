namespace DoAnDatVeXemPhim.Models
{
    //  Định nghĩa các mức hạng thẻ (Đồng, Bạc, Vàng)
    public class MembershipLevel
    {
        public int Id { get; set; }
        public string LevelName { get; set; } // Ví dụ: VIP, Vàng, Bạc
        public decimal MinSpending { get; set; } // Số tiền tối thiểu để lên hạng
        public double DiscountRate { get; set; } // % giảm giá đặc quyền (Ví dụ: 0.1 là giảm 10%)
    }
}
