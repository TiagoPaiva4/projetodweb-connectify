using System;
using Microsoft.AspNetCore.Http; // Necessário para IFormFile

namespace projetodweb_connectify.Models.DTOs
{
    /// <summary>
    /// DTO para representar um Perfil de autor de forma simplificada.
    /// </summary>
    public class AuthorProfileDto
    {
        /// <summary>
        /// Identificador único do perfil do autor.
        /// </summary>
        public int ProfileId { get; set; }

        /// <summary>
        /// Nome de utilizador do autor.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// URL da imagem de perfil do autor.
        /// </summary>
        public string? ProfileImageUrl { get; set; }
    }

    /// <summary>
    /// DTO para representar um TopicPost ao ser enviado para o cliente.
    /// </summary>
    public class TopicPostDto
    {
        /// <summary>
        /// Identificador único da publicação.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ID do tópico ao qual esta publicação pertence.
        /// </summary>
        public int TopicId { get; set; }

        /// <summary>
        /// Conteúdo textual da publicação.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// URL da imagem associada à publicação, se existir.
        /// </summary>
        public string? PostImageUrl { get; set; }

        /// <summary>
        /// Data e hora em que a publicação foi criada.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Número total de 'gostos' que a publicação recebeu.
        /// </summary>
        public int LikesCount { get; set; }

        /// <summary>
        /// Indica se a publicação foi 'gostada' pelo utilizador autenticado.
        /// </summary>
        public bool IsLikedByCurrentUser { get; set; }

        /// <summary>
        /// Objeto com os dados do perfil do autor da publicação.
        /// </summary>
        public AuthorProfileDto Author { get; set; }
    }

    /// <summary>
    /// DTO para receber os dados ao criar uma nova publicação.
    /// </summary>
    public class TopicPostCreateDto
    {
        /// <summary>
        /// Conteúdo textual da nova publicação.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// ID do tópico onde a nova publicação será criada.
        /// </summary>
        public int TopicId { get; set; }

        /// <summary>
        /// Ficheiro de imagem para a nova publicação, enviado através do formulário. É opcional.
        /// </summary>
        public IFormFile? PostImageFile { get; set; }
    }

    /// <summary>
    /// DTO para receber os dados ao editar uma publicação existente.
    /// </summary>
    public class TopicPostEditDto
    {
        /// <summary>
        /// O novo conteúdo textual para a publicação a ser editada.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Um novo ficheiro de imagem para substituir o existente. É opcional.
        /// </summary>
        public IFormFile? PostImageFile { get; set; }
    }
}