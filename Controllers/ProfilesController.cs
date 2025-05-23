﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using Microsoft.AspNetCore.Authorization;

namespace projetodweb_connectify.Controllers
{
    [Authorize]
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

        // GET: Profiles/Browse
        [HttpGet("Profiles/Browse")]
        public async Task<IActionResult> BrowseUsers(string searchQuery) // Adiciona searchQuery para pesquisa
        {
            var loggedInUserEmail = User.Identity?.Name;
            Users? loggedInAppUser = null;

            if (!string.IsNullOrEmpty(loggedInUserEmail))
            {
                loggedInAppUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == loggedInUserEmail);
            }

            IQueryable<Users> query = _context.Users
                                              .Include(u => u.Profile) // Incluir perfil para mostrar nome e foto
                                              .OrderBy(u => u.Username); // Ordenar por nome de utilizador

            if (loggedInAppUser != null)
            {
                // Excluir o próprio utilizador logado da lista de "outros utilizadores"
                query = query.Where(u => u.Id != loggedInAppUser.Id);
            }

            // Filtro de pesquisa (simples, pelo Username ou Nome do Perfil)
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(u => u.Username.Contains(searchQuery) ||
                                         (u.Profile != null && u.Profile.Name.Contains(searchQuery)));
            }

            var users = await query.ToListAsync();

            ViewData["CurrentFilter"] = searchQuery; // Para manter o valor na caixa de pesquisa
            return View("BrowseUsersView", users); // Uma nova view para listar os utilizadores
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

            // É crucial incluir FriendshipsInitiated e FriendshipsReceived para obter os amigos
            var appUser = await _context.Users
                .Include(u => u.FriendshipsInitiated) // Amizades que este user iniciou
                    .ThenInclude(fi => fi.User2)      // O amigo (User2) nestas amizades
                        .ThenInclude(u2 => u2.Profile) // O perfil do amigo (User2)
                .Include(u => u.FriendshipsReceived) // Amizades que este user recebeu
                    .ThenInclude(fr => fr.User1)      // O amigo (User1) nestas amizades
                        .ThenInclude(u1 => u1.Profile) // O perfil do amigo (User1)
                .FirstOrDefaultAsync(u => u.Username == userEmail);

            if (appUser == null)
            {
                return NotFound("Utilizador não encontrado.");
            }

            var profile = await _context.Profiles
                .Where(p => p.UserId == appUser.Id)
                .Include(p => p.User) // O User já foi parcialmente carregado acima, mas isto garante consistência
                .Include(p => p.SavedTopics).ThenInclude(st => st.Topic).ThenInclude(t => t.Creator).ThenInclude(c => c.User)
                .FirstOrDefaultAsync();

            if (profile == null)
            {
                return NotFound("Perfil não encontrado. Por favor, crie ou complete o seu perfil.");
            }

            // --- Popular DisplaySavedTopics (como antes) ---
            if (profile.SavedTopics != null)
            {
                profile.DisplaySavedTopics = profile.SavedTopics
                    .OrderByDescending(st => st.SavedAt)
                    .Select(st => st.Topic)
                    .ToList();
            }

            // --- Fetch Personal Topic and its Posts (como antes) ---
            profile.PersonalTopic = await _context.Topics
                .Include(t => t.Posts.OrderByDescending(p => p.CreatedAt))
                    .ThenInclude(tp => tp.Profile)
                .FirstOrDefaultAsync(t => t.CreatedBy == profile.Id && t.IsPersonal);

            if (profile.PersonalTopic != null)
            {
                profile.PersonalTopicPosts = profile.PersonalTopic.Posts.ToList();
            }

            // --- Fetch OTHER Created Topics (como antes) ---
            profile.CreatedTopics = await _context.Topics
                .Where(t => t.CreatedBy == profile.Id && !t.IsPersonal)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // --- CARREGAR AMIGOS ---
            var friendsList = new List<Users>();
            // Amigos de amizades iniciadas pelo utilizador atual
            friendsList.AddRange(appUser.FriendshipsInitiated
                .Where(f => f.Status == FriendshipStatus.Accepted)
                .Select(f => f.User2)); // User2 é o amigo

            // Amigos de amizades recebidas pelo utilizador atual
            friendsList.AddRange(appUser.FriendshipsReceived
                .Where(f => f.Status == FriendshipStatus.Accepted)
                .Select(f => f.User1)); // User1 é o amigo

            // Garantir que não há duplicados e ordenar (opcional)
            profile.Friends = friendsList.DistinctBy(u => u.Id).OrderBy(u => u.Username).ToList();

            Console.WriteLine($"Loaded {profile.Friends.Count} friends for display.");

            return View("Index", profile); // Ou o nome da sua view MyProfile
        }

        // GET: Profiles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            // Include User for display
            // Include saved topics count/list if you want to show it on other people's profiles too
            var profile = await _context.Profiles
                .Include(p => p.User)
                // Example: Include count of saved topics for general profiles
                // .Include(p => p.SavedTopics)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (profile == null) return NotFound();

            return View(profile);
        }


        // GET: Profiles/Create
        public IActionResult Create()
        {
            // Should likely only be accessible if user DOES NOT have a profile yet
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail)) return Unauthorized();
            var appUser = _context.Users.FirstOrDefault(u => u.Username == userEmail);
            if (appUser != null)
            {
                bool profileExists = _context.Profiles.Any(p => p.UserId == appUser.Id);
                if (profileExists)
                {
                    TempData["InfoMessage"] = "Já possui um perfil.";
                    return RedirectToAction(nameof(MyProfile));
                }
            }
            // Maybe pre-populate User details if possible, but UserId will be set on POST
            return View();
        }

        // POST: Profiles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Type,Bio,ProfilePicture")] Profile profile) 
        {
            var identityName = User.Identity?.Name;
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == identityName);
            if (appUser == null) return Unauthorized();

            bool profileExists = await _context.Profiles.AnyAsync(p => p.UserId == appUser.Id);
            if (profileExists)
            {
                ModelState.AddModelError(string.Empty, "Já existe um perfil para este utilizador.");
            }

            profile.UserId = appUser.Id;
            profile.CreatedAt = DateTime.UtcNow;

            // --- Create the Personal Topic ---
            var personalTopic = new Topic
            {
                Title = $"Perfil de {profile.Name ?? appUser.Username}", 
                Description = $"Posts pessoais de {profile.Name ?? appUser.Username}.",
                IsPersonal = true,
                IsPrivate = true, // Personal topics are usually private to the profile context
                Creator = profile, // Link via navigation property
                CreatedAt = DateTime.UtcNow
                // CreatedBy will be set automatically if Creator is set and relationship is configured,
                // otherwise set profile.Id after saving profile but before saving topic (needs two steps)
            };
            // Link the profile to the topic
            profile.PersonalTopic = personalTopic; // Link navigation property

            ModelState.Remove("User");
            ModelState.Remove("SavedTopics");
            ModelState.Remove("DisplaySavedTopics");
            ModelState.Remove("PersonalTopic"); // Let EF handle this via navigation property
            ModelState.Remove("PersonalTopicPosts");
            ModelState.Remove("CreatedTopics");
            ModelState.Remove("UserId");
            ModelState.Remove("Id");
            ModelState.Remove("CreatedAt");


            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(profile.ProfilePicture))
                {
                    profile.ProfilePicture = "/images/defaultuser.png";
                }

                _context.Add(profile); // This will also add the linked personalTopic due to the relationship
                await _context.SaveChangesAsync();

                // --- Verification (Optional): Ensure Personal Topic got the Profile ID ---
                var createdProfile = await _context.Profiles.Include(p => p.PersonalTopic).FirstOrDefaultAsync(p => p.UserId == appUser.Id);
                if (createdProfile?.PersonalTopic != null && createdProfile.PersonalTopic.CreatedBy == 0)
                {
                    // If CreatedBy wasn't set automatically, update it now
                    createdProfile.PersonalTopic.CreatedBy = createdProfile.Id;
                    _context.Update(createdProfile.PersonalTopic);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Personal Topic CreatedBy explicitly set to Profile ID: {createdProfile.Id}");
                }
                else if (createdProfile?.PersonalTopic != null)
                {
                    Console.WriteLine($"Personal Topic correctly linked with CreatedBy: {createdProfile.PersonalTopic.CreatedBy}");
                }
                else
                {
                    Console.WriteLine($"Warning: Personal Topic not found or not linked after saving profile.");
                }
                // --- End Verification ---

                TempData["SuccessMessage"] = "Perfil e tópico pessoal criados com sucesso!";
                return RedirectToAction(nameof(MyProfile));
            }

            Console.WriteLine("ModelState inválido no Create POST:");
            LogModelStateErrors(ModelState);

            return View(profile);
        }

        // GET: Profiles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var identityName = User.Identity?.Name;
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == identityName);
            if (appUser == null) return Unauthorized();

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.Id == id && p.UserId == appUser.Id);

            if (profile == null)
            {
                TempData["ErrorMessage"] = "Perfil não encontrado ou não tem permissão para o editar.";
                return RedirectToAction(nameof(MyProfile));
            }
            return View(profile);
        }


        // POST: Profiles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Type,Bio")] Profile profileViewModel, IFormFile? ProfilePictureFile)
        {
            var identityName = User.Identity?.Name;
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == identityName);
            if (appUser == null) return Unauthorized("Utilizador não encontrado.");

            var profileToUpdate = await _context.Profiles.FirstOrDefaultAsync(p => p.Id == id && p.UserId == appUser.Id);
            if (profileToUpdate == null) return NotFound("Perfil não encontrado ou não pertence ao utilizador atual.");

            profileToUpdate.Name = profileViewModel.Name;
            profileToUpdate.Bio = profileViewModel.Bio;
            profileToUpdate.Type = profileViewModel.Type;

            ModelState.Remove("User");
            ModelState.Remove("UserId");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("ProfilePicture");
            ModelState.Remove("SavedTopics");
            ModelState.Remove("DisplaySavedTopics");
            ModelState.Remove("PersonalTopic");
            ModelState.Remove("PersonalTopicPosts");
            ModelState.Remove("CreatedTopics");

            if (ProfilePictureFile != null && ProfilePictureFile.Length > 0)
            {
                Console.WriteLine($"Processing uploaded file: {ProfilePictureFile.FileName}");
                try
                {
                    // (File saving logic - same as before)
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profile");
                    Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ProfilePictureFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    if (!string.IsNullOrEmpty(profileToUpdate.ProfilePicture) && profileToUpdate.ProfilePicture != "/images/defaultuser.png" && !profileToUpdate.ProfilePicture.StartsWith("/images/profile/"))
                    {
                        // Handle potential old default or unexpected path format if needed
                        Console.WriteLine($"Old profile picture path might not be standard: {profileToUpdate.ProfilePicture}");
                    }
                    else if (!string.IsNullOrEmpty(profileToUpdate.ProfilePicture) && profileToUpdate.ProfilePicture != "/images/defaultuser.png")
                    {
                        try
                        {
                            var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", profileToUpdate.ProfilePicture.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                                Console.WriteLine($"Deleted old profile picture: {oldFilePath}");
                            }
                            else
                            {
                                Console.WriteLine($"Old profile picture file not found: {oldFilePath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error deleting old profile picture: {ex.Message}");
                            // Log but don't prevent new upload
                        }
                    }


                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProfilePictureFile.CopyToAsync(stream);
                    }
                    profileToUpdate.ProfilePicture = "/images/profile/" + uniqueFileName;
                    Console.WriteLine($"Profile picture path updated to: {profileToUpdate.ProfilePicture}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error uploading profile picture: {ex.Message}");
                    ModelState.AddModelError(nameof(ProfilePictureFile), "Erro ao carregar a imagem.");
                    return View(profileToUpdate);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Perfil atualizado com sucesso!";
                    return RedirectToAction(nameof(MyProfile));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Console.WriteLine($"Concurrency Error: {ex.Message}");
                    ModelState.AddModelError(string.Empty, "Os dados foram modificados por outro utilizador. Recarregue a página e tente novamente.");
                    var entry = ex.Entries.Single();
                    await entry.ReloadAsync();
                    ModelState.AddModelError(string.Empty, "Os valores atuais da base de dados foram carregados.");
                    // Return the reloaded entity for the user to review/resubmit
                    return View(await _context.Profiles.FindAsync(id)); // Re-fetch the potentially changed data
                }
                catch (DbUpdateException ex)
                {
                    Console.WriteLine($"Database Error: {ex.InnerException?.Message ?? ex.Message}");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao guardar na base de dados.");
                }
            }

            Console.WriteLine("ModelState inválido ou erro ao guardar. Retornando View.");
            LogModelStateErrors(ModelState);
            return View(profileToUpdate);
        }


        // GET: Profiles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var identityName = User.Identity?.Name;
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == identityName);
            if (appUser == null) return Unauthorized();

            var profile = await _context.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == appUser.Id);

            if (profile == null)
            {
                TempData["ErrorMessage"] = "Perfil não encontrado ou não tem permissão para o excluir.";
                return RedirectToAction(nameof(MyProfile));
            }
            return View(profile);
        }

        // POST: Profiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var identityName = User.Identity?.Name;
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == identityName);
            if (appUser == null) return Unauthorized();

            // Find profile including related data that might prevent deletion
            var profile = await _context.Profiles
                .Include(p => p.PersonalTopic) // Need to delete this too potentially
                .Include(p => p.CreatedTopics) // Check if creator of other topics
                .Include(p => p.SavedTopics)   // Need to delete these links
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == appUser.Id);

            if (profile == null)
            {
                TempData["ErrorMessage"] = "Perfil não encontrado ou não autorizado a excluir.";
                return RedirectToAction(nameof(MyProfile));
            }

            try
            {
                // --- Manual Cascade Deletion (if needed or safer) ---

                // 1. Remove SavedTopic links
                if (profile.SavedTopics.Any())
                {
                    _context.SavedTopics.RemoveRange(profile.SavedTopics);
                    Console.WriteLine($"Removed {profile.SavedTopics.Count} SavedTopic entries for profile {id}.");
                }

                // 2. Handle Personal Topic Posts (if cascade delete isn't set up properly for Topic->Posts)
                //    Usually Topic deletion handles this, but check your DbContext OnModelCreating.

                // 3. Delete Personal Topic (if it exists)
                if (profile.PersonalTopic != null)
                {
                    // Need to load posts of personal topic if not already loaded and cascade isn't reliable
                    var personalTopicPosts = await _context.TopicPosts.Where(tp => tp.TopicId == profile.PersonalTopic.Id).ToListAsync();
                    if (personalTopicPosts.Any())
                    {
                        _context.TopicPosts.RemoveRange(personalTopicPosts);
                        Console.WriteLine($"Removed {personalTopicPosts.Count} posts from personal topic {profile.PersonalTopic.Id}.");
                    }
                    _context.Topics.Remove(profile.PersonalTopic);
                    Console.WriteLine($"Removed personal topic {profile.PersonalTopic.Id} for profile {id}.");
                }

                // 4. Handle Other Created Topics? (Decide policy: Delete them? Orphan them? Prevent profile deletion?)
                //    Current logic seems to prevent deletion of creator if topics exist (FK constraint).
                //    For now, let's assume profile deletion is blocked if other topics exist,
                //    or they need to be deleted/reassigned manually first.

                // 5. Handle Posts created by this profile in OTHER topics (TopicPosts where ProfileId = profile.Id)
                //    Similar decision: Delete them? Orphan them? Prevent deletion?
                //    Let's assume deletion is blocked by FK or needs manual cleanup.

                // 6. Handle Comments by this profile... (similar decision)

                // 7. Finally, remove the profile itself
                _context.Profiles.Remove(profile);
                Console.WriteLine($"Removing profile {id}.");

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Perfil excluído com sucesso.";

                // Sign user out after deleting their profile data
                // This requires `SignInManager<ApplicationUser>` dependency injection if using Identity
                // await _signInManager.SignOutAsync(); // Uncomment if using Identity SignInManager
                // For now, just redirect to home
                return RedirectToAction("Index", "Home");
            }
            catch (DbUpdateException ex) // Catch potential FK constraint violations
            {
                Console.WriteLine($"Error deleting profile {id}: {ex.InnerException?.Message ?? ex.Message}");
                TempData["ErrorMessage"] = "Não foi possível excluir o perfil. Pode ser necessário remover manualmente os tópicos criados, posts ou comentários associados primeiro.";
                // You could try to provide more specific error messages based on `ex.InnerException` if needed.
                return RedirectToAction(nameof(Delete), new { id = id });
            }
        }

        // GET: Profiles/User/{usernameOrId}
        [HttpGet("Profiles/User/{identifier}")] // Rota mais descritiva
        public async Task<IActionResult> UserProfile(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return BadRequest("Identificador do utilizador não fornecido.");
            }

            Users? targetAppUser = null;

            // Tentar encontrar por ID numérico primeiro
            if (int.TryParse(identifier, out int userId))
            {
                targetAppUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            }

            // Se não encontrou por ID ou não era um ID numérico, tentar por Username
            if (targetAppUser == null)
            {
                targetAppUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == identifier);
            }

            if (targetAppUser == null)
            {
                return NotFound("Utilizador não encontrado.");
            }

            // --- Obter o utilizador LOGADO (para verificar se é o próprio perfil ou para o status de amizade) ---
            var loggedInUserEmail = User.Identity?.Name; // Username do utilizador logado
            Users? loggedInAppUser = null;
            bool isOwnProfile = false;

            if (!string.IsNullOrEmpty(loggedInUserEmail))
            {
                loggedInAppUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == loggedInUserEmail);
                if (loggedInAppUser != null && loggedInAppUser.Id == targetAppUser.Id)
                {
                    isOwnProfile = true;
                }
            }

            // Se for o próprio perfil, podemos redirecionar para MyProfile para consistência de UI
            // ou simplesmente continuar e adaptar a View para mostrar/esconder botões.
            // Redirecionar pode ser mais limpo para evitar duplicar muita lógica de "dono do perfil".
            if (isOwnProfile)
            {
                // Opcional: Redirecionar para a experiência de "MyProfile"
                // return RedirectToAction("MyProfile");
                // Ou continuar e a View terá que lidar com isso. Para este exemplo, vamos continuar.
            }


            var profile = await _context.Profiles
                .Where(p => p.UserId == targetAppUser.Id)
                .Include(p => p.User) // Inclui o Users dono do Profile
                                      // Não precisamos de 'SavedTopics' do *targetUser* para mostrar ao *loggedInUser*
                                      // A menos que queira mostrar "Tópicos guardados por este utilizador" (se forem públicos)
                .FirstOrDefaultAsync();

            if (profile == null)
            {
                // Mesmo que o utilizador exista, pode não ter um perfil criado.
                // Você pode querer mostrar uma página básica com o username e uma mensagem.
                // Ou criar um perfil 'vazio' on-the-fly para visualização (menos comum).
                // Por agora, retornamos NotFound se o Profile específico não existir.
                return NotFound($"Perfil para {targetAppUser.Username} não encontrado.");
            }

            // --- Popular Tópicos Criados (Públicos) pelo targetUser ---
            // Apenas tópicos que não são privados E não são o tópico pessoal (a menos que queira mostrar posts do perfil)
            profile.CreatedTopics = await _context.Topics
                                       .Where(t => t.CreatedBy == profile.Id && !t.IsPersonal && !t.IsPrivate) // Só públicos e não pessoais
                                       .OrderByDescending(t => t.CreatedAt)
                                       .ToListAsync();

            // --- Popular Posts do Tópico Pessoal (se quiser mostrá-los) ---
            // Considere se os posts no tópico pessoal devem ser visíveis para outros.
            // Se sim, certifique-se que o tópico pessoal não é `IsPrivate`.
            profile.PersonalTopic = await _context.Topics
                .Include(t => t.Posts.OrderByDescending(post => post.CreatedAt)) // Ordenar posts aqui
                    .ThenInclude(tp => tp.Profile) // Profile do criador do Post (que será o dono do perfil)
                .FirstOrDefaultAsync(t => t.CreatedBy == profile.Id && t.IsPersonal && !t.IsPrivate); // Apenas se o tópico pessoal for público

            if (profile.PersonalTopic != null)
            {
                profile.PersonalTopicPosts = profile.PersonalTopic.Posts.ToList();
            }
            else
            {
                profile.PersonalTopicPosts = new List<TopicPost>();
            }

            // Passar um ViewModel se precisar de mais dados (como o status da amizade)
            var viewModel = new UserProfileViewModel
            {
                Profile = profile,
                IsOwnProfile = isOwnProfile,
                LoggedInUserId = loggedInAppUser?.Id // Passa o ID do utilizador logado para o JavaScript
                                                     // FriendshipStatus será carregado por JavaScript
            };

            return View("UserProfileView", viewModel); // Usar uma nova View
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


        // Helper to log ModelState errors
        private void LogModelStateErrors(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
        {
            foreach (var key in modelState.Keys)
            {
                var state = modelState[key];
                if (state.Errors.Any())
                {
                    Console.WriteLine($"  - {key}: {string.Join("; ", state.Errors.Select(e => e.ErrorMessage))}");
                }
            }
        }


    }
}