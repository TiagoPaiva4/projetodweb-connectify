// Certifique-se que o namespace está correto para o seu projeto
namespace projetodweb_connectify.Models.DTOs
{
    using Microsoft.AspNetCore.Http; // Necessário para IFormFile
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

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
        // O TopicDetailDto e outros DTOs relacionados (FriendshipDto, etc.) também precisam estar definidos
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

    // --- CLASSE EM FALTA ADICIONADA AQUI ---
    /// <summary>
    /// DTO para a criação de um novo perfil.
    /// Contém apenas os campos que o utilizador pode preencher no formulário.
    /// </summary>
    public class ProfileCreateDto
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "O tipo de perfil é obrigatório.")]
        public string Type { get; set; }

        [StringLength(500, ErrorMessage = "A biografia não pode exceder 500 caracteres.")]
        public string? Bio { get; set; }

        // O ficheiro da imagem de perfil é opcional na criação
        public IFormFile? ProfilePictureFile { get; set; }
    }
    // --- FIM DA CLASSE ADICIONADA ---


    /// <summary>
    /// DTO para editar o perfil do utilizador.
    /// </summary>
    public class ProfileEditDto
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "A biografia não pode exceder 500 caracteres.")]
        public string Bio { get; set; }

        [Required(ErrorMessage = "O tipo de perfil é obrigatório.")]
        public string Type { get; set; }

        public IFormFile? ProfilePictureFile { get; set; }
    }

    // Nota: Certifique-se que FriendshipDto, TopicSummaryDto, FriendshipStatusDto etc.
    // também estão definidos ou no mesmo ficheiro ou noutro ficheiro DTO.
}