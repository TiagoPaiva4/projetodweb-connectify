using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using System.IO; // Necessário para trabalhar com ficheiros
using System.Net.Http;
using System.Threading.Tasks;

namespace projetodweb_connectify.Controllers
{
    [Authorize]
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

        [HttpGet]
        public IActionResult MyProfile()
        {
            return View();
        }

        [HttpGet("Search")]
        public IActionResult Search()
        {
            return View();
        }

        [HttpGet("profile/{username}")]
        public IActionResult UserProfile(string username)
        {
            ViewData["ProfileUsername"] = username;
            return View();
        }

    

        // ==================================================================
        // MÉTODO 1: PARA MOSTRAR O FORMULÁRIO DE EDIÇÃO (HTTP GET)
        // ==================================================================
        /// <summary>
        /// GET: /Profiles/Edit
        /// Apresenta o formulário para editar o perfil do utilizador autenticado.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            // Obter o NOME DE UTILIZADOR do utilizador logado
            var username = _userManager.GetUserName(User);
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("Utilizador não autenticado.");
            }

            // Usar o username para encontrar o nosso utilizador da aplicação (que tem um ID int)
            var appUser = await _context.Users
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(u => u.Username == username);
            if (appUser == null)
            {
                return NotFound("Utilizador da aplicação não encontrado.");
            }

            // Agora usamos o ID inteiro do nosso utilizador para encontrar o perfil
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == appUser.Id);

            if (profile == null)
            {
                return NotFound("Perfil não encontrado para este utilizador.");
            }

            // Passa o objeto 'profile' para a View.
            // A sua View 'Edit.cshtml' vai usar este objeto para preencher os campos do formulário.
            return View(profile);
        }

        // ==================================================================
        // MÉTODO 2: PARA PROCESSAR O FORMULÁRIO SUBMETIDO (HTTP POST)
        // ==================================================================
        /// <summary>
        /// POST: /Profiles/Edit
        /// Processa os dados submetidos do formulário de edição.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Bio")] Profile profile, IFormFile? ProfilePicture)
        {
            // Validação 1: O ID do formulário deve corresponder ao ID na rota.
            if (id != profile.Id)
            {
                return NotFound();
            }

            // Validação 2: Garantir que o utilizador a editar é o dono do perfil.
            var username = _userManager.GetUserName(User);
            var appUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);

            var originalProfile = await _context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

            if (appUser == null || originalProfile == null || originalProfile.UserId != appUser.Id)
            {
                // Se o perfil não existe ou não pertence ao utilizador logado, proíbe a ação.
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Lógica para o upload da imagem de perfil
                    if (ProfilePicture != null && ProfilePicture.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }
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
                        // Se nenhuma imagem nova foi enviada, mantém a antiga.
                        profile.ProfilePicture = originalProfile.ProfilePicture;
                    }

                    // Preenche os campos que não vêm do formulário para não os perder.
                    profile.UserId = originalProfile.UserId;
                    profile.CreatedAt = originalProfile.CreatedAt;
                    profile.Type = originalProfile.Type; // Manter o tipo de perfil

                    // Atualiza a entidade na base de dados
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
                // Após guardar com sucesso, redireciona para a página de perfil.
                return RedirectToAction(nameof(MyProfile));
            }

            // Se o modelo não for válido, retorna para a mesma view,
            // mostrando os erros de validação. O 'profile' que é passado de volta
            // contém os dados que o utilizador inseriu para que não os perca.
            return View(profile);
        }
    }
}