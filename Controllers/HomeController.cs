using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using projetodweb_connectify.Models;
using projetodweb_connectify.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace projetodweb_connectify.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new Feed
            {
                IsUserAuthenticated = User.Identity?.IsAuthenticated ?? false
            };

            // --- Tópicos Recomendados ---
            // Obter tópicos públicos e não pessoais para exibir a todos os visitantes.
            viewModel.RecommendedTopics = await _context.Topics
                .Include(t => t.Category) // Inclui a categoria para ser apresentada na view.
                .Include(t => t.Creator).ThenInclude(c => c.User) // Inclui o criador e os dados do seu utilizador.
                .Where(t => !t.IsPrivate && !t.IsPersonal)
                .OrderByDescending(t => t.CreatedAt)
                // Alternativa: ordenar por popularidade (pode ter impacto na performance).
                // .OrderByDescending(t => t.Posts.Count()) 
                .Take(5)
                .ToListAsync();

            // --- Posts Gerais (Feed "Para Si") ---
            // Obter os posts mais recentes de tópicos públicos para o feed geral.
            viewModel.GeneralPosts = await _context.TopicPosts
                .Include(tp => tp.Profile).ThenInclude(p => p.User)
                .Include(tp => tp.Topic).ThenInclude(t => t.Category)
                .Include(tp => tp.Likes)       // Carrega os 'likes' de cada post.
                .Include(tp => tp.Comments)    // Carrega os comentários para contagem.
                .Where(tp => tp.Topic != null && !tp.Topic.IsPrivate && !tp.Topic.IsPersonal)
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();

            // Se o utilizador estiver autenticado, carregar conteúdo personalizado.
            if (viewModel.IsUserAuthenticated)
            {
                Users? appUser = null;
                Profile? userProfile = null;
                var identityName = User.Identity?.Name;

                if (!string.IsNullOrEmpty(identityName))
                {
                    appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == identityName);
                    if (appUser != null)
                    {
                        // Usar AsNoTracking para uma consulta mais rápida, pois não vamos modificar este perfil.
                        userProfile = await _context.Profiles.AsNoTracking()
                                                .FirstOrDefaultAsync(p => p.UserId == appUser.Id);
                        if (userProfile != null)
                        {
                            viewModel.CurrentUserProfileId = userProfile.Id;
                        }
                    }
                }

                // Apenas continuar se o utilizador e o seu perfil foram encontrados.
                if (appUser != null && userProfile != null)
                {
                    // --- Posts de Amigos ---
                    var friendUserIds = await _context.Friendships
                        .Where(f => (f.User1Id == appUser.Id || f.User2Id == appUser.Id) && f.Status == FriendshipStatus.Accepted)
                        .Select(f => f.User1Id == appUser.Id ? f.User2Id : f.User1Id)
                        .Distinct()
                        .ToListAsync();

                    viewModel.UserHasFriends = friendUserIds.Any();
                    if (viewModel.UserHasFriends)
                    {
                        var friendProfileIds = await _context.Profiles
                            .Where(p => friendUserIds.Contains(p.UserId))
                            .Select(p => p.Id)
                            .ToListAsync();

                        viewModel.FriendsPosts = await _context.TopicPosts
                            .Include(tp => tp.Profile).ThenInclude(p => p.User)
                            .Include(tp => tp.Topic).ThenInclude(t => t.Category)
                            .Include(tp => tp.Likes)
                            .Include(tp => tp.Comments)
                            .Where(tp => friendProfileIds.Contains(tp.ProfileId))
                            .OrderByDescending(p => p.CreatedAt)
                            .Take(20)
                            .ToListAsync();
                    }

                    // --- Sugestões de Amizade ---
                    var existingRelationsUserIds = await _context.Friendships
                        .Where(f => f.User1Id == appUser.Id || f.User2Id == appUser.Id)
                        .Select(f => f.User1Id == appUser.Id ? f.User2Id : f.User1Id)
                        .Distinct()
                        .ToListAsync();

                    // Adiciona o ID do próprio utilizador para garantir que não aparece a si mesmo nas sugestões.
                    existingRelationsUserIds.Add(appUser.Id);

                    // Procurar perfis de utilizadores que não sejam amigos ou o próprio utilizador.
                    viewModel.FriendshipSuggestions = await _context.Profiles
                        .Include(p => p.User)
                        .Where(p => !existingRelationsUserIds.Contains(p.UserId))
                        // Ordenar por data de criação ou usar Guid.NewGuid() para uma ordem aleatória (se o SGBD suportar).
                        .OrderBy(p => p.CreatedAt)
                        .Take(5)
                        .ToListAsync();
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