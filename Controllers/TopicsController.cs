using System;
using System.Collections.Generic;
using System.IO; // Adicionado para Path, Directory, FileStream
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // Adicionado para IFormFile
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

        // Construtor sem IWebHostEnvironment
        public TopicsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Topics
        public async Task<IActionResult> Index()
        {
            var topics = await _context.Topics
                                     .Include(t => t.Creator)
                                      .ThenInclude(c => c.User)
                                     .Where(t => !t.IsPersonal)
                                     .OrderByDescending(t => t.CreatedAt)
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

            var topic = await _context.Topics
                .Include(t => t.Creator)
                    .ThenInclude(c => c.User)
                .Include(t => t.Posts)
                    .ThenInclude(p => p.Profile)
                        .ThenInclude(profile => profile.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (topic == null)
            {
                return NotFound();
            }

            if (topic.Posts != null)
            {
                topic.Posts = topic.Posts.OrderByDescending(p => p.CreatedAt).ToList();
            }

            // --- NOVA LÓGICA PARA VERIFICAR O CRIADOR ---
            bool isCurrentUserTheCreator = false;
            int? currentUserProfileId = null; // Para verificar autores de posts

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var email = User.Identity.Name;
                if (!string.IsNullOrEmpty(email))
                {
                    var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
                    if (appUser != null)
                    {
                        var userProfile = await _context.Profiles.AsNoTracking() // AsNoTracking aqui é seguro, só estamos a ler
                                                .FirstOrDefaultAsync(p => p.UserId == appUser.Id);
                        if (userProfile != null)
                        {
                            currentUserProfileId = userProfile.Id; // Guardar o ID do perfil do user logado
                            if (topic.Creator != null && topic.CreatedBy == userProfile.Id) // CreatedBy é o ProfileId do criador do tópico
                            {
                                isCurrentUserTheCreator = true;
                            }
                        }
                    }
                }
            }

            ViewBag.IsCurrentUserTheCreator = isCurrentUserTheCreator;
            ViewBag.CurrentUserProfileId = currentUserProfileId; // Passar o ID do perfil do user logado para a view
                                                                 // --- FIM DA NOVA LÓGICA ---

            return View(topic);
        }

        // GET: Topics/Create
        [Authorize] // Recomendo autorizar a criação de tópicos
        public IActionResult Create()
        {
            // ViewData["CreatedBy"] não é mais necessário aqui, pois será definido pelo user logado
            return View();
        }

        // POST: Topics/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Recomendo autorizar a criação de tópicos
        public async Task<IActionResult> Create([Bind("IsPrivate,Title,Description")] Topic topic, IFormFile? topicImageFile)
        {
            // Valores automáticos
            topic.IsPersonal = false;
            topic.CreatedAt = DateTime.UtcNow;

            // Obter utilizador logado
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
                return RedirectToAction("MyProfile", "Profiles"); // Ou para a página de criação de perfil
            }
            topic.CreatedBy = userProfile.Id; // FK para Profile
            // topic.Creator = userProfile; // O EF Core deve ligar isto automaticamente se CreatedBy for definido


            // Remover validações para campos preenchidos pelo sistema ou não vindos do form diretamente
            ModelState.Remove(nameof(Topic.Id));
            ModelState.Remove(nameof(Topic.IsPersonal));
            ModelState.Remove(nameof(Topic.CreatedBy));
            ModelState.Remove(nameof(Topic.Creator)); // Será inferido pelo EF Core a partir de CreatedBy
            ModelState.Remove(nameof(Topic.CreatedAt));
            ModelState.Remove(nameof(Topic.TopicImageUrl)); // Será definido pela lógica de upload
            ModelState.Remove(nameof(Topic.Posts));
            ModelState.Remove(nameof(Topic.Savers));


            if (ModelState.IsValid)
            {
                // Lógica de Upload da Imagem do Tópico
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
                        // Considerar adicionar um erro ao ModelState ou usar uma imagem padrão em caso de falha
                        ModelState.AddModelError("topicImageFile", $"Erro ao carregar a imagem: {ex.Message}");
                        topic.TopicImageUrl = "/images/topics/default_topic.jpeg"; // Caminho para a imagem padrão
                        // Se houver erro no upload mas o resto for válido, pode-se optar por guardar com a imagem padrão
                        // ou retornar a view com o erro. Aqui, vamos prosseguir com a padrão e logar o erro.
                    }
                }
                else
                {
                    // Nenhuma imagem enviada, usar a padrão
                    topic.TopicImageUrl = "/images/topics/default_topic.jpeg";
                }

                _context.Add(topic);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tópico criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            // Log ModelState errors if not valid
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

            var topic = await _context.Topics.FindAsync(id);
            if (topic == null)
            {
                return NotFound();
            }

            // Verificar se o utilizador logado é o criador do tópico
            var email = User.Identity?.Name;
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
            if (appUser == null) return Unauthorized();
            var userProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);
            if (userProfile == null || topic.CreatedBy != userProfile.Id)
            {
                return Forbid("Não tem permissão para editar este tópico.");
            }

            // ViewData["CreatedBy"] não é mais necessário, já que a permissão é verificada
            return View(topic);
        }

        // POST: Topics/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,TopicImageUrl,IsPrivate")] Topic updatedTopicViewModel, IFormFile? topicImageFile)
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

            // Carregar o tópico existente da base de dados para verificar o criador e obter valores não editáveis
            // Usar AsNoTracking() para evitar conflitos de tracking se o ModelState for inválido e retornarmos a view com updatedTopicViewModel
            var existingTopic = await _context.Topics.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (existingTopic == null)
            {
                return NotFound("Tópico não encontrado.");
            }

            if (existingTopic.CreatedBy != userProfile.Id)
            {
                return Forbid("Não tem permissão para editar este tópico.");
            }

            // Atribuir valores não editáveis do tópico existente para o updatedTopicViewModel
            // para garantir que não são sobrescritos indevidamente e para que o ModelState seja validado corretamente.
            updatedTopicViewModel.CreatedBy = existingTopic.CreatedBy;
            updatedTopicViewModel.CreatedAt = existingTopic.CreatedAt;
            updatedTopicViewModel.IsPersonal = existingTopic.IsPersonal;
            // TopicImageUrl será tratado pela lógica de upload, mas é bom ter o valor atual no ViewModel se nenhuma nova imagem for enviada.
            // Se topicImageFile for null, o valor de TopicImageUrl do Bind (do hidden field) será usado.

            ModelState.Remove(nameof(Topic.Creator));
            ModelState.Remove(nameof(Topic.CreatedBy)); // Já definido
            ModelState.Remove(nameof(Topic.CreatedAt)); // Já definido
            ModelState.Remove(nameof(Topic.IsPersonal)); // Já definido
            ModelState.Remove(nameof(Topic.Posts));
            ModelState.Remove(nameof(Topic.Savers));
            // Se TopicImageUrl não estivesse no Bind, precisaria remover aqui também. Como está, deixamos.

            if (ModelState.IsValid)
            {
                try
                {
                    string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string currentImageUrl = updatedTopicViewModel.TopicImageUrl; // Valor do hidden field ou BD

                    if (topicImageFile != null && topicImageFile.Length > 0)
                    {
                        // Apagar a imagem antiga se existir e não for a imagem padrão
                        if (!string.IsNullOrEmpty(currentImageUrl) && currentImageUrl != "/images/default_topic.jpeg")
                        {
                            string oldImagePath = Path.Combine(wwwRootPath, currentImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                try
                                {
                                    System.IO.File.Delete(oldImagePath);
                                    Console.WriteLine($"Imagem antiga '{oldImagePath}' eliminada.");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Erro ao eliminar imagem antiga '{oldImagePath}': {ex.Message}");
                                    // Logar e continuar, não impedir o upload da nova.
                                }
                            }
                        }

                        // Guardar a nova imagem
                        string uploadsFolder = Path.Combine(wwwRootPath, "images", "topics"); 
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(topicImageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await topicImageFile.CopyToAsync(fileStream);
                        }
                        updatedTopicViewModel.TopicImageUrl = "/images/topics/" + uniqueFileName;
                        Console.WriteLine($"Nova imagem do tópico guardada: {updatedTopicViewModel.TopicImageUrl}");
                    }
                    // else: Nenhuma nova imagem enviada, updatedTopicViewModel.TopicImageUrl já tem o valor do campo hidden.

                    // Agora, aplicamos as alterações ao `existingTopic` que será de facto atualizado no contexto.
                    // Ou, se preferir, pode configurar o `_context.Entry(updatedTopicViewModel).State = EntityState.Modified;`
                    // mas isso requer que todas as propriedades estejam corretas.
                    // É mais seguro atualizar o objeto que veio da BD:
                    var topicToUpdate = await _context.Topics.FindAsync(id); // Re-fetch para atualizar
                    if (topicToUpdate == null) return NotFound("Tópico desapareceu durante a edição."); // Segurança

                    topicToUpdate.Title = updatedTopicViewModel.Title;
                    topicToUpdate.Description = updatedTopicViewModel.Description;
                    topicToUpdate.TopicImageUrl = updatedTopicViewModel.TopicImageUrl; // Pode ser o novo ou o antigo (do hidden)
                    topicToUpdate.IsPrivate = updatedTopicViewModel.IsPrivate;


                    _context.Update(topicToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Tópico atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Topics.Any(t => t.Id == updatedTopicViewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "O tópico foi modificado por outro utilizador. Por favor, recarregue a página.");
                        // Poderia recarregar os dados e devolver ao user
                        var currentValues = await _context.Topics.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
                        return View(currentValues); // Devolver os valores atuais da BD
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao editar tópico: {ex.Message}");
                    ModelState.AddModelError(string.Empty, $"Ocorreu um erro inesperado: {ex.Message}");
                    return View(updatedTopicViewModel); // Retorna com o modelo que tentou submeter
                }
                return RedirectToAction(nameof(Index));
            }

            Console.WriteLine("ModelState inválido no Edit POST:");
            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                if (state.Errors.Any())
                {
                    Console.WriteLine($"  - {key}: {string.Join("; ", state.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            return View(updatedTopicViewModel); // Retorna com o modelo que tentou submeter
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
                .Include(t => t.Creator) // Para mostrar o nome do criador na view de confirmação
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (topic == null)
            {
                return NotFound();
            }

            // Verificar se o utilizador logado é o criador do tópico
            var email = User.Identity?.Name;
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
            if (appUser == null) return Unauthorized();
            var userProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);

            if (userProfile == null || topic.CreatedBy != userProfile.Id)
            {
                return Forbid("Não tem permissão para eliminar este tópico.");
            }

            // Bloqueia a eliminação do tópico pessoal do utilizador (se esta lógica ainda for desejada aqui)
            if (topic.IsPersonal && topic.IsPrivate) // Esta condição define o tópico pessoal no seu ProfilesController
            {
                TempData["ErrorMessage"] = "Não pode eliminar o seu tópico de perfil pessoal diretamente.";
                return RedirectToAction("MyProfile", "Profiles"); // Ou para onde for apropriado
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
                // Lógica para apagar a imagem associada ao tópico (se não for a padrão)
                if (!string.IsNullOrEmpty(topic.TopicImageUrl) && topic.TopicImageUrl != "/images/default_topic_image.png")
                {
                    string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string imagePath = Path.Combine(wwwRootPath, topic.TopicImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                        Console.WriteLine($"Imagem do tópico '{imagePath}' eliminada.");
                    }
                }

                // O EF Core deve tratar de eliminar Posts e SavedTopics associados se as relações
                // estiverem configuradas com `CascadeOnDelete` (o que é comum para FKs não nulas).
                // Se não estiverem, precisaria remover Posts e SavedTopics manualmente antes de remover o Topic.
                // Ex: var posts = _context.TopicPosts.Where(p => p.TopicId == id).ToList(); _context.TopicPosts.RemoveRange(posts);
                // Ex: var savers = _context.SavedTopics.Where(s => s.TopicId == id).ToList(); _context.SavedTopics.RemoveRange(savers);

                _context.Topics.Remove(topic);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tópico eliminado com sucesso!";
            }
            catch (DbUpdateException ex) // Captura erros de FK se o cascade delete não estiver configurado
            {
                Console.WriteLine($"Erro ao eliminar tópico (DbUpdateException): {ex.InnerException?.Message ?? ex.Message}");
                TempData["ErrorMessage"] = "Não foi possível eliminar o tópico. Pode ter publicações ou outras dependências.";
                // Redirecionar para a página de detalhes do tópico ou para o index
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

            // Tentar redirecionar para a página anterior, se possível
            string? returnUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) // Segurança: verificar se é URL local
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index)); // Fallback
        }


        // POST: Topics/UnsaveTopic/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Adicionar Authorize aqui também
        public async Task<IActionResult> UnsaveTopic(int id) // id is TopicId
        {
            var userEmail = User.Identity?.Name; // Usar o mesmo método para obter o identificador do user
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
                // Embora raro se o user está autenticado e SaveTopic funciona, é uma verificação de segurança
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
            // Fallback mais sensato pode ser a página de perfil onde os tópicos guardados são listados
            return RedirectToAction("MyProfile", "Profiles");
        }
    }
}