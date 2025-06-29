using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;

namespace projetodweb_connectify.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string _wwwRootPath;

        public CategoriesController(ApplicationDbContext context, Microsoft.AspNetCore.Hosting.IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _wwwRootPath = webHostEnvironment.WebRootPath;
        }

        // GET: Admin/Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(categories);
        }

        // GET: Admin/Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.Topics)
                .FirstOrDefaultAsync(m => m.Id == id); 

            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // GET: Admin/Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Category category, IFormFile? categoryImageFile)
        {
            ModelState.Remove(nameof(Category.Id)); // PK
            ModelState.Remove(nameof(Category.Topics));
            ModelState.Remove(nameof(Category.CategoryImageUrl));

            if (ModelState.IsValid)
            {
                if (categoryImageFile != null && categoryImageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_wwwRootPath, "images", "categories");
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
                        Console.WriteLine($"Erro ao carregar imagem da categoria (Admin): {ex.Message}");
                        ModelState.AddModelError("categoryImageFile", "Erro ao carregar a imagem.");
                        category.CategoryImageUrl = "/images/categories/default_category_image.png"; // Imagem padrão 
                    }
                }
                else
                {
                    category.CategoryImageUrl = "/images/categories/default_category_image.png"; // Imagem padrão 
                }


                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Categoria criada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Admin/Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            // FindAsync usa a chave primária definida no modelo, que é 'Id'
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // CORRIGIDO: O Bind deve usar 'Id' para corresponder à PK do modelo
        public async Task<IActionResult> Edit(int routeId, [Bind("Id,Name,Description,CategoryImageUrl")] Category categoryViewModel, IFormFile? categoryImageFile)
        {                                 
            if (routeId != categoryViewModel.Id) 
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Category.Topics));

            if (ModelState.IsValid)
            {
                try
                {
                    // Buscar a entidade pelo Id da rota para garantir que estamos atualizando a correta
                    var categoryToUpdate = await _context.Categories.FindAsync(routeId);
                    if (categoryToUpdate == null) return NotFound();

                    string defaultCategoryImage = "/images/categories/default_category_image.png";

                    categoryToUpdate.Name = categoryViewModel.Name;
                    categoryToUpdate.Description = categoryViewModel.Description;

                    if (categoryImageFile != null && categoryImageFile.Length > 0)
                    {
                        // Apagar imagem antiga se existir e não for a padrão
                        if (!string.IsNullOrEmpty(categoryToUpdate.CategoryImageUrl) && categoryToUpdate.CategoryImageUrl != defaultCategoryImage)
                        {
                            string oldImagePath = Path.Combine(_wwwRootPath, categoryToUpdate.CategoryImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                try { System.IO.File.Delete(oldImagePath); }
                                catch (Exception ex) { Console.WriteLine($"Erro ao apagar imagem antiga da categoria (Admin): {ex.Message}"); }
                            }
                        }

                        // Guardar nova imagem
                        string uploadsFolder = Path.Combine(_wwwRootPath, "images", "categories");
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
                        // Manter a imagem atual se nenhuma nova for enviada, usando o valor do ViewModel
                        categoryToUpdate.CategoryImageUrl = categoryViewModel.CategoryImageUrl;
                    }


                    _context.Update(categoryToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Categoria atualizada com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(categoryViewModel.Id)) 
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
            return View(categoryViewModel);
        }

        // GET: Admin/Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id); 

            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Admin/Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id) // Este 'id' vem da rota
        {
            // FindAsync usa a PK, que é 'Id'
            var category = await _context.Categories.FindAsync(id);

            if (category != null)
            {
                var hasTopics = await _context.Topics.AnyAsync(t => t.CategoryId == category.Id);
                var fkOnTopic = _context.Model.FindEntityType(typeof(Topic))?.GetForeignKeys()
                                  .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Category) &&
                                                         fk.Properties.Any(p => p.Name == "CategoryId")); // Em Topic, a FK para Category é CategoryId

                // A verificação aqui assume que a FK na tabela Topic que aponta para Category se chama 'CategoryId'
                // e que essa propriedade em Topic corresponde ao 'Id' da Categoria.
                // Se a propriedade FK em Topic for diferente, ajuste `t => t.NomeDaFKParaCategory == category.Id`
                if (hasTopics && fkOnTopic?.DeleteBehavior == DeleteBehavior.Restrict)
                {
                    TempData["ErrorMessage"] = "Não é possível excluir esta categoria pois existem tópicos associados a ela e a regra de exclusão impede. Mova ou exclua os tópicos primeiro.";
                    return RedirectToAction(nameof(Index));
                }

                string defaultCategoryImage = "/images/categories/default_category_image.png"; // Imagem padrão
                if (!string.IsNullOrEmpty(category.CategoryImageUrl) && category.CategoryImageUrl != defaultCategoryImage)
                {
                    string imagePath = Path.Combine(_wwwRootPath, category.CategoryImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        try { System.IO.File.Delete(imagePath); }
                        catch (Exception ex) { Console.WriteLine($"Erro ao apagar imagem da categoria (Admin): {ex.Message}"); }
                    }
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Categoria excluída com sucesso!";
            }
            else
            {
                TempData["ErrorMessage"] = "Categoria não encontrada.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id); 
        }
    }
}