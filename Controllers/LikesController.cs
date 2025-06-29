using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;

namespace projetodweb_connectify.Controllers
{
    // IMPORTANTE: Garante que apenas utilizadores autenticados podem executar estas ações.
    [Authorize]
    public class LikesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LikesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Método auxiliar para obter o ID do Perfil do utilizador atual.
        /// Resolve o problema de obter o ID (inteiro) do Perfil a partir do nome de utilizador (string) da identidade.
        /// </summary>
        /// <returns>O ID (inteiro) do Perfil do utilizador atual, ou null se não for encontrado.</returns>
        private int? GetCurrentProfileId()
        {
            // Passo 1: Obter o NOME DE UTILIZADOR a partir do cookie de autenticação. Este é o nosso vínculo fiável.
            var identityUserName = User.Identity?.Name;

            // Se não for possível obter um nome de utilizador, o utilizador não está devidamente autenticado.
            if (string.IsNullOrEmpty(identityUserName))
            {
                return null;
            }

            // Passo 2: Encontrar o utilizador na tabela personalizada 'Users' que corresponde a este nome de utilizador.
            var appUser = _context.Users.FirstOrDefault(u => u.Username == identityUserName);

            // Se nenhum utilizador for encontrado, significa que existe uma inconsistência de dados.
            if (appUser == null)
            {
                return null;
            }

            // Passo 3: Com o ID do nosso utilizador, encontrar o perfil associado.
            // Esta é uma comparação correta de 'int' para 'int'.
            var profile = _context.Profiles.FirstOrDefault(p => p.UserId == appUser.Id);

            // Passo 4: Devolver o ID do Perfil. Se não existir perfil, o resultado será nulo.
            return profile?.Id;
        }

        /// <summary>
        /// Gere a ação de 'gostar' e 'não gostar' de uma publicação.
        /// </summary>
        /// <param name="id">O ID da publicação (TopicPost) a interagir.</param>
        [HttpPost]
        [ValidateAntiForgeryToken] // Medida de segurança contra ataques CSRF.
        public async Task<IActionResult> TogglePostLike(int id)
        {
            var profileId = GetCurrentProfileId();
            if (profileId == null)
            {
                // Se não for possível obter um ID de perfil, o utilizador não tem autorização.
                return Unauthorized(new { message = "É necessário estar autenticado para gostar de uma publicação." });
            }

            // Verificar se já existe um 'gosto' deste perfil para esta publicação.
            var existingLike = await _context.TopicPostLikes
                .FirstOrDefaultAsync(l => l.TopicPostId == id && l.ProfileId == profileId.Value);

            if (existingLike != null)
            {
                // Se já existe, o utilizador está a anular o 'gosto'. Remove-se o 'gosto' existente.
                _context.TopicPostLikes.Remove(existingLike);
            }
            else
            {
                // Caso contrário, o utilizador está a 'gostar' da publicação. Adiciona-se um novo 'gosto'.
                var newLike = new TopicPostLike
                {
                    TopicPostId = id,
                    ProfileId = profileId.Value
                };
                _context.TopicPostLikes.Add(newLike);
            }

            await _context.SaveChangesAsync(); // Grava as alterações na base de dados.

            // Obter a nova contagem atualizada de 'gostos' para a publicação.
            var newLikeCount = await _context.TopicPostLikes.CountAsync(l => l.TopicPostId == id);

            // Devolver a nova contagem para o front-end (via JSON).
            return Json(new { count = newLikeCount });
        }

        /// <summary>
        /// Gere a ação de 'gostar' e 'não gostar' de um comentário.
        /// </summary>
        /// <param name="id">O ID do comentário (TopicComment) a interagir.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCommentLike(int id)
        {
            var profileId = GetCurrentProfileId();
            if (profileId == null)
            {
                return Unauthorized(new { message = "É necessário estar autenticado para gostar de um comentário." });
            }

            // Verificar se já existe um 'gosto' deste perfil para este comentário.
            var existingLike = await _context.TopicCommentLikes
                .FirstOrDefaultAsync(l => l.TopicCommentId == id && l.ProfileId == profileId.Value);

            if (existingLike != null)
            {
                // Anular o 'gosto' no comentário.
                _context.TopicCommentLikes.Remove(existingLike);
            }
            else
            {
                // Dar 'gosto' ao comentário.
                var newLike = new TopicCommentLike
                {
                    TopicCommentId = id,
                    ProfileId = profileId.Value
                };
                _context.TopicCommentLikes.Add(newLike);
            }

            await _context.SaveChangesAsync();

            // Obter a nova contagem atualizada de 'gostos' para o comentário.
            var newLikeCount = await _context.TopicCommentLikes.CountAsync(l => l.TopicCommentId == id);

            // Devolver a nova contagem para o front-end.
            return Json(new { count = newLikeCount });
        }
    }
}