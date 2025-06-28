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
    [Route("api/events")]
    [ApiController]
    public class EventsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public EventsApiController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        /// <summary>
        /// Obtém uma lista de todos os eventos. Acessível publicamente.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventSummaryDto>>> GetEvents()
        {
            var events = await _context.Events
                .Include(e => e.Creator)
                .OrderByDescending(e => e.StartDateTime)
                .Select(e => new EventSummaryDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    StartDateTime = e.StartDateTime,
                    Location = e.Location,
                    EventImageUrl = e.EventImageUrl,
                    CreatorUsername = e.Creator.Username
                })
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>
        /// Obtém os detalhes de um evento específico. Acessível publicamente.
        /// </summary>
        /// <param name="id">O ID do evento.</param>
        [HttpGet("{id}")]
        public async Task<ActionResult<EventDetailDto>> GetEvent(int id)
        {
            var currentUserId = GetCurrentUserId();

            var eventDetail = await _context.Events
                .Where(e => e.Id == id)
                .Include(e => e.Creator)
                .Select(e => new EventDetailDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    StartDateTime = e.StartDateTime,
                    EndDateTime = e.EndDateTime,
                    Location = e.Location,
                    EventImageUrl = e.EventImageUrl,
                    CreatedAt = e.CreatedAt,
                    CreatorId = e.CreatorId,
                    CreatorUsername = e.Creator.Username,
                    IsCurrentUserTheCreator = currentUserId.HasValue && e.CreatorId == currentUserId.Value
                })
                .FirstOrDefaultAsync();

            if (eventDetail == null)
            {
                return NotFound();
            }

            return Ok(eventDetail);
        }

        /// <summary>
        /// Cria um novo evento. Requer autenticação.
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<EventDetailDto>> CreateEvent([FromForm] EventCreateDto createDto)
        {
            var creatorId = GetCurrentUserId();
            if (creatorId == null)
            {
                return Unauthorized(new { message = "É necessário estar autenticado para criar um evento." });
            }

            var newEvent = new Event
            {
                Title = createDto.Title,
                Description = createDto.Description,
                StartDateTime = createDto.StartDateTime,
                EndDateTime = createDto.EndDateTime,
                Location = createDto.Location,
                CreatorId = creatorId.Value,
                CreatedAt = DateTime.UtcNow
            };

            if (createDto.EventImageFile != null)
            {
                newEvent.EventImageUrl = await SaveImage(createDto.EventImageFile);
            }

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            var resultDto = await GetEvent(newEvent.Id); // Reutiliza a lógica do GET para retornar o DTO completo

            return CreatedAtAction(nameof(GetEvent), new { id = newEvent.Id }, resultDto.Value);
        }

        /// <summary>
        /// Apaga um evento. Apenas o criador do evento ou um Admin pode fazer isso.
        /// </summary>
        /// <param name="id">O ID do evento a apagar.</param>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var eventToDelete = await _context.Events.FindAsync(id);
            if (eventToDelete == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            // Ação permitida apenas para o dono do evento ou um Admin
            if (eventToDelete.CreatorId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid(); // 403 Forbidden
            }

            var imagePath = eventToDelete.EventImageUrl;
            _context.Events.Remove(eventToDelete);
            await _context.SaveChangesAsync();

            DeleteImage(imagePath);

            return NoContent();
        }

        #region Helper Methods
        private int? GetCurrentUserId()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;

            // Esta consulta é otimizada para apenas buscar o ID do utilizador.
            var user = _context.Users.AsNoTracking().FirstOrDefault(u => u.Username == username);
            return user?.Id;
        }

        private async Task<string> SaveImage(IFormFile imageFile)
        {
            string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "events");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return "/images/events/" + uniqueFileName;
        }

        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            // Evitar apagar uma imagem padrão, se houver
            // if (imageUrl == "/images/events/default.png") return;

            string imagePath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(imagePath))
            {
                try { System.IO.File.Delete(imagePath); }
                catch (Exception ex) { Console.WriteLine($"Erro ao apagar imagem do evento: {ex.Message}"); }
            }
        }
        #endregion
    }
}