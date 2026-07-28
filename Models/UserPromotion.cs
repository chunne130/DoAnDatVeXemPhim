using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace DoAnDatVeXemPhim.Models
{
    public class UserPromotion
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        public virtual IdentityUser? User { get; set; }
        
        [Required]
        public int PromotionId { get; set; }
        public virtual Promotion? Promotion { get; set; }
        
        public bool IsUsed { get; set; } = false;
        
        public DateTime AcquiredDate { get; set; } = DateTime.Now;
        
        public DateTime? UsedDate { get; set; }
    }
}
