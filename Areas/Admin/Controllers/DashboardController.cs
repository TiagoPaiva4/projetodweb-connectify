// Areas/Admin/Controllers/DashboardController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data; 
using System.Linq;
using System.Threading.Tasks;

namespace projetodweb_connectify.Areas.Admin.Controllers
{
    [Area("Admin")] // Especifica que este controller pertence à área "Admin"
    [Authorize(Roles = "admin")] // Apenas usuários com a role "Admin" podem acessar
    public class DashboardController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context; // Se for gerenciar outros dados além de usuários/roles

        public DashboardController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: /Admin/Dashboard/Index
        public async Task<IActionResult> Index()
        {
            ViewBag.UserCount = await _userManager.Users.CountAsync();
            ViewBag.RoleCount = await _roleManager.Roles.CountAsync();
            // Adicione mais dados que você queira mostrar no painel admin
            return View();
        }

        // GET: /Admin/Dashboard/Users
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            // Você pode querer criar um ViewModel para passar mais informações sobre cada usuário
            // incluindo suas roles.
            return View(users);
        }

        // Exemplo: Ver detalhes de um usuário e suas roles
        // GET: /Admin/Dashboard/UserDetails/guid-do-usuario
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

            // Crie um ViewModel se precisar de mais lógica ou dados combinados
            ViewBag.UserRoles = userRoles;
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            return View(user);
        }

        // POST: /Admin/Dashboard/AssignRoleToUser
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

            // Evitar que o admin se remova da role Admin (ou a última role Admin)
            if (user.UserName == User.Identity.Name && roleName == "Admin")
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Contains("Admin")) // Se já está na role, a ação é "remover"
                {
                    // Poderia verificar se é o último admin, mas por simplicidade, vamos apenas avisar
                    // Numa app real, teria lógica para impedir remover o último admin
                }
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

        // POST: /Admin/Dashboard/RemoveRoleFromUser
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

            // Proteção para não remover a si mesmo da role Admin se for o único admin.
            // Esta é uma lógica simplificada. Uma real seria mais robusta.
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

        // Você pode adicionar mais actions aqui para gerenciar roles, configurações, etc.
    }
}