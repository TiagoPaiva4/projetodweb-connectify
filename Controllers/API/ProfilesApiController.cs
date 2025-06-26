using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using projetodweb_connectify.Models.DTOs;

namespace projetodweb_connectify.Controllers.API
{
    [Route("api/profiles")]
    [ApiController]
    [Authorize] // Proteger todos os endpoints da API por padrão
    public class ProfilesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProfilesApiController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        /// <summary>
        /// Procura por utilizadores/perfis, excluindo o próprio utilizador.
        /// GET: /api/profiles?searchQuery=...
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProfileSummaryDto>>> SearchProfiles([FromQuery] string searchQuery)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var query = _context.Profiles
                .Include(p => p.User)
                .Where(p => p.UserId != currentUser.Id); // Excluir o próprio utilizador da pesquisa

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(p => p.User.Username.Contains(searchQuery) || p.Name.Contains(searchQuery));
            }

            var profiles = await query
                .OrderBy(p => p.Name)
                .Select(p => new ProfileSummaryDto
                {
                    ProfileId = p.Id,
                    UserId = p.UserId,
                    Username = p.User.Username,
                    Name = p.Name,
                    ProfilePictureUrl = p.ProfilePicture
                })
                .ToListAsync();

            return Ok(profiles);
        }

        /// <summary>
        /// Obtém os dados detalhados do perfil do utilizador atualmente autenticado.
        /// GET: /api/profiles/me
        /// </summary>
        [HttpGet("me")]
        public async Task<ActionResult<MyProfileDetailDto>> GetMyProfile()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized(new { message = "Utilizador não autenticado." });

            var profile = await _context.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);

            if (profile == null) return NotFound(new { message = "Perfil não encontrado. Por favor, crie um." });

            var myProfileDto = new MyProfileDetailDto
            {
                ProfileId = profile.Id,
                UserId = profile.UserId,
                Username = profile.User.Username,
                Name = profile.Name,
                Bio = profile.Bio,
                Type = profile.Type,
                ProfilePictureUrl = profile.ProfilePicture,
                CreatedAt = profile.CreatedAt,
                Friends = await LoadFriendsDtoAsync(currentUser.Id),
                SavedTopics = await LoadSavedTopicsDtoAsync(profile.Id),
                CreatedTopics = await LoadCreatedTopicsDtoAsync(profile.Id)
            };

            return Ok(myProfileDto);
        }

        /// <summary>
        /// Obtém os dados públicos de um perfil de utilizador, especificado pelo seu ID de Perfil.
        /// GET: /api/profiles/5
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserProfileDetailDto>> GetUserProfile(int id)
        {
            var profile = await _context.Profiles
                .AsNoTracking() // Melhor performance para leitura
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null) return NotFound(new { message = "Perfil não encontrado." });

            var currentUser = await GetCurrentUserAsync();

            var userProfileDto = new UserProfileDetailDto
            {
                ProfileId = profile.Id,
                UserId = profile.UserId,
                Username = profile.User.Username,
                Name = profile.Name,
                Bio = profile.Bio,
                Type = profile.Type,
                ProfilePictureUrl = profile.ProfilePicture,
                CreatedAt = profile.CreatedAt,
                CreatedTopics = await LoadCreatedTopicsDtoAsync(profile.Id),
                FriendshipStatus = await GetFriendshipStatusAsync(profile.UserId, currentUser?.Id)
            };

            return Ok(userProfileDto);
        }

        /// <summary>
        /// Cria um novo perfil para o utilizador autenticado.
        /// POST: /api/profiles
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateProfile([FromForm] ProfileCreateDto createDto)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            if (await _context.Profiles.AnyAsync(p => p.UserId == currentUser.Id))
            {
                return Conflict(new { message = "Já existe um perfil para este utilizador." });
            }

            var profile = new Profile
            {
                Name = createDto.Name,
                Type = createDto.Type,
                Bio = createDto.Bio,
                UserId = currentUser.Id,
                CreatedAt = DateTime.UtcNow,
                ProfilePicture = "/images/defaultuser.png" // Default picture
            };

            if (createDto.ProfilePictureFile != null)
            {
                profile.ProfilePicture = await SaveImage(createDto.ProfilePictureFile);
            }

            // Lógica para criar o Tópico Pessoal, igual ao ProfilesController
            var personalTopic = new Topic
            {
                Title = $"Perfil de {profile.Name ?? currentUser.Username}",
                Description = $"Posts pessoais de {profile.Name ?? currentUser.Username}.",
                IsPersonal = true,
                IsPrivate = true, // Tópicos pessoais devem ser privados por padrão
                Creator = profile,
                CreatedAt = DateTime.UtcNow
            };
            profile.PersonalTopic = personalTopic;

            _context.Profiles.Add(profile); // Adiciona o perfil e o tópico pessoal associado
            await _context.SaveChangesAsync();

            // Retorna o perfil recém-criado, chamando a action GetUserProfile
            return CreatedAtAction(nameof(GetUserProfile), new { id = profile.Id }, profile);
        }

        /// <summary>
        /// Atualiza o perfil do utilizador autenticado.
        /// PUT: /api/profiles/me
        /// </summary>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromForm] ProfileEditDto editDto)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var profileToUpdate = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
            if (profileToUpdate == null) return NotFound(new { message = "Perfil não encontrado para atualização." });

            profileToUpdate.Name = editDto.Name;
            profileToUpdate.Bio = editDto.Bio;
            profileToUpdate.Type = editDto.Type;

            if (editDto.ProfilePictureFile != null)
            {
                // Apaga a imagem antiga antes de guardar a nova
                DeleteImage(profileToUpdate.ProfilePicture);
                profileToUpdate.ProfilePicture = await SaveImage(editDto.ProfilePictureFile);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Verifica se o perfil ainda existe
                if (!await _context.Profiles.AnyAsync(p => p.Id == profileToUpdate.Id))
                {
                    return NotFound();
                }
                else
                {
                    return Conflict(new { message = "Os dados foram modificados por outro utilizador. Recarregue e tente novamente." });
                }
            }

            return NoContent(); // Sucesso
        }


        /// <summary>
        /// Exclui o perfil do utilizador autenticado e todos os dados associados.
        /// DELETE: /api/profiles/me
        /// </summary>
        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMyProfile()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var profile = await _context.Profiles
                .Include(p => p.PersonalTopic)
                .Include(p => p.SavedTopics)
                .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);

            if (profile == null) return NotFound(new { message = "Perfil não encontrado para exclusão." });

            try
            {
                // 1. Remove SavedTopic links
                if (profile.SavedTopics.Any())
                {
                    _context.SavedTopics.RemoveRange(profile.SavedTopics);
                }

                // 2. Remove o Tópico Pessoal (e os seus posts se a cascata estiver configurada)
                if (profile.PersonalTopic != null)
                {
                    // Se a cascata Topic -> TopicPost não estiver a funcionar, é preciso remover os posts manualmente
                    var personalTopicPosts = await _context.TopicPosts.Where(tp => tp.TopicId == profile.PersonalTopic.Id).ToListAsync();
                    if (personalTopicPosts.Any())
                    {
                        _context.TopicPosts.RemoveRange(personalTopicPosts);
                    }
                    _context.Topics.Remove(profile.PersonalTopic);
                }

                // 3. Finalmente, remove o perfil
                _context.Profiles.Remove(profile);

                // 4. Apaga a imagem de perfil do servidor
                DeleteImage(profile.ProfilePicture);

                await _context.SaveChangesAsync();

                // Idealmente, o cliente deve deslogar o utilizador após receber esta resposta
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                // Erro de FK, provavelmente porque o utilizador ainda é criador de outros tópicos, etc.
                return Conflict(new { message = "Não foi possível excluir o perfil. Pode ser necessário remover manualmente os tópicos criados ou posts associados primeiro.", error = ex.InnerException?.Message });
            }
        }

        #region Helper Methods

        private async Task<Users?> GetCurrentUserAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        private async Task<List<FriendshipDto>> LoadFriendsDtoAsync(int currentUserId)
        {
            var friendships = await _context.Friendships
                .Where(f => f.Status == FriendshipStatus.Accepted && (f.User1Id == currentUserId || f.User2Id == currentUserId))
                .Include(f => f.User1.Profile)
                .Include(f => f.User2.Profile)
                .ToListAsync();

            return friendships.Select(f => {
                // Determina qual dos utilizadores na amizade é o "amigo" (e não o utilizador atual)
                var friendUser = f.User1Id == currentUserId ? f.User2 : f.User1;

                // Mapeia para o seu FriendshipDto com os nomes de propriedade corretos
                return new FriendshipDto
                {
                    FriendshipId = f.Id,
                    FriendUserId = friendUser.Id,
                    FriendUsername = friendUser.Username,
                    FriendProfileImageUrl = friendUser.Profile?.ProfilePicture,
                    Status = f.Status,
                    RequestDate = f.RequestDate
                };
            }).ToList();
        }

        private async Task<List<TopicSummaryDto>> LoadSavedTopicsDtoAsync(int profileId)
        {
            return await _context.SavedTopics
                .Where(st => st.ProfileId == profileId)
                .Select(st => st.Topic) // Pega a entidade Topic de cada SavedTopic
                .Include(t => t.Category)
                .Include(t => t.Creator.User)
                .Include(t => t.Posts) // Inclui os posts para poder contá-los
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TopicSummaryDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    TopicImageUrl = t.TopicImageUrl, // Assumindo que a propriedade no modelo Topic se chama TopicImage
                    CategoryName = t.Category.Name,
                    CreatorUsername = t.Creator.User.Username,
                    CreatedAt = t.CreatedAt,
                    PostCount = t.Posts.Count() // Conta o número de posts no tópico
                }).ToListAsync();
        }

        private async Task<List<TopicSummaryDto>> LoadCreatedTopicsDtoAsync(int profileId)
        {
            return await _context.Topics
                .Where(t => t.CreatedBy == profileId && !t.IsPersonal)
                .Include(t => t.Category)
                .Include(t => t.Creator.User)
                .Include(t => t.Posts) // Inclui os posts para poder contá-los
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TopicSummaryDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    TopicImageUrl = t.TopicImageUrl, // Assumindo que a propriedade no modelo Topic se chama TopicImage
                    CategoryName = t.Category.Name,
                    CreatorUsername = t.Creator.User.Username,
                    CreatedAt = t.CreatedAt,
                    PostCount = t.Posts.Count() // Conta o número de posts no tópico
                }).ToListAsync();
        }

        private async Task<FriendshipStatusDto> GetFriendshipStatusAsync(int otherUserId, int? currentUserId)
        {
            if (currentUserId == null) return new FriendshipStatusDto { Status = "not_logged_in" };
            if (currentUserId.Value == otherUserId) return new FriendshipStatusDto { Status = "self" };

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => (f.User1Id == currentUserId.Value && f.User2Id == otherUserId) || (f.User1Id == otherUserId && f.User2Id == currentUserId.Value));

            if (friendship == null) return new FriendshipStatusDto { Status = "not_friends" };

            switch (friendship.Status)
            {
                case FriendshipStatus.Accepted: return new FriendshipStatusDto { Status = "friends", FriendshipId = friendship.Id };
                case FriendshipStatus.Pending:
                    return friendship.User1Id == currentUserId.Value
                        ? new FriendshipStatusDto { Status = "pending_sent", FriendshipId = friendship.Id }
                        : new FriendshipStatusDto { Status = "pending_received", FriendshipId = friendship.Id };
                default: return new FriendshipStatusDto { Status = "not_friends" };
            }
        }

        private async Task<string> SaveImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0) return string.Empty;

            string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "profile");
            Directory.CreateDirectory(uploadsFolder); // Garante que a pasta exista

            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return "/images/profile/" + uniqueFileName;
        }

        private void DeleteImage(string imageUrl)
        {
            // Não apaga se a URL for nula, vazia ou a imagem padrão
            if (string.IsNullOrEmpty(imageUrl) || imageUrl == "/images/defaultuser.png") return;

            try
            {
                // Converte a URL para um caminho físico
                var filePath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                // Log do erro, mas não quebra a aplicação
                Console.WriteLine($"Erro ao apagar a imagem antiga: {ex.Message}");
            }
        }

        // ViewModel para a página de perfil de outro utilizador
        public class UserProfileViewModel
        {
            public Profile Profile { get; set; } = null!;
            public bool IsOwnProfile { get; set; }
            public int? LoggedInUserId { get; set; }
            // Não vamos pré-carregar o FriendshipStatus aqui, deixaremos para o JavaScript
            // para manter o carregamento inicial da página rápido e a lógica centralizada no FriendshipsController.
        }

        #endregion
    }
}