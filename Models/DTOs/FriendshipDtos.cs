using projetodweb_connectify.Models; // Para usar o enum FriendshipStatus
using System; // Necessário para DateTime

namespace projetodweb_connectify.Models.DTOs
{
    /// <summary>
    /// Representa os dados de uma amizade para serem enviados ao cliente (frontend).
    /// </summary>
    public class FriendshipDto
    {
        /// <summary>
        /// Identificador único da relação de amizade.
        /// </summary>
        public int FriendshipId { get; set; }

        /// <summary>
        /// ID do utilizador que é o amigo na perspetiva do utilizador atual.
        /// </summary>
        public int FriendUserId { get; set; }

        /// <summary>
        /// Nome de utilizador do amigo.
        /// </summary>
        public string FriendUsername { get; set; }

        /// <summary>
        /// URL da imagem de perfil do amigo.
        /// </summary>
        public string? FriendProfileImageUrl { get; set; }

        /// <summary>
        /// O estado atual da amizade (ex: 'Pendente', 'Aceite').
        /// </summary>
        public FriendshipStatus Status { get; set; }

        /// <summary>
        /// A data em que o pedido de amizade foi efetuado.
        /// </summary>
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
        /// O ID da amizade, se existir uma relação pendente ou aceite.
        /// </summary>
        public int? FriendshipId { get; set; }

        /// <summary>
        /// Uma mensagem descritiva sobre o estado da amizade, para ser apresentada ao utilizador.
        /// </summary>
        public string Message { get; set; }
    }
}