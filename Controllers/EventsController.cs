using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
// Assuming your models are in this namespace
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

        // GET: Events
        public async Task<IActionResult> Index()
        {
            // FIX: Replaced "@ => @.Creator" with a valid lambda expression "e => e.Creator"
            var applicationDbContext = _context.Events.Include(e => e.Creator);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // FIX: Replaced "@ => @.Creator" with a valid lambda expression "e => e.Creator"
            var @event = await _context.Events
                .Include(e => e.Creator)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // GET: /Events/Create
        // This action is called when a user navigates to the create page.
        // It responds ONLY to GET requests.
        [HttpGet]
        public IActionResult Create()
        {
            // Creates an empty ViewModel to be used by the form.
            var viewModel = new EventCreateViewModel();
            return View(viewModel);
        }

        // POST: /Events/Create
        // This action is called when the user submits the creation form.
        // It responds ONLY to POST requests.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventCreateViewModel viewModel)
        {
            // Check if the user-submitted data is valid
            // (according to the rules in EventCreateViewModel).
            if (ModelState.IsValid)
            {
                // 1. Get the username of the logged-in user
                var identityUsername = User.Identity.Name;
                if (string.IsNullOrEmpty(identityUsername))
                {
                    // If for some reason the user is not logged in, don't continue.
                    // Challenge() will trigger the authentication flow (e.g., redirect to login page).
                    return Challenge();
                }

                // 2. Find the user's record in our 'Users' table
                var creator = await _context.Users.FirstOrDefaultAsync(u => u.Username == identityUsername);

                if (creator == null)
                {
                    // Add an error to the model if the user is not found in the database.
                    ModelState.AddModelError(string.Empty, "Your user profile was not found. Cannot create the event.");
                    // Return to the view to display the error.
                    return View(viewModel);
                }

                // 3. Map data from the ViewModel to a new 'Event' object
                var newEvent = new Event
                {
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    StartDateTime = viewModel.StartDateTime,
                    EndDateTime = viewModel.EndDateTime,
                    Location = viewModel.Location,
                    EventImageUrl = viewModel.EventImageUrl,

                    // 4. Fill in the automatic and required data
                    CreatorId = creator.Id,
                    CreatedAt = DateTime.UtcNow
                };

                // 5. Add the new event to the context and save to the database
                _context.Add(newEvent);
                await _context.SaveChangesAsync();

                // 6. Show a success message and redirect to the event list
                TempData["SuccessMessage"] = "Event created successfully!";
                return RedirectToAction(nameof(Index));
            }

            // If ModelState is not valid (e.g., a required field was left blank),
            // return to the form, keeping the data the user has already filled in.
            return View(viewModel);
        }

        // GET: Events/Edit/5
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

        // POST: Events/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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

        // GET: Events/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // FIX: Replaced "@ => @.Creator" with a valid lambda expression "e => e.Creator"
            var @event = await _context.Events
                .Include(e => e.Creator)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // POST: Events/Delete/5
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