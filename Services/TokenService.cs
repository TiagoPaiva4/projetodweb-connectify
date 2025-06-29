using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System;

namespace projetodweb_connectify.Services
{
    /// <summary>
    /// Serviço responsável pela geração de JSON Web Tokens (JWT) para autenticação.
    /// </summary>
    public class TokenService
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// Construtor que injeta a configuração da aplicação (appsettings.json).
        /// </summary>
        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Gera um token JWT para um determinado utilizador, com a possibilidade de incluir 'claims' adicionais.
        /// </summary>
        /// <param name="user">O objeto `IdentityUser` para o qual o token será gerado.</param>
        /// <param name="additionalClaims">Uma coleção opcional de 'claims' personalizadas a serem adicionadas ao token (ex: roles, IDs de perfil).</param>
        /// <returns>Uma string que representa o token JWT.</returns>
        public string GenerateToken(IdentityUser user, IEnumerable<Claim> additionalClaims = null)
        {
            // Obtém as configurações de JWT a partir do ficheiro de configuração.
            var jwtSettings = _config.GetRequiredSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Inicia a lista de 'claims' (reivindicações) padrão do token.
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),       // O 'Subject' (sujeito) do token, que é o ID único do utilizador.
                new Claim(JwtRegisteredClaimNames.Email, user.Email!), // O email do utilizador.
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // O 'JWT ID', um identificador único para o próprio token.
            };

            // Se forem fornecidas 'claims' adicionais, adiciona-as à lista.
            if (additionalClaims != null)
            {
                claims.AddRange(additionalClaims);
            }

            // Cria o objeto do token com todas as informações necessárias.
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims, // Utiliza a lista de 'claims' combinada.
                expires: DateTime.UtcNow.AddHours(Convert.ToDouble(jwtSettings["ExpireHours"])),
                signingCredentials: creds
            );

            // Serializa o objeto do token para o seu formato de string final.
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}