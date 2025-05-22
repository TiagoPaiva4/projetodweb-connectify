using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        public int ConversationId { get; set; }
        [ForeignKey("ConversationId")]
        public virtual Conversation Conversation { get; set; } = null!;

        public int SenderId { get; set; } // ID do utilizador que enviou
        [ForeignKey("SenderId")]
        public virtual Users Sender { get; set; } = null!;

        // O RecipientId é útil para saber para quem a mensagem é destinada dentro da conversa,
        // especialmente para marcar como lida.
        public int RecipientId { get; set; }
        [ForeignKey("RecipientId")]
        public virtual Users Recipient { get; set; } = null!;


        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; } // Nullable, data em que o destinatário leu
    }
}