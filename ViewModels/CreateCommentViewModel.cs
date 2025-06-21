using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.ViewModels // Ensure this namespace matches your project
{
    public class CreateCommentViewModel
    {
        [Required]
        public int TopicPostId { get; set; }

        [Required(ErrorMessage = "O conteúdo do comentário é obrigatório.")]
        [StringLength(1000, ErrorMessage = "O comentário não pode exceder os 1000 caracteres.")]
        [Display(Name = "Seu comentário")]
        public string Content { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}