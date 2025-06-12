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

        public int SenderId { get; set; } // ID do Users que enviou
        [ForeignKey("SenderId")]
        public virtual Users Sender { get; set; } = null!;

        // O RecipientId é quem recebe a mensagem dentro da conversa.
        // Útil para marcar como lida, etc.
        public int RecipientId { get; set; }
        [ForeignKey("RecipientId")]
        public virtual Users Recipient { get; set; } = null!;

        [Required]
        [MaxLength(2000)] // Limite o tamanho da mensagem
        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; } // Quando o Recipient leu a mensagem
    }
}