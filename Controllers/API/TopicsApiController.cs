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
    [Route("api/topics")]
    [ApiController]
    public class TopicsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public TopicsApiController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        /// <summary>
        /// Obtém uma lista de todos os tópicos públicos.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TopicSummaryDto>>> GetTopics()
        {
            var topics = await _context.Topics
                .Where(t => !t.IsPersonal && !t.IsPrivate)
                .Include(t => t.Creator.User)
                .Include(t => t.Category)
                .Include(t => t.Posts)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TopicSummaryDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    TopicImageUrl = t.TopicImageUrl,
                    CategoryName = t.Category.Name,
                    CreatorUsername = t.Creator.User.Username,
                    CreatedAt = t.CreatedAt,
                    PostCount = t.Posts.Count
                })
                .ToListAsync();

            return Ok(topics);
        }

        /// <summary>
        /// Obtém os detalhes de um tópico específico, incluindo todos os seus posts e comentários.
        /// </summary>
        /// <param name="id">O ID do tópico.</param>
        [HttpGet("{id}")]
        public async Task<ActionResult<TopicDetailDto>> GetTopicDetails(int id)
        {
            var currentUserProfileId = await GetCurrentProfileIdAsync();

            var topic = await _context.Topics
                .Include(t => t.Creator.User)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (topic == null) return NotFound();

            // Carregar posts e seus detalhes numa consulta separada para melhor legibilidade
            var posts = await _context.TopicPosts
                .Where(p => p.TopicId == id)
                .Include(p => p.Profile.User)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.CreatedAt) 
                .Select(p => new TopicPostDto
                {
                    Id = p.Id,
                    Content = p.Content,
                    PostImageUrl = p.PostImageUrl,
                    CreatedAt = p.CreatedAt,
                    LikesCount = p.Likes.Count,
                    IsLikedByCurrentUser = currentUserProfileId.HasValue && p.Likes.Any(l => l.ProfileId == currentUserProfileId.Value),
                    Author = new AuthorProfileDto
                    {
                        ProfileId = p.ProfileId,
                        Username = p.Profile.User.Username,
                        ProfileImageUrl = p.Profile.ProfilePicture
                    }
                })
                .ToListAsync();

            var topicDetail = new TopicDetailDto
            {
                Id = topic.Id,
                Title = topic.Title,
                Description = topic.Description,
                TopicImageUrl = topic.TopicImageUrl,
                CategoryName = topic.Category.Name,
                CreatedAt = topic.CreatedAt,
                Creator = new AuthorProfileDto { ProfileId = topic.CreatedBy, Username = topic.Creator.User.Username, ProfileImageUrl = topic.Creator.ProfilePicture },
                IsCurrentUserTheCreator = currentUserProfileId.HasValue && topic.CreatedBy == currentUserProfileId.Value,
                IsSavedByCurrentUser = currentUserProfileId.HasValue && await _context.SavedTopics.AnyAsync(st => st.TopicId == id && st.ProfileId == currentUserProfileId.Value),
                Posts = posts
            };

            return Ok(topicDetail);
        }

        /// <summary>
        /// Cria um novo tópico. Requer autenticação.
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<TopicSummaryDto>> CreateTopic([FromForm] TopicCreateDto createDto)
        {
            var profileId = await GetCurrentProfileIdAsync();
            if (profileId == null) return Unauthorized(new { message = "É necessário ter um perfil para criar um tópico." });

            var newTopic = new Topic
            {
                Title = createDto.Title,
                Description = createDto.Description,
                CategoryId = createDto.CategoryId,
                IsPrivate = createDto.IsPrivate,
                IsPersonal = false,
                CreatedBy = profileId.Value,
                CreatedAt = DateTime.UtcNow,
                TopicImageUrl = "/images/topics/default_topic.jpeg" // Padrão
            };

            if (createDto.TopicImageFile != null)
            {
                newTopic.TopicImageUrl = await SaveImage(createDto.TopicImageFile);
            }

            _context.Topics.Add(newTopic);
            await _context.SaveChangesAsync();

            var resultDto = await _context.Topics.Where(t => t.Id == newTopic.Id).Select(t => new TopicSummaryDto { Id = t.Id, Title = t.Title /*...*/}).FirstOrDefaultAsync(); // Mapear para DTO

            return CreatedAtAction(nameof(GetTopicDetails), new { id = newTopic.Id }, resultDto);
        }

        /// <summary>
        /// Adiciona um tópico da lista de salvos do utilizador.
        /// </summary>
        [HttpPost("{id}/save")]
        [Authorize]
        public async Task<IActionResult> SaveTopic(int id)
        {
            var profileId = await GetCurrentProfileIdAsync();
            if (profileId == null) return Unauthorized();

            var topicExists = await _context.Topics.AnyAsync(t => t.Id == id);
            if (!topicExists) return NotFound(new { message = "Tópico não encontrado." });

            var isAlreadySaved = await _context.SavedTopics.AnyAsync(st => st.TopicId == id && st.ProfileId == profileId.Value);
            if (isAlreadySaved) return Conflict(new { message = "Tópico já está salvo." });

            var savedTopic = new SavedTopic { ProfileId = profileId.Value, TopicId = id, SavedAt = DateTime.UtcNow };
            _context.SavedTopics.Add(savedTopic);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tópico salvo com sucesso." });
        }

        /// <summary>
        /// Remove um tópico da lista de salvos do utilizador.
        /// </summary>
        [HttpDelete("{id}/save")]
        [Authorize]
        public async Task<IActionResult> UnsaveTopic(int id)
        {
            var profileId = await GetCurrentProfileIdAsync();
            if (profileId == null) return Unauthorized();

            var savedTopic = await _context.SavedTopics.FirstOrDefaultAsync(st => st.TopicId == id && st.ProfileId == profileId.Value);
            if (savedTopic == null) return NotFound(new { message = "Este tópico não está na sua lista de salvos." });

            _context.SavedTopics.Remove(savedTopic);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #region Helper Methods
        private async Task<int?> GetCurrentProfileIdAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;
            var user = await _context.Users.AsNoTracking().Include(u => u.Profile).FirstOrDefaultAsync(u => u.Username == username);
            return user?.Profile?.Id;
        }
        private async Task<string> SaveImage(IFormFile imageFile)
        {
            string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "topics");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create)) { await imageFile.CopyToAsync(fileStream); }
            return "/images/topics/" + uniqueFileName;
        }
        #endregion
    }
}