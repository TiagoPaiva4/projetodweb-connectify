using projetodweb_connectify.Models;
using System.Collections.Generic;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// Representa os dados exibidos no feed de um utilizador,
    /// incluindo publicações gerais, publicações de amigos, sugestões de amizades e tópicos recomendados.
    /// </summary>
    public class Feed
    {
        /// <summary>
        /// Lista de publicações públicas disponíveis para todos os utilizadores.
        /// </summary>
        public List<TopicPost> GeneralPosts { get; set; } = new List<TopicPost>();

        /// <summary>
        /// Lista de publicações feitas por amigos do utilizador autenticado.
        /// </summary>
        public List<TopicPost> FriendsPosts { get; set; } = new List<TopicPost>();

        /// <summary>
        /// ID do perfil do utilizador atualmente autenticado (se aplicável).
        /// </summary>
        public int? CurrentUserProfileId { get; set; }

        /// <summary>
        /// Indica se o utilizador está autenticado no sistema.
        /// </summary>
        public bool IsUserAuthenticated { get; set; }

        /// <summary>
        /// Indica se o utilizador tem amigos na plataforma.
        /// </summary>
        public bool UserHasFriends { get; set; }

        /// <summary>
        /// Lista de perfis sugeridos como potenciais amigos para o utilizador.
        /// </summary>
        public List<Profile> FriendshipSuggestions { get; set; } = new List<Profile>();

        /// <summary>
        /// Lista de tópicos recomendados com base nos interesses ou atividade do utilizador.
        /// </summary>
        public List<Topic> RecommendedTopics { get; set; } = new List<Topic>();
    }
}
