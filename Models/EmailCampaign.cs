using System;
using System.ComponentModel.DataAnnotations;

namespace DoAnDatVeXemPhim.Models
{
    public class EmailCampaign
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Subject { get; set; }
        
        [Required]
        public string BodyHtml { get; set; }
        
        [StringLength(100)]
        public string TargetAudience { get; set; } // Ví dụ: "All", "VIP", "HasBirthday"
        
        [StringLength(20)]
        public string Status { get; set; } = "PENDING"; // PENDING, SENT, CANCELLED
        
        public DateTime? ScheduledDate { get; set; }
        
        public DateTime? SentDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
