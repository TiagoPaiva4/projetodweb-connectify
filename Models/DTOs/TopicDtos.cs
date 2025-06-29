using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http; // Necessário para IFormFile

namespace projetodweb_connectify.Models.DTOs
{

    /// <summary>
    /// DTO para a lista de tópicos (visão resumida).
    /// </summary>
    public class TopicSummaryDto
    {
        /// <summary>
        /// Identificador único do tópico.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Título do tópico.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// URL da imagem de capa do tópico.
        /// </summary>
        public string? TopicImageUrl { get; set; }

        /// <summary>
        /// Nome da categoria a que o tópico pertence.
        /// </summary>
        public string CategoryName { get; set; }

        /// <summary>
        /// Nome de utilizador do criador do tópico.
        /// </summary>
        public string CreatorUsername { get; set; }

        /// <summary>
        /// Data de criação do tópico.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Número total de publicações dentro deste tópico.
        /// </summary>
        public int PostCount { get; set; }
    }

    /// <summary>
    /// DTO para a visão detalhada de um tópico, incluindo as suas publicações.
    /// </summary>
    public class TopicDetailDto
    {
        /// <summary>
        /// Identificador único do tópico.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Título do tópico.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Descrição detalhada do tópico.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// URL da imagem de capa do tópico.
        /// </summary>
        public string? TopicImageUrl { get; set; }

        /// <summary>
        /// Nome da categoria a que o tópico pertence.
        /// </summary>
        public string CategoryName { get; set; }

        /// <summary>
        /// Objeto com os dados do perfil do criador do tópico.
        /// </summary>
        public AuthorProfileDto Creator { get; set; }

        /// <summary>
        /// Data de criação do tópico.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Indica se o utilizador autenticado é o criador do tópico.
        /// </summary>
        public bool IsCurrentUserTheCreator { get; set; }

        /// <summary>
        /// Indica se o tópico está na lista de guardados do utilizador autenticado.
        /// </summary>
        public bool IsSavedByCurrentUser { get; set; }

        /// <summary>
        /// Lista de publicações (DTOs) que pertencem a este tópico.
        /// </summary>
        public List<TopicPostDto> Posts { get; set; }
    }

    /// <summary>
    /// DTO para criar um novo tópico.
    /// </summary>
    public class TopicCreateDto
    {
        /// <summary>
        /// Título para o novo tópico.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Descrição para o novo tópico.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// ID da categoria à qual o novo tópico pertencerá.
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// Define se o novo tópico será privado (acessível apenas por convite/ligação, por exemplo).
        /// </summary>
        public bool IsPrivate { get; set; }

        /// <summary>
        /// Ficheiro de imagem para o novo tópico, enviado através do formulário. É opcional.
        /// </summary>
        public IFormFile? TopicImageFile { get; set; }
    }

    /// <summary>
    /// DTO para editar um tópico existente.
    /// </summary>
    public class TopicEditDto
    {
        /// <summary>
        /// O novo título para o tópico.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// A nova descrição para o tópico.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// O novo ID da categoria para o tópico.
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// O novo estado de privacidade para o tópico.
        /// </summary>
        public bool IsPrivate { get; set; }

        /// <summary>
        /// Um novo ficheiro de imagem para substituir o existente. É opcional.
        /// </summary>
        public IFormFile? TopicImageFile { get; set; }
    }
}