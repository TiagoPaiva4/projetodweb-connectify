using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// representa o perfil de um utilizador na rede social.
    /// contém informações como tipo de perfil, biografia, foto de perfil e data de criação.
    /// </summary>  
    public class Profile
    {
        /// <summary>
        /// identificador único do perfil.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// identificador do utilizador associado ao perfil.
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// referência para o utilizador dono do perfil.
        /// </summary>
        public Users User { get; set; } = null!;

        /// <summary>
        /// tipo de perfil: pessoal ou público
        /// </summary>
        [Required, MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// biografia do utilizador, onde pode adicionar uma breve descrição sobre si.
        /// </summary>
        [MaxLength(1000)]
        public string? Bio { get; set; }

        /// <summary>
        /// url da foto de perfil do utilizador.
        /// </summary>
        public string? ProfilePicture { get; set; }

        /// <summary>
        /// data e hora de criação do perfil. 
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
