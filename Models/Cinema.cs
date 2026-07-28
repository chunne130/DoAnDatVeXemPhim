using System.ComponentModel.DataAnnotations;

namespace DoAnDatVeXemPhim.Models
{
    public class Cinema
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên rạp không được để trống")]
        [Display(Name = "Tên Cụm Rạp")]
        public string Name { get; set; } // Ví dụ: Cinema Hub Sư Vạn Hạnh

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Display(Name = "Thành phố")]
        public string City { get; set; } = "Hồ Chí Minh";

        [Display(Name = "Quận/Huyện")]
        public string? District { get; set; }

        [Display(Name = "Hotline")]
        public string? Hotline { get; set; } = "1900 2026";

        [Display(Name = "Giờ hoạt động")]
        public string? OperatingHours { get; set; } = "08:30 - 23:00";

        // Mối quan hệ: 1 Rạp có nhiều Phòng chiếu
        public ICollection<CinemaHall>? CinemaHalls { get; set; }
    }
}