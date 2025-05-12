using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// Representa uma categoria à qual os tópicos podem pertencer.
    /// Contém informações como nome, descrição e imagem associada.
    /// </summary>
    public class Category
    {
        /// <summary>
        /// Identificador único da categoria.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nome da categoria. Este campo é obrigatório e tem um limite de 100 caracteres.
        /// </summary>
        [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
        [MaxLength(100, ErrorMessage = "O nome da categoria não pode exceder 100 caracteres.")]
        [Display(Name = "Nome da Categoria")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Descrição opcional da categoria, com um limite de 500 caracteres.
        /// </summary>
        [MaxLength(500, ErrorMessage = "A descrição não pode exceder 500 caracteres.")]
        [Display(Name = "Descrição (Opcional)")]
        public string? Description { get; set; }

        /// <summary>
        /// URL ou caminho relativo para a imagem associada à categoria.
        /// </summary>
        [Display(Name = "Imagem da Categoria")]
        public string? CategoryImageUrl { get; set; } // Armazena o caminho relativo da imagem

        /// <summary>
        /// Coleção de tópicos associados a esta categoria.
        /// </summary>
        public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();
    }
}
