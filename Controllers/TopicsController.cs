using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
            // Carregar Tópicos (com Categoria incluída para a tabela principal)
            var topics = await _context.Topics
                                     .Include(t => t.Creator)
                                        .ThenInclude(c => c.User)
                                     .Include(t => t.Category) 
                                     .Where(t => !t.IsPersonal)
                                     .OrderByDescending(t => t.CreatedAt)
                                     .ToListAsync();

            // Carregar Categorias para exibir no topo
            var categories = await _context.Categories
                                         // .Include(c => c.Topics) // Opcional: incluir para contar ou ordenar por popularidade
                                         .OrderBy(c => c.Name) // Ordenar alfabeticamente, por exemplo
                                         .ToListAsync();
            ViewData["CategoriesList"] = categories; // Passar a lista para a view

            // Lógica para obter o ProfileID do utilizador logado (se estiver logado)
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var email = User.Identity.Name;
                if (!string.IsNullOrEmpty(email))
                {
                    var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
                    if (appUser != null)
                    {
                        var userProfile = await _context.Profiles.AsNoTracking()
                                                .FirstOrDefaultAsync(p => p.UserId == appUser.Id);
                        if (userProfile != null)
                        {
                            ViewData["CurrentUserProfileId"] = userProfile.Id;
                        }
                    }
                }
            }

            return View(topics);
        }

        // GET: Topics/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics
                .Include(t => t.Creator)
                    .ThenInclude(c => c.User)
                .Include(t => t.Category)
                .Include(t => t.Posts) // Load Posts
                    .ThenInclude(post => post.Profile) // For each Post, load its author's Profile
                        .ThenInclude(authorProfile => authorProfile.User) // And the User for that Profile
                .Include(t => t.Posts) // Still on Posts, now load Comments for each Post
                    .ThenInclude(post => post.Comments) // For each Post, load its Comments collection
                        .ThenInclude(comment => comment.Profile) // For each Comment, load the commenter's Profile
                            .ThenInclude(commenterProfile => commenterProfile.User) // And the User for that Profile
                // --- ADDED THIS SECTION TO LOAD POST LIKES ---
                .Include(t => t.Posts)
                .ThenInclude(post => post.Likes)
                // --- ADDED THIS SECTION TO LOAD COMMENT LIKES ---
                .Include(t => t.Posts)
                .ThenInclude(p => p.Comments)
                .ThenInclude(c => c.Likes)
                
                .FirstOrDefaultAsync(m => m.Id == id);

            if (topic == null)
            {
                return NotFound();
            }

            if (topic.Posts != null)
            {
                // Order posts
                var orderedPosts = topic.Posts.OrderByDescending(p => p.CreatedAt).ToList();
        
                // For each post, order its comments
                foreach (var post in orderedPosts)
                {
                    if (post.Comments != null)
                    {
                        // Order comments by creation date (e.g., oldest first, or newest first)
                        post.Comments = post.Comments.OrderBy(c => c.CreatedAt).ToList(); 
                        // Or use .OrderByDescending(c => c.CreatedAt) for newest first
                    }
                }
                topic.Posts = orderedPosts;
            }

            bool isCurrentUserTheCreator = false;
            int? currentUserProfileId = null;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var email = User.Identity.Name;
                if (!string.IsNullOrEmpty(email))
                {
                    var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
                    if (appUser != null)
                    {
                        var userProfile = await _context.Profiles.AsNoTracking()
                                                .FirstOrDefaultAsync(p => p.UserId == appUser.Id);
                        if (userProfile != null)
                        {
                            currentUserProfileId = userProfile.Id;
                            if (topic.Creator != null && topic.CreatedBy == userProfile.Id)
                            {
                                isCurrentUserTheCreator = true;
                            }
                        }
                    }
                }
            }

            ViewBag.IsCurrentUserTheCreator = isCurrentUserTheCreator;
            ViewBag.CurrentUserProfileId = currentUserProfileId;

            return View(topic);
        }

        // GET: Topics/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            ViewData["CategoryId"] = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Topics/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        // <<< ALTERADO: Adicionado CategoryId ao Bind
        public async Task<IActionResult> Create([Bind("IsPrivate,Title,Description,CategoryId")] Topic topic, IFormFile? topicImageFile)
        {
            topic.IsPersonal = false;
            topic.IsPrivate = false;
            topic.CreatedAt = DateTime.UtcNow;

            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Utilizador não autenticado.");
            }

            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
            if (appUser == null)
            {
                return NotFound("Utilizador não encontrado na base de dados.");
            }

            var userProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);
            if (userProfile == null)
            {
                TempData["ErrorMessage"] = "Precisa de criar um perfil antes de poder criar tópicos.";
                return RedirectToAction("MyProfile", "Profiles");
            }
            topic.CreatedBy = userProfile.Id;

            ModelState.Remove(nameof(Topic.Id));
            ModelState.Remove(nameof(Topic.IsPersonal));
            ModelState.Remove(nameof(Topic.CreatedBy));
            ModelState.Remove(nameof(Topic.Creator));
            ModelState.Remove(nameof(Topic.CreatedAt));
            ModelState.Remove(nameof(Topic.TopicImageUrl));
            ModelState.Remove(nameof(Topic.Posts));
            ModelState.Remove(nameof(Topic.Savers));
            ModelState.Remove(nameof(Topic.Category)); 

            if (ModelState.IsValid)
            {
                if (topicImageFile != null && topicImageFile.Length > 0)
                {
                    string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string uploadsFolder = Path.Combine(wwwRootPath, "images", "topics");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(topicImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    try
                    {
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await topicImageFile.CopyToAsync(fileStream);
                        }
                        topic.TopicImageUrl = "/images/topics/" + uniqueFileName;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao carregar a imagem do tópico: {ex.Message}");
                        ModelState.AddModelError("topicImageFile", $"Erro ao carregar a imagem: {ex.Message}");
                        topic.TopicImageUrl = "/images/topics/default_topic.jpeg";
                    }
                }
                else
                {
                    topic.TopicImageUrl = "/images/topics/default_topic.jpeg";
                }

                _context.Add(topic);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tópico criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            // <<< ALTERADO: Recarregar ViewData["CategoryId"] se ModelState for inválido
            ViewData["CategoryId"] = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", topic.CategoryId);
            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                if (state.Errors.Any())
                {
                    Console.WriteLine($"Erro no campo '{key}': {string.Join("; ", state.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            return View(topic);
        }

        // GET: Topics/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //Incluir Categoria ao carregar para edição
            var topic = await _context.Topics.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == id);
            if (topic == null)
            {
                return NotFound();
            }

            var email = User.Identity?.Name;
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
            if (appUser == null) return Unauthorized();
            var userProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);
            if (userProfile == null || topic.CreatedBy != userProfile.Id)
            {
                return Forbid("Não tem permissão para editar este tópico.");
            }

            //Carregar ViewData["CategoryId"] para o dropdown
            ViewData["CategoryId"] = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", topic.CategoryId);
            return View(topic);
        }

        // POST: Topics/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,TopicImageUrl,IsPrivate,CategoryId")] Topic updatedTopicViewModel, IFormFile? topicImageFile)
        {
            if (id != updatedTopicViewModel.Id)
            {
                return NotFound("ID do tópico inválido.");
            }

            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
            if (appUser == null) return Unauthorized("Utilizador não encontrado.");

            var userProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);
            if (userProfile == null) return Unauthorized("Perfil do utilizador não encontrado.");

            var existingTopicForAuth = await _context.Topics.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (existingTopicForAuth == null)
            {
                return NotFound("Tópico não encontrado.");
            }
            if (existingTopicForAuth.CreatedBy != userProfile.Id)
            {
                return Forbid("Não tem permissão para editar este tópico.");
            }

            // Atribuir valores não editáveis explicitamente ao ViewModel antes da validação, se necessário
            // No entanto, para CategoryId, queremos o valor do formulário.
            // Vamos buscar o tópico real para atualizar depois da validação.

            ModelState.Remove(nameof(Topic.Creator));
            ModelState.Remove(nameof(Topic.CreatedBy));
            ModelState.Remove(nameof(Topic.CreatedAt));
            ModelState.Remove(nameof(Topic.IsPersonal));
            ModelState.Remove(nameof(Topic.Posts));
            ModelState.Remove(nameof(Topic.Savers));
            ModelState.Remove(nameof(Topic.Category));

            if (ModelState.IsValid)
            {
                try
                {
                    var topicToUpdate = await _context.Topics.FindAsync(id);
                    if (topicToUpdate == null) return NotFound("Tópico desapareceu durante a edição.");

                    // Lógica de imagem (mantida)
                    string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string currentImageUrlOnDb = topicToUpdate.TopicImageUrl; // Usar o valor da BD para a imagem antiga

                    if (topicImageFile != null && topicImageFile.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(currentImageUrlOnDb) && currentImageUrlOnDb != "/images/topics/default_topic.jpeg")
                        {
                            string oldImagePath = Path.Combine(wwwRootPath, currentImageUrlOnDb.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                try { System.IO.File.Delete(oldImagePath); Console.WriteLine($"Imagem antiga '{oldImagePath}' eliminada."); }
                                catch (Exception ex) { Console.WriteLine($"Erro ao eliminar imagem antiga '{oldImagePath}': {ex.Message}"); }
                            }
                        }
                        string uploadsFolder = Path.Combine(wwwRootPath, "images", "topics");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(topicImageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create)) { await topicImageFile.CopyToAsync(fileStream); }
                        topicToUpdate.TopicImageUrl = "/images/topics/" + uniqueFileName;
                        Console.WriteLine($"Nova imagem do tópico guardada: {topicToUpdate.TopicImageUrl}");
                    }
                    else if (updatedTopicViewModel.TopicImageUrl != currentImageUrlOnDb) // Se nenhuma nova imagem foi enviada, mas o campo hidden TopicImageUrl mudou (ex: foi limpo intencionalmente)
                    {
                        // Esta lógica pode precisar de ajuste se você permitir limpar a imagem para voltar à padrão via formulário
                        // Por agora, se não há ficheiro novo, mantemos a imagem que já estava ou a que veio no hidden field
                        // A atribuição abaixo (topicToUpdate.TopicImageUrl = updatedTopicViewModel.TopicImageUrl) lida com isto
                        // se o campo hidden TopicImageUrl estiver correto.
                        // No entanto, é mais seguro: se não houver ficheiro, não mudar topicToUpdate.TopicImageUrl aqui,
                        // e deixar que a atribuição geral abaixo o faça, ou explicitamente:
                        // topicToUpdate.TopicImageUrl = updatedTopicViewModel.TopicImageUrl;
                    }


                    // Atualizar propriedades do topicToUpdate com os valores do ViewModel
                    topicToUpdate.Title = updatedTopicViewModel.Title;
                    topicToUpdate.Description = updatedTopicViewModel.Description;
                    // TopicImageUrl já foi atualizado acima se um novo ficheiro foi enviado.
                    // Se nenhum novo ficheiro foi enviado, e o hidden field `TopicImageUrl` tem um valor,
                    // ele será usado se atribuirmos explicitamente (ou se a propriedade TopicImageUrl no topicToUpdate não for tocada e o ViewModel o tiver):
                    if (topicImageFile == null) // Se não houve upload de novo ficheiro
                    {
                        topicToUpdate.TopicImageUrl = updatedTopicViewModel.TopicImageUrl; // Usar valor do campo hidden
                    }
                    topicToUpdate.IsPrivate = updatedTopicViewModel.IsPrivate;
                    topicToUpdate.CategoryId = updatedTopicViewModel.CategoryId; // <<< ADICIONADO: Atualizar CategoryId

                    _context.Update(topicToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Tópico atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Topics.Any(t => t.Id == updatedTopicViewModel.Id)) return NotFound();
                    ModelState.AddModelError(string.Empty, "O tópico foi modificado. Recarregue a página.");
                    var currentValues = await _context.Topics.AsNoTracking().Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == id);
                    ViewData["CategoryId"] = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", currentValues?.CategoryId);
                    return View(currentValues);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao editar tópico: {ex.Message}");
                    ModelState.AddModelError(string.Empty, $"Ocorreu um erro: {ex.Message}");
                    ViewData["CategoryId"] = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", updatedTopicViewModel.CategoryId);
                    return View(updatedTopicViewModel);
                }
                return RedirectToAction(nameof(Index));
            }

            Console.WriteLine("ModelState inválido no Edit POST:");
            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                if (state.Errors.Any()) { Console.WriteLine($"  - {key}: {string.Join("; ", state.Errors.Select(e => e.ErrorMessage))}"); }
            }
            // <<< ALTERADO: Recarregar ViewData["CategoryId"] se ModelState for inválido
            ViewData["CategoryId"] = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", updatedTopicViewModel.CategoryId);
            return View(updatedTopicViewModel);
        }

        // GET: Topics/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics
                .Include(t => t.Creator)
                    .ThenInclude(c => c.User)
                .Include(t => t.Category) 
                .FirstOrDefaultAsync(m => m.Id == id);

            if (topic == null)
            {
                return NotFound();
            }

            var email = User.Identity?.Name;
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
            if (appUser == null) return Unauthorized();
            var userProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);

            if (userProfile == null || topic.CreatedBy != userProfile.Id)
            {
                return Forbid("Não tem permissão para eliminar este tópico.");
            }

            if (topic.IsPersonal && topic.IsPrivate)
            {
                TempData["ErrorMessage"] = "Não pode eliminar o seu tópico de perfil pessoal diretamente.";
                return RedirectToAction("MyProfile", "Profiles");
            }

            return View(topic);
        }

        // POST: Topics/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
            if (user == null) return Unauthorized();

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null) return Unauthorized();

            var topic = await _context.Topics.FindAsync(id);
            if (topic == null)
            {
                return NotFound();
            }

            if (topic.CreatedBy != profile.Id)
            {
                return Forbid("Não tem permissão para eliminar este tópico.");
            }

            if (topic.IsPersonal && topic.IsPrivate)
            {
                TempData["ErrorMessage"] = "Não pode eliminar o seu tópico de perfil pessoal diretamente.";
                return RedirectToAction("MyProfile", "Profiles");
            }

            try
            {
                if (!string.IsNullOrEmpty(topic.TopicImageUrl) && topic.TopicImageUrl != "/images/topics/default_topic.jpeg") 
                {
                    string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string imagePath = Path.Combine(wwwRootPath, topic.TopicImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                        Console.WriteLine($"Imagem do tópico '{imagePath}' eliminada.");
                    }
                }
                _context.Topics.Remove(topic);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tópico eliminado com sucesso!";
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Erro ao eliminar tópico (DbUpdateException): {ex.InnerException?.Message ?? ex.Message}");
                TempData["ErrorMessage"] = "Não foi possível eliminar o tópico. Pode ter publicações ou outras dependências.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao eliminar tópico: {ex.Message}");
                TempData["ErrorMessage"] = "Ocorreu um erro ao tentar eliminar o tópico.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Topics/SaveTopic/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SaveTopic(int id)
        {
            var topicToSave = await _context.Topics.FindAsync(id);
            if (topicToSave == null)
            {
                return NotFound("Tópico não encontrado.");
            }

            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("Utilizador não identificado.");
            }

            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == userEmail);
            if (appUser == null)
            {
                return Unauthorized("Conta de utilizador não encontrada.");
            }

            var userProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);
            if (userProfile == null)
            {
                TempData["ErrorMessage"] = "Precisa de criar um perfil antes de poder guardar tópicos.";
                return RedirectToAction("MyProfile", "Profiles");
            }
            int profileId = userProfile.Id;

            bool alreadySaved = await _context.SavedTopics
                                              .AnyAsync(st => st.TopicId == id && st.ProfileId == profileId);

            if (!alreadySaved)
            {
                var savedTopicEntry = new SavedTopic
                {
                    ProfileId = profileId,
                    TopicId = id,
                    SavedAt = DateTime.UtcNow
                };
                _context.SavedTopics.Add(savedTopicEntry);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tópico guardado com sucesso!";
            }
            else
            {
                TempData["InfoMessage"] = "Este tópico já está na sua lista de guardados.";
            }

            string? returnUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }


        // POST: Topics/UnsaveTopic/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UnsaveTopic(int id)
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("Utilizador não identificado.");
            }

            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == userEmail);
            if (appUser == null)
            {
                return Unauthorized("Conta de utilizador não encontrada.");
            }

            var userProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);
            if (userProfile == null)
            {
                return NotFound("Perfil do utilizador não encontrado.");
            }
            int profileId = userProfile.Id;

            var savedTopicEntry = await _context.SavedTopics
                                                .FirstOrDefaultAsync(st => st.TopicId == id && st.ProfileId == profileId);

            if (savedTopicEntry != null)
            {
                _context.SavedTopics.Remove(savedTopicEntry);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tópico removido da sua lista de guardados.";
            }
            else
            {
                TempData["InfoMessage"] = "Este tópico não estava na sua lista de guardados.";
            }

            string? returnUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("MyProfile", "Profiles");
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] 
        public async Task<IActionResult> AddComment(int topicPostId, string content, string? returnUrl)
        {
            // 1. Get the logged-in user's ProfileId
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Utilizador não autenticado.";
                return RedirectToLocal(returnUrl); 
            }

            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == userEmail);
            if (appUser == null)
            {
                TempData["ErrorMessage"] = "Utilizador não encontrado.";
                return RedirectToLocal(returnUrl);
            }

            var userProfile = await _context.Profiles.AsNoTracking()
                                            .FirstOrDefaultAsync(p => p.UserId == appUser.Id);
            if (userProfile == null)
            {
                TempData["ErrorMessage"] = "Perfil não encontrado. Por favor, crie um perfil para comentar.";
                return RedirectToLocal(returnUrl);
            }

            // 2. Validate input
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "O conteúdo do comentário não pode estar vazio.";
                return RedirectToLocal(returnUrl);
            }
            if (content.Length > 1000) 
            {
                TempData["ErrorMessage"] = "O comentário excede o limite de 1000 caracteres.";
                return RedirectToLocal(returnUrl);
            }

            // 3. Check if the TopicPost exists
            var postExists = await _context.TopicPosts.AnyAsync(p => p.Id == topicPostId);
            if (!postExists)
            {
                TempData["ErrorMessage"] = "A publicação à qual está a tentar comentar não foi encontrada.";
                return RedirectToLocal(returnUrl);
            }

            // 4. Create and save the comment
            var newComment = new TopicComment
            {
                TopicPostId = topicPostId,
                ProfileId = userProfile.Id, 
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.TopicComments.Add(newComment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comentário adicionado com sucesso!";

            // 5. Redirect back to the page
            return RedirectToLocal(returnUrl);
        }

        // Helper method for safe redirection
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                // Fallback to a default page, e.g., Topic Index or Home
                return RedirectToAction(nameof(Index)); 
            }
        }

    }
}