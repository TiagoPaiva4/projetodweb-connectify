using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public int SenderId { get; set; }

        /// <summary>
        /// propriedade de navegação para o utilizador que enviou a mensagem.
        /// </summary>
        [ForeignKey(nameof(SenderId))]
        public virtual Users Sender { get; set; } = null!;

        /// <summary>
        /// conteúdo da mensagem.
        /// </summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// data e hora de criação da mensagem.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// lista de destinatários da mensagem.
        /// </summary>
        public virtual ICollection<MessageRecipient> Recipients { get; set; } = new List<MessageRecipient>();
    }
}
