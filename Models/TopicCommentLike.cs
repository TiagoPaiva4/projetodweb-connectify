using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// Representa um "gosto" dado por um perfil a um comentário de uma publicação.
    /// Esta é uma entidade de junção para a relação Muitos-para-Muitos entre Profile e TopicComment.
    /// </summary>
    public class TopicCommentLike
    {
        /// <summary>
        /// Chave estrangeira que referencia o comentário que recebeu o "gosto".
        /// Parte da chave primária composta.
        /// </summary>
        [Required]
        public int TopicCommentId { get; set; }
        /// <summary>
        /// Propriedade de navegação para o comentário associado.
        /// </summary>
        [ForeignKey(nameof(TopicCommentId))]
        public virtual TopicComment TopicComment { get; set; }

        /// <summary>
        /// Chave estrangeira que referencia o perfil que deu o "gosto".
        /// Parte da chave primária composta.
        /// </summary>
        [Required]
        public int ProfileId { get; set; }
        /// <summary>
        /// Propriedade de navegação para o perfil associado.
        /// </summary>
        [ForeignKey(nameof(ProfileId))]
        public virtual Profile Profile { get; set; }
    }
}