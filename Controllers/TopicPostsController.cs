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
        public IActionResult Create()
        {
            ViewData["ProfileId"] = new SelectList(_context.Profiles, "Id", "Id");
            ViewData["TopicId"] = new SelectList(_context.Topics, "Id", "Title");
            return View();
        }

        // POST: TopicPosts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,TopicId,ProfileId,Content,CreatedAt")] TopicPost topicPost)
        {
            if (ModelState.IsValid)
            {
                _context.Add(topicPost);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProfileId"] = new SelectList(_context.Profiles, "Id", "Id", topicPost.ProfileId);
            ViewData["TopicId"] = new SelectList(_context.Topics, "Id", "Title", topicPost.TopicId);
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
