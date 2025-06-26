using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;  
using projetodweb_connectify.Models; 
using projetodweb_connectify.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace projetodweb_connectify.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly TokenService _tokenService;

        // Injetamos todas as dependências necessárias através do construtor
        public AuthController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            TokenService tokenService)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Endpoint para autenticar um utilizador e gerar um token JWT.
        /// </summary>
        /// <param name="loginDto">Objeto com o email e a password do utilizador.</param>
        /// <returns>Um token JWT e informações do utilizador em caso de sucesso.</returns>
        [HttpPost("login")]
        [AllowAnonymous] // Permite o acesso a este endpoint sem autenticação prévia
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // --- Passo 1: Autenticar contra o ASP.NET Core Identity ---
            var identityUser = await _userManager.FindByEmailAsync(loginDto.Email);
            if (identityUser == null)
            {
                // Não especificamos se o email ou a password estão errados por segurança
                return Unauthorized(new { message = "Email ou password inválidos." });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(identityUser, loginDto.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Email ou password inválidos." });
            }

            // --- Passo 2: Encontrar o perfil personalizado associado ---
            // Usamos o 'UserName' do IdentityUser (que por padrão é o email) para encontrar o nosso User personalizado.
            // O .Include() é crucial para carregar os dados do Perfil na mesma consulta.
            var customUser = await _context.Users
                                           .Include(u => u.Profile)
                                           .FirstOrDefaultAsync(u => u.Username == identityUser.UserName);

            // Verificação de segurança: Se o utilizador está autenticado, ele DEVE ter um perfil.
            if (customUser?.Profile == null)
            {
                // Este é um erro de integridade de dados no servidor, não um erro do cliente.
                // É uma boa prática logar este tipo de erro.
                return StatusCode(500, new { message = "Ocorreu um erro interno: o perfil do utilizador não foi encontrado." });
            }

            // --- Passo 3: Preparar as claims personalizadas para o token ---
            var customClaims = new[]
            {
                // O claim "profileId" é a informação crucial que queremos no token.
                new Claim("profileId", customUser.Profile.Id.ToString()),
                // Podemos adicionar outras informações úteis, como o nome do perfil.
                new Claim(ClaimTypes.Name, customUser.Profile.Name)
            };

            // --- Passo 4: Gerar o token JWT ---
            // Chamamos o nosso serviço para criar o token, passando as claims extra.
            var token = _tokenService.GenerateToken(identityUser, customClaims);

            // --- Passo 5: Retornar a resposta de sucesso ---
            return Ok(new
            {
                token = token,
                user = new
                {
                    profileId = customUser.Profile.Id,
                    name = customUser.Profile.Name,
                    email = identityUser.Email
                }
            });
        }
    }

    /// <summary>
    /// DTO para o processo de login padrão.
    /// Pode mover esta classe para uma pasta 'DTOs' se preferir.
    /// </summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O formato do email é inválido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "A password é obrigatória.")]
        public string Password { get; set; }
    }
}