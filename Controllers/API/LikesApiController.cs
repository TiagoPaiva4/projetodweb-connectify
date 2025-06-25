using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using projetodweb_connectify.Models.DTOs;
using System.Linq;
using System.Threading.Tasks;

namespace projetodweb_connectify.Controllers.API
{
    [Route("api/likes")]
    [ApiController]
    [Authorize] // Exige que o utilizador esteja autenticado para todas as ações
    public class LikesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LikesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/likes/post/5
        /// <summary>
        /// Adiciona ou remove um "like" de um post (TopicPost).
        /// </summary>
        /// <param name="id">O ID do TopicPost a gostar/desgostar.</param>
        /// <returns>Um objeto com a nova contagem de "likes" e o estado atual.</returns>
        [HttpPost("post/{id}")]
        public async Task<ActionResult<LikeToggleResultDto>> TogglePostLike(int id)
        {
            var profileId = GetCurrentProfileId();
            if (profileId == null)
            {
                return Unauthorized(new { message = "Sessão inválida. Por favor, faça login novamente." });
            }

            // Verifica se o TopicPost existe para evitar erros
            var postExists = await _context.TopicPosts.AnyAsync(p => p.Id == id);
            if (!postExists)
            {
                return NotFound(new { message = "O post não foi encontrado." });
            }

            var existingLike = await _context.TopicPostLikes
                .FirstOrDefaultAsync(l => l.TopicPostId == id && l.ProfileId == profileId.Value);

            bool wasLiked;

            if (existingLike != null)
            {
                // Já existe, então remove (unlike)
                _context.TopicPostLikes.Remove(existingLike);
                wasLiked = false; // A ação resultou em "não gostado"
            }
            else
            {
                // Não existe, então adiciona (like)
                var newLike = new TopicPostLike
                {
                    TopicPostId = id,
                    ProfileId = profileId.Value
                };
                _context.TopicPostLikes.Add(newLike);
                wasLiked = true; // A ação resultou em "gostado"
            }

            await _context.SaveChangesAsync();

            var newLikeCount = await _context.TopicPostLikes.CountAsync(l => l.TopicPostId == id);

            var result = new LikeToggleResultDto
            {
                NewLikeCount = newLikeCount,
                WasLiked = wasLiked
            };

            return Ok(result);
        }

        // POST: api/likes/comment/10
        /// <summary>
        /// Adiciona ou remove um "like" de um comentário.
        /// </summary>
        /// <param name="id">O ID do TopicComment a gostar/desgostar.</param>
        /// <returns>Um objeto com a nova contagem de "likes" e o estado atual.</returns>
        [HttpPost("comment/{id}")]
        public async Task<ActionResult<LikeToggleResultDto>> ToggleCommentLike(int id)
        {
            var profileId = GetCurrentProfileId();
            if (profileId == null)
            {
                return Unauthorized(new { message = "Sessão inválida. Por favor, faça login novamente." });
            }

            var commentExists = await _context.TopicComments.AnyAsync(c => c.Id == id);
            if (!commentExists)
            {
                return NotFound(new { message = "O comentário não foi encontrado." });
            }

            var existingLike = await _context.TopicCommentLikes
                .FirstOrDefaultAsync(l => l.TopicCommentId == id && l.ProfileId == profileId.Value);

            bool wasLiked;

            if (existingLike != null)
            {
                _context.TopicCommentLikes.Remove(existingLike);
                wasLiked = false;
            }
            else
            {
                var newLike = new TopicCommentLike
                {
                    TopicCommentId = id,
                    ProfileId = profileId.Value
                };
                _context.TopicCommentLikes.Add(newLike);
                wasLiked = true;
            }

            await _context.SaveChangesAsync();

            var newLikeCount = await _context.TopicCommentLikes.CountAsync(l => l.TopicCommentId == id);

            var result = new LikeToggleResultDto
            {
                NewLikeCount = newLikeCount,
                WasLiked = wasLiked
            };

            return Ok(result);
        }

        #region Helper Methods
        /// <summary>
        /// Método auxiliar para obter o ID do Perfil do utilizador autenticado.
        /// Esta lógica é específica da sua implementação de utilizadores.
        /// </summary>
        private int? GetCurrentProfileId()
        {
            var identityUserName = User.Identity?.Name;
            if (string.IsNullOrEmpty(identityUserName)) return null;

            // Esta consulta pode ser otimizada para evitar duas chamadas à BD
            var profileId = _context.Users
                .Where(u => u.Username == identityUserName)
                .Select(u => u.Profile.Id) // Seleciona diretamente o ID do perfil associado
                .FirstOrDefault();

            return profileId == 0 ? null : profileId;
        }
        #endregion
    }
}