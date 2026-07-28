using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnDatVeXemPhim.Models
{
    public class Showtime
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian chiếu")]
        [Display(Name = "Thời gian bắt đầu")]
        public DateTime StartTime { get; set; }

        // Thêm EndTime để hệ thống tự tính xem khi nào suất chiếu kết thúc, 
        // tránh việc xếp 2 phim trùng giờ vào cùng 1 phòng.
        [Display(Name = "Thời gian kết thúc")]
        public DateTime EndTime { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá vé")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 10000000, ErrorMessage = "Giá vé không hợp lệ")]
        [Display(Name = "Giá vé gốc")]
        public decimal BasePrice { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Định dạng")]
        public string Format { get; set; } = "2D"; // "2D", "3D", "IMAX"

        // Trạng thái suất chiếu để Admin có thể đóng/mở bán vé nhanh
        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;

        // Foreign Keys
        [Required(ErrorMessage = "Vui lòng chọn phim")]
        public int MovieId { get; set; }
        [ForeignKey("MovieId")]
        public virtual Movie? Movie { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng chiếu")]
        public int CinemaHallId { get; set; }
        [ForeignKey("CinemaHallId")]
        public virtual CinemaHall? CinemaHall { get; set; }

        // Quan hệ 1-N: Một suất chiếu sẽ có nhiều Vé (Tickets) hoặc đơn đặt (Bookings)
        // Đây là bước chuẩn bị cho tính năng Đặt Vé
        public virtual ICollection<OrderDetail>? OrderDetails { get; set; }


    }
}