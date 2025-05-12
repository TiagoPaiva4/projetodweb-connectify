using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        /// Indica se o tópico é o tópico pessoal do utilizador.
        /// </summary>
        public bool IsPersonal { get; set; } = false;

        /// <summary>
        /// Indica se o tópico é privado (apenas o utilizador e amigos podem ver).
        /// </summary>
        public bool IsPrivate { get; set; } = false;

        /// <summary>
        /// título do tópico.
        /// </summary>
        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// descrição do tópico.
        /// </summary>
        [MaxLength(2000)]
        public string? Description { get; set; }

        /// <summary>
        /// identificador do utilizador que criou o tópico.
        /// </summary>
        [Required]
        public int CreatedBy { get; set; }

        /// <summary>
        /// referência para o perfil do criador do tópico.
        /// </summary>
        [ForeignKey(nameof(CreatedBy))]
        public Profile? Creator { get; set; } 

        /// <summary>
        /// data e hora em que o tópico foi criado.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Lista de publicações associadas a este tópico.
        /// </summary>
        public ICollection<TopicPost> Posts { get; set; } = new List<TopicPost>();


        // --- NEW NAVIGATION PROPERTY for Savers ---
        /// <summary>
        /// Collection navigation property for the profiles that have saved this topic.
        /// </summary>
        public virtual ICollection<SavedTopic> Savers { get; set; } = new List<SavedTopic>(); // Renamed for clarity
    }
}
