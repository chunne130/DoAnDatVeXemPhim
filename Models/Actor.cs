using System.ComponentModel.DataAnnotations;

namespace DoAnDatVeXemPhim.Models
{
    public class Actor
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên diễn viên không được để trống")]
        [StringLength(100)]
        [Display(Name = "Tên diễn viên")]
        public string Name { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string? ProfilePictureUrl { get; set; }

        // Many-to-Many relationship with Movie
        public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();
    }
}
