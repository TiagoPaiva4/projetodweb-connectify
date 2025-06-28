// In Models/DTOs/UpdateProfileDto.cs
using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.Models.DTOs
{
    public class UpdateProfileDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(50)]
        public string? Type { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        // A imagem será tratada separadamente como IFormFile
    }
}