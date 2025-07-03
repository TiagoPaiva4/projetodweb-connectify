// Ficheiro: Services/ChatHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using projetodweb_connectify.Models.DTOs;

namespace projetodweb_connectify.Services
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        // Método que o cliente (JavaScript) vai invocar para enviar uma mensagem.
        public async Task SendMessage(string recipientUserIdStr, string content)
        {
            // 1. Identificar o remetente (utilizador atual)
            var senderUsername = Context.User?.Identity?.Name;
            if (string.IsNullOrEmpty(senderUsername)) return;

            var sender = await _context.Users.AsNoTracking()
                                       .FirstOrDefaultAsync(u => u.Username == senderUsername);
            if (sender == null) return;

            // 2. Validar o destinatário
            if (!int.TryParse(recipientUserIdStr, out int recipientUserId)) return;
            var recipient = await _context.Users.AsNoTracking()
                                          .FirstOrDefaultAsync(u => u.Id == recipientUserId);
            if (recipient == null) return;

            // 3. Guardar a mensagem na base de dados (reutilizando a lógica do seu API)
            var conversation = await GetOrCreateConversationAsync(sender.Id, recipient.Id);

            var newMessage = new Message
            {
                ConversationId = conversation.Id,
                SenderId = sender.Id,
                RecipientId = recipient.Id,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            conversation.LastMessageAt = newMessage.SentAt;
            _context.Messages.Add(newMessage);
            await _context.SaveChangesAsync();

            // 4. Preparar o DTO para enviar de volta aos clientes
            var messageDto = new MessageDto
            {
                Id = newMessage.Id,
                Content = newMessage.Content,
                SentAt = newMessage.SentAt,
                SenderId = newMessage.SenderId,
                ReadAt = null
            };

            // 5. Enviar a mensagem para o destinatário e para o remetente

            // Envia para o destinatário (se estiver online)
            await Clients.User(recipient.Username).SendAsync("ReceiveMessage", messageDto);

            // Envia para o remetente (para que a sua própria UI seja atualizada)
            await Clients.Caller.SendAsync("ReceiveMessage", messageDto);
        }

        // Método auxiliar copiado da sua API para manter a consistência
        private async Task<Conversation> GetOrCreateConversationAsync(int userId1, int userId2)
        {
            int p1Id = Math.Min(userId1, userId2);
            int p2Id = Math.Max(userId1, userId2);

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Participant1Id == p1Id && c.Participant2Id == p2Id);

            if (conversation == null)
            {
                conversation = new Conversation { Participant1Id = p1Id, Participant2Id = p2Id };
                _context.Conversations.Add(conversation);
                // Não é preciso guardar aqui, pois a chamada principal já o fará
            }
            return conversation;
        }
    }
}