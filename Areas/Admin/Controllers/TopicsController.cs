using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;


namespace projetodweb_connectify.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
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
                _context.Add(topic);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }


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
        public async Task<IActionResult> Edit(int id, [Bind("Id,IsPersonal,IsPrivate,Title,Description,CreatedBy,CreatedAt")] Topic topic)
        {

            if (id != topic.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(topic);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TopicExists(topic.Id))
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


            ViewData["CreatedBy"] = new SelectList(_context.Profiles, "Id", "Id", topic.CreatedBy);


            return View(topic);

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

            var topic = await _context.Topics.FindAsync(id);

            if (topic != null)
            {
                _context.Topics.Remove(topic);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        private bool TopicExists(int id)
        {
            return _context.Topics.Any(e => e.Id == id);
        }


    }


}