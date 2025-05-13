using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// representa uma publicação dentro de um tópico de discussão na rede social.
    /// contém informações sobre o autor, conteúdo, data de criação e comentários associados.
    /// </summary>
    public class TopicPost
    {
        /// <summary>
        /// identificador único da publicação dentro do tópico.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// identificador do tópico ao qual esta publicação pertence.
        /// </summary>
        [Required]
        public int TopicId { get; set; }

        /// <summary>
        /// referência para o tópico associado a esta publicação.
        /// </summary>
        [ForeignKey(nameof(TopicId))]
        public Topic? Topic { get; set; } 

        /// <summary>
        /// identificador do perfil que criou a publicação.
        /// </summary>
        [Required]
        public int ProfileId { get; set; }

        /// <summary>
        /// referência para o perfil do autor da publicação.
        /// </summary>
        [ForeignKey(nameof(ProfileId))]
        public Profile? Profile { get; set; }

        /// <summary>
        /// conteúdo da publicação.
        /// </summary>
        [Required]
        [MaxLength(3000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// URL ou caminho da imagem associada à publicação, caso exista.
        /// </summary>
        [Display(Name = "Imagem da Publicação")]
        public string? PostImageUrl { get; set; } // Anulável, pois a imagem é opcional

        /// <summary>
        /// data e hora em que a publicação foi criada.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// lista de comentários associados a esta publicação.
        /// </summary>
        public ICollection<TopicComment> Comments { get; set; } = new List<TopicComment>();
    }
}
