using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using projetodweb_connectify.Models; // Para Feed, ErrorViewModel, etc.
using projetodweb_connectify.Data;    // Para ApplicationDbContext
using Microsoft.EntityFrameworkCore;    // Para ToListAsync, Include, etc.
using System.Linq;                      // Para LINQ queries
using System.Threading.Tasks;           // Para Task
// Microsoft.AspNetCore.Identity já não é necessário aqui para UserManager

namespace projetodweb_connectify.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        // private readonly UserManager<Users> _userManager; // REMOVIDO

        // Construtor modificado
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Certifique-se que você tem uma classe Feed com as propriedades necessárias
            // ou use HomeIndexViewModel se essa for a sua classe ViewModel.
            // Ex: public class Feed { public List<TopicPost> GeneralPosts { get; set; } ... etc. }
            var viewModel = new Feed(); // Se sua classe for Feed
            // var viewModel = new HomeIndexViewModel(); // Se sua classe for HomeIndexViewModel

            viewModel.IsUserAuthenticated = User.Identity?.IsAuthenticated ?? false;

            // --- Posts Gerais ("Para Você") ---
            viewModel.GeneralPosts = await _context.TopicPosts
                .Include(tp => tp.Profile)
                    .ThenInclude(p => p.User)
                .Include(tp => tp.Topic)
                    .ThenInclude(t => t.Category)
                .Where(tp => tp.Topic != null && !tp.Topic.IsPrivate && !tp.Topic.IsPersonal)
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();

            if (viewModel.IsUserAuthenticated)
            {
                Users? appUser = null; // Declarar fora para usar depois na lógica de amigos
                Profile? userProfile = null; // Declarar fora para usar o Id

                // Lógica para obter o utilizador e o ProfileID do utilizador logado
                var identityName = User.Identity?.Name; // Geralmente é o UserName
                if (!string.IsNullOrEmpty(identityName))
                {
                    appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == identityName); // Buscar pelo UserName
                    if (appUser != null)
                    {
                        userProfile = await _context.Profiles.AsNoTracking()
                                                .FirstOrDefaultAsync(p => p.UserId == appUser.Id);
                        if (userProfile != null)
                        {
                            viewModel.CurrentUserProfileId = userProfile.Id;
                            // ViewData["CurrentUserProfileId"] = userProfile.Id; // Se precisar no ViewData também para partials
                        }
                    }
                }
                // Fim da lógica de obtenção do utilizador e perfil


                if (appUser != null && userProfile != null) // Continuar apenas se encontrarmos o utilizador e o perfil
                {
                    // --- Posts dos Amigos ---
                    // Obter IDs dos Users amigos (usando appUser.Id)
                    var friendUserIds = await _context.Friendships
                        .Where(f => f.User1Id == appUser.Id && f.Status == FriendshipStatus.Accepted)
                        .Select(f => f.User2Id)
                        .Union(
                            _context.Friendships
                            .Where(f => f.User2Id == appUser.Id && f.Status == FriendshipStatus.Accepted)
                            .Select(f => f.User1Id)
                        )
                        .Distinct()
                        .ToListAsync();

                    if (friendUserIds.Any())
                    {
                        // Converter UserIDs de amigos para ProfileIDs de amigos
                        var friendProfileIds = await _context.Profiles
                            .Where(p => friendUserIds.Contains(p.UserId))
                            .Select(p => p.Id)
                            .ToListAsync();

                        viewModel.UserHasFriends = friendProfileIds.Any();

                        if (viewModel.UserHasFriends)
                        {
                            viewModel.FriendsPosts = await _context.TopicPosts
                                .Include(tp => tp.Profile)
                                    .ThenInclude(p => p.User)
                                .Include(tp => tp.Topic)
                                    .ThenInclude(t => t.Category)
                                .Where(tp => friendProfileIds.Contains(tp.ProfileId))
                                .OrderByDescending(p => p.CreatedAt)
                                .Take(20)
                                .ToListAsync();
                        }
                    }
                    else
                    {
                        viewModel.UserHasFriends = false;
                    }
                }
            }

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}