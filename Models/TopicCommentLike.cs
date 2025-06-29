using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// Representa um "gosto" dado por um perfil a um coment�rio de uma publica��o.
    /// Esta � uma entidade de jun��o para a rela��o Muitos-para-Muitos entre Profile e TopicComment.
    /// </summary>
    public class TopicCommentLike
    {
        /// <summary>
        /// Chave estrangeira que referencia o coment�rio que recebeu o "gosto".
        /// Parte da chave prim�ria composta.
        /// </summary>
        [Required]
        public int TopicCommentId { get; set; }
        /// <summary>
        /// Propriedade de navega��o para o coment�rio associado.
        /// </summary>
        [ForeignKey(nameof(TopicCommentId))]
        public virtual TopicComment TopicComment { get; set; }

        /// <summary>
        /// Chave estrangeira que referencia o perfil que deu o "gosto".
        /// Parte da chave prim�ria composta.
        /// </summary>
        [Required]
        public int ProfileId { get; set; }
        /// <summary>
        /// Propriedade de navega��o para o perfil associado.
        /// </summary>
        [ForeignKey(nameof(ProfileId))]
        public virtual Profile Profile { get; set; }
    }
}