using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.Models.DTOs
{
    /// <summary>
    /// DTO para a lista de utilizadores e para detalhes (visão do admin).
    /// NUNCA expõe o PasswordHash.
    /// </summary>
    public class UserAdminViewDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ProfileName { get; set; } // Opcional: nome do perfil associado
    }

    /// <summary>
    /// DTO para criar um novo utilizador. Recebe uma password em texto plano.
    /// </summary>
    public class UserCreateDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }
    }

    /// <summary>
    /// DTO para editar um utilizador. A password é opcional.
    /// </summary>
    public class UserEditDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        // Se a password for fornecida, ela será atualizada. Se for nula, será ignorada.
        [StringLength(100, MinimumLength = 6)]
        public string? Password { get; set; }
    }
}