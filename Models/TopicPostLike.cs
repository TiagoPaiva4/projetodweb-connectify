using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// Representa um "gosto" dado por um perfil a uma publica��o de um t�pico.
    /// Esta � uma entidade de jun��o para a rela��o Muitos-para-Muitos entre Profile e TopicPost.
    /// </summary>
    public class TopicPostLike
    {
        /// <summary>
        /// Chave estrangeira que referencia a publica��o que recebeu o "gosto".
        /// Parte da chave prim�ria composta.
        /// </summary>
        [Required]
        public int TopicPostId { get; set; }
        /// <summary>
        /// Propriedade de navega��o para a publica��o associada.
        /// </summary>
        [ForeignKey(nameof(TopicPostId))]
        public virtual TopicPost TopicPost { get; set; }

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