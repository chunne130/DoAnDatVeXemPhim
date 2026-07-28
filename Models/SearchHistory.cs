using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnDatVeXemPhim.Models
{
    public class SearchHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(250)]
        public string Keyword { get; set; }

        public DateTime SearchDate { get; set; } = DateTime.Now;

        // Lưu UserId nếu đã đăng nhập, ngược lại null cho khách vãng lai
        public string? UserId { get; set; }
    }
}
