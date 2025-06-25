namespace projetodweb_connectify.Models.DTOs
{
    /// <summary>
    /// Representa um resumo de uma conversa para a lista de chats.
    /// </summary>
    public class ConversationSummaryDto
    {
        public int ConversationId { get; set; }
        public int OtherParticipantId { get; set; }
        public string OtherParticipantUsername { get; set; }
        public string? OtherParticipantImageUrl { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageAt { get; set; }
        public int UnreadMessagesCount { get; set; }
    }

    /// <summary>
    /// Representa uma única mensagem dentro de uma conversa.
    /// </summary>
    public class MessageDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public int SenderId { get; set; }
        public bool IsSentByCurrentUser { get; set; } // Facilita a renderização no frontend
        public DateTime? ReadAt { get; set; }
    }

    /// <summary>
    /// DTO para receber os dados ao enviar uma nova mensagem.
    /// </summary>
    public class MessageCreateDto
    {
        public int RecipientUserId { get; set; }
        public string Content { get; set; }
    }
}