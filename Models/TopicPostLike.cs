using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// Representa um "gosto" dado por um perfil a uma publicação de um tópico.
    /// Esta é uma entidade de junção para a relação Muitos-para-Muitos entre Profile e TopicPost.
    /// </summary>
    public class TopicPostLike
    {
        /// <summary>
        /// Chave estrangeira que referencia a publicação que recebeu o "gosto".
        /// Parte da chave primária composta.
        /// </summary>
        [Required]
        public int TopicPostId { get; set; }
        /// <summary>
        /// Propriedade de navegação para a publicação associada.
        /// </summary>
        [ForeignKey(nameof(TopicPostId))]
        public virtual TopicPost TopicPost { get; set; }

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