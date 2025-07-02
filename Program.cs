using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using projetodweb_connectify.Data;
using projetodweb_connectify.Services;
using projetodweb_connectify.Services.Email;
using System.IO;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURAÇÃO DOS SERVIÇOS ---

// Configuração da Base de Dados
// Cadeia de ligação para Tiago
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// Cadeia de ligação para Mário (Docker)
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnectionDocker") ?? throw new InvalidOperationException("Connection string 'DefaultConnectionDocker' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configuração do ASP.NET Core Identity (Utilizadores, Roles, etc.)
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Personaliza o comportamento da autenticação para distinguir entre Views e APIs.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        // Se o pedido for para um endpoint de API (começando com /api/),
        // retorna um estado 401 (Unauthorized) em vez de redirecionar.
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        else // Para todos os outros pedidos (Views), redireciona para a página de login.
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };
});

// Adiciona os serviços para Controladores (MVC e API) e Views.
builder.Services.AddControllersWithViews();

// Adicionar o serviço do SignalR à coleção de serviços.
builder.Services.AddSignalR();

// Configuração do serviço de sessão.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configuração do serviço de envio de emails.
builder.Services.AddTransient<ICustomEmailSender, EmailSender>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Configuração do Swagger para documentação da API.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Define as informações gerais da API que aparecem no topo do Swagger.
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Connectify API",
        Description = "API para a plataforma social Connectify.",
    });

    // Configura o Swagger para ler os comentários XML dos controladores e modelos.
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    options.IncludeXmlComments(xmlPath);
});

// --- CONFIGURAÇÃO DA APLICAÇÃO (MIDDLEWARE PIPELINE) ---

var app = builder.Build();

// Ativa as ferramentas de migração da base de dados no ambiente de desenvolvimento.
app.UseMigrationsEndPoint();

// Ativa o Swagger e a sua interface de utilizador (UI).
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Connectify API V1");
});

// Ativa o middleware de sessão.
app.UseSession();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Ativa os middlewares de Autenticação e Autorização.
// A ordem é importante: primeiro identifica-se o utilizador (Authentication),
// depois verifica-se se ele tem permissão (Authorization).
app.UseAuthentication();
app.UseAuthorization();

// --- MAPEAMENTO DE ROTAS E ENDPOINTS ---

// Mapeia o Hub do SignalR para o seu endpoint específico.
app.MapHub<LikesHub>("/likesHub");

// Rota para a área de Administração.
app.MapControllerRoute(
    name: "AdminArea",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Rota padrão para a aplicação principal.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mapeia as rotas para os controladores de API marcados com [ApiController].
app.MapControllers();

// Mapeia as Razor Pages, necessárias para as páginas do Identity (Login, Registo, etc.).
app.MapRazorPages();

app.Run();