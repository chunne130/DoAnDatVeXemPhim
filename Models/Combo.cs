using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnDatVeXemPhim.Models
{
    public class Combo
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên Combo không được để trống")]
        [Display(Name = "Tên Combo")]
        [StringLength(150)]
        public string Name { get; set; }

        [Display(Name = "Mô tả chi tiết")]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; } // Ví dụ: 1 Bắp lớn + 2 Pepsi

        [Required(ErrorMessage = "Vui lòng nhập giá tiền")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá tiền không được âm")]
        [Column(TypeName = "decimal(18, 2)")] // Đảm bảo SQL Server nhận đúng kiểu decimal
        [Display(Name = "Giá bán (VNĐ)")]
        [DisplayFormat(DataFormatString = "{0:N0}")] // Hiện 100,000 thay vì 100000.00
        public decimal Price { get; set; }

        [Display(Name = "Hình ảnh Combo")]
        public string? ImageUrl { get; set; }

        // Liên kết với bảng trung gian khi đặt hàng
        public virtual ICollection<OrderCombo>? OrderCombos { get; set; }
    }
}