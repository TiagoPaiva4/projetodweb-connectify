// Ficheiro: Models/EventCreateViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.Models
{
    public class EventCreateViewModel
    {
        [Required(ErrorMessage = "O título é obrigatório.")]
        [StringLength(200)]
        [Display(Name = "Título do Evento")]
        public string Title { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [Display(Name = "Descrição")]
        public string Description { get; set; }

        [Required(ErrorMessage = "A data de início é obrigatória.")]
        [Display(Name = "Data e Hora de Início")]
        public DateTime StartDateTime { get; set; }

        [Display(Name = "Data e Hora de Fim (Opcional)")]
        public DateTime? EndDateTime { get; set; }

        [StringLength(255)]
        [Display(Name = "Localização")]
        public string Location { get; set; }

        [Display(Name = "URL da Imagem do Evento (Opcional)")]
        [Url(ErrorMessage = "Por favor, insira um URL válido.")]
        public string? EventImageUrl { get; set; }
    }
}