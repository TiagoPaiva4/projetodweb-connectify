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
            var viewModel = new Feed();
            viewModel.IsUserAuthenticated = User.Identity?.IsAuthenticated ?? false;

            // --- T�picos Recomendados ---
            // Pegar alguns t�picos p�blicos, n�o pessoais, ordenados por data de cria��o
            // ou por alguma m�trica de popularidade se voc� tiver.
            viewModel.RecommendedTopics = await _context.Topics
                .Include(t => t.Category) // Para mostrar a categoria se desejar
                .Include(t => t.Creator).ThenInclude(c => c.User) // Para mostrar o criador
                .Where(t => !t.IsPrivate && !t.IsPersonal)
                .OrderByDescending(t => t.CreatedAt) // Mais recentes primeiro
                                                     // .OrderByDescending(t => t.Posts.Count()) // Mais populares (se a rela��o Posts estiver carregada e for perform�tico)
                .Take(5) // Quantidade de t�picos recomendados
                .ToListAsync();

            // --- Posts Gerais ("Para Voc�") ---
            viewModel.GeneralPosts = await _context.TopicPosts
                .Include(tp => tp.Profile).ThenInclude(p => p.User)
                .Include(tp => tp.Topic).ThenInclude(t => t.Category)
                .Include(tp => tp.Likes)       // Load likes for each post
                .Include(tp => tp.Comments)    // Load comments for the count
                .Where(tp => tp.Topic != null && !tp.Topic.IsPrivate && !tp.Topic.IsPersonal)
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();

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
                        userProfile = await _context.Profiles.AsNoTracking()
                                                .FirstOrDefaultAsync(p => p.UserId == appUser.Id);
                        if (userProfile != null)
                        {
                            viewModel.CurrentUserProfileId = userProfile.Id;
                        }
                    }
                }

                if (appUser != null && userProfile != null)
                {
                    // --- Posts dos Amigos ---
                    var friendUserIds = await _context.Friendships
                        .Where(f => (f.User1Id == appUser.Id || f.User2Id == appUser.Id) && f.Status == FriendshipStatus.Accepted)
                        .Select(f => f.User1Id == appUser.Id ? f.User2Id : f.User1Id)
                        .Distinct()
                        .ToListAsync();

                    if (friendUserIds.Any())
                    {
                        var friendProfileIds = await _context.Profiles
                            .Where(p => friendUserIds.Contains(p.UserId))
                            .Select(p => p.Id)
                            .ToListAsync();
                        viewModel.UserHasFriends = friendProfileIds.Any();
                        if (viewModel.UserHasFriends)
                        {
                            viewModel.FriendsPosts = await _context.TopicPosts
                                .Include(tp => tp.Profile).ThenInclude(p => p.User)
                                .Include(tp => tp.Topic).ThenInclude(t => t.Category)
                                .Include(tp => tp.Likes)       // Load likes for each post
                                .Include(tp => tp.Comments)    // Load comments for the count
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

                    // --- Sugest�es de Amizade ---
                    var existingRelationsUserIds = await _context.Friendships
                        .Where(f => f.User1Id == appUser.Id || f.User2Id == appUser.Id)
                        .Select(f => f.User1Id == appUser.Id ? f.User2Id : f.User1Id)
                        .Distinct()
                        .ToListAsync();

                    // Adiciona o pr�prio ID do usu�rio para garantir que ele n�o apare�a nas sugest�es
                    existingRelationsUserIds.Add(appUser.Id);

                    // Buscar perfis de usu�rios que n�o est�o em existingRelationsUserIds
                    // e que n�o s�o o pr�prio usu�rio
                    viewModel.FriendshipSuggestions = await _context.Profiles
                        .Include(p => p.User) // Incluir Users para ter o Username/Id
                        .Where(p => !existingRelationsUserIds.Contains(p.UserId))
                        .OrderBy(p => p.CreatedAt) // Ou Guid.NewGuid() para aleat�rio no DB que suporta, ou outra l�gica
                        .Take(5) // Pegar 5 sugest�es
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