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
    [Route("api/topic-posts")]
    [ApiController]
    [Authorize] // Todas as ações exigem login, exceto as marcadas com [AllowAnonymous]
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
        /// Obtém todos os posts de um tópico específico.
        /// </summary>
        /// <param name="topicId">O ID do tópico.</param>
        [HttpGet("by-topic/{topicId}")]
        [AllowAnonymous] // Geralmente, a visualização de posts é pública
        public async Task<ActionResult<IEnumerable<TopicPostDto>>> GetPostsForTopic(int topicId)
        {
            var currentUserProfileId = await GetCurrentProfileIdAsync();

            var posts = await _context.TopicPosts
                .Where(p => p.TopicId == topicId)
                .Include(p => p.Profile).ThenInclude(pr => pr.User)
                .Include(p => p.Likes) // Incluir os likes para contagem
                .OrderBy(p => p.CreatedAt)
                .Select(p => new TopicPostDto
                {
                    Id = p.Id,
                    Content = p.Content,
                    PostImageUrl = p.PostImageUrl,
                    CreatedAt = p.CreatedAt,
                    LikesCount = p.Likes.Count,
                    // Verifica se o utilizador atual (se houver) deu like neste post
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
        /// Cria um novo post num tópico.
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
                newPost.PostImageUrl = await SaveImage(createDto.PostImageFile);
            }

            _context.TopicPosts.Add(newPost);
            await _context.SaveChangesAsync();

            // Carregar os dados do post recém-criado para retornar um DTO completo
            var createdPostDto = await _context.TopicPosts
                .Where(p => p.Id == newPost.Id)
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
        /// Atualiza um post existente.
        /// </summary>
        /// <param name="id">O ID do post a editar.</param>
        /// <param name="editDto">Os novos dados do post.</param>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(int id, [FromForm] TopicPostEditDto editDto)
        {
            var postToUpdate = await _context.TopicPosts.FindAsync(id);
            if (postToUpdate == null) return NotFound(new { message = "Post não encontrado." });

            var profileId = await GetCurrentProfileIdAsync();
            if (postToUpdate.ProfileId != profileId)
            {
                // Adicionar verificação de Admin, se aplicável
                if (!User.IsInRole("Admin"))
                {
                    return Forbid(); // Utilizador não é o dono nem admin
                }
            }

            postToUpdate.Content = editDto.Content;

            if (editDto.PostImageFile != null)
            {
                DeleteImage(postToUpdate.PostImageUrl); // Apaga a imagem antiga
                postToUpdate.PostImageUrl = await SaveImage(editDto.PostImageFile); // Guarda a nova
            }

            _context.Entry(postToUpdate).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Apaga um post.
        /// </summary>
        /// <param name="id">O ID do post a apagar.</param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var postToDelete = await _context.TopicPosts.FindAsync(id);
            if (postToDelete == null) return NotFound();

            var profileId = await GetCurrentProfileIdAsync();
            if (postToDelete.ProfileId != profileId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Guardar o caminho da imagem ANTES de apagar o registo
            var imagePath = postToDelete.PostImageUrl;

            _context.TopicPosts.Remove(postToDelete);
            await _context.SaveChangesAsync();

            // Apagar o ficheiro da imagem DEPOIS de a transação na BD ser bem-sucedida
            DeleteImage(imagePath);

            return NoContent();
        }

        #region Helper Methods
        private async Task<int?> GetCurrentProfileIdAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;

            var user = await _context.Users.AsNoTracking()
                                     .Include(u => u.Profile)
                                     .FirstOrDefaultAsync(u => u.Username == username);
            return user?.Profile?.Id;
        }

        private async Task<string> SaveImage(IFormFile imageFile)
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
                try { System.IO.File.Delete(imagePath); }
                catch (Exception ex) { Console.WriteLine($"Erro ao apagar imagem do post: {ex.Message}"); }
            }
        }
        #endregion
    }
}