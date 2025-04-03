using System.ComponentModel.DataAnnotations;

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
        public Topic Topic { get; set; }

        /// <summary>
        /// identificador do perfil que criou a publicação.
        /// </summary>
        [Required]
        public int ProfileId { get; set; }

        /// <summary>
        /// referência para o perfil do autor da publicação.
        /// </summary>
        public Profile Profile { get; set; }

        /// <summary>
        /// conteúdo da publicação.
        /// </summary>
        [Required]
        public string Content { get; set; }

        /// <summary>
        /// data e hora em que a publicação foi criada.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// lista de comentários associados a esta publicação.
        /// </summary>
        public ICollection<TopicComment> Comments { get; set; }
    }
}
