// In projetodweb_connectify/Controllers/API/ProfilesApiController.cs

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
    // MUDANÇA: Removido 'AuthenticationSchemes = "Bearer"' para usar a autenticação de cookie padrão, 
    // facilitando a chamada a partir do JavaScript da sua página CSHTML.
    [Authorize]
    public class ProfilesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProfilesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        public async Task<ActionResult<ProfileDto>> GetMyProfile()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                                     .AsNoTracking()
                                     .Include(u => u.Profile)
                                     .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || user.Profile == null)
            {
                return NotFound(new { message = "Perfil não encontrado." });
            }

            var profileDto = new ProfileDto
            {
                Id = user.Profile.Id,
                Name = user.Profile.Name,
                Type = user.Profile.Type,
                Bio = user.Profile.Bio,
                ProfilePicture = user.Profile.ProfilePicture,
                Username = user.Username,
                CreatedAt = user.Profile.CreatedAt
            };

            // --- Lógica para buscar posts do tópico pessoal (já existente) ---
            var personalTopic = await _context.Topics
                .AsNoTracking()
                .Include(t => t.Posts)
                .Where(t => t.CreatedBy == user.Profile.Id && t.IsPersonal == true)
                .FirstOrDefaultAsync();

            if (personalTopic != null && personalTopic.Posts.Any())
            {
                profileDto.PersonalTopicPosts = personalTopic.Posts
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new TopicPostDto
                    {
                        Id = p.Id,
                        Content = p.Content,
                        CreatedAt = p.CreatedAt,
                        PostImageUrl = p.PostImageUrl
                    }).ToList();
            }

            // ----- NOVA LÓGICA PARA IR BUSCAR OS TÓPICOS CRIADOS -----

            // 1. Ir buscar todos os tópicos criados pelo utilizador, excluindo o pessoal.
            var createdTopics = await _context.Topics
                .AsNoTracking()
                .Where(t => t.CreatedBy == user.Profile.Id && t.IsPersonal == false)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TopicSummaryDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    TopicImageUrl = t.TopicImageUrl,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            // 2. Adicionar a lista ao DTO
            profileDto.CreatedTopics = createdTopics;

            // 1. Ir buscar os registos de SavedTopic para este utilizador, incluindo os detalhes do tópico associado.
            var savedTopics = await _context.SavedTopics
                .AsNoTracking()
                .Where(st => st.ProfileId == user.Profile.Id)
                .Include(st => st.Topic) // Crucial para aceder ao Título, Imagem, etc.
                .OrderByDescending(st => st.SavedAt) // Ordenar pelos mais recentemente guardados
                .Select(st => new SavedTopicDto
                {
                    TopicId = st.Topic.Id,
                    Title = st.Topic.Title,
                    TopicImageUrl = st.Topic.TopicImageUrl,
                    SavedAt = st.SavedAt // Usamos a data de quando foi guardado
                })
                .ToListAsync();

            // 2. Adicionar a lista ao DTO do perfil
            profileDto.SavedTopics = savedTopics;


            return Ok(profileDto);
        }
    }
}