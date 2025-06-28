using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace projetodweb_connectify.Controllers
{
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;

    // MUDANÇA 2: O tipo no construtor também foi alterado para corresponder.
    public ProfilesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult MyProfile()
    {
        // Não precisa de passar nenhum modelo aqui. 
        // A view vai buscar os dados dinamicamente.
        return View();
    }

    [HttpGet("Search")] // Responde a /Profiles/Search
    public IActionResult Search()
    {
        return View();
    }


    // Rota alterada para ser mais específica
    [HttpGet("profile/{username}")]
    public IActionResult UserProfile(string username)
    {
        ViewData["ProfileUsername"] = username;
        return View(); // Retorna a view UserProfile.cshtml
    }
}