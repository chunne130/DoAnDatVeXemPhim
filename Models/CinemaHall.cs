using DoAnDatVeXemPhim.Models;
using System.ComponentModel.DataAnnotations;

public class CinemaHall
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Tên Phòng")]
    public string Name { get; set; } // Ví dụ: Phòng 01

    [Display(Name = "Tổng số ghế")]
    public int TotalSeats { get; set; }

    // --- THÊM KHÓA NGOẠI ---
    [Display(Name = "Thuộc Cụm Rạp")]
    public int? CinemaId { get; set; }

    public Cinema? Cinema { get; set; }
    // -----------------------

    public ICollection<Showtime>? Showtimes { get; set; }

    public virtual ICollection<Seat>? Seats { get; set; }
}