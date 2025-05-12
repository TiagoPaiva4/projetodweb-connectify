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

        // GET: Profiles (Index - Redirects to MyProfile for logged-in users)
        public IActionResult Index()
        {
            return RedirectToAction(nameof(MyProfile));
        }

        // GET: Profiles/MyProfile
        [HttpGet("Profiles/MyProfile")]
        public async Task<IActionResult> MyProfile()
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized();
            }

            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == userEmail);
            if (appUser == null)
            {
                return NotFound("Utilizador não encontrado.");
            }

            var profile = await _context.Profiles
                                        .Include(p => p.User)
                                        .FirstOrDefaultAsync(p => p.UserId == appUser.Id);

            if (profile == null)
            {
                return NotFound("Perfil não encontrado. Por favor, crie ou complete o seu perfil.");
            }

            // --- Fetch Personal Topic and its Posts (TopicPost entities) ---
            // Include the Posts (which are TopicPost entities) and their associated Profile (author)
            profile.PersonalTopic = await _context.Topics
                .Include(t => t.Posts)                 // Include the collection of TopicPost
                    .ThenInclude(tp => tp.Profile)     // Include the Profile (author) of each TopicPost
                .FirstOrDefaultAsync(t => t.CreatedBy == profile.Id && t.IsPersonal);

            if (profile.PersonalTopic != null)
            {
                // Assign the TopicPost entities, ordered by newest first
                // The 'Posts' collection on Topic *is* the ICollection<TopicPost>
                profile.PersonalTopicPosts = profile.PersonalTopic.Posts
                                                   .OrderByDescending(tp => tp.CreatedAt) // Order by TopicPost creation date
                                                   .ToList();
            }
            else
            {
                profile.PersonalTopicPosts = new List<TopicPost>(); // Ensure it's not null for the view
            }

            // --- Fetch OTHER Created Topics (Non-Personal, Non-Private) ---
            Console.WriteLine($"Fetching non-personal, non-private topics where Topic.CreatedBy == {profile.Id}");
            profile.CreatedTopics = await _context.Topics
                                           .Where(t => t.CreatedBy == profile.Id && !t.IsPersonal && !t.IsPrivate)
                                           .OrderByDescending(t => t.CreatedAt)
                                           .ToListAsync();

            // Use the "Index" view
            return View("Index", profile);
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
