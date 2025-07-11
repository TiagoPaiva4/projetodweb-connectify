using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using projetodweb_connectify.Data;
using projetodweb_connectify.Services.Email;

var builder = WebApplication.CreateBuilder(args);


// Builder Tiago
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Builder Mário
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnectionDocker") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --- CONFIGURAÇÃO DO IDENTITY AJUSTADA ---
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// --- AJUSTE NA AUTENTICAÇÃO PARA FUNCIONAR COM APIS E VIEWS ---
// Esta configuração customiza o comportamento do Identity para que,
// quando uma API não autorizada é chamada, ele retorne um status HTTP 401 (Unauthorized)
// em vez de redirecionar para a página de login, o que é o correto para APIs.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        // Se o pedido for para um endpoint de API (começando com /api/),
        // não redirecione, apenas retorne o status 401.
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        else // Para todos os outros pedidos (Views), redirecione para a página de login.
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };
});


// Registra os serviços para controladores MVC e API.
// AddControllersWithViews já faz isso.
builder.Services.AddControllersWithViews();


// Configurar o uso de 'cookies' de sessão. Já estava bom.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromSeconds(600); // Aumentei para 10 minutos
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//Builder da confirmação de email. Já estava bom.
builder.Services.AddTransient<ICustomEmailSender, EmailSender>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// --- ADICIONAR SERVIÇOS PARA O SWAGGER (DOCUMENTAÇÃO DA API) ---
builder.Services.AddEndpointsApiExplorer();

// --- CONFIGURAÇÃO DO SWAGGER PARA LER COMENTÁRIOS XML ---
builder.Services.AddSwaggerGen(options =>
{
    // Define as informações gerais da sua API que aparecem no topo do Swagger
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Connectify API",
        Description = "API para a plataforma social Connectify.",
    });

    // Esta é a parte mais importante:
    // Diz ao Swagger para encontrar e carregar o ficheiro XML gerado.
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
    app.UseMigrationsEndPoint();
    // --- ATIVAR O SWAGGER EM AMBIENTE DE DESENVOLVIMENTO ---
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Connectify API V1");
    });



// Começar a usar, realmente, os 'cookies' de sessão
app.UseSession();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// --- ATIVAR MIDDLEWARE DE AUTENTICAÇÃO E AUTORIZAÇÃO ---
// UseAuthentication identifica quem é o utilizador.
// UseAuthorization verifica se o utilizador identificado tem permissão para aceder ao recurso.
app.UseAuthentication(); // Adicionado para garantir que o Identity é ativado
app.UseAuthorization();


// Mapeamento de Rotas
// Route for the Admin Area
app.MapControllerRoute(
    name: "AdminArea",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Default route for the main application
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// --- MAPEAMENTO DOS CONTROLADORES DE API ---
// Esta linha é essencial. Ela diz ao ASP.NET para procurar
// os controladores marcados com [ApiController] e mapear as suas rotas.
// Ex: "GET /api/categories-auth" será mapeado para o método GetCategories do seu CategoriesAuthController.
app.MapControllers();

app.MapRazorPages(); // Necessário para as páginas do Identity (Login, Register, etc.)

app.Run();