using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.Models
{
    public enum FriendshipStatus
    {
        Pending,   // Request sent, awaiting response
        Accepted,  // Friendship is active
        Rejected,  // Request was declined
        Blocked    // One user has blocked the other (optional, could be a separate system)
    }
    /// <summary>
    /// representa a relação de amizade entre dois utilizadores na rede social.
    /// contém os identificadores dos utilizadores e o estado da amizade.
    /// </summary>
    public class Friendship
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Id do utilizador que enviou o pedido de amizade.
        /// Corresponde a User1Id no seu modelo Users para FriendshipsInitiated.
        /// </summary>
        [Required]
        public int User1Id { get; set; } // The ID of the user who sent the request


        /// <summary>
        /// Utilizador que enviou o pedido de amizade.
        /// </summary>
        [ForeignKey("User1Id")]
        public virtual Users User1 { get; set; } = null!;

        /// <summary>
        /// Id do utilizador que recebeu o pedido de amizade.
        /// Corresponde a User2Id no seu modelo Users para FriendshipsReceived.
        /// </summary>
        [Required]
        public int User2Id { get; set; } // The ID of the user who received the request

        /// <summary>
        /// Utilizador que recebeu o pedido de amizade.
        /// </summary>
        [ForeignKey("User2Id")]
        public virtual Users User2 { get; set; } = null!;

        /// <summary>
        /// Estado atual da amizade (ex: Pendente, Aceite, Rejeitada).
        /// </summary>
        [Required]
        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

        /// <summary>
        /// Data e hora em que o pedido de amizade foi enviado.
        /// </summary>
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Data e hora em que o pedido de amizade foi aceite (nullable).
        /// </summary>
        public DateTime? AcceptanceDate { get; set; }
    }
}
