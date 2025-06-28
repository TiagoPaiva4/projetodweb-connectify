using System;
using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.Models.DTOs
{
    /// <summary>
    /// DTO para a lista de eventos (visão resumida).
    /// </summary>
    public class EventSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime StartDateTime { get; set; }
        public string Location { get; set; }
        public string? EventImageUrl { get; set; }
        public string CreatorUsername { get; set; }
    }

    /// <summary>
    /// DTO para os detalhes de um evento.
    /// </summary>
    public class EventDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public string Location { get; set; }
        public string? EventImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatorId { get; set; }
        public string CreatorUsername { get; set; }
        public bool IsCurrentUserTheCreator { get; set; }
    }

    /// <summary>
    /// DTO para receber dados ao criar um novo evento.
    /// Usa o IFormFile para upload de imagem.
    /// </summary>
    public class EventCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }
        [Required]
        [StringLength(2000)]
        public string Description { get; set; }
        [Required]
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        [Required]
        [StringLength(200)]
        public string Location { get; set; }
        public IFormFile? EventImageFile { get; set; }
    }

    /// <summary>
    /// DTO para receber o status de participação num evento.
    /// </summary>
    public class EventAttendanceDto
    {
        [Required]
        public AttendanceStatus Status { get; set; }
    }
}