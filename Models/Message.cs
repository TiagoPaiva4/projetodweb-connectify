using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// representa uma mensagem enviada por um utilizador, 
    /// contendo o remetente, o conteúdo e os destinatários.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// identificador único da mensagem.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// identificador do utilizador que enviou a mensagem.
        /// </summary>
        [ForeignKey("Sender")]
        public int SenderId { get; set; }

        /// <summary>
        /// conteúdo da mensagem.
        /// </summary>
        [Required]
        public string Content { get; set; }

        /// <summary>
        /// data e hora de criação da mensagem.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// propriedade de navegação para o utilizador que enviou a mensagem.
        /// </summary>
        public virtual User Sender { get; set; }

        /// <summary>
        /// lista de destinatários da mensagem.
        /// </summary>
        public ICollection<MessageRecipient> Recipients { get; set; }
    }
}
