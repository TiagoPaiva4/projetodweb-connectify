using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// representa um tópico de discussão na rede social.
    /// contém informações como título, descrição, criador e data de criação.
    /// </summary>
    public class Topic
    {
        /// <summary>
        /// identificador único do tópico.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// título do tópico.
        /// </summary>
        [Required, MaxLength(255)]
        public string Title { get; set; }

        /// <summary>
        /// descrição do tópico.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// identificador do utilizador que criou o tópico.
        /// </summary>
        [Required]
        public int CreatedBy { get; set; }

        /// <summary>
        /// referência para o perfil do criador do tópico.
        /// </summary>
        public Profile Creator { get; set; }

        /// <summary>
        /// data e hora em que o tópico foi criado.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
