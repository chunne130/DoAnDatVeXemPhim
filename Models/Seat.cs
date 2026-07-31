using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnDatVeXemPhim.Models
{
    public class Seat
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số ghế")]
        [StringLength(10)]
        [Display(Name = "Số ghế")]
        public string SeatNumber { get; set; } // Ví dụ: A1, A2, B1...

        [Required]
        [StringLength(20)]
        [Display(Name = "Loại ghế")]
        public string SeatType { get; set; } = "Normal"; // "Normal", "VIP", "Sweetbox"
        [Display(Name = "Trạng thái (Bảo trì)")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Phòng chiếu")]
        public int CinemaHallId { get; set; }

        [ForeignKey("CinemaHallId")]
        public virtual CinemaHall? CinemaHall { get; set; }

        // Liên kết với OrderDetail để biết ghế này đã được bán trong suất chiếu nào chưa
        public virtual ICollection<OrderDetail>? OrderDetails { get; set; }
    }
}