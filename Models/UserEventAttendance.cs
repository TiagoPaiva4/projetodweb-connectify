using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using projetodweb_connectify.Models;

/// <summary>
/// Enumeração que representa os diferentes estados de presença num evento.
/// </summary>
public enum AttendanceStatus
{
    Going,
    Interested,
    NotGoing,
    Maybe
}

/// <summary>
/// Representa a relação entre um utilizador e um evento, indicando a intenção de participação.
/// </summary>
public class UserEventAttendance
{
    /// <summary>
    /// ID do utilizador que participou ou respondeu ao evento.
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Navegação para o utilizador associado à participação.
    /// </summary>
    public virtual Users User { get; set; }

    /// <summary>
    /// ID do evento a que o utilizador respondeu.
    /// </summary>
    [Required]
    public int EventId { get; set; }

    /// <summary>
    /// Navegação para o evento associado à participação.
    /// </summary>
    public virtual Event Event { get; set; }

    /// <summary>
    /// Estado da participação do utilizador no evento (Ex: Going, Interested).
    /// </summary>
    [Required]
    public AttendanceStatus Status { get; set; }

    /// <summary>
    /// Data e hora da última atualização da resposta do utilizador ao evento.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
