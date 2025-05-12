using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;

namespace projetodweb_connectify.Controllers
{
    public class TopicsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TopicsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Topics
        public async Task<IActionResult> Index()
        {
            // Eager load the Creator profile, and optionally the User associated with the creator profile
            var topics = await _context.Topics
                                     .Include(t => t.Creator)          // Include the Profile object of the creator
                                      .ThenInclude(c => c.User)     // Include the User object linked to the creator's Profile
                                     .Where(t => !t.IsPersonal)       // Exclude personal profile topics from the main index
                                     .OrderByDescending(t => t.CreatedAt) // Optional: Order by creation date
                                     .ToListAsync();
            return View(topics);
        }

        // GET: Topics/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Log the ID being requested
            Console.WriteLine($"Fetching details for Topic ID: {id}");

            var topic = await _context.Topics
                .Include(t => t.Creator) // Creator of the Topic
                    .ThenInclude(c => c.User) // User who is the creator of the topic
                .Include(t => t.Posts)   
                    .ThenInclude(p => p.Profile) // For each post, include its author's Profile
                        .ThenInclude(profile => profile.User) // For each post's Profile, include the User
                .FirstOrDefaultAsync(m => m.Id == id);

            if (topic == null)
            {
                return NotFound();
            }

            if (topic.Posts != null)
            {
                // Order posts after loading and checking count
                topic.Posts = topic.Posts.OrderByDescending(p => p.CreatedAt).ToList();
            }
            else
            {
                // This case should ideally not happen if .Include(t => t.Posts) is used,
                // as EF Core usually initializes the collection.
                Console.WriteLine($"Topic ID: {id} - Posts collection is NULL.");
            }

