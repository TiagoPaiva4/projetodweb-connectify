using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projetodweb_connectify.Models
{
    public class Conversation
    {
        [Key]
        public int Id { get; set; }

        // Para uma conversa 1-para-1, teremos dois participantes.
        // Para simplificar a busca e evitar duplicados (UserA-UserB vs UserB-UserA),
        // podemos convencionar que User1Id sempre será o menor ID.
        public int Participant1Id { get; set; }
        [ForeignKey("Participant1Id")]
        public virtual Users Participant1 { get; set; } = null!;

        public int Participant2Id { get; set; }
        [ForeignKey("Participant2Id")]
        public virtual Users Participant2 { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastMessageAt { get; set; } // Para ordenar a lista de conversas

        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}