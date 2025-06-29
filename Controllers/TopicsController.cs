using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using System.IO;

namespace projetodweb_connectify.Controllers
{
    [Authorize] // Aplicar autorização a nível de controlador se a maioria das ações a exigir.
    public class TopicsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TopicsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: Apresenta a página principal de tópicos, listando tópicos públicos e categorias.
        /// </summary>
        [AllowAnonymous] // Permitir que utilizadores não autenticados vejam a lista de tópicos.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Carrega os tópicos públicos, incluindo dados do criador e da categoria para exibição.
            var topics = await _context.Topics
                                     .Include(t => t.Creator).ThenInclude(c => c.User)
                                     .Include(t => t.Category)
                                     .Where(t => !t.IsPersonal && !t.IsPrivate)
                                     .OrderByDescending(t => t.CreatedAt)
                                     .ToListAsync();

            // Carrega as categorias para serem exibidas como filtros ou destaques na página.
            ViewData["CategoriesList"] = await _context.Categories
                                         .OrderBy(c => c.Name)
                                         .ToListAsync();

            if (User.Identity?.IsAuthenticated ?? false)
            {
                ViewData["CurrentUserProfileId"] = (await GetUserProfileAsync())?.Id;
            }

            return View(topics);
        }

        /// <summary>
        /// GET: Apresenta os detalhes de um tópico específico, incluindo as suas publicações, comentários e gostos.
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Otimização: A consulta foi reestruturada para evitar múltiplas chamadas `.Include(t => t.Posts)`.
            var topic = await _context.Topics
                .Include(t => t.Creator).ThenInclude(c => c.User)
                .Include(t => t.Category)
                .Include(t => t.Posts) // Carrega todas as publicações...
                    .ThenInclude(post => post.Profile).ThenInclude(p => p.User) // ...e o perfil/utilizador do autor da publicação.
                .Include(t => t.Posts)
                    .ThenInclude(post => post.Likes) // ...e os gostos de cada publicação.
                .Include(t => t.Posts)
                    .ThenInclude(post => post.Comments).ThenInclude(c => c.Profile).ThenInclude(p => p.User) // ...e os comentários com o perfil/utilizador do autor.
                .Include(t => t.Posts)
                    .ThenInclude(post => post.Comments).ThenInclude(c => c.Likes) // ...e os gostos de cada comentário.
                .FirstOrDefaultAsync(m => m.Id == id);

            if (topic == null) return NotFound();

            // Ordenar publicações e os seus respetivos comentários no controlador.
            if (topic.Posts != null)
            {
                var orderedPosts = topic.Posts.OrderByDescending(p => p.CreatedAt).ToList();
                foreach (var post in orderedPosts)
                {
                    post.Comments = post.Comments?.OrderBy(c => c.CreatedAt).ToList() ?? new List<TopicComment>();
                }
                topic.Posts = orderedPosts;
            }

            var userProfile = await GetUserProfileAsync();
            ViewBag.CurrentUserProfileId = userProfile?.Id;
            ViewBag.IsCurrentUserTheCreator = userProfile != null && topic.CreatedBy == userProfile.Id;

            return View(topic);
        }

        /// <summary>
        /// GET: Apresenta o formulário para a criação de um novo tópico.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateCategoriesDropDownList();
            return View();
        }

        /// <summary>
        /// POST: Processa a criação de um novo tópico.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,CategoryId,IsPrivate")] Topic topic, IFormFile? topicImageFile)
        {
            var userProfile = await GetUserProfileAsync();
            if (userProfile == null)
            {
                TempData["ErrorMessage"] = "É necessário ter um perfil para criar tópicos.";
                return RedirectToAction("MyProfile", "Profiles");
            }

            topic.CreatedBy = userProfile.Id;
            topic.CreatedAt = DateTime.UtcNow;
            topic.IsPersonal = false; // Tópicos criados por esta via nunca são pessoais.

            // Remover do ModelState campos que não vêm do formulário ou são definidos no servidor.
            ModelState.Remove(nameof(Topic.Creator));
            ModelState.Remove(nameof(Topic.CreatedBy));
            ModelState.Remove(nameof(Topic.CreatedAt));
            ModelState.Remove(nameof(Topic.IsPersonal));
            ModelState.Remove(nameof(Topic.TopicImageUrl));

            if (ModelState.IsValid)
            {
                if (topicImageFile != null && topicImageFile.Length > 0)
                {
                    topic.TopicImageUrl = await SaveTopicImageAsync(topicImageFile);
                }
                else
                {
                    topic.TopicImageUrl = "/images/topics/default_topic.jpeg"; // Imagem padrão.
                }

                _context.Add(topic);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tópico criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateCategoriesDropDownList(topic.CategoryId);
            return View(topic);
        }

        /// <summary>
        /// GET: Apresenta o formulário para editar um tópico existente.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var topic = await _context.Topics.FindAsync(id);
            if (topic == null) return NotFound();

            var userProfile = await GetUserProfileAsync();
            if (userProfile == null || topic.CreatedBy != userProfile.Id)
            {
                return Forbid("Não tem permissão para editar este tópico.");
            }

            await PopulateCategoriesDropDownList(topic.CategoryId);
            return View(topic);
        }

        /// <summary>
        /// POST: Processa a edição de um tópico existente.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,TopicImageUrl,IsPrivate,CategoryId")] Topic updatedTopic, IFormFile? topicImageFile)
        {
            if (id != updatedTopic.Id) return NotFound();

            var topicToUpdate = await _context.Topics.FirstOrDefaultAsync(t => t.Id == id);
            if (topicToUpdate == null) return NotFound();

            var userProfile = await GetUserProfileAsync();
            if (userProfile == null || topicToUpdate.CreatedBy != userProfile.Id)
            {
                return Forbid("Não tem permissão para editar este tópico.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Lógica para atualizar a imagem.
                    if (topicImageFile != null && topicImageFile.Length > 0)
                    {
                        // Se existir uma imagem antiga (e não for a padrão), elimina-a.
                        if (!string.IsNullOrEmpty(topicToUpdate.TopicImageUrl) && topicToUpdate.TopicImageUrl != "/images/topics/default_topic.jpeg")
                        {
                            DeleteTopicImage(topicToUpdate.TopicImageUrl);
                        }
                        topicToUpdate.TopicImageUrl = await SaveTopicImageAsync(topicImageFile);
                    }

                    // Atualiza as propriedades do tópico.
                    topicToUpdate.Title = updatedTopic.Title;
                    topicToUpdate.Description = updatedTopic.Description;
                    topicToUpdate.IsPrivate = updatedTopic.IsPrivate;
                    topicToUpdate.CategoryId = updatedTopic.CategoryId;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Tópico atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Topics.Any(t => t.Id == updatedTopic.Id)) return NotFound();
                    else throw;
                }
            }

            await PopulateCategoriesDropDownList(updatedTopic.CategoryId);
            return View(updatedTopic);
        }

        /// <summary>
        /// GET: Apresenta a página de confirmação para eliminar um tópico.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var topic = await _context.Topics
                .Include(t => t.Creator).ThenInclude(c => c.User)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (topic == null) return NotFound();

            var userProfile = await GetUserProfileAsync();
            if (userProfile == null || topic.CreatedBy != userProfile.Id)
            {
                return Forbid("Não tem permissão para eliminar este tópico.");
            }

            if (topic.IsPersonal)
            {
                TempData["ErrorMessage"] = "Não pode eliminar o seu tópico de perfil pessoal.";
                return RedirectToAction("MyProfile", "Profiles");
            }

            return View(topic);
        }

        /// <summary>
        /// POST: Confirma e executa a eliminação de um tópico.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var topic = await _context.Topics.FindAsync(id);
            if (topic == null) return NotFound();

            var userProfile = await GetUserProfileAsync();
            if (userProfile == null || topic.CreatedBy != userProfile.Id)
            {
                return Forbid("Não tem permissão para eliminar este tópico.");
            }

            if (topic.IsPersonal)
            {
                return RedirectToAction(nameof(Index)); // Segurança extra.
            }

            try
            {
                DeleteTopicImage(topic.TopicImageUrl);
                _context.Topics.Remove(topic);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tópico eliminado com sucesso!";
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = "Não foi possível eliminar o tópico. Verifique se existem publicações associadas.";
                Console.WriteLine($"Erro de BD ao eliminar tópico: {ex.InnerException?.Message ?? ex.Message}");
                return RedirectToAction(nameof(Details), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// POST: Adiciona um comentário a uma publicação específica.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int topicPostId, string content, string? returnUrl)
        {
            var userProfile = await GetUserProfileAsync();
            if (userProfile == null)
            {
                TempData["ErrorMessage"] = "É necessário estar autenticado e ter um perfil para comentar.";
                return RedirectToLocal(returnUrl);
            }

            if (string.IsNullOrWhiteSpace(content) || content.Length > 1000)
            {
                TempData["ErrorMessage"] = "O comentário está vazio ou excede o limite de 1000 caracteres.";
                return RedirectToLocal(returnUrl);
            }

            if (!await _context.TopicPosts.AnyAsync(p => p.Id == topicPostId))
            {
                TempData["ErrorMessage"] = "A publicação que está a tentar comentar já não existe.";
                return RedirectToLocal(returnUrl);
            }

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
            return RedirectToLocal(returnUrl);
        }

        // --- MÉTODOS AUXILIARES ---

        private async Task<Profile?> GetUserProfileAsync()
        {
            if (!(User.Identity?.IsAuthenticated ?? false)) return null;

            var username = User.Identity.Name;
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return null;

            return await _context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == user.Id);
        }

        private async Task PopulateCategoriesDropDownList(object? selectedCategory = null)
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.CategoryId = new SelectList(categories, "Id", "Name", selectedCategory);
        }

        private async Task<string> SaveTopicImageAsync(IFormFile imageFile)
        {
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "topics");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return "/images/topics/" + uniqueFileName;
        }

        private void DeleteTopicImage(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || imageUrl == "/images/topics/default_topic.jpeg") return;

            try
            {
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imageUrl.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                // É preferível registar o erro a deixar que a aplicação falhe por não conseguir apagar um ficheiro.
                Console.WriteLine($"Erro ao eliminar ficheiro de imagem do tópico: {ex.Message}");
            }
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index)); // Fallback seguro.
        }
    }
}