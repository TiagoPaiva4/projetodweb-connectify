using System; // Necessário para DateTime

namespace projetodweb_connectify.Models.DTOs
{
    /// <summary>
    /// DTO (Data Transfer Object) para representar um tópico que foi guardado por um utilizador.
    /// </summary>
    public class SavedTopicDto
    {
        /// <summary>
        /// O identificador único do tópico que foi guardado.
        /// </summary>
        public int TopicId { get; set; }

        /// <summary>
        /// O título do tópico guardado.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// O URL da imagem de capa do tópico guardado.
        /// </summary>
        public string? TopicImageUrl { get; set; }

        /// <summary>
        /// A data e hora em que o tópico foi guardado pelo utilizador.
        /// </summary>
        public DateTime SavedAt { get; set; }
    }
}