using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using Microsoft.AspNetCore.Authorization; // Adicionado para clareza, embora já implícito pelas ações.

namespace projetodweb_connectify.Controllers
{
    [Authorize] // É uma boa prática definir a autorização ao nível do controlador se a maioria das ações a exigir.
    public class TopicPostsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TopicPostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Apresenta uma lista de todas as publicações. Geralmente útil para uma área de administração.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var allPosts = await _context.TopicPosts
                .Include(t => t.Profile)
                .Include(t => t.Topic)
                .ToListAsync();
            return View(allPosts);
        }

        /// <summary>
        /// Apresenta os detalhes de um TÓPICO, incluindo todas as suas publicações.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var topic = await _context.Topics
                .Include(t => t.Creator).ThenInclude(c => c.User) // Autor do Tópico
                .Include(t => t.Posts).ThenInclude(p => p.Profile).ThenInclude(profile => profile.User) // Publicações e seus autores
                .FirstOrDefaultAsync(m => m.Id == id);

            if (topic == null) return NotFound();

            // É uma boa prática ordenar os dados no controlador antes de os passar para a View.
            if (topic.Posts != null)
            {
                topic.Posts = topic.Posts.OrderByDescending(p => p.CreatedAt).ToList();
            }

            return View(topic);
        }

        /// <summary>
        /// GET: Apresenta o formulário para criar uma nova publicação num tópico específico.
        /// </summary>
        /// <param name="topicId">O ID do tópico onde a publicação será criada.</param>
        [HttpGet]
        public async Task<IActionResult> Create(int topicId)
        {
            if (topicId == 0) return BadRequest("O ID do Tópico é inválido.");

            var topic = await _context.Topics.FindAsync(topicId);
            if (topic == null) return NotFound("Tópico não encontrado.");

            var topicPostViewModel = new TopicPost { TopicId = topicId };
            ViewBag.TopicTitle = topic.Title;

            return View(topicPostViewModel);
        }

        /// <summary>
        /// POST: Processa a criação de uma nova publicação.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Content, TopicId")] TopicPost topicPost, IFormFile? postImageFile)
        {
            // Remover do ModelState propriedades que são definidas no servidor, para evitar erros de validação desnecessários.
            ModelState.Remove(nameof(TopicPost.Id));
            ModelState.Remove(nameof(TopicPost.CreatedAt));
            ModelState.Remove(nameof(TopicPost.ProfileId));
            ModelState.Remove(nameof(TopicPost.PostImageUrl)); // Será definido na lógica de upload.

            if (ModelState.IsValid)
            {
                topicPost.CreatedAt = DateTime.UtcNow;

                var username = User.Identity?.Name;
                if (username == null) return Unauthorized();
                var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (appUser == null) return NotFound("Utilizador não encontrado.");
                var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);
                if (profile == null) return NotFound("Perfil não encontrado.");
                topicPost.ProfileId = profile.Id;

                // Lógica de Upload da Imagem
                if (postImageFile != null && postImageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "posts");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(postImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    try
                    {
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await postImageFile.CopyToAsync(fileStream);
                        }
                        topicPost.PostImageUrl = "/images/posts/" + uniqueFileName; // Guardar caminho relativo para uso em HTML.
                    }
                    catch (Exception ex)
                    {
                        // NOTA: Numa aplicação de produção, use um sistema de logging em vez de Console.WriteLine.
                        Console.WriteLine($"Erro ao carregar a imagem da publicação: {ex.Message}");
                        ModelState.AddModelError("postImageFile", $"Erro ao carregar a imagem: {ex.Message}");
                    }
                }

                // Apenas continuar se não ocorreram erros durante o upload da imagem.
                if (ModelState.ErrorCount == 0)
                {
                    _context.Add(topicPost);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Publicação criada com sucesso!";
                    return RedirectToAction("Details", "Topics", new { id = topicPost.TopicId });
                }
            }

            // Se o modelo for inválido, recarregar dados necessários para a View.
            var topic = await _context.Topics.FindAsync(topicPost.TopicId);
            ViewBag.TopicTitle = topic?.Title ?? "Tópico Desconhecido";
            return View(topicPost);
        }

        /// <summary>
        /// GET: Apresenta o formulário para editar uma publicação existente.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var topicPost = await _context.TopicPosts.FindAsync(id);
            if (topicPost == null) return NotFound();

            // Verificação de permissão: o utilizador autenticado deve ser o autor da publicação.
            var profile = await GetUserProfileAsync();
            if (profile == null || topicPost.ProfileId != profile.Id)
            {
                return Forbid("Não tem permissão para editar esta publicação.");
            }

            var topic = await _context.Topics.FindAsync(topicPost.TopicId);
            ViewBag.TopicTitle = topic?.Title;

            return View(topicPost);
        }

        /// <summary>
        /// POST: Processa a edição de uma publicação.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Content,PostImageUrl,TopicId")] TopicPost topicPostViewModel, IFormFile? postImageFile)
        {
            if (id != topicPostViewModel.Id) return NotFound();

            var postToUpdate = await _context.TopicPosts.FindAsync(id);
            if (postToUpdate == null) return NotFound();

            // Verificação de permissão.
            var profile = await GetUserProfileAsync();
            if (profile == null || postToUpdate.ProfileId != profile.Id)
            {
                return Forbid("Não tem permissão para editar esta publicação.");
            }

            ModelState.Remove(nameof(TopicPost.ProfileId)); // Não deve ser alterado.

            if (ModelState.IsValid)
            {
                try
                {
                    postToUpdate.Content = topicPostViewModel.Content;

                    // Lógica para atualizar a imagem.
                    if (postImageFile != null && postImageFile.Length > 0)
                    {
                        string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                        // Eliminar a imagem antiga, se existir.
                        if (!string.IsNullOrEmpty(postToUpdate.PostImageUrl))
                        {
                            string oldImagePath = Path.Combine(wwwRootPath, postToUpdate.PostImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                try { System.IO.File.Delete(oldImagePath); } catch (Exception ex) { Console.WriteLine($"Erro ao apagar imagem antiga: {ex.Message}"); }
                            }
                        }

                        // Guardar a nova imagem.
                        string uploadsFolder = Path.Combine(wwwRootPath, "images", "posts");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(postImageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create)) { await postImageFile.CopyToAsync(fileStream); }
                        postToUpdate.PostImageUrl = "/images/posts/" + uniqueFileName;
                    }
                    else
                    {
                        // Se não for enviada uma nova imagem, mantém-se o valor que veio do formulário (via hidden field).
                        postToUpdate.PostImageUrl = topicPostViewModel.PostImageUrl;
                    }

                    _context.Update(postToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Publicação atualizada com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TopicPostExists(topicPostViewModel.Id)) return NotFound(); else throw;
                }

                return RedirectToAction("Details", "Topics", new { id = postToUpdate.TopicId });
            }

            ViewBag.TopicTitle = (await _context.Topics.FindAsync(topicPostViewModel.TopicId))?.Title;
            return View(topicPostViewModel);
        }

        /// <summary>
        /// GET: Apresenta a página de confirmação para eliminar uma publicação.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var topicPost = await _context.TopicPosts
                .Include(t => t.Profile).ThenInclude(p => p.User)
                .Include(t => t.Topic)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (topicPost == null) return NotFound();

            var profile = await GetUserProfileAsync();
            if (profile == null || topicPost.ProfileId != profile.Id)
            {
                return Forbid("Não tem permissão para apagar esta publicação.");
            }

            return View(topicPost);
        }

        /// <summary>
        /// POST: Confirma e executa a eliminação de uma publicação e dos seus dados associados.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var topicPost = await _context.TopicPosts.FindAsync(id);
            if (topicPost == null) return NotFound();

            var profile = await GetUserProfileAsync();
            if (profile == null || topicPost.ProfileId != profile.Id)
            {
                return Forbid("Não tem permissão para apagar esta publicação.");
            }

            // Guardar o ID do tópico para o redirecionamento final.
            int topicId = topicPost.TopicId;

            try
            {
                // Eliminar a imagem associada do sistema de ficheiros.
                if (!string.IsNullOrEmpty(topicPost.PostImageUrl))
                {
                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", topicPost.PostImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        try { System.IO.File.Delete(imagePath); } catch (Exception ex) { Console.WriteLine($"Erro ao apagar a imagem da publicação: {ex.Message}"); }
                    }
                }

                // É crucial remover dependências (como comentários) antes de apagar o post,
                // a menos que a base de dados esteja configurada com "ON DELETE CASCADE".
                var comments = await _context.TopicComments.Where(c => c.TopicPostId == id).ToListAsync();
                if (comments.Any()) _context.TopicComments.RemoveRange(comments);

                _context.TopicPosts.Remove(topicPost);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Publicação eliminada com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocorreu um erro ao tentar eliminar a publicação.";
                Console.WriteLine($"Erro ao eliminar publicação: {ex.Message}");
            }

            return RedirectToAction("Details", "Topics", new { id = topicId });
        }

        private bool TopicPostExists(int id)
        {
            return _context.TopicPosts.Any(e => e.Id == id);
        }

        /// <summary>
        /// Método auxiliar para obter o perfil do utilizador autenticado.
        /// </summary>
        private async Task<Profile?> GetUserProfileAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return null;

            return await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        }
    }
}