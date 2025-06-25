using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using projetodweb_connectify.Models.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace projetodweb_connectify.Controllers.API
{
    //[ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/profiles")]
    [ApiController]
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
        /// Procura por utilizadores/perfis. Acessível a utilizadores autenticados.
        /// </summary>
        /// <param name="searchQuery">O termo de pesquisa.</param>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ProfileSummaryDto>>> SearchProfiles([FromQuery] string searchQuery)
        {
            var currentUserId = (await GetCurrentUserAsync())?.Id;

            var query = _context.Profiles
                .Include(p => p.User)
                .Where(p => p.UserId != currentUserId); // Excluir o próprio utilizador

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(p => p.User.Username.Contains(searchQuery) || p.Name.Contains(searchQuery));
            }

            var profiles = await query
                .OrderBy(p => p.User.Username)
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
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<MyProfileDetailDto>> GetMyProfile()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var profile = await _context.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);

            if (profile == null) return NotFound(new { message = "Perfil não encontrado." });

            // Carregar dados agregados
            var friends = await LoadFriendsDtoAsync(currentUser.Id);
            var savedTopics = await LoadSavedTopicsDtoAsync(profile.Id);
            var createdTopics = await LoadCreatedTopicsDtoAsync(profile.Id);

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
                Friends = friends,
                SavedTopics = savedTopics,
                CreatedTopics = createdTopics
            };

            return Ok(myProfileDto);
        }

        /// <summary>
        /// Obtém os dados públicos de um perfil de utilizador, especificado pelo seu ID de utilizador.
        /// </summary>
        /// <param name="userId">O ID do utilizador (não o ID do perfil).</param>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<UserProfileDetailDto>> GetUserProfile(int userId)
        {
            var profile = await _context.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null) return NotFound();

            var createdTopics = await LoadCreatedTopicsDtoAsync(profile.Id);

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
                CreatedTopics = createdTopics,
                FriendshipStatus = await GetFriendshipStatusAsync(userId) // Obtém o status em relação ao user logado
            };

            return Ok(userProfileDto);
        }

        /// <summary>
        /// Atualiza o perfil do utilizador autenticado.
        /// </summary>
        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMyProfile([FromForm] ProfileEditDto editDto)
        {
            var currentUserId = (await GetCurrentUserAsync())?.Id;
            if (currentUserId == null) return Unauthorized();

            var profileToUpdate = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == currentUserId.Value);
            if (profileToUpdate == null) return NotFound();

            profileToUpdate.Name = editDto.Name;
            profileToUpdate.Bio = editDto.Bio;
            profileToUpdate.Type = editDto.Type;

            if (editDto.ProfilePictureFile != null)
            {
                DeleteImage(profileToUpdate.ProfilePicture);
                profileToUpdate.ProfilePicture = await SaveImage(editDto.ProfilePictureFile);
            }

            _context.Profiles.Update(profileToUpdate);
            await _context.SaveChangesAsync();
            return NoContent();
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
            return await _context.Friendships
                .Where(f => f.Status == FriendshipStatus.Accepted && (f.User1Id == currentUserId || f.User2Id == currentUserId))
                .Select(f => new FriendshipDto { /* Mapear para DTO */ }).ToListAsync();
        }

        private async Task<List<TopicSummaryDto>> LoadSavedTopicsDtoAsync(int profileId)
        {
            return await _context.SavedTopics.Where(st => st.ProfileId == profileId)
               .Include(st => st.Topic.Category)
               .Include(st => st.Topic.Creator.User)
               .OrderByDescending(st => st.SavedAt)
               .Select(st => new TopicSummaryDto { /* Mapear para DTO */}).ToListAsync();
        }

        private async Task<List<TopicSummaryDto>> LoadCreatedTopicsDtoAsync(int profileId)
        {
            return await _context.Topics.Where(t => t.CreatedBy == profileId && !t.IsPersonal)
                 .Include(t => t.Category)
                 .Include(t => t.Creator.User)
                 .OrderByDescending(t => t.CreatedAt)
                 .Select(t => new TopicSummaryDto { /* Mapear para DTO */ }).ToListAsync();
        }

        private async Task<FriendshipStatusDto> GetFriendshipStatusAsync(int otherUserId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return new FriendshipStatusDto { Status = "not_logged_in", Message = "Não autenticado." };
            if (currentUser.Id == otherUserId) return new FriendshipStatusDto { Status = "self", Message = "Este é o seu próprio perfil." };

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => (f.User1Id == currentUser.Id && f.User2Id == otherUserId) || (f.User1Id == otherUserId && f.User2Id == currentUser.Id));

            if (friendship == null) return new FriendshipStatusDto { Status = "not_friends", Message = "Sem relação de amizade." };

            switch (friendship.Status)
            {
                case FriendshipStatus.Accepted: return new FriendshipStatusDto { Status = "friends", FriendshipId = friendship.Id, Message = "São amigos." };
                case FriendshipStatus.Pending:
                    return friendship.User1Id == currentUser.Id
                        ? new FriendshipStatusDto { Status = "pending_sent", FriendshipId = friendship.Id, Message = "Pedido enviado." }
                        : new FriendshipStatusDto { Status = "pending_received", FriendshipId = friendship.Id, Message = "Recebeu um pedido." };
                default: return new FriendshipStatusDto { Status = "not_friends", Message = "Sem amizade ativa." };
            }
        }

        private async Task<string> SaveImage(IFormFile imageFile)
        {
            string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "profile");
            Directory.CreateDirectory(uploadsFolder);
            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create)) { await imageFile.CopyToAsync(stream); }
            return "/images/profile/" + uniqueFileName;
        }

        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || imageUrl == "/images/defaultuser.png") return;
            var filePath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath)) { try { System.IO.File.Delete(filePath); } catch (Exception ex) { Console.WriteLine(ex.Message); } }
        }
        #endregion
    }
}