using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// representa a relação de um tópico guardado por um perfil.
    /// permite saber que utilizador guardou que tópico e quando.
    /// </summary>
    public class SavedTopic
    {
        /// <summary>
        /// identificador do perfil que guardou o tópico.
        /// chave estrangeira para Profile.
        /// </summary>
        [Required]
        public int ProfileId { get; set; } // FK to Profile

        /// <summary>
        /// perfil que guardou o tópico.
        /// </summary>
        [ForeignKey(nameof(ProfileId))]
        public Profile SaverProfile { get; set; } = null!;

        /// <summary>
        /// identificador do tópico guardado.
        /// chave estrangeira para Topic.
        /// </summary>
        [Required]
        public int TopicId { get; set; } // FK to Topic

        /// <summary>
        /// tópico que foi guardado.
        /// </summary>
        [ForeignKey(nameof(TopicId))]
        public Topic Topic { get; set; } = null!;

        /// <summary>
        /// data e hora em que o tópico foi guardado pelo perfil.
        /// </summary>
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }
}
