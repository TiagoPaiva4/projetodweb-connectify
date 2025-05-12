using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // If you're restricting access

namespace projetodweb_connectify.Controllers
{
    [Authorize] // Apply authorization at controller level if most actions require it
    public class ProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfilesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Profiles
        public async Task<IActionResult> Index()
        {

            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized();
            }

            // Find the user by email (assuming Username is the email)
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == userEmail);
            if (appUser == null)
            {
                return NotFound("Utilizador não encontrado.");
            }

            // Find the profile associated with this user
            var profile = await _context.Profiles
                                        .Include(p => p.User)
                                        .FirstOrDefaultAsync(p => p.UserId == appUser.Id); 
            if (profile == null)
            {
                return NotFound("Perfil não encontrado. Por favor, crie um perfil.");
            }

            // Fetch topics created by this profile
            Console.WriteLine($"Fetching topics where Topic.CreatedBy == {profile.Id}");

            // --- QUERY MODIFICATION FOR DEBUGGING ---
            var topicsQuery = _context.Topics
                                      .Where(t => t.CreatedBy == profile.Id); // Temporarily remove other filters

            // Log the generated SQL (optional, but very helpful)
            // var sql = topicsQuery.ToQueryString();
            // Console.WriteLine($"Generated SQL for topics: {sql}");

            profile.CreatedTopics = await topicsQuery
                                           .OrderByDescending(t => t.CreatedAt)
                                           .ToListAsync();

            Console.WriteLine($"Number of topics fetched for Profile ID {profile.Id}: {profile.CreatedTopics.Count}");
            if (profile.CreatedTopics.Any())
            {
                foreach (var topic in profile.CreatedTopics)
                {
                    Console.WriteLine($"  - Topic ID: {topic.Id}, Title: {topic.Title}, CreatedBy: {topic.CreatedBy}, IsPersonal: {topic.IsPersonal}, IsPrivate: {topic.IsPrivate}");
                }
            }
            else
            {
                Console.WriteLine("No topics found matching the criteria.");
            }

            Console.WriteLine("--- MyProfile Action Ending ---");
            return View(profile);
        }


        /* Mudar futuramente a rota para este get em vez de usar o Index*/
        /* Falta o GET do tópico pessoal*/

        // GET: Profiles/MyProfile
        [HttpGet("Profiles/MyProfile")] 
        public async Task<IActionResult> MyProfile()
        {

            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized();
            }

            // Find the user by email (assuming Username is the email)
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == userEmail);
            if (appUser == null)
            {
                Console.WriteLine($"User with email '{userEmail}' not found in Users table. Returning NotFound.");
                return NotFound("Utilizador não encontrado.");
            }
            Console.WriteLine($"Found User - ID: {appUser.Id}, Username: {appUser.Username}");

            // Find the profile associated with this user
            var profile = await _context.Profiles
                                        .Include(p => p.User)
                                        .FirstOrDefaultAsync(p => p.UserId == appUser.Id); 

            if (profile == null)
            {
                return NotFound("Perfil não encontrado. Por favor, crie um perfil.");
            }

            // --- QUERY MODIFICATION FOR DEBUGGING ---
            var topicsQuery = _context.Topics
                                      .Where(t => t.CreatedBy == profile.Id); 

            // Log the generated SQL (optional, but very helpful)
            // var sql = topicsQuery.ToQueryString();
            // Console.WriteLine($"Generated SQL for topics: {sql}");

            profile.CreatedTopics = await topicsQuery
                                           .OrderByDescending(t => t.CreatedAt)
                                           .ToListAsync();

            Console.WriteLine($"Number of topics fetched for Profile ID {profile.Id}: {profile.CreatedTopics.Count}");
            if (profile.CreatedTopics.Any())
            {
                foreach (var topic in profile.CreatedTopics)
                {
                    Console.WriteLine($"  - Topic ID: {topic.Id}, Title: {topic.Title}, CreatedBy: {topic.CreatedBy}, IsPersonal: {topic.IsPersonal}, IsPrivate: {topic.IsPrivate}");
                }
            }
            else
            {
                Console.WriteLine("No topics found matching the criteria.");
            }

            // Re-apply original filter if needed after debugging
            // profile.CreatedTopics = profile.CreatedTopics
            //                                .Where(t => !t.IsPersonal && !t.IsPrivate)
            //                                .ToList();
            // Console.WriteLine($"Number of topics after IsPersonal/IsPrivate filter: {profile.CreatedTopics.Count}");

            return View(profile);
        }


        // GET: Profiles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var profile = await _context.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (profile == null)
            {
                return NotFound();
            }

            return View(profile);
        }

        // GET: Profiles/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Username");
            return View();
        }

        // POST: Profiles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,Type,Bio,ProfilePicture,CreatedAt")] Profile profile)
        {
            if (ModelState.IsValid)
            {
                _context.Add(profile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Username", profile.UserId);
            return View(profile);
        }

        // GET: Profiles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var profile = await _context.Profiles.FindAsync(id);
            if (profile == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Username", profile.UserId);
            return View(profile);
        }

        // POST: Profiles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,Type,Bio,ProfilePicture,Name,CreatedAt")] Profile profile, IFormFile ProfilePicture)

        {
            if (id != profile.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                Console.WriteLine("ModelState é válido.");
                try
                {
                    if (ProfilePicture != null && ProfilePicture.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profile");
                        Directory.CreateDirectory(uploadsFolder); // Garante que a pasta existe

                        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ProfilePicture.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ProfilePicture.CopyToAsync(stream);
                        }

                        // Atualiza o caminho da imagem no modelo
                        profile.ProfilePicture = "/images/profile/" + uniqueFileName;
                    }


                    _context.Update(profile);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProfileExists(profile.Id))
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
            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                foreach (var error in state.Errors)
                {
                    Console.WriteLine($"Erro no campo '{key}': {error.ErrorMessage}");
                }
            }

            Console.WriteLine("ModelState inválido. Retornando view com modelo.");
            return View(profile);
        }


        // GET: Profiles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var profile = await _context.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (profile == null)
            {
                return NotFound();
            }

            return View(profile);
        }

        // POST: Profiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var profile = await _context.Profiles.FindAsync(id);
            if (profile != null)
            {
                _context.Profiles.Remove(profile);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProfileExists(int id)
        {
            return _context.Profiles.Any(e => e.Id == id);
        }
    }
}
