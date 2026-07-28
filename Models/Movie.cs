using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnDatVeXemPhim.Models
{
    public class Movie
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [StringLength(250)]
        [Display(Name = "Tên phim")]
        public string Title { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Link Poster")]
        public string? PosterUrl { get; set; }

        [Display(Name = "Thời lượng (phút)")]
        public int Duration { get; set; }

        // THÊM DẤU ? ĐỂ CHỐNG LỖI KHI DỮ LIỆU TRỐNG
        [Display(Name = "Ngày khởi chiếu")]
        [DataType(DataType.Date)]
        public DateTime? ReleaseDate { get; set; }

        [Display(Name = "Link Trailer (Youtube)")]
        public string? TrailerUrl { get; set; }

        // Thêm trường Genre (chuỗi) nếu muốn hiện tên thể loại nhanh ở View
        [Display(Name = "Thể loại")]
        public string? GenreName { get; set; }

        // Liên kết với bảng Genre (nếu có bảng riêng)
        [Display(Name = "Mã thể loại")]
        public int GenreId { get; set; }

        [ForeignKey("GenreId")]
        public virtual Genre? Genre { get; set; }

        //thêm trường AgeRestriction để lưu trữ độ tuổi giới hạn của phim
        [Required(ErrorMessage = "Vui lòng chọn giới hạn độ tuổi cho phim!")]
        [Display(Name = "Giới hạn độ tuổi")]
        public string AgeRestriction { get; set; }

        // Quan hệ 1-N: Một bộ phim sẽ có danh sách nhiều suất chiếu (Showtimes)
        public virtual ICollection<Showtime>? Showtimes { get; set; }

        public virtual ICollection<MovieReview>? MovieReviews { get; set; }

        // --- ĐÃ THÊM: Tính năng Behavior Tracking ---
        [Display(Name = "Lượt xem")]
        public int ViewCount { get; set; } = 0;
    }
}