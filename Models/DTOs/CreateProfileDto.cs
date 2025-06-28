// In Models/DTOs/CreateProfileDto.cs
using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.Models.DTOs
{
    public class CreateProfileDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(50)]
        public string? Type { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }
    }
}