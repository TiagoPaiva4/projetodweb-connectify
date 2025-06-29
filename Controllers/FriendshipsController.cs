using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;

namespace projetodweb_connectify.Controllers
{
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class FriendshipsController : Controller // Exemplo: Controller para Views e APIs
    {
        private readonly ApplicationDbContext _context;

        public FriendshipsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Friendships OU Friendships/Index
        // Esta action serve a View principal das amizades
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Challenge(); // Ou RedirectToAction("Login", "Account")
            }

            var allUserFriendships = await _context.Friendships
                .Where(f => f.User1Id == currentUser.Id || f.User2Id == currentUser.Id)
                .Include(f => f.User1).ThenInclude(u1 => u1.Profile)
                .Include(f => f.User2).ThenInclude(u2 => u2.Profile)
                .OrderByDescending(f => f.RequestDate)
                .ToListAsync();

            ViewData["CurrentUserId"] = currentUser.Id;
            return View(allUserFriendships);
        }

        // --- ACTIONS PARA OS FORMULÁRIOS DA PÁGINA INDEX ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptFriendRequestFromPage(int friendshipId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Challenge();

            var friendship = await _context.Friendships
                .Include(f => f.User1) // <<----- ADICIONAR ESTA LINHA!
                                       // Opcional: Se precisar do User1.Profile para alguma mensagem ou lógica:
                                       // .ThenInclude(u1 => u1.Profile)
                .FirstOrDefaultAsync(f => f.Id == friendshipId && f.User2Id == currentUser.Id && f.Status == FriendshipStatus.Pending);

            if (friendship == null)
            {
                TempData["ErrorMessage"] = "Pedido de amizade não encontrado ou não autorizado.";
                return RedirectToAction(nameof(Index));
            }

            // Agora friendship.User1 não deve ser null se a amizade for encontrada
            if (friendship.User1 == null)
            {
                // Isso seria inesperado se o Include funcionou e a FK User1Id está correta.
                // Adicione um log ou tratamento de erro aqui se necessário.
                TempData["ErrorMessage"] = "Erro ao carregar dados do utilizador do pedido.";
                return RedirectToAction(nameof(Index));
            }

            friendship.Status = FriendshipStatus.Accepted;
            friendship.AcceptanceDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Agora é amigo de {friendship.User1.Username}!"; // Esta linha agora deve funcionar
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectFriendRequestFromPage(int friendshipId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Challenge();

            var friendship = await _context.Friendships
                .Include(f => f.User1)
                .FirstOrDefaultAsync(f => f.Id == friendshipId && f.User2Id == currentUser.Id && f.Status == FriendshipStatus.Pending);

            if (friendship == null)
            {
                TempData["ErrorMessage"] = "Pedido de amizade não encontrado ou não autorizado.";
                return RedirectToAction(nameof(Index));
            }

            // Opção: Marcar como Rejeitado ou Remover
            // Se marcar como rejeitado:
            // friendship.Status = FriendshipStatus.Rejected;
            // Se remover:
            _context.Friendships.Remove(friendship);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Pedido de amizade de {friendship.User1.Username} rejeitado.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFriendFromPage(int friendshipId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Challenge();

            var friendship = await _context.Friendships
                .Include(f => f.User1) // <<----- NECESSÁRIO
                .Include(f => f.User2) // <<----- NECESSÁRIO
                .FirstOrDefaultAsync(f => f.Id == friendshipId &&
                                             f.Status == FriendshipStatus.Accepted &&
                                             (f.User1Id == currentUser.Id || f.User2Id == currentUser.Id));

            if (friendship == null)
            {
                TempData["ErrorMessage"] = "Amizade não encontrada ou não autorizado.";
                return RedirectToAction(nameof(Index));
            }

            var friendUser = friendship.User1Id == currentUser.Id ? friendship.User2 : friendship.User1;
            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Amizade com {friendUser.Username} removida.";
            return RedirectToAction(nameof(Index));
        }


        // Helper para obter o nosso Users customizado (o seu já está bom)
        private async Task<Users?> GetCurrentUserAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;
            return await _context.Users.Include(u => u.Profile)
                                 .FirstOrDefaultAsync(u => u.Username == username);
        }


        // --- OS SEUS ENDPOINTS DE API EXISTENTES (Mantenha-os se precisar deles para AJAX em outras páginas) ---
        // Para que estes funcionem ao lado das actions MVC, eles PRECISAM ter rotas explícitas
        // que não conflitem com as rotas MVC e, idealmente, deveriam estar num controller com [ApiController].
        // Se este controller vai servir APENAS views, você pode remover os métodos de API abaixo.
        // Se você quer que este controller sirva AMBOS, as rotas de API devem ser distintas, e.g., prefixadas com "api/".

        [HttpPost("api/friendships/request/{targetUserId}")] // Rota explícita de API
        [Produces("application/json")] // Indica que retorna JSON
        public async Task<IActionResult> SendFriendRequest(int targetUserId)
        {
            // ... (lógica do seu SendFriendRequest original que retorna Ok(...) ou BadRequest(...))
            // Esta action será chamada por JavaScript, não por um formulário HTML tradicional.
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized(new { message = "Utilizador não autenticado ou não encontrado." });
            if (currentUser.Id == targetUserId) return BadRequest(new { message = "Não pode enviar um pedido de amizade para si mesmo." });
            var targetUser = await _context.Users.FindAsync(targetUserId);
            if (targetUser == null) return NotFound(new { message = "Utilizador alvo não encontrado." });
            var existingFriendship = await _context.Friendships
                .FirstOrDefaultAsync(f => (f.User1Id == currentUser.Id && f.User2Id == targetUserId) || (f.User1Id == targetUserId && f.User2Id == currentUser.Id));
            if (existingFriendship != null)
            {
                if (existingFriendship.Status == FriendshipStatus.Accepted) return BadRequest(new { message = "Já são amigos." });
                if (existingFriendship.Status == FriendshipStatus.Pending)
                {
                    if (existingFriendship.User1Id == currentUser.Id) return BadRequest(new { message = "Pedido de amizade já enviado e pendente." });
                    else return BadRequest(new { message = "Este utilizador já lhe enviou um pedido." });
                }
                _context.Friendships.Remove(existingFriendship);
            }
            var friendship = new Friendship { User1Id = currentUser.Id, User2Id = targetUserId, Status = FriendshipStatus.Pending, RequestDate = DateTime.UtcNow };
            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Pedido de amizade enviado com sucesso.", friendshipId = friendship.Id });
        }

        [HttpPost("api/friendships/accept/{requesterId}")]
        [Produces("application/json")]
        public async Task<IActionResult> AcceptFriendRequest(int requesterId)
        {
            // ... (lógica do seu AcceptFriendRequest original)
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized(new { message = "Utilizador não autenticado ou não encontrado." });
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.User1Id == requesterId && f.User2Id == currentUser.Id && f.Status == FriendshipStatus.Pending);
            if (friendship == null) return NotFound(new { message = "Pedido de amizade não encontrado ou já não está pendente." });
            friendship.Status = FriendshipStatus.Accepted;
            friendship.AcceptanceDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Pedido de amizade aceite." });
        }

        [HttpPost("api/friendships/reject/{requesterId}")]
        [Produces("application/json")]
        public async Task<IActionResult> RejectFriendRequest(int requesterId)
        {
            // ... (lógica do seu RejectFriendRequest original)
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized(new { message = "Utilizador não autenticado ou não encontrado." });
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.User1Id == requesterId && f.User2Id == currentUser.Id && f.Status == FriendshipStatus.Pending);
            if (friendship == null) return NotFound(new { message = "Pedido de amizade não encontrado ou já não está pendente." });
            friendship.Status = FriendshipStatus.Rejected; // Ou _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Pedido de amizade rejeitado." });
        }

        // ... (e assim por diante para os outros métodos de API como CancelFriendRequest, Unfriend, GetPendingRequests, GetSentRequests, GetFriends, GetFriendshipStatus)
        // Todos eles devem ter rotas explícitas [HttpGet("api/friendships/...")] ou [HttpPost("api/friendships/...")]
        // e idealmente Produces("application/json")
        // Exemplo para GetFriendshipStatus:
        [HttpGet("api/friendships/status/{otherUserId}")]
        [Produces("application/json")]
        public async Task<IActionResult> GetFriendshipStatus(int otherUserId)
        {
            // ... (lógica do seu GetFriendshipStatus original)
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized(new { message = "Utilizador não autenticado ou não encontrado." });
            if (currentUser.Id == otherUserId) return Ok(new { status = "self", message = "Este é o seu próprio perfil." });
            var targetUser = await _context.Users.FindAsync(otherUserId);
            if (targetUser == null) return NotFound(new { status = "user_not_found", message = "Utilizador alvo não encontrado." });
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => (f.User1Id == currentUser.Id && f.User2Id == otherUserId) || (f.User1Id == otherUserId && f.User2Id == currentUser.Id));
            if (friendship == null) return Ok(new { status = "not_friends", message = "Sem relação de amizade existente." });
            switch (friendship.Status)
            {
                case FriendshipStatus.Accepted: return Ok(new { status = "friends", friendshipId = friendship.Id, message = "São amigos." });
                case FriendshipStatus.Pending:
                    if (friendship.User1Id == currentUser.Id) return Ok(new { status = "pending_sent", friendshipId = friendship.Id, message = "Pedido de amizade enviado e pendente." });
                    else return Ok(new { status = "pending_received", friendshipId = friendship.Id, message = "Recebeu um pedido de amizade deste utilizador." });
                case FriendshipStatus.Rejected:
                    if (friendship.User1Id == currentUser.Id) return Ok(new { status = "rejected_sent", friendshipId = friendship.Id, message = "O seu pedido de amizade foi rejeitado." });
                    else return Ok(new { status = "rejected_received", friendshipId = friendship.Id, message = "Rejeitou o pedido de amizade deste utilizador." });
                default: return Ok(new { status = "unknown", message = "Estado de amizade desconhecido." });
            }
        }
    }
}