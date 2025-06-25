using System.Collections.Generic;

namespace projetodweb_connectify.Models.DTOs
{
    // Reutilizando DTOs de posts e autores
    // Estes deveriam estar em TopicPostDtos.cs, mas colocamo-los aqui para referência
    // caso ainda não existam.
    /*
    public class AuthorProfileDto { ... }
    public class TopicPostDto { ... }
    */

    /// <summary>
    /// DTO para a lista de tópicos (visão resumida).
    /// </summary>
    public class TopicSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? TopicImageUrl { get; set; }
        public string CategoryName { get; set; }
        public string CreatorUsername { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PostCount { get; set; }
    }

    /// <summary>
    /// DTO para a visão detalhada de um tópico, incluindo seus posts.
    /// </summary>
    public class TopicDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string? TopicImageUrl { get; set; }
        public string CategoryName { get; set; }
        public AuthorProfileDto Creator { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsCurrentUserTheCreator { get; set; }
        public bool IsSavedByCurrentUser { get; set; }
        public List<TopicPostDto> Posts { get; set; }
    }

    /// <summary>
    /// DTO para criar um novo tópico.
    /// </summary>
    public class TopicCreateDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public bool IsPrivate { get; set; }
        public IFormFile? TopicImageFile { get; set; }
    }

    /// <summary>
    /// DTO para editar um tópico existente.
    /// </summary>
    public class TopicEditDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public bool IsPrivate { get; set; }
        public IFormFile? TopicImageFile { get; set; }
    }
}