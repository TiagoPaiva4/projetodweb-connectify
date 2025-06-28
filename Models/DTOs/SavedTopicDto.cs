// In Models/DTOs/SavedTopicDto.cs
namespace projetodweb_connectify.Models.DTOs
{
    public class SavedTopicDto
    {
        // Detalhes do Tópico que foi guardado
        public int TopicId { get; set; }
        public string Title { get; set; }
        public string? TopicImageUrl { get; set; }

        // Data em que foi guardado
        public DateTime SavedAt { get; set; }
    }
}