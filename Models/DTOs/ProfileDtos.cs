// In Models/DTOs/ProfileDto.cs
namespace projetodweb_connectify.Models.DTOs
{
   
    // O DTO principal para o perfil
    public class ProfileDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Bio { get; set; }
        public string ProfilePicture { get; set; }
        public string Username { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<TopicPostDto> PersonalTopicPosts { get; set; } = new List<TopicPostDto>();

        public List<TopicSummaryDto> CreatedTopics { get; set; } = new List<TopicSummaryDto>();

        public List<SavedTopicDto> SavedTopics { get; set; } = new List<SavedTopicDto>();

        public FriendshipStatusDto FriendshipStatus { get; set; }
    }

    /// <summary>
    /// DTO leve para representar um perfil numa lista de utilizadores.
    /// </summary>
    public class ProfileSummaryDto
    {
        public string Username { get; set; }
        public string Name { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Bio { get; set; }
        public string Type { get; set; }

        public FriendshipStatusDto FriendshipStatus { get; set; }
    }
}