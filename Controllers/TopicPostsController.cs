using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;

namespace projetodweb_connectify.Controllers
{
    public class TopicPostsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TopicPostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TopicPosts
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.TopicPosts.Include(t => t.Profile).Include(t => t.Topic);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: TopicPosts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics
                .Include(t => t.Creator) // Creator of the Topic
                    .ThenInclude(c => c.User) // User who is the creator of the topic
                .Include(t => t.Posts)    // Include the collection of posts for this topic
                    .ThenInclude(p => p.Profile) // For each post, include its author's Profile
                        .ThenInclude(profile => profile.User) // For each post's Profile, include the User
                .FirstOrDefaultAsync(m => m.Id == id);

            if (topic == null)
            {
                return NotFound();
            }

            // Optionally, sort posts if you want them in a specific order (e.g., newest first)
            // This can also be done in the view, but doing it here can be cleaner.
            if (topic.Posts != null)
            {
                topic.Posts = topic.Posts.OrderByDescending(p => p.CreatedAt).ToList();
            }


            return View(topic);
        }

        // GET: TopicPosts/Create
        public async Task<IActionResult> Create(int topicId) // topicId vem da rota
        {
            if (topicId == 0)
            {
                return BadRequest("ID do Tópico é inválido.");
            }
            var topic = await _context.Topics.FindAsync(topicId);
            if (topic == null)
            {
                return NotFound("Tópico não encontrado.");
            }

            // Prepara o modelo para a view, incluindo o TopicId
            var topicPost = new TopicPost
            {
                TopicId = topicId
            };

            ViewBag.TopicTitle = topic.Title; // Passa o título para a view
            ViewBag.TopicId = topicId; // Passar também o Id, embora esteja no modelo

            return View(topicPost);
        }

        // POST: TopicPosts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Content, TopicId")] TopicPost topicPost, IFormFile? postImageFile)
        {
            // topicPost.TopicId deve vir do Bind (precisa de um campo hidden na view)

            // Remover validações desnecessárias
            ModelState.Remove(nameof(TopicPost.Id));
            ModelState.Remove(nameof(TopicPost.CreatedAt));
            ModelState.Remove(nameof(TopicPost.ProfileId));
            ModelState.Remove(nameof(TopicPost.Profile));
            ModelState.Remove(nameof(TopicPost.Topic));
            ModelState.Remove(nameof(TopicPost.Comments));
            ModelState.Remove(nameof(TopicPost.PostImageUrl)); // Será definido aqui

            if (ModelState.IsValid)
            {
                topicPost.CreatedAt = DateTime.UtcNow;

                // Obter perfil do utilizador logado
                var email = User.Identity?.Name;
                if (email == null) return Unauthorized();
                var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
                if (appUser == null) return NotFound("Utilizador não encontrado.");
                var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);
                if (profile == null) return NotFound("Perfil não encontrado.");

                topicPost.ProfileId = profile.Id;

                // Lógica de Upload da Imagem do Post
                if (postImageFile != null && postImageFile.Length > 0)
                {
                    string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string uploadsFolder = Path.Combine(wwwRootPath, "images", "posts"); // Pasta destino

                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(postImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    try
                    {
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await postImageFile.CopyToAsync(fileStream);
                        }
                        topicPost.PostImageUrl = "/images/posts/" + uniqueFileName; // Guardar caminho relativo
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao carregar a imagem do post: {ex.Message}");
                        ModelState.AddModelError("postImageFile", $"Erro ao carregar a imagem: {ex.Message}");
                        // Não retornar ainda, pois o post pode ser válido sem a imagem
                        // topicPost.PostImageUrl permanecerá null
                    }
                }
                // Se não houve erro de upload ou não foi enviado ficheiro, PostImageUrl é null ou tem o path

                if (ModelState.ErrorCount == 0) // Verificar se o upload não causou erro fatal no ModelState
                {
                    _context.Add(topicPost);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Publicação criada com sucesso!";
                    return RedirectToAction("Details", "Topics", new { id = topicPost.TopicId });
                }
            }

            // Se ModelState não for válido (incluindo erro de upload), recarregar dados necessários e retornar view
            var topic = await _context.Topics.FindAsync(topicPost.TopicId);
            ViewBag.TopicTitle = topic?.Title ?? "Tópico Desconhecido";
            ViewBag.TopicId = topicPost.TopicId; // Garantir que o TopicId está disponível
            return View(topicPost);
        }



        // GET: TopicPosts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var topicPost = await _context.TopicPosts.FindAsync(id);
            if (topicPost == null) return NotFound();

            // Verificar permissão
            var email = User.Identity?.Name;
            if (email == null) return Unauthorized();
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
            if (appUser == null) return Unauthorized();
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);
            if (profile == null || topicPost.ProfileId != profile.Id)
            {
                return Forbid("Não tem permissão para editar esta publicação.");
            }

            // Passar título do tópico para a view (opcional, mas útil)
            var topic = await _context.Topics.FindAsync(topicPost.TopicId);
            ViewBag.TopicTitle = topic?.Title;

            return View(topicPost);
        }

        // POST: TopicPosts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: TopicPosts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Content,PostImageUrl,TopicId")] TopicPost topicPostViewModel, IFormFile? postImageFile)
        {
            if (id != topicPostViewModel.Id) return NotFound();

            // Carregar post original para verificar autorização e atualizar
            var postToUpdate = await _context.TopicPosts.FindAsync(id);
            if (postToUpdate == null) return NotFound();

            // Verificar permissão
            var email = User.Identity?.Name;
            if (email == null) return Unauthorized();
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
            if (appUser == null) return Unauthorized();
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);
            if (profile == null || postToUpdate.ProfileId != profile.Id)
            {
                return Forbid("Não tem permissão para editar esta publicação.");
            }

            // Atribuir TopicId e ProfileId corretamente (não devem mudar na edição)
            topicPostViewModel.TopicId = postToUpdate.TopicId;
            topicPostViewModel.ProfileId = postToUpdate.ProfileId;


            // Remover validações desnecessárias
            ModelState.Remove(nameof(TopicPost.CreatedAt));
            ModelState.Remove(nameof(TopicPost.Profile));
            ModelState.Remove(nameof(TopicPost.Topic));
            ModelState.Remove(nameof(TopicPost.Comments));


            if (ModelState.IsValid)
            {
                try
                {
                    // Atualizar conteúdo
                    postToUpdate.Content = topicPostViewModel.Content;

                    // Lógica de Upload/Atualização de Imagem
                    if (postImageFile != null && postImageFile.Length > 0)
                    {
                        string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                        string uploadsFolder = Path.Combine(wwwRootPath, "images", "posts");
                        // Apagar imagem antiga (se existir e não for padrão - assumindo que não há padrão para posts)
                        if (!string.IsNullOrEmpty(postToUpdate.PostImageUrl))
                        {
                            string oldImagePath = Path.Combine(wwwRootPath, postToUpdate.PostImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                try { System.IO.File.Delete(oldImagePath); } catch (Exception ex) { Console.WriteLine($"Erro ao apagar imagem antiga do post: {ex.Message}"); }
                            }
                        }
                        // Salvar nova imagem
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(postImageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create)) { await postImageFile.CopyToAsync(fileStream); }
                        postToUpdate.PostImageUrl = "/images/posts/" + uniqueFileName;
                    }
                    else
                    {
                        // Nenhuma nova imagem enviada, manter o valor que veio do hidden field
                        // A linha abaixo é redundante se o campo hidden estiver correto, mas garante
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
                // Redirecionar para os detalhes do tópico onde o post está
                return RedirectToAction("Details", "Topics", new { id = postToUpdate.TopicId });
            }

            // Se inválido, retornar a view com o ViewModel e dados necessários
            var topic = await _context.Topics.FindAsync(topicPostViewModel.TopicId);
            ViewBag.TopicTitle = topic?.Title; // Recarregar título
            return View(topicPostViewModel);
        }


        // GET: TopicPosts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var topicPost = await _context.TopicPosts
                .Include(t => t.Profile) // Incluir para verificação e exibição
                    .ThenInclude(p => p.User)
                .Include(t => t.Topic) // Incluir para exibição
                .FirstOrDefaultAsync(m => m.Id == id);

            if (topicPost == null) return NotFound();

            // Verificar permissão
            var email = User.Identity?.Name;
            if (email == null) return Unauthorized();
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
            if (appUser == null) return Unauthorized();
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);
            if (profile == null || topicPost.ProfileId != profile.Id)
            {
                return Forbid("Não tem permissão para apagar esta publicação.");
            }

            return View(topicPost);
        }

        // POST: TopicPosts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var topicPost = await _context.TopicPosts.FindAsync(id);
            if (topicPost == null) return NotFound();

            // Verificar permissão ANTES de apagar
            var email = User.Identity?.Name;
            if (email == null) return Unauthorized();
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
            if (appUser == null) return Unauthorized();
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);
            if (profile == null || topicPost.ProfileId != profile.Id)
            {
                return Forbid("Não tem permissão para apagar esta publicação.");
            }

            int topicId = topicPost.TopicId; // Guardar para redirecionar

            try
            {
                // Apagar imagem associada
                if (!string.IsNullOrEmpty(topicPost.PostImageUrl))
                {
                    string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string imagePath = Path.Combine(wwwRootPath, topicPost.PostImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        try { System.IO.File.Delete(imagePath); } catch (Exception ex) { Console.WriteLine($"Erro ao apagar imagem do post: {ex.Message}"); }
                    }
                }

                // Remover comentários associados (se o cascade delete não estiver configurado)
                var comments = await _context.TopicComments.Where(c => c.TopicPostId == id).ToListAsync();
                if (comments.Any()) _context.TopicComments.RemoveRange(comments);


                _context.TopicPosts.Remove(topicPost);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Publicação eliminada com sucesso!";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao eliminar post: {ex.Message}");
                TempData["ErrorMessage"] = "Ocorreu um erro ao tentar eliminar a publicação.";
                // Retorna para detalhes do tópico mesmo em caso de erro
            }

            return RedirectToAction("Details", "Topics", new { id = topicId });
        }

        private bool TopicPostExists(int id)
        {
            return _context.TopicPosts.Any(e => e.Id == id);
        }
    }
}
