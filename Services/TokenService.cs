// No seu ficheiro Services/TokenService.cs

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace projetodweb_connectify.Services // <-- Verifique o seu namespace
{
    public class TokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        // ALTERAÇÃO: Adicionámos um parâmetro opcional 'additionalClaims'
        public string GenerateToken(IdentityUser user, IEnumerable<Claim> additionalClaims = null)
        {
            var jwtSettings = _config.GetRequiredSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Claims padrão que identificam o utilizador no sistema Identity
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),       // ID do IdentityUser (Subject)
                new Claim(JwtRegisteredClaimNames.Email, user.Email!), // Email
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // ID único do Token
            };

            // Se existirem claims adicionais, adiciona-as à lista
            if (additionalClaims != null)
            {
                claims.AddRange(additionalClaims);
            }

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims, // Usa a lista de claims combinada
                expires: DateTime.UtcNow.AddHours(Convert.ToDouble(jwtSettings["ExpireHours"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}