            return View(topic);
        }

        // GET: Topics/Create
        public IActionResult Create()
        {
            ViewData["CreatedBy"] = new SelectList(_context.Profiles, "Id", "Id");
            return View();
        }

        // POST: Topics/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,IsPersonal,IsPrivate,Title,Description,CreatedBy,CreatedAt")] Topic topic)
        {
            if (ModelState.IsValid)
            {
                // Preenche automaticamente os campos IsPersonal, IsPrivate e CreatedAt
                topic.IsPersonal = false;
                topic.IsPrivate = false;
                topic.CreatedAt = DateTime.UtcNow;

                // Obter o e-mail do utilizador logado
                var email = User.Identity?.Name;

                if (email == null)
                {
                    return Unauthorized();
                }

                // Buscar o utilizador no banco de dados com o e-mail logado
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);

                if (user == null)
                {
                    return NotFound("Utilizador não encontrado.");
                }

                // Encontrar o perfil do utilizador
                var profile = await _context.Profiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);

                // Atribuir o ID do utilizador logado ao campo CreatedBy
                topic.CreatedBy = user.Id;

                // Atribuir o perfil do utilizador à propriedade de navegação 'Creator'
                topic.Creator = profile;

                // Adicionar o tópico ao banco de dados
                _context.Add(topic);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                foreach (var error in state.Errors)
                {
                    Console.WriteLine($"Erro no campo '{key}': {error.ErrorMessage}");
                }
            }

            // Carregar as informações necessárias para a view, caso haja erros
            ViewData["CreatedBy"] = new SelectList(_context.Profiles, "Id", "Id", topic.CreatedBy);
            return View(topic);
        }


        // GET: Topics/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics.FindAsync(id);
            if (topic == null)
            {
                return NotFound();
            }
            ViewData["CreatedBy"] = new SelectList(_context.Profiles, "Id", "Id", topic.CreatedBy);
            return View(topic);
        }

        // POST: Topics/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description")] Topic updatedTopic)
        {
            if (!ModelState.IsValid)
            {
                return View(updatedTopic);
            }

            var email = User.Identity?.Name;
            if (email == null) return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
            if (user == null) return Unauthorized();

            var profile = await _context.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null) return Unauthorized();

            var existingTopic = await _context.Topics.FindAsync(id);
            if (existingTopic == null) return NotFound();

            if (existingTopic.CreatedBy != profile.Id)
            {
                return Forbid();
            }

            // Só atualiza os campos permitidos
            existingTopic.Title = updatedTopic.Title;
            existingTopic.Description = updatedTopic.Description;

            try
            {
                _context.Update(existingTopic);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Topics.Any(t => t.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Topics/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics
                .Include(t => t.Creator)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (topic == null)
            {
                return NotFound();
            }

            return View(topic);
        }

        // POST: Topics/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var email = User.Identity?.Name;
            if (email == null)
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
            if (user == null)
                return Unauthorized();

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null)
                return Unauthorized();

            var topic = await _context.Topics.FindAsync(id);
            if (topic == null)
                return NotFound();

            // Bloqueia a eliminação do tópico pessoail do utilizador
            if (topic.IsPersonal && topic.IsPrivate)
                return Forbid(); 

            // Verifica se o utilizador é o criador
            if (topic.CreatedBy != profile.Id)
                return Forbid();

            _context.Topics.Remove(topic);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Topics/SaveTopic/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // <<< Ensure user MUST be logged in to save
        public async Task<IActionResult> SaveTopic(int id) // id is TopicId
        {
            var topicToSave = await _context.Topics.FindAsync(id);
            if (topicToSave == null)
            {
                return NotFound("Tópico não encontrado.");
            }

            // --- Get current user's Profile ID ---
            var userEmail = User.Identity?.Name; // Using email as identifier
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("Utilizador não identificado.");
            }

            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == userEmail);
            if (appUser == null)
            {
                // Should not happen if [Authorize] is working, but good practice
                return Unauthorized("Conta de utilizador não encontrada.");
            }

            var userProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);
            if (userProfile == null)
            {
                // User is logged in but doesn't have a profile yet.
                // Redirect them to create one? Or just show an error.
                TempData["ErrorMessage"] = "Precisa de criar um perfil antes de poder guardar tópicos.";
                // Redirect to profile creation or MyProfile which might handle the creation flow
                return RedirectToAction("MyProfile", "Profiles");
                // Or: return Forbid("Perfil do utilizador não encontrado.");
            }
            int profileId = userProfile.Id;
            // --- End Get Profile ID ---


            // Check if already saved
            bool alreadySaved = await _context.SavedTopics
                                              .AnyAsync(st => st.TopicId == id && st.ProfileId == profileId);

            if (!alreadySaved)
            {
                Console.WriteLine($"User Profile {profileId} saving Topic {id}");
                var savedTopicEntry = new SavedTopic
                {
                    ProfileId = profileId,
                    TopicId = id,
                    SavedAt = DateTime.UtcNow
                };
                _context.SavedTopics.Add(savedTopicEntry);
                await _context.SaveChangesAsync();
                Console.WriteLine("Topic saved successfully.");
                TempData["SuccessMessage"] = "Tópico guardado com sucesso!"; // Feedback
            }
            else
            {
                Console.WriteLine($"Topic {id} already saved by Profile {profileId}.");
                TempData["InfoMessage"] = "Este tópico já está na sua lista de guardados."; // Feedback
            }

            // Redirect back to the Topic Index page after saving
            return RedirectToAction(nameof(Index));
        }


        // POST: Topics/UnsaveTopic/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnsaveTopic(int id) // id is TopicId
        {
            // No need to find the Topic itself, just the SavedTopic entry

            // Get current user's Profile ID
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int appUserId))
            {
                return Unauthorized("Utilizador não identificado.");
            }
            var userProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUserId);
            if (userProfile == null)
            {
                return NotFound("Perfil do utilizador não encontrado.");
            }
            int profileId = userProfile.Id;


            // Find the saved entry
            var savedTopicEntry = await _context.SavedTopics
                                                .FirstOrDefaultAsync(st => st.TopicId == id && st.ProfileId == profileId);

            if (savedTopicEntry != null)
            {
                Console.WriteLine($"User Profile {profileId} unsaving Topic {id}");
                _context.SavedTopics.Remove(savedTopicEntry);
                await _context.SaveChangesAsync();
                Console.WriteLine("Topic unsaved successfully.");
                TempData["SuccessMessage"] = "Tópico removido da sua lista de guardados."; // Feedback
            }
            else
            {
                Console.WriteLine($"Topic {id} was not found in saved list for Profile {profileId}.");
                // Optionally provide feedback: TempData["InfoMessage"] = "Este tópico não estava na sua lista.";
            }

            // Redirect back to where the user was (usually their profile page or the topic page)
            string? returnUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Fallback: If unsaving from profile, go back there. If from topic page, go there.
            // This might need refinement based on where the unsave button is placed.
            return RedirectToAction("MyProfile", "Profiles"); // Default fallback to profile
        }

    }
}
