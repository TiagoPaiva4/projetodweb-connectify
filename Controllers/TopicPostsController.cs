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
    public class TopicPostsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TopicPostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TopicPosts
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.TopicPosts.Include(t => t.Profile).Include(t => t.Topic);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: TopicPosts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topicPost = await _context.TopicPosts
                .Include(t => t.Profile)
                .Include(t => t.Topic)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (topicPost == null)
            {
                return NotFound();
            }

            return View(topicPost);
        }

        // GET: TopicPosts/Create
        public async Task<IActionResult> Create(int topicId)
        {
            if (topicId == 0)
            {
                return NotFound("Topic ID é obrigatório.");
            }
            var topic = await _context.Topics.FindAsync(topicId);
            if (topic == null)
            {
                // You might want to add a message to TempData or ViewBag
                return NotFound("Tópico não encontrado.");
            }

            var topicPost = new TopicPost
            {
                TopicId = topicId // THIS IS CRUCIAL
            };

            //Pass topic title to view
            ViewBag.TopicTitle = topic.Title;

            return View(topicPost);
        }

        // POST: TopicPosts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Content,CreatedAt")] TopicPost topicPost, int topicId)
        {
            // Verificar se o topicId foi passado corretamente
            if (topicId == 0) // This 'topicId' parameter is 0 because the form post URL doesn't include it
            {
                return NotFound("Tópico não encontrado."); // THIS IS LIKELY WHERE IT FAILS
            }

            topicPost.TopicId = topicId; // This line tries to use the parameter 'topicId'

            if (ModelState.IsValid)
            {
                topicPost.CreatedAt = DateTime.UtcNow;

                var email = User.Identity?.Name;
                if (email == null) return Unauthorized();

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
                if (user == null) return NotFound("Utilizador não encontrado.");

                var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (profile == null) return NotFound("Perfil não encontrado.");

                // Atribuir ProfileId ao post
                topicPost.ProfileId = profile.Id;

                // Carregar o tópico associado
                var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Id == topicPost.TopicId);
                if (topic == null) return NotFound("Tópico não encontrado.");

                // Atribuir a entidade Topic à publicação
                topicPost.Topic = topic;

                // Adicionar o post ao contexto e salvar
                _context.Add(topicPost);
                await _context.SaveChangesAsync();

                // Redirecionar para a página de detalhes do tópico após a criação do post
                return RedirectToAction("Details", "Topics", new { id = topicPost.TopicId });
            }

            // Se o modelo não for válido, imprimir os erros
            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                foreach (var error in state.Errors)
                {
                    Console.WriteLine($"Erro no campo '{key}': {error.ErrorMessage}");
                }
            }

            // Retornar a view com os erros
            return View(topicPost);
        }



        // GET: TopicPosts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topicPost = await _context.TopicPosts.FindAsync(id);
            if (topicPost == null)
            {
                return NotFound();
            }
            ViewData["ProfileId"] = new SelectList(_context.Profiles, "Id", "Id", topicPost.ProfileId);
            ViewData["TopicId"] = new SelectList(_context.Topics, "Id", "Title", topicPost.TopicId);
            return View(topicPost);
        }

        // POST: TopicPosts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TopicId,ProfileId,Content,CreatedAt")] TopicPost topicPost)
        {
            if (id != topicPost.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(topicPost);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TopicPostExists(topicPost.Id))
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
            ViewData["ProfileId"] = new SelectList(_context.Profiles, "Id", "Id", topicPost.ProfileId);
            ViewData["TopicId"] = new SelectList(_context.Topics, "Id", "Title", topicPost.TopicId);
            return View(topicPost);
        }

        // GET: TopicPosts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topicPost = await _context.TopicPosts
                .Include(t => t.Profile)
                .Include(t => t.Topic)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (topicPost == null)
            {
                return NotFound();
            }

            return View(topicPost);
        }

        // POST: TopicPosts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var topicPost = await _context.TopicPosts.FindAsync(id);
            if (topicPost != null)
            {
                _context.TopicPosts.Remove(topicPost);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TopicPostExists(int id)
        {
            return _context.TopicPosts.Any(e => e.Id == id);
        }
    }
}
