using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using projetodweb_connectify.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace projetodweb_connectify.Controllers.API
{
    [Route("api/messages")]
    [ApiController]
    [Authorize] 
    public class MessagesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MessagesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém a lista de resumos de todas as conversas do utilizador atual.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConversationSummaryDto>>> GetConversations()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var conversations = await _context.Conversations
                .Where(c => c.Participant1Id == currentUser.Id || c.Participant2Id == currentUser.Id)
                .Include(c => c.Participant1.Profile)
                .Include(c => c.Participant2.Profile)
                .Include(c => c.Messages)
                .Select(c => new ConversationSummaryDto
                {
                    ConversationId = c.Id,
                    // Lógica para encontrar o outro participante
                    OtherParticipantId = c.Participant1Id == currentUser.Id ? c.Participant2Id : c.Participant1Id,
                    OtherParticipantUsername = c.Participant1Id == currentUser.Id ? c.Participant2.Username : c.Participant1.Username,
                    OtherParticipantImageUrl = c.Participant1Id == currentUser.Id ? c.Participant2.Profile.ProfilePicture : c.Participant1.Profile.ProfilePicture,
                    // Obter a última mensagem
                    LastMessage = c.Messages.OrderByDescending(m => m.SentAt).Select(m => m.Content).FirstOrDefault() ?? "Nenhuma mensagem ainda.",
                    LastMessageAt = c.LastMessageAt,
                    // Contar mensagens não lidas para o utilizador atual
                    UnreadMessagesCount = c.Messages.Count(m => m.RecipientId == currentUser.Id && m.ReadAt == null)
                })
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            return Ok(conversations);
        }

        /// <summary>
        /// Obtém todas as mensagens de uma conversa com um utilizador específico.
        /// Marca as mensagens como lidas ao serem obtidas.
        /// </summary>
        /// <param name="otherUserId">O ID do outro utilizador na conversa.</param>
        [HttpGet("with/{otherUserId}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesWithUser(int otherUserId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();
            if (currentUser.Id == otherUserId) return BadRequest(new { message = "Não pode obter uma conversa consigo mesmo." });

            var conversation = await GetOrCreateConversationAsync(currentUser.Id, otherUserId);

            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversation.Id)
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    SentAt = m.SentAt,
                    SenderId = m.SenderId,
                    ReadAt = m.ReadAt,
                    IsSentByCurrentUser = m.SenderId == currentUser.Id
                })
                .ToListAsync();

            // Marcar mensagens como lidas (operação de escrita após a leitura)
            var unreadMessages = await _context.Messages
                .Where(m => m.ConversationId == conversation.Id && m.RecipientId == currentUser.Id && m.ReadAt == null)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.ReadAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }

            return Ok(messages);
        }

        /// <summary>
        /// Envia uma nova mensagem para um utilizador.
        /// </summary>
        /// <param name="createDto">Contém o ID do destinatário e o conteúdo da mensagem.</param>
        [HttpPost]
        public async Task<ActionResult<MessageDto>> SendMessage([FromBody] MessageCreateDto createDto)
        {
            if (string.IsNullOrWhiteSpace(createDto.Content) || createDto.Content.Length > 2000)
            {
                return BadRequest(new { message = "O conteúdo da mensagem é inválido." });
            }

            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();
            if (currentUser.Id == createDto.RecipientUserId) return BadRequest(new { message = "Não pode enviar uma mensagem para si mesmo." });

            // Encontra ou cria a conversa
            var conversation = await GetOrCreateConversationAsync(currentUser.Id, createDto.RecipientUserId);

            var newMessage = new Message
            {
                ConversationId = conversation.Id,
                SenderId = currentUser.Id,
                RecipientId = createDto.RecipientUserId,
                Content = createDto.Content,
                SentAt = DateTime.UtcNow
            };

            // Atualiza a data da última mensagem na conversa
            conversation.LastMessageAt = newMessage.SentAt;

            _context.Messages.Add(newMessage);
            await _context.SaveChangesAsync();

            // Mapeia a nova mensagem para um DTO para retornar ao cliente
            var resultDto = new MessageDto
            {
                Id = newMessage.Id,
                Content = newMessage.Content,
                SentAt = newMessage.SentAt,
                SenderId = newMessage.SenderId,
                IsSentByCurrentUser = true,
                ReadAt = null
            };

            // Retorna a mensagem recém-criada
            return CreatedAtAction(nameof(GetMessagesWithUser), new { otherUserId = createDto.RecipientUserId }, resultDto);
        }

        #region Helper Methods
        private async Task<Users?> GetCurrentUserAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
        }

        private async Task<Conversation> GetOrCreateConversationAsync(int userId1, int userId2)
        {
            // Convenção: Participant1 tem sempre o menor ID para evitar duplicados
            int p1Id = Math.Min(userId1, userId2);
            int p2Id = Math.Max(userId1, userId2);

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Participant1Id == p1Id && c.Participant2Id == p2Id);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    Participant1Id = p1Id,
                    Participant2Id = p2Id,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();
            }

            return conversation;
        }
        #endregion
    }
}