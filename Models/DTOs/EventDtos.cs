using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Necessário para IFormFile

namespace projetodweb_connectify.Models.DTOs
{
    /// <summary>
    /// DTO para a lista de eventos (visão resumida).
    /// </summary>
    public class EventSummaryDto
    {
        /// <summary>
        /// Identificador único do evento.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Título do evento.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Data e hora de início do evento.
        /// </summary>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Localização onde o evento irá decorrer.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// URL da imagem de capa do evento.
        /// </summary>
        public string? EventImageUrl { get; set; }

        /// <summary>
        /// Nome de utilizador do criador do evento.
        /// </summary>
        public string CreatorUsername { get; set; }
    }

    /// <summary>
    /// DTO para os detalhes de um evento.
    /// </summary>
    public class EventDetailDto
    {
        /// <summary>
        /// Identificador único do evento.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Título do evento.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Descrição detalhada do evento.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Data e hora de início do evento.
        /// </summary>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Data e hora de fim do evento (opcional).
        /// </summary>
        public DateTime? EndDateTime { get; set; }

        /// <summary>
        /// Localização onde o evento irá decorrer.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// URL da imagem de capa do evento.
        /// </summary>
        public string? EventImageUrl { get; set; }

        /// <summary>
        /// Data em que o registo do evento foi criado.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// ID do utilizador que criou o evento.
        /// </summary>
        public int CreatorId { get; set; }

        /// <summary>
        /// Nome de utilizador do criador do evento.
        /// </summary>
        public string CreatorUsername { get; set; }

        /// <summary>
        /// Indica se o utilizador autenticado é o criador do evento.
        /// </summary>
        public bool IsCurrentUserTheCreator { get; set; }
    }

    /// <summary>
    /// DTO para receber dados ao criar um novo evento.
    /// Usa o IFormFile para upload de imagem.
    /// </summary>
    public class EventCreateDto
    {
        /// <summary>
        /// Título para o novo evento.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        /// <summary>
        /// Descrição detalhada para o novo evento.
        /// </summary>
        [Required]
        [StringLength(2000)]
        public string Description { get; set; }

        /// <summary>
        /// Data e hora de início do novo evento.
        /// </summary>
        [Required]
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Data e hora de fim do novo evento (opcional).
        /// </summary>
        public DateTime? EndDateTime { get; set; }

        /// <summary>
        /// Localização do novo evento.
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Location { get; set; }

        /// <summary>
        /// Ficheiro de imagem para o evento, enviado através do formulário. É opcional.
        /// </summary>
        public IFormFile? EventImageFile { get; set; }
    }

    /// <summary>
    /// DTO para receber o estado de participação num evento.
    /// </summary>
    public class EventAttendanceDto
    {
        /// <summary>
        /// O estado de participação que o utilizador pretende definir (ex: 'A Participar', 'Interessado').
        /// </summary>
        [Required]
        public AttendanceStatus Status { get; set; }
    }
}