using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.ViewModels
{
    /// <summary>
    /// ViewModel que representa os dados necessários para criar um novo comentário
    /// através de um formulário.
    /// </summary>
    public class CreateCommentViewModel
    {
        /// <summary>
        /// O identificador da publicação à qual este comentário será associado.
        /// </summary>
        [Required]
        public int TopicPostId { get; set; }

        /// <summary>
        /// O conteúdo textual do comentário.
        /// </summary>
        [Required(ErrorMessage = "O conteúdo do comentário é obrigatório.")]
        [StringLength(1000, ErrorMessage = "O comentário não pode exceder os 1000 caracteres.")]
        [Display(Name = "Seu comentário")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// O URL para o qual o utilizador será redirecionado após a submissão do formulário.
        /// </summary>
        public string? ReturnUrl { get; aget; }
    }
}