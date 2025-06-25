using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;

namespace projetodweb_connectify.Controllers
{
    [Authorize]
    // REMOVA [Route("api/[controller]")] e [ApiController] DA CLASSE SE ESTE CONTROLLER
    // VAI SERVIR VIEWS MVC TRADICIONAIS E TAMBÉM TER ENDPOINTS DE API SEPARADOS.
    // public class FriendshipsController : Controller // << DEVE HERDAR DE CONTROLLER
    public class FriendshipsController : Controller // Exemplo: Controller para Views e APIs
    {
        private readonly ApplicationDbContext _context;

        public FriendshipsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Friendships OU Friendships/Index
        // Esta action serve a View principal das amizades
        // A única responsabilidade deste controlador é servir a página principal de amizades.
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Challenge();

            // A view não precisa mais de pré-carregar os dados.
            // O JavaScript irá chamar a API /api/friendships/pending para preencher a lista.
            ViewBag.CurrentUserId = currentUser.Id;
            return View(); // Retorna uma view praticamente vazia que será preenchida por JavaScript.
        }

       
        // Helper para obter o nosso Users customizado (o seu já está bom)
        private async Task<Users?> GetCurrentUserAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;
            return await _context.Users.Include(u => u.Profile)
                                 .FirstOrDefaultAsync(u => u.Username == username);
        }


    }
}