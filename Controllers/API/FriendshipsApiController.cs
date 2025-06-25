using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using projetodweb_connectify.Models.DTOs; // Importar os DTOs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace projetodweb_connectify.Controllers.API
{
    //[ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/friendships")]
    [ApiController]
    [Authorize] // Todas as ações aqui exigem autenticação
    public class FriendshipsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FriendshipsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém a lista de amigos aceites do utilizador atual.
        /// </summary>
        [HttpGet("friends")]
        public async Task<ActionResult<IEnumerable<FriendshipDto>>> GetFriends()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var friends = await _context.Friendships
                .Where(f => f.Status == FriendshipStatus.Accepted && (f.User1Id == currentUser.Id || f.User2Id == currentUser.Id))
                .Include(f => f.User1.Profile)
                .Include(f => f.User2.Profile)
                .Select(f => new FriendshipDto
                {
                    FriendshipId = f.Id,
                    Status = f.Status,
                    RequestDate = f.RequestDate,
                    // Determina qual dos dois utilizadores é o "amigo"
                    FriendUserId = f.User1Id == currentUser.Id ? f.User2Id : f.User1Id,
                    FriendUsername = f.User1Id == currentUser.Id ? f.User2.Username : f.User1.Username,
                    FriendProfileImageUrl = f.User1Id == currentUser.Id ? f.User2.Profile.ProfilePicture : f.User1.Profile.ProfilePicture
                })
                .OrderBy(dto => dto.FriendUsername)
                .ToListAsync();

            return Ok(friends);
        }

        /// <summary>
        /// Obtém a lista de pedidos de amizade pendentes recebidos pelo utilizador atual.
        /// </summary>
        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<FriendshipDto>>> GetPendingRequests()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var pendingRequests = await _context.Friendships
                .Where(f => f.User2Id == currentUser.Id && f.Status == FriendshipStatus.Pending)
                .Include(f => f.User1.Profile)
                .Select(f => new FriendshipDto
                {
                    FriendshipId = f.Id,
                    FriendUserId = f.User1Id,
                    FriendUsername = f.User1.Username,
                    FriendProfileImageUrl = f.User1.Profile.ProfilePicture,
                    Status = f.Status,
                    RequestDate = f.RequestDate
                })
                .OrderByDescending(dto => dto.RequestDate)
                .ToListAsync();

            return Ok(pendingRequests);
        }

        /// <summary>
        /// Envia um pedido de amizade para outro utilizador.
        /// </summary>
        /// <param name="targetUserId">O ID do utilizador a quem enviar o pedido.</param>
        [HttpPost("request/{targetUserId}")]
        public async Task<IActionResult> SendFriendRequest(int targetUserId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized(new { message = "Utilizador não autenticado." });
            if (currentUser.Id == targetUserId) return BadRequest(new { message = "Não pode adicionar-se a si mesmo." });

            var existing = await _context.Friendships
                .FirstOrDefaultAsync(f => (f.User1Id == currentUser.Id && f.User2Id == targetUserId) || (f.User1Id == targetUserId && f.User2Id == currentUser.Id));

            if (existing != null)
            {
                return Conflict(new { message = "Já existe uma relação (amizade ou pedido pendente) com este utilizador." });
            }

            var friendship = new Friendship { User1Id = currentUser.Id, User2Id = targetUserId, Status = FriendshipStatus.Pending, RequestDate = DateTime.UtcNow };
            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Pedido de amizade enviado.", friendshipId = friendship.Id });
        }

        /// <summary>
        /// Aceita um pedido de amizade pendente.
        /// </summary>
        /// <param name="friendshipId">O ID da amizade (o pedido).</param>
        [HttpPost("accept/{friendshipId}")]
        public async Task<IActionResult> AcceptFriendRequest(int friendshipId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.Id == friendshipId && f.User2Id == currentUser.Id && f.Status == FriendshipStatus.Pending);

            if (friendship == null) return NotFound(new { message = "Pedido de amizade não encontrado ou não autorizado." });

            friendship.Status = FriendshipStatus.Accepted;
            friendship.AcceptanceDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Pedido de amizade aceite." });
        }

        /// <summary>
        /// Rejeita um pedido de amizade pendente. A ação remove o registo do pedido.
        /// </summary>
        /// <param name="friendshipId">O ID do pedido de amizade a rejeitar.</param>
        [HttpPost("reject/{friendshipId}")]
        public async Task<IActionResult> RejectFriendRequest(int friendshipId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.Id == friendshipId && f.User2Id == currentUser.Id && f.Status == FriendshipStatus.Pending);

            if (friendship == null) return NotFound(new { message = "Pedido de amizade não encontrado ou não autorizado." });

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Pedido de amizade rejeitado." });
        }

        /// <summary>
        /// Remove uma amizade existente ou cancela um pedido enviado.
        /// </summary>
        /// <param name="friendshipId">O ID da amizade a remover/cancelar.</param>
        [HttpDelete("{friendshipId}")]
        public async Task<IActionResult> RemoveOrCancelFriendship(int friendshipId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.Id == friendshipId && (f.User1Id == currentUser.Id || f.User2Id == currentUser.Id));

            if (friendship == null) return NotFound(new { message = "Amizade não encontrada." });

            // Apenas o remetente pode cancelar um pedido pendente, mas ambos podem terminar uma amizade aceite.
            if (friendship.Status == FriendshipStatus.Pending && friendship.User2Id == currentUser.Id)
            {
                return Forbid("Não pode cancelar um pedido que recebeu. Deve aceitar ou rejeitar.");
            }

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
            return NoContent(); // Sucesso
        }

        /// <summary>
        /// Verifica o estado da amizade com outro utilizador.
        /// </summary>
        /// <param name="otherUserId">O ID do outro utilizador.</param>
        [HttpGet("status/{otherUserId}")]
        public async Task<ActionResult<FriendshipStatusDto>> GetFriendshipStatus(int otherUserId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();
            if (currentUser.Id == otherUserId) return Ok(new FriendshipStatusDto { Status = "self", Message = "Este é o seu próprio perfil." });

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => (f.User1Id == currentUser.Id && f.User2Id == otherUserId) || (f.User1Id == otherUserId && f.User2Id == currentUser.Id));

            if (friendship == null) return Ok(new FriendshipStatusDto { Status = "not_friends", Message = "Sem relação de amizade." });

            switch (friendship.Status)
            {
                case FriendshipStatus.Accepted:
                    return Ok(new FriendshipStatusDto { Status = "friends", FriendshipId = friendship.Id, Message = "São amigos." });
                case FriendshipStatus.Pending:
                    var status = friendship.User1Id == currentUser.Id ? "pending_sent" : "pending_received";
                    var message = status == "pending_sent" ? "Pedido enviado." : "Recebeu um pedido.";
                    return Ok(new FriendshipStatusDto { Status = status, FriendshipId = friendship.Id, Message = message });
                default: // Rejected, Blocked etc.
                    return Ok(new FriendshipStatusDto { Status = "not_friends", Message = "Sem amizade ativa." });
            }
        }

        // Helper para obter o nosso Users customizado
        private async Task<Users?> GetCurrentUserAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;
            return await _context.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Username == username);
        }
    }
}