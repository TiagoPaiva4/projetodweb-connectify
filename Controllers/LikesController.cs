using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;

namespace projetodweb_connectify.Controllers
{
    // IMPORTANTE: Garante que apenas utilizadores autenticados podem executar estas a��es.
    [Authorize]
    public class LikesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LikesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// M�todo auxiliar para obter o ID do Perfil do utilizador atual.
        /// Resolve o problema de obter o ID (inteiro) do Perfil a partir do nome de utilizador (string) da identidade.
        /// </summary>
        /// <returns>O ID (inteiro) do Perfil do utilizador atual, ou null se n�o for encontrado.</returns>
        private int? GetCurrentProfileId()
        {
            // Passo 1: Obter o NOME DE UTILIZADOR a partir do cookie de autentica��o. Este � o nosso v�nculo fi�vel.
            var identityUserName = User.Identity?.Name;

            // Se n�o for poss�vel obter um nome de utilizador, o utilizador n�o est� devidamente autenticado.
            if (string.IsNullOrEmpty(identityUserName))
            {
                return null;
            }

            // Passo 2: Encontrar o utilizador na tabela personalizada 'Users' que corresponde a este nome de utilizador.
            var appUser = _context.Users.FirstOrDefault(u => u.Username == identityUserName);

            // Se nenhum utilizador for encontrado, significa que existe uma inconsist�ncia de dados.
            if (appUser == null)
            {
                return null;
            }

            // Passo 3: Com o ID do nosso utilizador, encontrar o perfil associado.
            // Esta � uma compara��o correta de 'int' para 'int'.
            var profile = _context.Profiles.FirstOrDefault(p => p.UserId == appUser.Id);

            // Passo 4: Devolver o ID do Perfil. Se n�o existir perfil, o resultado ser� nulo.
            return profile?.Id;
        }

        /// <summary>
        /// Gere a a��o de 'gostar' e 'n�o gostar' de uma publica��o.
        /// </summary>
        /// <param name="id">O ID da publica��o (TopicPost) a interagir.</param>
        [HttpPost]
        [ValidateAntiForgeryToken] // Medida de seguran�a contra ataques CSRF.
        public async Task<IActionResult> TogglePostLike(int id)
        {
            var profileId = GetCurrentProfileId();
            if (profileId == null)
            {
                // Se n�o for poss�vel obter um ID de perfil, o utilizador n�o tem autoriza��o.
                return Unauthorized(new { message = "� necess�rio estar autenticado para gostar de uma publica��o." });
            }

            // Verificar se j� existe um 'gosto' deste perfil para esta publica��o.
            var existingLike = await _context.TopicPostLikes
                .FirstOrDefaultAsync(l => l.TopicPostId == id && l.ProfileId == profileId.Value);

            if (existingLike != null)
            {
                // Se j� existe, o utilizador est� a anular o 'gosto'. Remove-se o 'gosto' existente.
                _context.TopicPostLikes.Remove(existingLike);
            }
            else
            {
                // Caso contr�rio, o utilizador est� a 'gostar' da publica��o. Adiciona-se um novo 'gosto'.
                var newLike = new TopicPostLike
                {
                    TopicPostId = id,
                    ProfileId = profileId.Value
                };
                _context.TopicPostLikes.Add(newLike);
            }

            await _context.SaveChangesAsync(); // Grava as altera��es na base de dados.

            // Obter a nova contagem atualizada de 'gostos' para a publica��o.
            var newLikeCount = await _context.TopicPostLikes.CountAsync(l => l.TopicPostId == id);

            // Devolver a nova contagem para o front-end (via JSON).
            return Json(new { count = newLikeCount });
        }

        /// <summary>
        /// Gere a a��o de 'gostar' e 'n�o gostar' de um coment�rio.
        /// </summary>
        /// <param name="id">O ID do coment�rio (TopicComment) a interagir.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCommentLike(int id)
        {
            var profileId = GetCurrentProfileId();
            if (profileId == null)
            {
                return Unauthorized(new { message = "� necess�rio estar autenticado para gostar de um coment�rio." });
            }

            // Verificar se j� existe um 'gosto' deste perfil para este coment�rio.
            var existingLike = await _context.TopicCommentLikes
                .FirstOrDefaultAsync(l => l.TopicCommentId == id && l.ProfileId == profileId.Value);

            if (existingLike != null)
            {
                // Anular o 'gosto' no coment�rio.
                _context.TopicCommentLikes.Remove(existingLike);
            }
            else
            {
                // Dar 'gosto' ao coment�rio.
                var newLike = new TopicCommentLike
                {
                    TopicCommentId = id,
                    ProfileId = profileId.Value
                };
                _context.TopicCommentLikes.Add(newLike);
            }

            await _context.SaveChangesAsync();

            // Obter a nova contagem atualizada de 'gostos' para o coment�rio.
            var newLikeCount = await _context.TopicCommentLikes.CountAsync(l => l.TopicCommentId == id);

            // Devolver a nova contagem para o front-end.
            return Json(new { count = newLikeCount });
        }
    }
}