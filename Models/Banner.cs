using System;
using System.ComponentModel.DataAnnotations;

namespace DoAnDatVeXemPhim.Models
{
    public class Banner
    {
        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(200)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; }
        
        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }
        
        [Display(Name = "Đường dẫn (Link khi bấm vào)")]
        public string? TargetUrl { get; set; }
        
        [Display(Name = "Thời gian bắt đầu")]
        public DateTime StartDate { get; set; }
        
        [Display(Name = "Thời gian kết thúc")]
        public DateTime EndDate { get; set; }
        
        [Display(Name = "Kích hoạt (Hiển thị)")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0;
    }
}
