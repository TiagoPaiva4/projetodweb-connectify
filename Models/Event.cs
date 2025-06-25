using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using projetodweb_connectify.Models;

/// <summary>
/// Representa um evento criado por um utilizador na aplicação Connectify.
/// Inclui informações como título, descrição, datas, localização e participantes.
/// </summary>
public class Event
{
    /// <summary>
    /// Identificador único do evento.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Título do evento.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; }

    /// <summary>
    /// Descrição detalhada do evento.
    /// </summary>
    [Required]
    public string Description { get; set; }

    /// <summary>
    /// Data e hora de início do evento.
    /// </summary>
    [Required]
    public DateTime StartDateTime { get; set; }

    /// <summary>
    /// Data e hora de término do evento (opcional).
    /// </summary>
    public DateTime? EndDateTime { get; set; }

    /// <summary>
    /// Localização do evento, pode ser um endereço físico ou link online.
    /// </summary>
    [StringLength(255)]
    public string Location { get; set; }

    /// <summary>
    /// URL da imagem associada ao evento (opcional).
    /// </summary>
    public string? EventImageUrl { get; set; }

    /// <summary>
    /// ID do utilizador (Profile) que criou o evento.
    /// </summary>
    [Required]
    public int CreatorId { get; set; }

    /// <summary>
    /// Referência para o utilizador (Users) que criou o evento.
    /// </summary>
    [ForeignKey("CreatorId")]
    public virtual Users Creator { get; set; }

    /// <summary>
    /// Data e hora da criação do evento.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data e hora da última atualização do evento (opcional).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Lista de participações de utilizadores neste evento.
    /// </summary>
    public virtual ICollection<UserEventAttendance> Attendees { get; set; } = new List<UserEventAttendance>();
}
