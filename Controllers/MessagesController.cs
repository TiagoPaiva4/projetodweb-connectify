using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        /// <summary>
        /// Método auxiliar para obter o utilizador autenticado e os seus dados de perfil.
        /// </summary>
        /// <returns>O objeto 'Users' do utilizador atual ou null se não for encontrado.</returns>
        private async Task<Users?> GetCurrentUserAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;

            // O Include(u => u.Profile) é importante para ter acesso a dados como a foto de perfil.
            return await _context.Users.Include(u => u.Profile)
                                 .FirstOrDefaultAsync(u => u.Username == username);
        }

        /// <summary>
        /// Apresenta a página principal de mensagens, listando todas as conversas do utilizador.
        /// </summary>
        /// <param name="chatWith">Parâmetro opcional para abrir uma conversa específica ao carregar a página.</param>
        public async Task<IActionResult> Index(int? chatWith = null)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Challenge(); // Se não estiver autenticado, desafia para o login.

            var conversations = await _context.Conversations
                .Where(c => c.Participant1Id == currentUser.Id || c.Participant2Id == currentUser.Id)
                .Include(c => c.Participant1).ThenInclude(u => u.Profile) // Carrega dados do primeiro participante.
                .Include(c => c.Participant2).ThenInclude(u => u.Profile) // Carrega dados do segundo participante.
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1)) // Pega na última mensagem para preview.
                .OrderByDescending(c => c.LastMessageAt) // Ordena as conversas pela mais recente.
                .ToListAsync();

            ViewBag.CurrentUserId = currentUser.Id;

            // Se um ID de utilizador for passado na URL, prepara-o para ser usado pelo JavaScript e abrir o chat.
            if (chatWith.HasValue && chatWith.Value != currentUser.Id)
            {
                ViewBag.InitialChatOtherUserId = chatWith.Value;
            }

            return View(conversations);
        }

        /// <summary>
        /// Carrega a página de conversação completa com um utilizador específico.
        /// Esta action carrega uma view completa e não é para ser chamada via AJAX.
        /// </summary>
        /// <param name="otherUserId">O ID do outro utilizador na conversa.</param>
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

            // Garante uma chave de conversação consistente (menor ID primeiro) para evitar duplicados.
            int p1Id = Math.Min(currentUser.Id, otherUserId);
            int p2Id = Math.Max(currentUser.Id, otherUserId);

            var conversation = await _context.Conversations
                .Include(c => c.Messages.OrderBy(m => m.SentAt))
                    .ThenInclude(m => m.Sender)
                        .ThenInclude(s => s.Profile)
                .FirstOrDefaultAsync(c => c.Participant1Id == p1Id && c.Participant2Id == p2Id);

            if (conversation == null)
            {
                // Se a conversa não existe, cria-a.
                conversation = new Conversation
                {
                    Participant1Id = p1Id,
                    Participant2Id = p2Id,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow
                };
                _context.Conversations.Add(conversation);
                // É necessário guardar para que a conversação obtenha um ID, essencial para o envio de mensagens.
                await _context.SaveChangesAsync();
            }
            else
            {
                // Se a conversa existe, marca as mensagens por ler como lidas.
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
            return View(conversation);
        }

        /// <summary>
        /// Action chamada via AJAX para carregar ou atualizar o conteúdo de uma conversação sem recarregar a página inteira.
        /// </summary>
        /// <param name="otherUserId">O ID do outro utilizador na conversa.</param>
        /// <returns>Uma PartialView com o conteúdo do chat.</returns>
        [HttpGet]
        public async Task<IActionResult> ChatPartial(int otherUserId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            // Validação de segurança para o caso de a UI permitir esta chamada indevidamente.
            if (currentUser.Id == otherUserId)
            {
                return PartialView("_ErrorPartial", "Não pode conversar consigo mesmo.");
            }

            var otherUser = await _context.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == otherUserId);
            if (otherUser == null)
            {
                return PartialView("_ErrorPartial", "Utilizador não encontrado.");
            }

            int p1Id = Math.Min(currentUser.Id, otherUserId);
            int p2Id = Math.Max(currentUser.Id, otherUserId);

            var conversation = await _context.Conversations
                .Include(c => c.Messages.OrderBy(m => m.SentAt))
                    .ThenInclude(m => m.Sender)
                        .ThenInclude(s => s.Profile)
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
            else
            {
                var unreadMessages = conversation.Messages
                    .Where(m => m.RecipientId == currentUser.Id && m.ReadAt == null)
                    .ToList();
                foreach (var msg in unreadMessages) { msg.ReadAt = DateTime.UtcNow; }
                if (unreadMessages.Any()) { await _context.SaveChangesAsync(); }
            }

            ViewBag.OtherUser = otherUser;
            ViewBag.CurrentUserId = currentUser.Id;
            // Devolve a PartialView que contém a janela de chat.
            return PartialView("_ChatContentPartial", conversation);
        }

        /// <summary>
        /// Processa o envio de uma nova mensagem. Suporta tanto submissões de formulário tradicionais como chamadas AJAX.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int conversationId, int recipientUserId, string messageContent)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized(new { success = false, message = "Não autorizado." });

            if (string.IsNullOrWhiteSpace(messageContent) || messageContent.Length > 2000)
            {
                // Verifica se a requisição foi feita via AJAX.
                bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
                if (isAjax)
                {
                    return Json(new { success = false, message = "A mensagem não pode estar vazia ou exceder 2000 caracteres." });
                }

                // Fallback para formulário tradicional.
                TempData["ErrorMessage"] = "A mensagem não pode estar vazia ou exceder 2000 caracteres.";
                return RedirectToAction(nameof(Chat), new { otherUserId = recipientUserId });
            }

            var conversation = await _context.Conversations
                                    .FirstOrDefaultAsync(c => c.Id == conversationId &&
                                                               (c.Participant1Id == currentUser.Id || c.Participant2Id == currentUser.Id));

            if (conversation == null)
            {
                return Json(new { success = false, message = "Conversa não encontrada ou sem permissão." });
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
            conversation.LastMessageAt = message.SentAt;
            await _context.SaveChangesAsync();

            // Abordagem atual (simples): Devolve um JSON de sucesso e o cliente é responsável por recarregar os dados.
            return Json(new { success = true, message = "Mensagem enviada." });
        }
    }
}