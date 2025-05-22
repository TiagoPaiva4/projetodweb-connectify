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

        // IDs dos dois utilizadores na conversa
        // Para simplificar a busca, podemos convencionar que User1Id < User2Id
        // ou buscar por ambas as combinações.
        public int User1Id { get; set; }
        [ForeignKey("User1Id")]
        public virtual Users User1 { get; set; } = null!;

        public int User2Id { get; set; }
        [ForeignKey("User2Id")]
        public virtual Users User2 { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastMessageAt { get; set; } // Para ordenar conversas

        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
