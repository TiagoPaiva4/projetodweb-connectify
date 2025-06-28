using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations; // <-- CORREÇÃO 1: Adicionar este using

namespace projetodweb_connectify.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FriendshipsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FriendshipsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // DTO (Data Transfer Object) para o pedido de envio de amizade
        public class SendFriendRequestDto
        {
            [Required] // Este atributo agora será reconhecido
            public int TargetUserId { get; set; }
        }

        /// <summary>
        /// Obtém todas as amizades (aceites, pendentes, etc.) do utilizador atual.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Friendship>), 200)]
        public async Task<IActionResult> GetMyFriendships([FromQuery] FriendshipStatus? status)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            // CORREÇÃO 2: A ordem da consulta foi alterada.
            // Primeiro fazemos os Includes e ThenIncludes, e SÓ DEPOIS aplicamos o Where.
            var query = _context.Friendships
                .Include(f => f.User1)
                    .ThenInclude(u => u.Profile) // Agora isto funciona
                .Include(f => f.User2)
                    .ThenInclude(u => u.Profile) // E isto também
                .Where(f => f.User1Id == currentUser.Id || f.User2Id == currentUser.Id); // Filtro vem depois

            if (status.HasValue)
            {
                query = query.Where(f => f.Status == status.Value);
            }

            var friendships = await query.OrderByDescending(f => f.RequestDate).ToListAsync();

            return Ok(friendships);
        }

        /// <summary>
        /// Obtém o estado da amizade com um utilizador específico.
        /// </summary>
        [HttpGet("status/{otherUserId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetFriendshipStatus(int otherUserId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            if (currentUser.Id == otherUserId)
                return Ok(new { status = "self", message = "Este é o seu próprio perfil." });

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => (f.User1Id == currentUser.Id && f.User2Id == otherUserId) ||
                                          (f.User1Id == otherUserId && f.User2Id == currentUser.Id));

            if (friendship == null)
                return Ok(new { status = "not_friends", message = "Sem relação de amizade existente." });

            string specificStatus;
            switch (friendship.Status)
            {
                case FriendshipStatus.Accepted:
                    specificStatus = "friends";
                    break;
                case FriendshipStatus.Pending:
                    specificStatus = friendship.User1Id == currentUser.Id ? "pending_sent" : "pending_received";
                    break;
                case FriendshipStatus.Rejected:
                    specificStatus = friendship.User1Id == currentUser.Id ? "rejected_by_them" : "rejected_by_you";
                    break;
                default:
                    specificStatus = "unknown";
                    break;
            }

            return Ok(new { status = specificStatus, friendshipId = friendship.Id });
        }


        /// <summary>
        /// Envia um novo pedido de amizade para outro utilizador.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Friendship), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SendFriendRequest([FromBody] SendFriendRequestDto request)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            if (currentUser.Id == request.TargetUserId)
                return BadRequest(new { message = "Não pode enviar um pedido de amizade para si mesmo." });

            var targetUser = await _context.Users.FindAsync(request.TargetUserId);
            if (targetUser == null)
                return NotFound(new { message = "Utilizador alvo não encontrado." });

            var existingFriendship = await _context.Friendships
                .FirstOrDefaultAsync(f => (f.User1Id == currentUser.Id && f.User2Id == request.TargetUserId) ||
                                          (f.User1Id == request.TargetUserId && f.User2Id == currentUser.Id));

            if (existingFriendship != null)
            {
                if (existingFriendship.Status == FriendshipStatus.Accepted)
                    return BadRequest(new { message = "Já são amigos." });
                if (existingFriendship.Status == FriendshipStatus.Pending)
                    return BadRequest(new { message = "Já existe um pedido de amizade pendente." });

                _context.Friendships.Remove(existingFriendship);
            }

            var newFriendship = new Friendship
            {
                User1Id = currentUser.Id,
                User2Id = request.TargetUserId,
                Status = FriendshipStatus.Pending,
                RequestDate = DateTime.UtcNow
            };

            _context.Friendships.Add(newFriendship);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMyFriendships), new { id = newFriendship.Id }, newFriendship);
        }

        /// <summary>
        /// Aceita um pedido de amizade pendente.
        /// </summary>
        [HttpPost("{id}/accept")]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AcceptFriendRequest(int id)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.Id == id && f.Status == FriendshipStatus.Pending);

            if (friendship == null)
                return NotFound(new { message = "Pedido de amizade não encontrado ou já foi respondido." });

            if (friendship.User2Id != currentUser.Id)
                return Forbid();

            friendship.Status = FriendshipStatus.Accepted;
            friendship.AcceptanceDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Remove uma amizade, cancela um pedido enviado ou rejeita um pedido recebido.
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteFriendship(int id)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var friendship = await _context.Friendships.FindAsync(id);

            if (friendship == null)
                return NotFound();

            if (friendship.User1Id != currentUser.Id && friendship.User2Id != currentUser.Id)
                return Forbid();

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Helper
        private async Task<Users?> GetCurrentUserAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }
            return await _context.Users
                                 .Include(u => u.Profile)
                                 .FirstOrDefaultAsync(u => u.Username == username);
        }
    }
}