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
        [ForeignKey("Message")]
        public int MessageId { get; set; }

        /// <summary>
        /// identificador do utilizador destinatário.
        /// </summary>
        [ForeignKey("Recipient")]
        public int RecipientId { get; set; }

        /// <summary>
        /// data e hora que o destinatário leu a mensagem (NULL se não lida).
        /// </summary>
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// propriedade de navegação para a mensagem.
        /// </summary>
        public virtual Message Message { get; set; }

        /// <summary>
        /// propriedade de navegação para o utilizador destinatário.
        /// </summary>
        public virtual User Recipient { get; set; }
    }
}
