using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnDatVeXemPhim.Models
{
    public class OrderCombo
    {
        [Key]
        public int Id { get; set; }

        public int Quantity { get; set; }

        // Thêm cột này để lưu giá tiền tại thời điểm đặt
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        public int ComboId { get; set; }
        [ForeignKey("ComboId")]
        public virtual Combo? Combo { get; set; }
    }
}