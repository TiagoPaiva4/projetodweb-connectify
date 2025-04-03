using static System.Runtime.InteropServices.JavaScript.JSType;
using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// representa uma biblioteca digital associada a um perfil na rede social.
    /// permite armazenar e partilhar recursos digitais como artigos, links e documentos.
    /// </summary>
    public class DigitalLibrary
    {
        /// <summary>
        /// identificador único do recurso na biblioteca digital.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// identificador do perfil que adicionou o recurso.
        /// </summary>
        [Required]
        public int ProfileId { get; set; }

        /// <summary>
        /// referência para o perfil do utilizador que adicionou o recurso.
        /// </summary>
        public Profile Profile { get; set; }

        /// <summary>
        /// título do recurso digital.
        /// </summary>
        [Required, MaxLength(255)]
        public string Title { get; set; }

        /// <summary>
        /// descrição opcional do recurso digital.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// link para aceder ao recurso digital.
        /// </summary>
        public string ResourceLink { get; set; }

        /// <summary>
        /// data e hora em que o recurso foi adicionado à biblioteca.
        /// </summary>
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
