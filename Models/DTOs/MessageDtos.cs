using System; // Necessário para DateTime

namespace projetodweb_connectify.Models.DTOs
{
    /// <summary>
    /// Representa um resumo de uma conversa para a lista de chats.
    /// </summary>
    public class ConversationSummaryDto
    {
        /// <summary>
        /// Identificador único da conversa.
        /// </summary>
        public int ConversationId { get; set; }

        /// <summary>
        /// ID do outro participante na conversa.
        /// </summary>
        public int OtherParticipantId { get; set; }

        /// <summary>
        /// Nome de utilizador do outro participante.
        /// </summary>
        public string OtherParticipantUsername { get; set; }

        /// <summary>
        /// URL da imagem de perfil do outro participante.
        /// </summary>
        public string? OtherParticipantImageUrl { get; set; }

        /// <summary>
        /// O conteúdo da última mensagem trocada, para fins de pré-visualização.
        /// </summary>
        public string LastMessage { get; set; }

        /// <summary>
        /// A data e hora em que a última mensagem foi enviada.
        /// </summary>
        public DateTime LastMessageAt { get; set; }

        /// <summary>
        /// Número de mensagens não lidas pelo utilizador atual nesta conversa.
        /// </summary>
        public int UnreadMessagesCount { get; set; }
    }

    /// <summary>
    /// Representa uma única mensagem dentro de uma conversa.
    /// </summary>
    public class MessageDto
    {
        /// <summary>
        /// Identificador único da mensagem.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// O conteúdo textual da mensagem.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// A data e hora em que a mensagem foi enviada.
        /// </summary>
        public DateTime SentAt { get; set; }

        /// <summary>
        /// ID do utilizador que enviou a mensagem.
        /// </summary>
        public int SenderId { get; set; }

        /// <summary>
        /// Indica se a mensagem foi enviada pelo utilizador autenticado. Facilita a renderização no frontend.
        /// </summary>
        public bool IsSentByCurrentUser { get; set; }

        /// <summary>
        /// A data e hora em que a mensagem foi lida pelo destinatário. É nulo se ainda não foi lida.
        /// </summary>
        public DateTime? ReadAt { get; set; }
    }

    /// <summary>
    /// DTO para receber os dados ao enviar uma nova mensagem.
    /// </summary>
    public class MessageCreateDto
    {
        /// <summary>
        /// ID do utilizador que irá receber a mensagem (destinatário).
        /// </summary>
        public int RecipientUserId { get; set; }

        /// <summary>
        /// O conteúdo da nova mensagem a ser enviada.
        /// </summary>
        public string Content { get; set; }
    }
}