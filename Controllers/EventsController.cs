using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using System.Security.Claims;
using projetodweb_connectify.Models;

namespace projetodweb_connectify.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Events
        // Apresenta a lista de todos os eventos.
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Events.Include(e => e.Creator);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: /Events/Details/5
        // Mostra os detalhes de um evento específico, incluindo os participantes.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventModel = await _context.Events
                .Include(e => e.Creator)
                .Include(e => e.Attendees)
                    .ThenInclude(attendance => attendance.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (eventModel == null)
            {
                return NotFound();
            }

            return View(eventModel);
        }

        // GET: /Events/Create
        // Apresenta o formulário para criar um novo evento.
        [HttpGet]
        public IActionResult Create()
        {
            // Cria um ViewModel vazio para ser utilizado pelo formulário.
            var viewModel = new EventCreateViewModel();
            return View(viewModel);
        }

        // POST: /Events/Create
        // Processa os dados submetidos do formulário de criação de evento.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventCreateViewModel viewModel)
        {
            // Verifica se os dados submetidos pelo utilizador são válidos
            // (de acordo com as regras definidas no EventCreateViewModel).
            if (ModelState.IsValid)
            {
                // 1. Obter o nome de utilizador (username) do utilizador autenticado.
                var identityUsername = User.Identity.Name;
                if (string.IsNullOrEmpty(identityUsername))
                {
                    // Se o utilizador não estiver autenticado, aciona o fluxo de autenticação (ex: redireciona para a página de login).
                    return Challenge();
                }

                // 2. Encontrar o registo do utilizador na nossa tabela 'Users'.
                var creator = await _context.Users.FirstOrDefaultAsync(u => u.Username == identityUsername);
                if (creator == null)
                {
                    ModelState.AddModelError(string.Empty, "O seu perfil de utilizador não foi encontrado. Não é possível criar o evento.");
                    return View(viewModel);
                }

                // 3. Mapear os dados do ViewModel para um novo objeto 'Event'.
                var newEvent = new Event
                {
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    StartDateTime = viewModel.StartDateTime,
                    EndDateTime = viewModel.EndDateTime,
                    Location = viewModel.Location,
                    EventImageUrl = viewModel.EventImageUrl,
                    // Preenche os dados automáticos e obrigatórios.
                    CreatorId = creator.Id,
                    CreatedAt = DateTime.UtcNow
                };

                // 4. Adicionar o novo evento ao contexto e guardar na base de dados.
                _context.Add(newEvent);
                await _context.SaveChangesAsync();

                // 5. Mostrar uma mensagem de sucesso e redirecionar para a lista de eventos.
                TempData["SuccessMessage"] = "Evento criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            // Se o ModelState não for válido, retorna para o formulário,
            // mantendo os dados que o utilizador já preencheu.
            return View(viewModel);
        }

        // GET: /Events/Edit/5
        // Apresenta o formulário para editar um evento existente.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }
            ViewData["CreatorId"] = new SelectList(_context.Users, "Id", "Username", @event.CreatorId);
            return View(@event);
        }

        // POST: /Events/Edit/5
        // Processa os dados submetidos do formulário de edição.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,StartDateTime,EndDateTime,Location,EventImageUrl,CreatorId,CreatedAt,UpdatedAt")] Event @event)
        {
            if (id != @event.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(@event.Id))
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
            ViewData["CreatorId"] = new SelectList(_context.Users, "Id", "Username", @event.CreatorId);
            return View(@event);
        }

        // GET: /Events/Delete/5
        // Apresenta a página de confirmação para apagar um evento.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .Include(e => e.Creator)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // POST: /Events/Delete/5
        // Executa a ação de apagar o evento após a confirmação.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                _context.Events.Remove(@event);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }
    }
}