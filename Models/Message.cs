using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// Representa uma única mensagem trocada dentro de uma conversa.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Identificador único da mensagem.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Chave estrangeira para a conversa à qual esta mensagem pertence.
        /// </summary>
        public int ConversationId { get; set; }
        /// <summary>
        /// Propriedade de navegação para a conversa associada.
        /// </summary>
        [ForeignKey("ConversationId")]
        public virtual Conversation Conversation { get; set; } = null!;

        /// <summary>
        /// Chave estrangeira para o utilizador que enviou a mensagem (remetente).
        /// </summary>
        public int SenderId { get; set; }
        /// <summary>
        /// Propriedade de navegação para o utilizador remetente.
        /// </summary>
        [ForeignKey("SenderId")]
        public virtual Users Sender { get; set; } = null!;

        /// <summary>
        /// Chave estrangeira para o utilizador que recebeu a mensagem (destinatário).
        /// </summary>
        public int RecipientId { get; set; }
        /// <summary>
        /// Propriedade de navegação para o utilizador destinatário.
        /// </summary>
        [ForeignKey("RecipientId")]
        public virtual Users Recipient { get; set; } = null!;

        /// <summary>
        /// O conteúdo textual da mensagem.
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Data e hora em que a mensagem foi enviada.
        /// </summary>
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Data e hora em que a mensagem foi lida pelo destinatário. É nulo se ainda não foi lida.
        /// </summary>
        public DateTime? ReadAt { get; set; }
    }
}