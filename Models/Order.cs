using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnDatVeXemPhim.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = "MoMo";
        public bool IsPaid { get; set; } = false;

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<OrderCombo> OrderCombos { get; set; } = new List<OrderCombo>();

        // --- THÊM 2 TRƯỜNG NÀY ĐỂ KÉO DÂY SANG BẢNG PROMOTIONS ---
        public int? PromotionId { get; set; } // Dấu ? để cho phép NULL nếu đơn hàng đó khách không xài mã giảm giá
        public Promotion Promotion { get; set; }

        // THÊM DÒNG NÀY: Trạng thái đơn hàng (Mặc định ban đầu là PENDING - Chờ thanh toán)
        public string Status { get; set; } = "PENDING";

        // Thêm trường lưu ưu đãi VIP để dễ đối soát
        [Column(TypeName = "decimal(18,2)")]
        public decimal VipDiscount { get; set; } = 0;
        public string VipLevel { get; set; } = "";
    }
}