namespace projetodweb_connectify.Models.DTOs
{
    /// <summary>
    /// Representa o resultado de uma operação de "toggle like".
    /// </summary>
    public class LikeToggleResultDto
    {
        /// <summary>
        /// A nova contagem total de "likes" para o item.
        /// </summary>
        public int NewLikeCount { get; set; }

        /// <summary>
        /// Indica se o item está agora "gostado" pelo utilizador após a operação.
        /// 'true' se o utilizador gostou, 'false' se o utilizador removeu o gosto.
        /// </summary>
        public bool WasLiked { get; set; }
    }
}