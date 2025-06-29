using System;
using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// ViewModel que representa os dados necessários para criar um novo evento através do formulário.
    /// </summary>
    public class EventCreateViewModel
    {
        /// <summary>
        /// O título do evento.
        /// </summary>
        [Required(ErrorMessage = "O título é obrigatório.")]
        [StringLength(200)]
        [Display(Name = "Título do Evento")]
        public string Title { get; set; }

        /// <summary>
        /// A descrição detalhada do evento.
        /// </summary>
        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [Display(Name = "Descrição")]
        public string Description { get; set; }

        /// <summary>
        /// A data e hora em que o evento se inicia.
        /// </summary>
        [Required(ErrorMessage = "A data de início é obrigatória.")]
        [Display(Name = "Data e Hora de Início")]
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// A data e hora em que o evento termina. Este campo é opcional.
        /// </summary>
        [Display(Name = "Data e Hora de Fim (Opcional)")]
        public DateTime? EndDateTime { get; set; }

        /// <summary>
        /// A localização onde o evento irá decorrer.
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Localização")]
        public string Location { get; set; }

        /// <summary>
        /// O URL para a imagem de capa do evento. Este campo é opcional.
        /// </summary>
        [Display(Name = "URL da Imagem do Evento (Opcional)")]
        [Url(ErrorMessage = "Por favor, insira um URL válido.")]
        public string? EventImageUrl { get; set; }
    }
}