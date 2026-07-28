public class WalletTransaction
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public decimal Amount { get; set; } // Số tiền tăng hoặc giảm
    public string Type { get; set; } // "REFUND" (Hoàn tiền), "PAYMENT" (Thanh toán bằng ví), "TOPUP" (Nạp tiền)
    public string Description { get; set; } // Nội dung: "Hoàn tiền hủy đơn #63"
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual Wallet Wallet { get; set; }
}