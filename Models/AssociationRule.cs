using System;
using System.ComponentModel.DataAnnotations;

namespace DoAnDatVeXemPhim.Models
{
    public class AssociationRule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string RuleType { get; set; } // "Combo", "Movie", etc.

        [Required]
        public string Antecedent { get; set; } // Comma-separated IDs (e.g., "1,2")

        [Required]
        public string Consequent { get; set; } // Comma-separated IDs or single ID (e.g., "3")

        public double Support { get; set; }

        public double Confidence { get; set; }

        public double Lift { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
