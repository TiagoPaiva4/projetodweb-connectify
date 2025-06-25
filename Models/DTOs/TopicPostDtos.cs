namespace projetodweb_connectify.Models.DTOs
{
    /// <summary>
    /// DTO para representar um Perfil de autor de forma simplificada.
    /// </summary>
    public class AuthorProfileDto
    {
        public int ProfileId { get; set; }
        public string Username { get; set; }
        public string? ProfileImageUrl { get; set; }
    }

    /// <summary>
    /// DTO para representar um TopicPost ao ser enviado para o cliente.
    /// </summary>
    public class TopicPostDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string? PostImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public int LikesCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; } // Útil para o frontend
        public AuthorProfileDto Author { get; set; }
    }

    /// <summary>
    /// DTO para receber os dados ao criar um novo post.
    /// </summary>
    public class TopicPostCreateDto
    {
        public string Content { get; set; }
        public int TopicId { get; set; }
        public IFormFile? PostImageFile { get; set; }
    }

    /// <summary>
    /// DTO para receber os dados ao editar um post existente.
    /// </summary>
    public class TopicPostEditDto
    {
        public string Content { get; set; }
        public IFormFile? PostImageFile { get; set; }
    }
}