using System.ComponentModel.DataAnnotations.Schema;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// representa a relação entre uma mensagem e o seu destinatário, 
    /// permitindo verificar quando a mensagem foi lida.
    /// </summary>
    public class MessageRecipient
    {
        /// <summary>
        /// identificador da mensagem.
        /// </summary>
        public int MessageId { get; set; }

        /// <summary>
        /// identificador do utilizador destinatário.
        /// </summary>
        public int RecipientId { get; set; }

        /// <summary>
        /// data e hora que o destinatário leu a mensagem (NULL se não lida).
        /// </summary>
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// propriedade de navegação para a mensagem.
        /// </summary>
        [ForeignKey(nameof(MessageId))]
        public virtual Message Message { get; set; } = null!;

        /// <summary>
        /// propriedade de navegação para o utilizador destinatário.
        /// </summary>
        [ForeignKey(nameof(RecipientId))]
        public virtual Users Recipient { get; set; } = null!;
    }
}
