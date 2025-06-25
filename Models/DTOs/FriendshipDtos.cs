using projetodweb_connectify.Models; // Para usar o enum FriendshipStatus

namespace projetodweb_connectify.Models.DTOs
{
    /// <summary>
    /// Representa os dados de uma amizade para serem enviados ao cliente (frontend).
    /// </summary>
    public class FriendshipDto
    {
        public int FriendshipId { get; set; }
        public int FriendUserId { get; set; }
        public string FriendUsername { get; set; }
        public string? FriendProfileImageUrl { get; set; }
        public FriendshipStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
    }

    /// <summary>
    /// Representa o estado da relação entre o utilizador atual e outro utilizador.
    /// </summary>
    public class FriendshipStatusDto
    {
        /// <summary>
        /// O estado da relação: "self", "friends", "pending_sent", "pending_received", "not_friends".
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// O ID da amizade, se existir.
        /// </summary>
        public int? FriendshipId { get; set; }

        public string Message { get; set; }
    }
}