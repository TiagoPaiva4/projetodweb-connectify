using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using projetodweb_connectify.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace projetodweb_connectify.Controllers.API
{
    // A rota é mais explícita para indicar que é para gestão de admin
    [Route("api/admin/users")]
    [ApiController]
    // MUITO IMPORTANTE: Apenas utilizadores com o perfil "Admin" podem aceder a este controlador.
    [Authorize(Roles = "Admin")]
    public class UsersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém uma lista de todos os utilizadores no sistema.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserAdminViewDto>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Profile) // Incluir perfil para obter o nome
                .OrderBy(u => u.Username)
                .Select(u => new UserAdminViewDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    CreatedAt = u.CreatedAt,
                    ProfileName = u.Profile != null ? u.Profile.Name : "N/A"
                })
                .ToListAsync();

            return Ok(users);
        }

        /// <summary>
        /// Obtém os detalhes de um utilizador específico.
        /// </summary>
        /// <param name="id">O ID do utilizador.</param>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserAdminViewDto>> GetUser(int id)
        {
            var userDto = await _context.Users
                .Where(u => u.Id == id)
                .Include(u => u.Profile)
                .Select(u => new UserAdminViewDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    CreatedAt = u.CreatedAt,
                    ProfileName = u.Profile != null ? u.Profile.Name : "N/A"
                })
                .FirstOrDefaultAsync();

            if (userDto == null)
            {
                return NotFound();
            }

            return Ok(userDto);
        }

        /// <summary>
        /// Cria um novo utilizador. A password será processada (hashed).
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<UserAdminViewDto>> CreateUser(UserCreateDto createDto)
        {
            // Verificar se o username ou email já existem
            if (await _context.Users.AnyAsync(u => u.Username == createDto.Username || u.Email == createDto.Email))
            {
                return Conflict(new { message = "Um utilizador com este username ou email já existe." });
            }

            // *** ETAPA CRÍTICA: HASHING DA PASSWORD ***
            // O seu sistema precisa de uma forma de gerar um hash seguro da password.
            // BCrypt.Net.BCrypt.HashPassword(createDto.Password) é uma excelente opção.
            // Para este exemplo, vamos simular, mas você DEVE usar uma biblioteca real.
            string passwordHash = HashPassword(createDto.Password);
            if (string.IsNullOrEmpty(passwordHash))
            {
                return Problem("Falha ao processar a password.", statusCode: 500);
            }

            var newUser = new Users
            {
                Username = createDto.Username,
                Email = createDto.Email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Retornar os dados do utilizador criado (sem a password, claro)
            var resultDto = new UserAdminViewDto { Id = newUser.Id, Username = newUser.Username, Email = newUser.Email, CreatedAt = newUser.CreatedAt };

            return CreatedAtAction(nameof(GetUser), new { id = newUser.Id }, resultDto);
        }

        /// <summary>
        /// Atualiza os dados de um utilizador. A password é opcional.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserEditDto editDto)
        {
            var userToUpdate = await _context.Users.FindAsync(id);
            if (userToUpdate == null) return NotFound();

            // Verificar conflitos de username/email com OUTROS utilizadores
            if (await _context.Users.AnyAsync(u => u.Id != id && (u.Username == editDto.Username || u.Email == editDto.Email)))
            {
                return Conflict(new { message = "O novo username ou email já está a ser utilizado por outro utilizador." });
            }

            userToUpdate.Username = editDto.Username;
            userToUpdate.Email = editDto.Email;

            // Se uma nova password foi fornecida, faz o hash e atualiza
            if (!string.IsNullOrWhiteSpace(editDto.Password))
            {
                userToUpdate.PasswordHash = HashPassword(editDto.Password);
            }

            _context.Update(userToUpdate);
            await _context.SaveChangesAsync();

            return NoContent(); // Sucesso
        }

        /// <summary>
        /// Apaga um utilizador. Esta é uma ação destrutiva.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var userToDelete = await _context.Users.FindAsync(id);
            if (userToDelete == null) return NotFound();

            try
            {
                _context.Users.Remove(userToDelete);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Esta exceção ocorre frequentemente se o utilizador tiver dados associados (posts, perfil, etc.)
                // e as chaves estrangeiras não estiverem configuradas para apagar em cascata (ON DELETE CASCADE).
                return Conflict(new { message = "Não é possível apagar este utilizador porque ele tem dados associados (perfil, posts, etc.). Remova os dados associados primeiro." });
            }

            return NoContent(); // Sucesso
        }

        #region Helper Methods

        // !! AVISO IMPORTANTE !!
        // Este é um método de EXEMPLO. Você DEVE substituí-lo por uma implementação
        // de hashing de passwords real e segura, como a biblioteca BCrypt.Net.
        // Exemplo com BCrypt: return BCrypt.Net.BCrypt.HashPassword(password);
        private string HashPassword(string password)
        {
            // NUNCA guarde passwords em texto plano.
            // Para instalar o BCrypt: dotnet add package BCrypt.Net-Next
            if (string.IsNullOrWhiteSpace(password)) return null;
            return $"HASHED__{password}__REPLACE_WITH_REAL_HASHING_LOGIC"; // Placeholder
        }
        #endregion
    }
}