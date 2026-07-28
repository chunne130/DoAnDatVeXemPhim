using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace DoAnDatVeXemPhim.Models
{
    public class MovieFavorite
    {
        [Key] // Chốt Id làm khóa chính tự tăng
        public int Id { get; set; }

        // Liên kết tới người dùng (AspNetUsers)
        [Required]
        public string UserId { get; set; }

        // --- KÉO DÂY SANG ASPNETUSERS ---
        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }

        // Liên kết tới phim (Movies)
        [Required]
        public int MovieId { get; set; }

        [ForeignKey("MovieId")]
        public virtual Movie? Movie { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}