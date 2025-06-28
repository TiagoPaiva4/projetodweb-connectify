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
                CreatedAt = profileUser.Profile.CreatedAt
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

            var profileDto = new ProfileDto
            {
                Id = user.Profile.Id,
                Name = user.Profile.Name,
                Type = user.Profile.Type,
                Bio = user.Profile.Bio,
                ProfilePicture = user.Profile.ProfilePicture,
                Username = user.Username,
                CreatedAt = user.Profile.CreatedAt,
       
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
    }
}