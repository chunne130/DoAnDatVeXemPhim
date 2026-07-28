using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace DoAnDatVeXemPhim.Models
{
    public class CustomerProfile
    {
        [Key]
        public int Id { get; set; }

        // Liên kết với bảng AspNetUsers mặc định
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }

        // Các thông tin muốn khách tự sửa
        [Display(Name = "Họ và Tên")]
        public string FullName { get; set; }

        [Display(Name = "Giới tính")]
        public string Gender { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }

        [Display(Name = "Thành phố")]
        public string City { get; set; }

        [Display(Name = "Quận/Huyện")]
        public string District { get; set; }

        [Display(Name = "Địa chỉ chi tiết")]
        public string Address { get; set; }

        // --- THÊM TRƯỜNG BỔ SUNG ĐỂ TÍNH HẠNG THÀNH VIÊN ---
        public decimal TotalSpent { get; set; } = 0; // Tích lũy tiền mỗi khi Order hoàn thành
        public int? MembershipLevelId { get; set; }
        public MembershipLevel MembershipLevel { get; set; } // Navigation property

        // --- ĐIỂM THƯỞNG (REWARD POINTS) ---
        [Display(Name = "Điểm thưởng")]
        public int RewardPoints { get; set; } = 0; // 10,000đ = 1 điểm
    }
}