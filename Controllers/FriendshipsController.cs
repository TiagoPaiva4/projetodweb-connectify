using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;

namespace projetodweb_connectify.Controllers
{
    public class FriendshipsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FriendshipsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper para obter o nosso Users customizado
        private async Task<Users?> GetCurrentUserAsync()
        {
            // Assumimos que User.Identity.Name contém o Username do seu modelo Users
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                // Se o nome de utilizador não estiver disponível, o utilizador não está autenticado
                // ou o esquema de autenticação não está a popular User.Identity.Name corretamente.
                return null;
            }

            // Faz o lookup na sua tabela Users
            return await _context.Users
                                 .Include(u => u.Profile) // Opcional, mas pode ser útil
                                 .FirstOrDefaultAsync(u => u.Username == username);
        }

        /// <summary>
        /// Envia um pedido de amizade para outro utilizador.
        /// </summary>
        /// <param name="targetUserId">ID do utilizador para quem o pedido é enviado.</param>
        [HttpPost("request/{targetUserId}")]
        public async Task<IActionResult> SendFriendRequest(int targetUserId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized("Utilizador não autenticado ou não encontrado.");
            }

            if (currentUser.Id == targetUserId)
            {
                return BadRequest("Não pode enviar um pedido de amizade para si mesmo.");
            }

            var targetUser = await _context.Users.FindAsync(targetUserId);
            if (targetUser == null)
            {
                return NotFound("Utilizador alvo não encontrado.");
            }

            var existingFriendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.User1Id == currentUser.Id && f.User2Id == targetUserId) ||
                    (f.User1Id == targetUserId && f.User2Id == currentUser.Id));

            if (existingFriendship != null)
            {
                if (existingFriendship.Status == FriendshipStatus.Accepted)
                {
                    return BadRequest("Já são amigos.");
                }
                if (existingFriendship.Status == FriendshipStatus.Pending)
                {
                    if (existingFriendship.User1Id == currentUser.Id)
                        return BadRequest("Pedido de amizade já enviado e pendente.");
                    else // Pedido pendente recebido do targetUser
                        return BadRequest("Este utilizador já lhe enviou um pedido. Verifique os seus pedidos pendentes.");
                }
                // Se foi Rejected, poderia permitir reenviar, ou remover o registro anterior
                _context.Friendships.Remove(existingFriendship); // Remover o antigo rejeitado para permitir novo pedido
            }

            var friendship = new Friendship
            {
                User1Id = currentUser.Id,
                User2Id = targetUserId,
                Status = FriendshipStatus.Pending,
                RequestDate = DateTime.UtcNow
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Pedido de amizade enviado com sucesso.", friendshipId = friendship.Id });
        }

        /// <summary>
        /// Aceita um pedido de amizade pendente.
        /// </summary>
        /// <param name="requesterId">ID do utilizador que enviou o pedido.</param>
        [HttpPost("accept/{requesterId}")]
        public async Task<IActionResult> AcceptFriendRequest(int requesterId)
        {
            var currentUser = await GetCurrentUserAsync(); // Quem está a aceitar
            if (currentUser == null)
            {
                return Unauthorized("Utilizador não autenticado ou não encontrado.");
            }

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.User1Id == requesterId && f.User2Id == currentUser.Id && f.Status == FriendshipStatus.Pending);

            if (friendship == null)
            {
                return NotFound("Pedido de amizade não encontrado ou já não está pendente.");
            }

            friendship.Status = FriendshipStatus.Accepted;
            friendship.AcceptanceDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Pedido de amizade aceite." });
        }

        /// <summary>
        /// Rejeita um pedido de amizade pendente.
        /// </summary>
        /// <param name="requesterId">ID do utilizador que enviou o pedido.</param>
        [HttpPost("reject/{requesterId}")]
        public async Task<IActionResult> RejectFriendRequest(int requesterId)
        {
            var currentUser = await GetCurrentUserAsync(); // Quem está a rejeitar
            if (currentUser == null)
            {
                return Unauthorized("Utilizador não autenticado ou não encontrado.");
            }

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.User1Id == requesterId && f.User2Id == currentUser.Id && f.Status == FriendshipStatus.Pending);

            if (friendship == null)
            {
                return NotFound("Pedido de amizade não encontrado ou já não está pendente.");
            }

            // Opção 1: Marcar como Rejeitado
            friendship.Status = FriendshipStatus.Rejected;
            // Opção 2: Remover o pedido (se preferir não manter o histórico de rejeição)
            // _context.Friendships.Remove(friendship);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Pedido de amizade rejeitado." });
        }

        /// <summary>
        /// Cancela um pedido de amizade enviado pelo utilizador atual.
        /// </summary>
        /// <param name="receiverId">ID do utilizador para quem o pedido foi enviado.</param>
        [HttpPost("cancel/{receiverId}")]
        public async Task<IActionResult> CancelFriendRequest(int receiverId)
        {
            var currentUser = await GetCurrentUserAsync(); // Quem enviou e quer cancelar
            if (currentUser == null)
            {
                return Unauthorized("Utilizador não autenticado ou não encontrado.");
            }

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.User1Id == currentUser.Id && f.User2Id == receiverId && f.Status == FriendshipStatus.Pending);

            if (friendship == null)
            {
                return NotFound("Pedido de amizade pendente não encontrado para cancelar.");
            }

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Pedido de amizade cancelado." });
        }

        /// <summary>
        /// Remove uma amizade existente.
        /// </summary>
        /// <param name="friendId">ID do utilizador com quem se quer terminar a amizade.</param>
        [HttpPost("unfriend/{friendId}")]
        public async Task<IActionResult> Unfriend(int friendId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized("Utilizador não autenticado ou não encontrado.");
            }

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.Status == FriendshipStatus.Accepted &&
                                     ((f.User1Id == currentUser.Id && f.User2Id == friendId) ||
                                      (f.User1Id == friendId && f.User2Id == currentUser.Id)));

            if (friendship == null)
            {
                return NotFound("Amizade não encontrada.");
            }

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Amizade removida." });
        }

        /// <summary>
        /// Obtém todos os pedidos de amizade pendentes recebidos pelo utilizador atual.
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized("Utilizador não autenticado ou não encontrado.");
            }

            var pendingRequests = await _context.Friendships
                .Where(f => f.User2Id == currentUser.Id && f.Status == FriendshipStatus.Pending)
                .Include(f => f.User1)
                    .ThenInclude(u1 => u1.Profile)
                .Select(f => new
                {
                    FriendshipId = f.Id, // Adicionado para que se possa aceitar/rejeitar
                    f.RequestDate,
                    Requester = new
                    {
                        f.User1.Id,
                        f.User1.Username,
                        ProfileName = f.User1.Profile != null ? f.User1.Profile.Name : f.User1.Username,
                        ProfilePicture = f.User1.Profile != null ? f.User1.Profile.ProfilePicture : "/images/defaultuser.png"
                    }
                })
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return Ok(pendingRequests);
        }

        /// <summary>
        /// Obtém todos os pedidos de amizade enviados pelo utilizador atual que estão pendentes.
        /// </summary>
        [HttpGet("sent")]
        public async Task<IActionResult> GetSentRequests()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized("Utilizador não autenticado ou não encontrado.");
            }

            var sentRequests = await _context.Friendships
                .Where(f => f.User1Id == currentUser.Id && f.Status == FriendshipStatus.Pending)
                .Include(f => f.User2)
                    .ThenInclude(u2 => u2.Profile)
                .Select(f => new
                {
                    FriendshipId = f.Id, // Adicionado para que se possa cancelar
                    f.RequestDate,
                    Receiver = new
                    {
                        f.User2.Id,
                        f.User2.Username,
                        ProfileName = f.User2.Profile != null ? f.User2.Profile.Name : f.User2.Username,
                        ProfilePicture = f.User2.Profile != null ? f.User2.Profile.ProfilePicture : "/images/defaultuser.png"
                    }
                })
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return Ok(sentRequests);
        }

        /// <summary>
        /// Obtém a lista de amigos do utilizador atual.
        /// </summary>
        [HttpGet("friends")]
        public async Task<IActionResult> GetFriends()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized("Utilizador não autenticado ou não encontrado.");
            }

            var friends = await _context.Friendships
                .Where(f => f.Status == FriendshipStatus.Accepted && (f.User1Id == currentUser.Id || f.User2Id == currentUser.Id))
                .Include(f => f.User1).ThenInclude(u1 => u1.Profile) // Inclui User1 e seu Profile
                .Include(f => f.User2).ThenInclude(u2 => u2.Profile) // Inclui User2 e seu Profile
                .Select(f => f.User1Id == currentUser.Id ? f.User2 : f.User1) // Seleciona o *outro* utilizador na amizade
                .Select(u => new // Projeção para um formato de dados mais limpo
                {
                    u.Id,
                    u.Username,
                    ProfileName = u.Profile != null ? u.Profile.Name : u.Username,
                    ProfilePicture = u.Profile != null ? u.Profile.ProfilePicture : "/images/defaultuser.png",
                    // Adicione aqui a data em que se tornaram amigos, se tiver essa informação no Friendship.AcceptanceDate
                })
                .Distinct() // Garante que não há duplicados se ambos os lados forem incluídos de alguma forma complexa
                .ToListAsync();

            return Ok(friends);
        }

        /// <summary>
        /// Verifica o estado da amizade com um utilizador específico.
        /// Útil para a UI determinar qual botão mostrar (Adicionar, Cancelar, Aceitar, etc.).
        /// </summary>
        /// <param name="otherUserId">ID do outro utilizador.</param>
        [HttpGet("status/{otherUserId}")]
        public async Task<IActionResult> GetFriendshipStatus(int otherUserId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized("Utilizador não autenticado ou não encontrado.");
            }

            if (currentUser.Id == otherUserId)
            {
                return Ok(new { status = "self", message = "Este é o seu próprio perfil." });
            }

            var targetUser = await _context.Users.FindAsync(otherUserId);
            if (targetUser == null)
            {
                return NotFound(new { status = "user_not_found", message = "Utilizador alvo não encontrado." });
            }

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.User1Id == currentUser.Id && f.User2Id == otherUserId) ||
                    (f.User1Id == otherUserId && f.User2Id == currentUser.Id));

            if (friendship == null)
            {
                return Ok(new { status = "not_friends", message = "Sem relação de amizade existente." });
            }

            switch (friendship.Status)
            {
                case FriendshipStatus.Accepted:
                    return Ok(new { status = "friends", friendshipId = friendship.Id, message = "São amigos." });
                case FriendshipStatus.Pending:
                    if (friendship.User1Id == currentUser.Id) // O utilizador atual enviou o pedido
                        return Ok(new { status = "pending_sent", friendshipId = friendship.Id, message = "Pedido de amizade enviado e pendente." });
                    else // O utilizador atual recebeu o pedido (friendship.User2Id == currentUser.Id)
                        return Ok(new { status = "pending_received", friendshipId = friendship.Id, message = "Recebeu um pedido de amizade deste utilizador." });
                case FriendshipStatus.Rejected:
                    // Poderia ser mais granular: quem rejeitou e quando
                    if (friendship.User1Id == currentUser.Id) // O utilizador atual enviou e foi rejeitado
                        return Ok(new { status = "rejected_sent", friendshipId = friendship.Id, message = "O seu pedido de amizade foi rejeitado." });
                    else // O utilizador atual recebeu e rejeitou
                        return Ok(new { status = "rejected_received", friendshipId = friendship.Id, message = "Rejeitou o pedido de amizade deste utilizador." });
                default:
                    return Ok(new { status = "unknown", message = "Estado de amizade desconhecido." });
            }
        }
    }    
}
