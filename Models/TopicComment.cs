using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// representa um comentário feito numa publicação dentro de um tópico.
    /// contém informações sobre o utilizador, a publicação à qual pertence e o conteúdo do comentário.
    /// </summary>
    public class TopicComment
    {
        /// <summary>
        /// identificador único do comentário.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// identificador da publicação à qual este comentário pertence.
        /// </summary>
        [Required]
        public int TopicPostId { get; set; }

        /// <summary>
        /// referência para a publicação associada a este comentário.
        /// </summary>
        public TopicPost TopicPost { get; set; }

        /// <summary>
        /// identificador do perfil que criou o comentário.
        /// </summary>
        [Required]
        public int ProfileId { get; set; }

        /// <summary>
        /// referência para o perfil do autor do comentário.
        /// </summary>
        public Profile Profile { get; set; }

        /// <summary>
        /// Conteúdo do comentário (apenas em texto).
        /// </summary>
        [Required]
        public string Content { get; set; }

        /// <summary>
        /// data e hora em que o comentário foi criado.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
