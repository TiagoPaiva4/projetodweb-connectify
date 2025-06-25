using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.Models.DTOs
{
    // DTO para criar uma categoria
    public class CategoryCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // O ficheiro da imagem enviado no formulário
        public IFormFile? CategoryImageFile { get; set; }
    }

    // DTO para editar uma categoria
    public class CategoryEditDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // A nova imagem (opcional)
        public IFormFile? CategoryImageFile { get; set; }
    }
}