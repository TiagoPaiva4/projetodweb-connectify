using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using System.IO; // Necessário para interagir com o sistema de ficheiros.
using System.Threading.Tasks;

namespace projetodweb_connectify.Controllers
{
    [Authorize]
    // Oculta este controlador da documentação da API (Swagger), pois serve para renderizar Views.
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfilesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Apresenta a página de perfil do utilizador autenticado.
        /// </summary>
        [HttpGet]
        public IActionResult MyProfile()
        {
            return View();
        }

        /// <summary>
        /// Apresenta a página de pesquisa de utilizadores.
        /// </summary>
        [HttpGet("Search")]
        public IActionResult Search()
        {
            return View();
        }

        /// <summary>
        /// Apresenta o perfil público de um utilizador específico.
        /// </summary>
        /// <param name="username">O nome de utilizador do perfil a ser exibido.</param>
        [HttpGet("profile/{username}")]
        public IActionResult UserProfile(string username)
        {
            ViewData["ProfileUsername"] = username;
            return View();
        }

        /// <summary>
        /// GET: /Profiles/Edit
        /// Apresenta o formulário para editar os dados do perfil do utilizador autenticado.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            // Obter o nome de utilizador (username) a partir do cookie de autenticação.
            var username = _userManager.GetUserName(User);
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("Utilizador não autenticado.");
            }

            // Com o username, encontrar a nossa entidade 'Users' que contém o ID numérico.
            var appUser = await _context.Users
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(u => u.Username == username);
            if (appUser == null)
            {
                return NotFound("Utilizador da aplicação não encontrado.");
            }

            // Usar o ID do utilizador para encontrar o perfil correspondente.
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);

            if (profile == null)
            {
                return NotFound("Perfil não encontrado para este utilizador.");
            }

            return View(profile);
        }

        /// <summary>
        /// POST: /Profiles/Edit
        /// Processa os dados submetidos do formulário de edição do perfil.
        /// </summary>
        /// <param name="id">O ID do perfil a ser editado.</param>
        /// <param name="profile">O objeto de perfil com os novos dados do formulário.</param>
        /// <param name="ProfilePicture">O ficheiro de imagem de perfil (opcional).</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Bio")] Profile profile, IFormFile? ProfilePicture)
        {
            // Validação 1: O ID do perfil no URL deve corresponder ao ID do perfil enviado no formulário.
            if (id != profile.Id)
            {
                return NotFound();
            }

            // Validação 2: Garantir que o utilizador autenticado é o proprietário do perfil que tenta editar.
            var username = _userManager.GetUserName(User);
            var appUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);

            // Obter o perfil original da base de dados (sem tracking) para comparar e reter dados não editáveis.
            var originalProfile = await _context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

            // Se o perfil não pertence ao utilizador autenticado, a edição é proibida.
            if (appUser == null || originalProfile == null || originalProfile.UserId != appUser.Id)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Lógica para o upload da nova imagem de perfil.
                    if (ProfilePicture != null && ProfilePicture.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Criar um nome de ficheiro único para evitar conflitos de nomes.
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ProfilePicture.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await ProfilePicture.CopyToAsync(fileStream);
                        }
                        profile.ProfilePicture = "/images/profiles/" + uniqueFileName;
                    }
                    else
                    {
                        // Se não for enviada uma nova imagem, manter a imagem de perfil existente.
                        profile.ProfilePicture = originalProfile.ProfilePicture;
                    }

                    // Reatribuir valores que não vêm do formulário para evitar que sejam perdidos.
                    profile.UserId = originalProfile.UserId;
                    profile.CreatedAt = originalProfile.CreatedAt;
                    profile.Type = originalProfile.Type;

                    _context.Update(profile);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Profiles.Any(e => e.Id == profile.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(MyProfile));
            }

            // Se o modelo não for válido, devolve a mesma view com os dados que o utilizador
            // já tinha inserido, para que não os perca e possa corrigir os erros de validação.
            return View(profile);
        }
    }
}