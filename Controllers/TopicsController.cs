using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;

namespace projetodweb_connectify.Controllers
{
    public class TopicsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TopicsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Topics
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Topics.Include(t => t.Creator);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Topics/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics
                .Include(t => t.Creator)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (topic == null)
            {
                return NotFound();
            }

            return View(topic);
        }

        // GET: Topics/Create
        public IActionResult Create()
        {
            ViewData["CreatedBy"] = new SelectList(_context.Profiles, "Id", "Id");
            return View();
        }

        // POST: Topics/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,IsPersonal,IsPrivate,Title,Description,CreatedBy,CreatedAt")] Topic topic)
        {
            if (ModelState.IsValid)
            {
                // Preenche automaticamente os campos IsPersonal, IsPrivate e CreatedAt
                topic.IsPersonal = false;
                topic.IsPrivate = false;
                topic.CreatedAt = DateTime.UtcNow;

                // Obter o e-mail do utilizador logado
                var email = User.Identity?.Name;

                if (email == null)
                {
                    return Unauthorized();
                }

                // Buscar o utilizador no banco de dados com o e-mail logado
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);

                if (user == null)
                {
                    return NotFound("Utilizador não encontrado.");
                }

                // Encontrar o perfil do utilizador
                var profile = await _context.Profiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);


                // Atribuir o ID do utilizador logado ao campo CreatedBy
                topic.CreatedBy = user.Id;

                // Atribuir o perfil do utilizador à propriedade de navegação 'Creator'
                topic.Creator = profile;

                // Adicionar o tópico ao banco de dados
                _context.Add(topic);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                foreach (var error in state.Errors)
                {
                    Console.WriteLine($"Erro no campo '{key}': {error.ErrorMessage}");
                }
            }

            // Carregar as informações necessárias para a view, caso haja erros
            ViewData["CreatedBy"] = new SelectList(_context.Profiles, "Id", "Id", topic.CreatedBy);
            return View(topic);
        }


        // GET: Topics/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics.FindAsync(id);
            if (topic == null)
            {
                return NotFound();
            }
            ViewData["CreatedBy"] = new SelectList(_context.Profiles, "Id", "Id", topic.CreatedBy);
            return View(topic);
        }

        // POST: Topics/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description")] Topic updatedTopic)
        {
            if (!ModelState.IsValid)
            {
                return View(updatedTopic);
            }

            var email = User.Identity?.Name;
            if (email == null) return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
            if (user == null) return Unauthorized();

            var profile = await _context.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null) return Unauthorized();

            var existingTopic = await _context.Topics.FindAsync(id);
            if (existingTopic == null) return NotFound();

            if (existingTopic.CreatedBy != profile.Id)
            {
                return Forbid();
            }

            // Só atualiza os campos permitidos
            existingTopic.Title = updatedTopic.Title;
            existingTopic.Description = updatedTopic.Description;

            try
            {
                _context.Update(existingTopic);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Topics.Any(t => t.Id == id))
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




        // GET: Topics/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics
                .Include(t => t.Creator)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (topic == null)
            {
                return NotFound();
            }

            return View(topic);
        }

        // POST: Topics/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var email = User.Identity?.Name;
            if (email == null)
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
            if (user == null)
                return Unauthorized();

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null)
                return Unauthorized();

            var topic = await _context.Topics.FindAsync(id);
            if (topic == null)
                return NotFound();

            // Bloqueia a eliminação do tópico pessoail do utilizador
            if (topic.IsPersonal && topic.IsPrivate)
                return Forbid(); 

            // Verifica se o utilizador é o criador
            if (topic.CreatedBy != profile.Id)
                return Forbid();

            _context.Topics.Remove(topic);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }



        private bool TopicExists(int id)
        {
            return _context.Topics.Any(e => e.Id == id);
        }
    }
}
