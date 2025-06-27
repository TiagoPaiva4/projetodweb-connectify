using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using projetodweb_connectify.Data;
using projetodweb_connectify.Services;
using projetodweb_connectify.Services.Email;

// ===================================================================
// 1. CONFIGURAÇÃO INICIAL
// ===================================================================
var builder = WebApplication.CreateBuilder(args);


// ===================================================================
// 2. REGISTO DE SERVIÇOS NA INJEÇÃO DE DEPENDÊNCIA
// ===================================================================

// --- Base de Dados e Identity ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();


// =============================================================================================
// --- CONFIGURAÇÃO DA AUTENTICAÇÃO (AJUSTADA PARA SEGUIR O SEU EXEMPLO) ---
// =============================================================================================
var jwtSettings = builder.Configuration.GetRequiredSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]
    ?? throw new InvalidOperationException("A chave JWT 'Key' não pode ser nula."));

builder.Services.AddAuthentication(options => {
    // Deixar vazio, como no seu exemplo. O ASP.NET Core irá inferir os padrões.
})
   .AddCookie("Cookies", options => {
       // Configura o cookie para a autenticação web/MVC
       options.LoginPath = "/Identity/Account/Login";
       options.AccessDeniedPath = "/Identity/Account/AccessDenied";
   })
   .AddJwtBearer("Bearer", options => {
       // Configura a validação do token JWT para a API
       options.TokenValidationParameters = new TokenValidationParameters
       {
           ValidateIssuer = true,
           ValidateAudience = true,
           ValidateLifetime = true,
           ValidateIssuerSigningKey = true,
           ValidIssuer = jwtSettings["Issuer"],
           ValidAudience = jwtSettings["Audience"],
           IssuerSigningKey = new SymmetricSecurityKey(key)
       };
   });

// Registo do seu serviço para criar tokens
builder.Services.AddScoped<TokenService>();
// =============================================================================================


// --- Customização para API não redirecionar para Login ---
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };
});


// --- Serviços de MVC, Sessão e Email ---
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromSeconds(600);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddTransient<ICustomEmailSender, EmailSender>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));


// --- Serviços do Swagger para documentação da API ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Connectify API",
        Description = "API para a plataforma social Connectify.",
    });

    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});


// ===================================================================
// 3. CONSTRUÇÃO DA APLICAÇÃO E CONFIGURAÇÃO DO PIPELINE (MIDDLEWARE)
// ===================================================================
var app = builder.Build();

// Configuração do pipeline de pedidos HTTP
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Connectify API V1");
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Ativação da Autenticação e Autorização
app.UseAuthentication();
app.UseAuthorization();


// ===================================================================
// 4. MAPEAMENTO DE ROTAS E ENDPOINTS
// ===================================================================
app.MapControllers();
app.MapControllerRoute(
    name: "AdminArea",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();


// ===================================================================
// 5. EXECUÇÃO DA APLICAÇÃO
// ===================================================================
app.Run();