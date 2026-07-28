using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoAnDatVeXemPhim.Models
{
    public class Genre
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên thể loại không được để trống")]
        [Display(Name = "Tên thể loại")]
        [StringLength(100)]
        public string Name { get; set; }

        public virtual ICollection<Movie>? Movies { get; set; }
    }
}