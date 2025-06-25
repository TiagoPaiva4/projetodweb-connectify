using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.Models.DTOs
{
    /// <summary>
    /// DTO para uma visão resumida de um perfil, usado em listas de pesquisa.
    /// </summary>
    public class ProfileSummaryDto
    {
        public int ProfileId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

    /// <summary>
    /// DTO detalhado para o perfil do utilizador LOGADO (/api/profiles/me).
    /// Inclui dados privados como tópicos salvos.
    /// </summary>
    public class MyProfileDetailDto
    {
        public int ProfileId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; }
        public string Type { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<FriendshipDto> Friends { get; set; } = new List<FriendshipDto>();
        public List<TopicSummaryDto> SavedTopics { get; set; } = new List<TopicSummaryDto>();
        public List<TopicSummaryDto> CreatedTopics { get; set; } = new List<TopicSummaryDto>();
        public TopicDetailDto? PersonalTopic { get; set; } // O tópico pessoal com seus posts
    }

    /// <summary>
    /// DTO detalhado para o perfil de OUTRO utilizador.
    /// Não inclui dados privados.
    /// </summary>
    public class UserProfileDetailDto
    {
        public int ProfileId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; }
        public string Type { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public FriendshipStatusDto FriendshipStatus { get; set; } // Status da amizade com o utilizador logado
        public List<TopicSummaryDto> CreatedTopics { get; set; } = new List<TopicSummaryDto>();
    }

    /// <summary>
    /// DTO para editar o perfil do utilizador.
    /// </summary>
    public class ProfileEditDto
    {
        [StringLength(100)]
        public string Name { get; set; }
        [StringLength(500)]
        public string Bio { get; set; }
        public string Type { get; set; }
        public IFormFile? ProfilePictureFile { get; set; }
    }
}