using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// Representa uma conversa entre dois utilizadores.
    /// </summary>
    public class Conversation
    {
        /// <summary>
        /// Identificador único da conversa.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Chave estrangeira para o primeiro participante da conversa.
        /// Por convenção, pode ser o utilizador com o ID mais baixo para evitar duplicados.
        /// </summary>
        public int Participant1Id { get; set; }
        /// <summary>
        /// Propriedade de navegação para o primeiro participante.
        /// </summary>
        [ForeignKey("Participant1Id")]
        public virtual Users Participant1 { get; set; } = null!;

        /// <summary>
        /// Chave estrangeira para o segundo participante da conversa.
        /// </summary>
        public int Participant2Id { get; set; }
        /// <summary>
        /// Propriedade de navegação para o segundo participante.
        /// </summary>
        [ForeignKey("Participant2Id")]
        public virtual Users Participant2 { get; set; } = null!;

        /// <summary>
        /// Data e hora em que a conversa foi iniciada.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Data e hora da última mensagem enviada, útil para ordenar a lista de conversas.
        /// </summary>
        public DateTime LastMessageAt { get; set; }

        /// <summary>
        /// Coleção de todas as mensagens que pertencem a esta conversa.
        /// </summary>
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}