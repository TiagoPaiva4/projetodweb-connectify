using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using projetodweb_connectify.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;

namespace projetodweb_connectify.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager; // Necessário para CheckPasswordSignInAsync
        private readonly TokenService _tokenService;

        public AuthController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            TokenService tokenService)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager; // Mantemos a injeção para validar a password
            _tokenService = tokenService;
        }

        /// <summary>
        /// Endpoint para autenticar um utilizador e gerar um token JWT.
        /// Este endpoint é para ser usado por clientes de API (ex: JavaScript, Mobile).
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // 1. Procura o utilizador pelo email na base de dados do Identity
            var identityUser = await _userManager.FindByEmailAsync(loginDto.Email);
            if (identityUser == null)
            {
                // Não encontrou o utilizador. Retorna Unauthorized para não dar pistas a atacantes.
                return Unauthorized(new { message = "Email ou password inválidos." });
            }

            // 2. Verifica se a password fornecida está correta para esse utilizador.
            //    CheckPasswordSignInAsync é o método correto para isto, pois não cria um cookie.
            var result = await _signInManager.CheckPasswordSignInAsync(identityUser, loginDto.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                // A password está incorreta.
                return Unauthorized(new { message = "Email ou password inválidos." });
            }

            // Se chegamos aqui, o utilizador está autenticado com sucesso.
            // Agora, vamos buscar os dados do perfil personalizado para enriquecer o token.

            var customUser = await _context.Users
                                           .Include(u => u.Profile)
                                           .FirstOrDefaultAsync(u => u.Username == identityUser.UserName);

            if (customUser?.Profile == null)
            {
                // Erro de integridade de dados no servidor.
                return StatusCode(500, new { message = "Erro interno: perfil do utilizador não encontrado." });
            }

            // 3. Prepara as 'claims' (informações) que irão dentro do token.
            var claims = new[]
            {
                // Claims padrão que o Identity pode usar
                new Claim(JwtRegisteredClaimNames.Sub, identityUser.Id),
                new Claim(JwtRegisteredClaimNames.Email, identityUser.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // ID único para o token
                
                // Suas claims personalizadas
                new Claim("profileId", customUser.Profile.Id.ToString()),
                new Claim(ClaimTypes.Name, customUser.Profile.Name), // Nome de exibição
                new Claim("username", customUser.Username)
            };

            // 4. Gera o token JWT usando o seu TokenService.
            // (Assumindo que o seu TokenService faz o que o GenerateJwtToken do exemplo faz)
            var token = _tokenService.GenerateToken(identityUser, claims);

            // 5. Retorna a resposta de sucesso com o token e os dados do utilizador.
            return Ok(new
            {
                token = token,
                user = new
                {
                    profileId = customUser.Profile.Id,
                    userId = customUser.Id,
                    name = customUser.Profile.Name,
                    profilePictureUrl = customUser.Profile.ProfilePicture,
                    email = identityUser.Email
                }
            });
        }
    }
}