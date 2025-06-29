using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using System.Linq;
using System.Threading.Tasks;

namespace projetodweb_connectify.Areas.Admin.Controllers
{
    [Area("Admin")] // Especifica que este controlador pertence à área "Admin".
    [Authorize(Roles = "admin")] // Apenas utilizadores com a role "admin" podem aceder.
    public class DashboardController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context; // Para gerir outros dados da aplicação, se necessário.

        public DashboardController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.UserCount = await _userManager.Users.CountAsync();
            ViewBag.RoleCount = await _roleManager.Roles.CountAsync();
            // Adicionar aqui outras estatísticas para o painel de administração.
            return View();
        }

        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            // Para uma visualização mais rica, pode ser útil criar um ViewModel
            // que inclua as roles de cada utilizador.
            return View(users);
        }

        // Apresenta os detalhes de um utilizador e as suas roles.
        public async Task<IActionResult> UserDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            ViewBag.UserRoles = userRoles;
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRoleToUser(string userId, string roleName)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roleName))
            {
                TempData["ErrorMessage"] = "Usuário ou Role inválida.";
                return RedirectToAction(nameof(UserDetails), new { id = userId });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Usuário não encontrado.";
                return RedirectToAction(nameof(Users));
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                TempData["ErrorMessage"] = "Role não existe.";
                return RedirectToAction(nameof(UserDetails), new { id = userId });
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                TempData["InfoMessage"] = $"Usuário '{user.UserName}' já está na role '{roleName}'.";
            }
            else
            {
                var result = await _userManager.AddToRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"Role '{roleName}' atribuída a '{user.UserName}' com sucesso.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Erro ao atribuir role: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }
            return RedirectToAction(nameof(UserDetails), new { id = userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRoleFromUser(string userId, string roleName)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roleName))
            {
                TempData["ErrorMessage"] = "Usuário ou Role inválida.";
                return RedirectToAction(nameof(UserDetails), new { id = userId });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Usuário não encontrado.";
                return RedirectToAction(nameof(Users));
            }

            // Proteção para não remover a si mesmo da role "Admin" se for o único administrador.
            // Numa aplicação real, esta lógica deve ser robusta para garantir a segurança.
            if (user.Email == User.Identity.Name && roleName == "Admin")
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1)
                {
                    TempData["ErrorMessage"] = "Não é possível remover o último administrador.";
                    return RedirectToAction(nameof(UserDetails), new { id = userId });
                }
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"Role '{roleName}' removida de '{user.UserName}' com sucesso.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Erro ao remover role: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }
            else
            {
                TempData["InfoMessage"] = $"Usuário '{user.UserName}' não está na role '{roleName}'.";
            }
            return RedirectToAction(nameof(UserDetails), new { id = userId });
        }
    }
}