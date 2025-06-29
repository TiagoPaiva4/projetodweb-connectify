using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Necessário para IFormFile
using System.Collections.Generic; // Necessário para List<>
using System; // Necessário para DateTime

namespace projetodweb_connectify.Models.DTOs
{
    /// <summary>
    /// DTO (Data Transfer Object) principal para representar os dados completos de um perfil.
    /// </summary>
    public class ProfileDto
    {
        /// <summary>
        /// Identificador único do perfil.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identificador do utilizador associado a este perfil.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Nome de exibição do perfil.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Tipo de perfil (ex: "Pessoal", "Empresa").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Biografia ou descrição do perfil.
        /// </summary>
        public string Bio { get; set; }

        /// <summary>
        /// URL da imagem de perfil.
        /// </summary>
        public string ProfilePicture { get; set; }

        /// <summary>
        /// Nome de utilizador para login.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Data de criação do perfil.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Lista das publicações feitas no tópico pessoal do utilizador.
        /// </summary>
        public List<TopicPostDto> PersonalTopicPosts { get; set; } = new List<TopicPostDto>();

        /// <summary>
        /// ID do tópico pessoal associado a este perfil.
        /// </summary>
        public int? PersonalTopicId { get; set; }

        /// <summary>
        /// Lista dos tópicos públicos criados por este utilizador.
        /// </summary>
        public List<TopicSummaryDto> CreatedTopics { get; set; } = new List<TopicSummaryDto>();

        /// <summary>
        /// Lista dos tópicos que este utilizador guardou.
        /// </summary>
        public List<SavedTopicDto> SavedTopics { get; set; } = new List<SavedTopicDto>();

        /// <summary>
        /// Estado da amizade em relação ao utilizador autenticado (ex: "amigos", "pendente").
        /// </summary>
        public string FriendshipStatus { get; set; }

        /// <summary>
        /// Número total de amigos que o utilizador tem.
        /// </summary>
        public int FriendsCount { get; set; }
    }

    /// <summary>
    /// DTO leve para representar um perfil numa lista de utilizadores.
    /// </summary>
    public class ProfileSummaryDto
    {
        /// <summary>
        /// Nome de utilizador para login.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Nome de exibição do perfil.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// URL da imagem de perfil.
        /// </summary>
        public string? ProfilePicture { get; set; }

        /// <summary>
        /// Biografia ou descrição do perfil.
        /// </summary>
        public string? Bio { get; set; }

        /// <summary>
        /// Tipo de perfil (ex: "Pessoal", "Empresa").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Objeto que detalha o estado da amizade com o utilizador autenticado.
        /// </summary>
        public FriendshipStatusDto FriendshipStatus { get; set; }
    }

    /// <summary>
    /// DTO para receber os dados ao atualizar um perfil.
    /// </summary>
    public class ProfileUpdateDto
    {
        /// <summary>
        /// O novo nome de exibição para o perfil.
        /// </summary>
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// A nova biografia para o perfil (opcional).
        /// </summary>
        [MaxLength(1000)]
        public string? Bio { get; set; }

        /// <summary>
        /// O novo ficheiro de imagem de perfil, enviado através do formulário.
        /// </summary>
        public IFormFile? ProfilePictureFile { get; set; }
    }
}