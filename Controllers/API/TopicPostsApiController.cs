using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using projetodweb_connectify.Models.DTOs;
using System; // Adicionado para corrigir o erro 'Console' não existe.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace projetodweb_connectify.Controllers.API
{
    [Route("api/topic-posts")]
    [ApiController]
    [Authorize]
    public class TopicPostsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public TopicPostsApiController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        /// <summary>
        /// Obtém todas as publicações de um tópico específico.
        /// </summary>
        /// <param name="topicId">O ID do tópico.</param>
        [HttpGet("by-topic/{topicId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<TopicPostDto>>> GetPostsForTopic(int topicId)
        {
            var currentUserProfileId = await GetCurrentProfileIdAsync();

            var posts = await _context.TopicPosts
                .Where(p => p.TopicId == topicId)
                .Include(p => p.Profile).ThenInclude(pr => pr.User)
                .Include(p => p.Likes)
                .OrderBy(p => p.CreatedAt)
                .Select(p => new TopicPostDto
                {
                    Id = p.Id,
                    Content = p.Content,
                    PostImageUrl = p.PostImageUrl,
                    CreatedAt = p.CreatedAt,
                    LikesCount = p.Likes.Count,
                    // Verifica se o utilizador atual (se houver) deu "gosto" nesta publicação.
                    IsLikedByCurrentUser = currentUserProfileId.HasValue && p.Likes.Any(l => l.ProfileId == currentUserProfileId.Value),
                    Author = new AuthorProfileDto
                    {
                        ProfileId = p.Profile.Id,
                        Username = p.Profile.User.Username,
                        ProfileImageUrl = p.Profile.ProfilePicture
                    }
                })
                .ToListAsync();

            return Ok(posts);
        }

        /// <summary>
        /// Cria uma nova publicação num tópico.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TopicPostDto>> CreatePost([FromForm] TopicPostCreateDto createDto)
        {
            var profileId = await GetCurrentProfileIdAsync();
            if (profileId == null) return Unauthorized(new { message = "Utilizador não autenticado." });

            var topicExists = await _context.Topics.AnyAsync(t => t.Id == createDto.TopicId);
            if (!topicExists) return NotFound(new { message = "Tópico não encontrado." });

            var newPost = new TopicPost
            {
                Content = createDto.Content,
                TopicId = createDto.TopicId,
                ProfileId = profileId.Value,
                CreatedAt = DateTime.UtcNow
            };

            if (createDto.PostImageFile != null)
            {
                newPost.PostImageUrl = await SaveImageAsync(createDto.PostImageFile);
            }

            _context.TopicPosts.Add(newPost);
            await _context.SaveChangesAsync();

            // Carrega os dados da publicação recém-criada para retornar um DTO completo.
            var createdPostDto = await _context.TopicPosts
                .Where(p => p.Id == newPost.Id)
                .Include(p => p.Profile).ThenInclude(pr => pr.User)
                .Select(p => new TopicPostDto
                {
                    Id = p.Id,
                    Content = p.Content,
                    PostImageUrl = p.PostImageUrl,
                    CreatedAt = p.CreatedAt,
                    LikesCount = 0,
                    IsLikedByCurrentUser = false,
                    Author = new AuthorProfileDto
                    {
                        ProfileId = p.Profile.Id,
                        Username = p.Profile.User.Username,
                        ProfileImageUrl = p.Profile.ProfilePicture
                    }
                })
                .FirstOrDefaultAsync();

            return CreatedAtAction(nameof(GetPostsForTopic), new { topicId = newPost.TopicId }, createdPostDto);
        }

        /// <summary>
        /// Atualiza uma publicação existente.
        /// </summary>
        /// <param name="id">O ID da publicação a editar.</param>
        /// <param name="editDto">Os novos dados da publicação.</param>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(int id, [FromForm] TopicPostEditDto editDto)
        {
            var postToUpdate = await _context.TopicPosts.FindAsync(id);
            if (postToUpdate == null) return NotFound(new { message = "Publicação não encontrada." });

            var profileId = await GetCurrentProfileIdAsync();
            // Apenas o autor da publicação ou um administrador podem editar.
            if (postToUpdate.ProfileId != profileId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            postToUpdate.Content = editDto.Content;

            if (editDto.PostImageFile != null)
            {
                DeleteImage(postToUpdate.PostImageUrl); // Apaga a imagem antiga.
                postToUpdate.PostImageUrl = await SaveImageAsync(editDto.PostImageFile); // Guarda a nova.
            }

            _context.Entry(postToUpdate).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Apaga uma publicação.
        /// </summary>
        /// <param name="id">O ID da publicação a apagar.</param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var postToDelete = await _context.TopicPosts.FindAsync(id);
            if (postToDelete == null) return NotFound();

            var profileId = await GetCurrentProfileIdAsync();
            // Apenas o autor da publicação ou um administrador podem apagar.
            if (postToDelete.ProfileId != profileId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Guarda o caminho da imagem antes de apagar o registo da BD.
            var imagePath = postToDelete.PostImageUrl;

            _context.TopicPosts.Remove(postToDelete);
            await _context.SaveChangesAsync();

            // Apaga o ficheiro da imagem depois de a transação na BD ser bem-sucedida.
            DeleteImage(imagePath);

            return NoContent();
        }

        #region Métodos Auxiliares
        private async Task<int?> GetCurrentProfileIdAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;

            var user = await _context.Users.AsNoTracking()
                                     .Include(u => u.Profile)
                                     .FirstOrDefaultAsync(u => u.Username == username);
            return user?.Profile?.Id;
        }

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "posts");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return "/images/posts/" + uniqueFileName;
        }

        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            string imagePath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(imagePath))
            {
                try
                {
                    System.IO.File.Delete(imagePath);
                }
                catch (Exception ex)
                {
                    // Numa aplicação real, seria ideal usar um logger aqui.
                    Console.WriteLine($"Erro ao apagar imagem da publicação: {ex.Message}");
                }
            }
        }
        #endregion
    }
}