using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace projetodweb_connectify.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
        [MaxLength(100, ErrorMessage = "O nome da categoria não pode exceder 100 caracteres.")]
        [Display(Name = "Nome da Categoria")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "A descrição não pode exceder 500 caracteres.")]
        [Display(Name = "Descrição (Opcional)")]
        public string? Description { get; set; }

        // Propriedade de navegação: Uma categoria pode ter muitos tópicos
        public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();
    }
}
