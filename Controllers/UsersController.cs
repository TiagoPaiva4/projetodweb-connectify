using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;

namespace projetodweb_connectify.Controllers
{
    // Este controlador é protegido para garantir que apenas administradores possam gerir os utilizadores.
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: Apresenta a lista de todos os utilizadores (função administrativa).
        /// </summary>
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        /// <summary>
        /// GET: Apresenta os detalhes de um utilizador específico.
        /// </summary>
        /// <param name="id">O ID do utilizador a ser visualizado.</param>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        /// <summary>
        /// GET: Apresenta o formulário para criar um novo utilizador.
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// POST: Processa a criação de um novo utilizador.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Por segurança, o 'bind' exclui campos sensíveis como a 'PasswordHash' ou o 'Id'.
        // A criação de um utilizador deve gerar internamente o 'hash' da palavra-passe.
        public async Task<IActionResult> Create([Bind("Username,Email")] Users user)
        {
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            {
                ModelState.AddModelError("Username", "Este nome de utilizador já está a ser utilizado.");
            }
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Este email já se encontra registado.");
            }

            if (ModelState.IsValid)
            {
                // A 'PasswordHash' deveria ser gerada aqui a partir de uma palavra-passe segura.
                // Exemplo conceptual: user.PasswordHash = GerarHashSeguro(passwordRecebida);
                user.CreatedAt = DateTime.UtcNow;

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        /// <summary>
        /// GET: Apresenta o formulário para editar um utilizador.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        /// <summary>
        /// POST: Processa a edição de um utilizador.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        // A 'PasswordHash' é intencionalmente omitida do 'bind' para impedir a sua alteração
        // através deste formulário. A alteração de palavra-passe deve ser uma funcionalidade separada.
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Email")] Users userViewModel)
        {
            if (id != userViewModel.Id)
            {
                return NotFound();
            }

            var userToUpdate = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (userToUpdate == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Atualizam-se apenas os campos pretendidos para preservar dados como 'CreatedAt' e 'PasswordHash'.
                    userToUpdate.Username = userViewModel.Username;
                    userToUpdate.Email = userViewModel.Email;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsersExists(userViewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(userViewModel);
        }

        /// <summary>
        /// GET: Apresenta a página de confirmação para eliminar um utilizador.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        /// <summary>
        /// POST: Confirma e executa a eliminação de um utilizador.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                // A remoção de um utilizador deve ter em conta os seus dados associados (perfis, publicações, etc.)
                // para evitar registos órfãos na base de dados, idealmente através de eliminação em cascata.
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Método privado para verificar se um utilizador existe, com base no seu ID.
        /// </summary>
        private bool UsersExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}