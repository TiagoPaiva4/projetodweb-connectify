using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Necessário para IFormFile

namespace projetodweb_connectify.Models.DTOs
{
    /// <summary>
    /// DTO (Data Transfer Object) que representa os dados necessários para criar uma nova categoria.
    /// </summary>
    public class CategoryCreateDto
    {
        /// <summary>
        /// Nome da categoria. É um campo obrigatório.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Descrição opcional da categoria.
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Ficheiro da imagem da categoria, enviado através do formulário de criação. É opcional.
        /// </summary>
        public IFormFile? CategoryImageFile { get; set; }
    }

    /// <summary>
    /// DTO que representa os dados necessários para editar uma categoria existente.
    /// </summary>
    public class CategoryEditDto
    {
        /// <summary>
        /// Identificador único da categoria a ser editada.
        /// </summary>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// Novo nome para a categoria. É um campo obrigatório.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Nova descrição opcional para a categoria.
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Novo ficheiro de imagem para substituir a imagem existente.
        /// Se não for fornecido, a imagem atual é mantida.
        /// </summary>
        public IFormFile? CategoryImageFile { get; set; }
    }
}