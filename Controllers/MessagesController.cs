// Controllers/MessagesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Para List

namespace projetodweb_connectify.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper para obter o utilizador atual
        private async Task<Users?> GetCurrentUserAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;
            // Incluir Profile para mostrar foto e nome
            return await _context.Users.Include(u => u.Profile)
                                 .FirstOrDefaultAsync(u => u.Username == username);
        }

        // GET: Messages  (ou Messages/Index)
        // Lista as conversas do utilizador atual
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Challenge(); // Ou redirecionar para Login

            var conversations = await _context.Conversations
                .Where(c => c.Participant1Id == currentUser.Id || c.Participant2Id == currentUser.Id)
                .Include(c => c.Participant1).ThenInclude(p => p.Profile) // Incluir perfil do participante 1
                .Include(c => c.Participant2).ThenInclude(p => p.Profile) // Incluir perfil do participante 2
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1)) // Para pegar a última mensagem
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            ViewBag.CurrentUserId = currentUser.Id;
            return View(conversations);
        }


        // GET: Messages/Chat/{otherUserId}
        // Mostra a conversa com um utilizador específico
        public async Task<IActionResult> Chat(int otherUserId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Challenge();

            if (currentUser.Id == otherUserId)
            {
                TempData["ErrorMessage"] = "Não pode conversar consigo mesmo.";
                return RedirectToAction(nameof(Index));
            }

            var otherUser = await _context.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == otherUserId);
            if (otherUser == null)
            {
                TempData["ErrorMessage"] = "Utilizador não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Determinar Participant1Id e Participant2Id (convenção: menor ID primeiro)
            int p1Id = Math.Min(currentUser.Id, otherUserId);
            int p2Id = Math.Max(currentUser.Id, otherUserId);

            var conversation = await _context.Conversations
                .Include(c => c.Messages.OrderBy(m => m.SentAt)) // Ordenar mensagens pela data de envio
                    .ThenInclude(m => m.Sender) // Para saber quem enviou cada mensagem
                        .ThenInclude(s => s.Profile) // Para foto do remetente na mensagem
                .FirstOrDefaultAsync(c => c.Participant1Id == p1Id && c.Participant2Id == p2Id);

            if (conversation == null)
            {
                // Criar uma nova conversa se não existir
                conversation = new Conversation
                {
                    Participant1Id = p1Id,
                    Participant2Id = p2Id,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow // Pode ser a data de criação inicialmente
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync(); // Salvar para obter o Conversation.Id
            }
            else
            {
                // Marcar mensagens como lidas pelo currentUser nesta conversa
                var unreadMessages = conversation.Messages
                    .Where(m => m.RecipientId == currentUser.Id && m.ReadAt == null)
                    .ToList();

                foreach (var msg in unreadMessages)
                {
                    msg.ReadAt = DateTime.UtcNow;
                }
                if (unreadMessages.Any())
                {
                    await _context.SaveChangesAsync();
                }
            }

            ViewBag.OtherUser = otherUser;
            ViewBag.CurrentUserId = currentUser.Id;
            return View(conversation); // Passar o objeto Conversation para a View
        }

        // POST: Messages/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int conversationId, int recipientUserId, string messageContent)
        {
            if (string.IsNullOrWhiteSpace(messageContent) || messageContent.Length > 2000)
            {
                TempData["ErrorMessage"] = "A mensagem não pode estar vazia ou exceder 2000 caracteres.";
                // Precisamos do otherUserId para redirecionar de volta para o chat correto
                var convForRedirect = await _context.Conversations.FindAsync(conversationId);
                if (convForRedirect != null)
                {
                    var currentUserSenderId = (await GetCurrentUserAsync())?.Id;
                    var otherIdForRedirect = convForRedirect.Participant1Id == currentUserSenderId ? convForRedirect.Participant2Id : convForRedirect.Participant1Id;
                    return RedirectToAction(nameof(Chat), new { otherUserId = otherIdForRedirect });
                }
                return RedirectToAction(nameof(Index)); // Fallback
            }

            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Challenge();

            var conversation = await _context.Conversations
                                    .FirstOrDefaultAsync(c => c.Id == conversationId &&
                                                               (c.Participant1Id == currentUser.Id || c.Participant2Id == currentUser.Id));

            if (conversation == null)
            {
                TempData["ErrorMessage"] = "Conversa não encontrada ou não tem permissão.";
                return RedirectToAction(nameof(Index));
            }

            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = currentUser.Id,
                RecipientId = recipientUserId,
                Content = messageContent,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            conversation.LastMessageAt = message.SentAt; // Atualizar LastMessageAt na conversa

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Chat), new { otherUserId = recipientUserId });
        }
    }
}