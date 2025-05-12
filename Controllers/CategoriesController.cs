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
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            return View(await _context.Categories.ToListAsync());
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .Include(c => c.Topics)
                    .ThenInclude(t => t.Creator)
                        .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null) return NotFound();

            if (category.Topics != null)
            {
                category.Topics = category.Topics.OrderByDescending(t => t.CreatedAt).ToList();
            }

            return View(category);
        }


        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Adicionar IFormFile categoryImageFile e remover CategoryImageUrl do Bind
        public async Task<IActionResult> Create([Bind("Name,Description")] Category category, IFormFile? categoryImageFile)
        {
            // Remover CategoryImageUrl da validação explícita do ModelState, pois será definido aqui
            ModelState.Remove(nameof(Category.CategoryImageUrl));
            ModelState.Remove(nameof(Category.Id)); // Id é gerado pela BD
            ModelState.Remove(nameof(Category.Topics)); // Propriedade de navegação


            if (ModelState.IsValid)
            {
                // Lógica de Upload da Imagem da Categoria
                if (categoryImageFile != null && categoryImageFile.Length > 0)
                {
                    string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string uploadsFolder = Path.Combine(wwwRootPath, "images", "categories");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(categoryImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    try
                    {
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await categoryImageFile.CopyToAsync(fileStream);
                        }
                        category.CategoryImageUrl = "/images/categories/" + uniqueFileName;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao carregar a imagem da categoria: {ex.Message}");
                        ModelState.AddModelError("categoryImageFile", $"Erro ao carregar a imagem: {ex.Message}");
                        // Definir uma imagem padrão em caso de erro no upload
                        category.CategoryImageUrl = "/images/categories/default_category_image.png";
                    }
                }
                else
                {
                    // Nenhuma imagem enviada, usar a padrão (se existir uma)
                    category.CategoryImageUrl = "/images/categories/default_category_image.png"; // Ajuste o caminho da sua padrão
                }

                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Categoria criada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Adicionar IFormFile e CategoryImageUrl ao Bind (para o campo hidden)
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,CategoryImageUrl")] Category categoryViewModel, IFormFile? categoryImageFile)
        {
            if (id != categoryViewModel.Id) return NotFound();

            ModelState.Remove(nameof(Category.Topics));

            if (ModelState.IsValid)
            {
                try
                {
                    var categoryToUpdate = await _context.Categories.FindAsync(id);
                    if (categoryToUpdate == null) return NotFound();

                    string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string defaultCategoryImage = "/images/categories/default_category_image.png"; // Ajuste

                    // Atualizar propriedades básicas
                    categoryToUpdate.Name = categoryViewModel.Name;
                    categoryToUpdate.Description = categoryViewModel.Description;

                    // Lógica de Imagem
                    if (categoryImageFile != null && categoryImageFile.Length > 0)
                    {
                        // Apagar imagem antiga se existir e não for a padrão
                        if (!string.IsNullOrEmpty(categoryToUpdate.CategoryImageUrl) && categoryToUpdate.CategoryImageUrl != defaultCategoryImage)
                        {
                            string oldImagePath = Path.Combine(wwwRootPath, categoryToUpdate.CategoryImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                try { System.IO.File.Delete(oldImagePath); }
                                catch (Exception ex) { Console.WriteLine($"Erro ao apagar imagem antiga da categoria: {ex.Message}"); }
                            }
                        }

                        // Guardar nova imagem
                        string uploadsFolder = Path.Combine(wwwRootPath, "images", "categories");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(categoryImageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await categoryImageFile.CopyToAsync(fileStream);
                        }
                        categoryToUpdate.CategoryImageUrl = "/images/categories/" + uniqueFileName;
                    }
                    else
                    {
                        // Nenhuma nova imagem foi enviada, manter a imagem que veio do hidden field (ou a da BD se o hidden field estiver vazio)
                        categoryToUpdate.CategoryImageUrl = categoryViewModel.CategoryImageUrl; // Usa o valor do campo hidden
                    }


                    _context.Update(categoryToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Categoria atualizada com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(categoryViewModel.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(categoryViewModel);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            // Incluir a imagem para mostrar na view de confirmação
            var category = await _context.Categories.FirstOrDefaultAsync(m => m.Id == id);
            if (category == null) return NotFound();
            return View(category);
        }


        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                // Antes de apagar a categoria, apagar a imagem associada (se não for a padrão)
                // e se a política de exclusão dos tópicos for Restrict, verificar se há tópicos.
                // Se for SetNull, os tópicos ficarão sem categoria.

                // Verificar se há tópicos associados (se a FK em Topic para Category é Restrict)
                bool hasTopics = await _context.Topics.AnyAsync(t => t.CategoryId == id);
                if (hasTopics && _context.Model.FindEntityType(typeof(Topic))?.GetForeignKeys().FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Category))?.DeleteBehavior == DeleteBehavior.Restrict)
                {
                    TempData["ErrorMessage"] = "Não é possível apagar esta categoria pois existem tópicos associados a ela. Mova ou apague os tópicos primeiro.";
                    return RedirectToAction(nameof(Index)); // Ou para Details(id)
                }


                // Apagar imagem
                if (!string.IsNullOrEmpty(category.CategoryImageUrl) && category.CategoryImageUrl != "/images/categories/default_category_image.png") // Ajuste
                {
                    string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string imagePath = Path.Combine(wwwRootPath, category.CategoryImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        try { System.IO.File.Delete(imagePath); }
                        catch (Exception ex) { Console.WriteLine($"Erro ao apagar imagem da categoria: {ex.Message}"); }
                    }
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Categoria eliminada com sucesso!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}
