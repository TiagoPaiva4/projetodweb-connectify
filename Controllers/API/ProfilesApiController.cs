using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using projetodweb_connectify.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace projetodweb_connectify.Controllers.API
{
    [Route("api/profiles")]
    [ApiController]
    [Authorize]
    public class ProfilesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProfilesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Devolve uma lista resumida de todos os perfis de utilizadores (exceto o próprio).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProfileSummaryDto>>> GetAllProfiles()
        {
            // Obtém o nome de utilizador do utilizador autenticado para o excluir da lista
            var currentUsername = User.Identity?.Name;

            var profiles = await _context.Users
                .AsNoTracking()
                .Where(u => u.Profile != null && u.Username != currentUsername) // Garante que o utilizador tem perfil e não é o próprio
                .OrderBy(u => u.Profile.Name) // Ordena por nome de perfil para consistência
                .Select(u => new ProfileSummaryDto
                {
                    // Selecionamos apenas os campos necessários para a lista de exploração
                    Username = u.Username,
                    Name = u.Profile.Name,
                    ProfilePicture = u.Profile.ProfilePicture,
                    Bio = u.Profile.Bio,
                    Type = u.Profile.Type
                    // Nota: O FriendshipStatus foi removido deste DTO ou será nulo
                })
                .ToListAsync();

            return Ok(profiles);
        }

        /// <summary>
        /// Devolve os dados públicos de um perfil específico pelo username.
        /// </summary>
        /// <summary>
        /// Devolve os dados públicos de um perfil específico pelo username.
        /// </summary>
        [HttpGet("{username}")]
        public async Task<ActionResult<ProfileDto>> GetProfileByUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
                return BadRequest("O nome de utilizador é obrigatório.");

            // Encontrar o utilizador logado pelo seu nome (username)
            var loggedInUsername = User.Identity?.Name;
            if (string.IsNullOrEmpty(loggedInUsername))
                return Unauthorized("Não foi possível identificar o utilizador logado.");

            var loggedInUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == loggedInUsername);

            if (loggedInUser == null)
                return Unauthorized("Utilizador logado não encontrado na base de dados.");

            // Encontrar o utilizador do perfil que está a ser visitado
            var profileUser = await _context.Users
                .AsNoTracking()
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (profileUser == null || profileUser.Profile == null)
                return NotFound(new { message = $"Perfil para '{username}' não encontrado." });

            // ======================= INÍCIO DA ALTERAÇÃO =======================
            // Contar o número de amizades aceites para o UTILIZADOR DO PERFIL VISITADO.
            var friendsCount = await _context.Friendships
                .AsNoTracking()
                .CountAsync(f =>
                    (f.User1Id == profileUser.Id || f.User2Id == profileUser.Id) &&
                    f.Status == FriendshipStatus.Accepted
                );
            // ======================= FIM DA ALTERAÇÃO =======================

            // =========== INÍCIO DA LÓGICA DE CORREÇÃO ===========

            // 1. Criar o DTO e preencher os novos campos
            var profileDto = new ProfileDto
            {
                Id = profileUser.Profile.Id, // ProfileId
                UserId = profileUser.Id,     // UserId (int)
                Name = profileUser.Profile.Name,
                Type = profileUser.Profile.Type,
                Bio = profileUser.Profile.Bio,
                ProfilePicture = profileUser.Profile.ProfilePicture,
                Username = profileUser.Username,
                CreatedAt = profileUser.Profile.CreatedAt,
                FriendsCount = friendsCount
            };

            // 2. Determinar o estado da amizade
            if (loggedInUser.Id == profileUser.Id)
            {
                profileDto.FriendshipStatus = "self";
            }
            else
            {
                // Verifica se existe uma amizade entre o utilizador logado e o do perfil
                var friendship = await _context.Friendships
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f =>
                        (f.User1Id == loggedInUser.Id && f.User2Id == profileUser.Id) ||
                        (f.User1Id == profileUser.Id && f.User2Id == loggedInUser.Id));

                if (friendship == null)
                {
                    profileDto.FriendshipStatus = "not_friends";
                }
                else
                {
                    switch (friendship.Status)
                    {
                        case FriendshipStatus.Accepted:
                            profileDto.FriendshipStatus = "friends";
                            break;
                        case FriendshipStatus.Pending:
                            // Se o utilizador logado foi quem enviou o pedido (User1)
                            if (friendship.User1Id == loggedInUser.Id)
                            {
                                profileDto.FriendshipStatus = "pending_sent";
                            }
                            else // Se o utilizador logado recebeu o pedido (User2)
                            {
                                profileDto.FriendshipStatus = "pending_received";
                            }
                            break;
                        case FriendshipStatus.Rejected:
                            // Na sua UI, "rejeitado" funciona como "não amigos" para um novo pedido.
                            profileDto.FriendshipStatus = "not_friends";
                            break;
                        // Adicionar outros casos se necessário (Blocked, etc.)
                        default:
                            profileDto.FriendshipStatus = "not_friends";
                            break;
                    }
                }
            }

            // 3. Lógica para obter posts do mural pessoal (públicos)
            var personalTopic = await _context.Topics
                .AsNoTracking()
                .Include(t => t.Posts)
                .Where(t => t.CreatedBy == profileUser.Profile.Id && t.IsPersonal)
                .FirstOrDefaultAsync();

            if (personalTopic != null && personalTopic.Posts.Any())
            {
                profileDto.PersonalTopicPosts = personalTopic.Posts
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new TopicPostDto
                    {
                        Id = p.Id,
                        TopicId = p.TopicId, // <-- PREENCHER O TopicId
                        Content = p.Content,
                        CreatedAt = p.CreatedAt,
                        PostImageUrl = p.PostImageUrl
                    })
                    .ToList();
            }

            // ... (A lógica para CreatedTopics permanece a mesma) ...
            profileDto.CreatedTopics = await _context.Topics
                .AsNoTracking()
                .Where(t => t.CreatedBy == profileUser.Profile.Id && !t.IsPersonal)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TopicSummaryDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    TopicImageUrl = t.TopicImageUrl,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            // =========== FIM DA LÓGICA DE CORREÇÃO ===========

            return Ok(profileDto);
        }


        /// <summary>
        /// Devolve os dados completos do perfil do utilizador autenticado.
        /// </summary>
        [HttpGet("me")]
        public async Task<ActionResult<ProfileDto>> GetMyProfile()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || user.Profile == null)
                return NotFound(new { message = "Perfil não encontrado." });

            // ======================= INÍCIO DA ALTERAÇÃO =======================
            // 1. Contar o número de amizades aceites para este utilizador.
            //    Uma amizade é contada se o status for 'Accepted' e o ID do utilizador
            //    estiver em User1Id ou User2Id.
            var friendsCount = await _context.Friendships
                .AsNoTracking()
                .CountAsync(f =>
                    (f.User1Id == user.Id || f.User2Id == user.Id) &&
                    f.Status == FriendshipStatus.Accepted
                );
            // ======================= FIM DA ALTERAÇÃO =======================

            var profileDto = new ProfileDto
            {
                Id = user.Profile.Id,
                Name = user.Profile.Name,
                Type = user.Profile.Type,
                Bio = user.Profile.Bio,
                ProfilePicture = user.Profile.ProfilePicture,
                Username = user.Username,
                CreatedAt = user.Profile.CreatedAt,
                FriendsCount = friendsCount
            };

            // O resto da sua lógica para obter posts, tópicos criados e tópicos guardados
            // permanece aqui, pois pertence ao perfil do próprio utilizador.

            var personalTopic = await _context.Topics
                .AsNoTracking()
                .Include(t => t.Posts)
                .Where(t => t.CreatedBy == user.Profile.Id && t.IsPersonal)
                .FirstOrDefaultAsync();

            if (personalTopic != null && personalTopic.Posts.Any())
            {
                profileDto.PersonalTopicPosts = personalTopic.Posts
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new TopicPostDto { Id = p.Id, Content = p.Content, CreatedAt = p.CreatedAt, PostImageUrl = p.PostImageUrl })
                    .ToList();
            }

            profileDto.CreatedTopics = await _context.Topics
                .AsNoTracking()
                .Where(t => t.CreatedBy == user.Profile.Id && !t.IsPersonal)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TopicSummaryDto { Id = t.Id, Title = t.Title, TopicImageUrl = t.TopicImageUrl, CreatedAt = t.CreatedAt })
                .ToListAsync();

            profileDto.SavedTopics = await _context.SavedTopics
                .AsNoTracking()
                .Where(st => st.ProfileId == user.Profile.Id && st.Topic != null)
                .Include(st => st.Topic)
                .OrderByDescending(st => st.SavedAt)
                .Select(st => new SavedTopicDto { TopicId = st.Topic.Id, Title = st.Topic.Title, TopicImageUrl = st.Topic.TopicImageUrl, SavedAt = st.SavedAt })
                .ToListAsync();

            return Ok(profileDto);
        }


        /// <summary>
        /// Atualiza o perfil do utilizador autenticado.
        /// </summary>
        /// <remarks>
        /// Este método aceita dados de formulário (multipart/form-data) para permitir o upload de imagem.
        /// </remarks>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromForm] ProfileUpdateDto profileUpdateDto)
        {
            // 1. Obter o utilizador logado
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (appUser == null)
            {
                return NotFound(new { message = "Utilizador não encontrado." });
            }

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);
            if (profile == null)
            {
                return NotFound(new { message = "Perfil não encontrado." });
            }

            // 2. Atualizar as propriedades do perfil com os dados do DTO
            profile.Name = profileUpdateDto.Name;
            profile.Bio = profileUpdateDto.Bio;

            // 3. Lógica para upload de imagem (exatamente como no seu controller MVC)
            if (profileUpdateDto.ProfilePictureFile != null && profileUpdateDto.ProfilePictureFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(profileUpdateDto.ProfilePictureFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profileUpdateDto.ProfilePictureFile.CopyToAsync(fileStream);
                }
                profile.ProfilePicture = "/images/profiles/" + uniqueFileName;
            }

            // 4. Salvar as alterações
            try
            {
                _context.Update(profile);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "Ocorreu um conflito de concorrência. Tente novamente." });
            }

            // 5. Retornar o perfil atualizado (opcional, mas boa prática)
            // Para isso, precisaríamos de remapear para o ProfileDto, mas por simplicidade, vamos retornar NoContent.
            return NoContent(); // HTTP 204 - Sucesso, sem conteúdo para retornar
        }
    }
}