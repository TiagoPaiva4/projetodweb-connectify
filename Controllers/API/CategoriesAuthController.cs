using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using projetodweb_connectify.Models.DTOs;

namespace projetodweb_connectify.Controllers.API
{
    //[ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/categories-auth")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Protege todas as ações por defeito para Admins
    public class CategoriesAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        // Injetar IWebHostEnvironment para obter o caminho para wwwroot
        public CategoriesAuthController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        /// <summary>
        /// Devolve a lista de todas as Categorias. Acessível por qualquer pessoa.
        /// </summary>
        [HttpGet]
        [AllowAnonymous] // Permite acesso público a este método específico
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            // Retorna todas as categorias, ordenadas por nome
            return await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        }

        /// <summary>
        /// Devolve os detalhes de uma Categoria, incluindo os seus Tópicos. Acessível por qualquer pessoa.
        /// </summary>
        /// <param name="id">ID da Categoria</param>
        [HttpGet("{id}")]
        [AllowAnonymous] // Permite acesso público a este método específico
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Topics)
                    .ThenInclude(t => t.Creator)
                        .ThenInclude(p => p.User)
                .AsNoTracking() // Boa prática para operações de apenas leitura
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound(new { message = "Categoria não encontrada." });
            }

            // Ordenar tópicos por data de criação descendente
            if (category.Topics != null)
            {
                category.Topics = category.Topics.OrderByDescending(t => t.CreatedAt).ToList();
            }

            return category;
        }

        /// <summary>
        /// Cria uma nova Categoria. Requer perfil de Administrador.
        /// </summary>
        /// <param name="categoryDto">Dados da categoria a criar, incluindo nome, descrição e ficheiro de imagem.</param>
        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory([FromForm] CategoryCreateDto categoryDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newCategory = new Category
            {
                Name = categoryDto.Name,
                Description = categoryDto.Description
            };

            // Lógica de Upload da Imagem
            if (categoryDto.CategoryImageFile != null && categoryDto.CategoryImageFile.Length > 0)
            {
                newCategory.CategoryImageUrl = await SaveImage(categoryDto.CategoryImageFile);
            }
            else
            {
                // Define uma imagem padrão se nenhuma for enviada
                newCategory.CategoryImageUrl = "/images/categories/default_category_image.png";
            }

            _context.Categories.Add(newCategory);
            await _context.SaveChangesAsync();

            // Retorna um status 201 Created com a localização e o objeto criado
            return CreatedAtAction(nameof(GetCategory), new { id = newCategory.Id }, newCategory);
        }

        /// <summary>
        /// Atualiza uma Categoria existente. Requer perfil de Administrador.
        /// </summary>
        /// <param name="id">ID da Categoria a editar</param>
        /// <param name="categoryDto">Novos dados da Categoria</param>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int id, [FromForm] CategoryEditDto categoryDto)
        {
            if (id != categoryDto.Id)
            {
                return BadRequest(new { message = "O ID na URL não corresponde ao ID no corpo do pedido." });
            }

            var categoryToUpdate = await _context.Categories.FindAsync(id);

            if (categoryToUpdate == null)
            {
                return NotFound(new { message = "Categoria não encontrada." });
            }

            // Atualiza as propriedades
            categoryToUpdate.Name = categoryDto.Name;
            categoryToUpdate.Description = categoryDto.Description;

            // Se uma nova imagem foi enviada, processa-a
            if (categoryDto.CategoryImageFile != null && categoryDto.CategoryImageFile.Length > 0)
            {
                // Apaga a imagem antiga, se existir e não for a padrão
                DeleteImage(categoryToUpdate.CategoryImageUrl);
                // Guarda a nova imagem e atualiza o URL
                categoryToUpdate.CategoryImageUrl = await SaveImage(categoryDto.CategoryImageFile);
            }

            _context.Entry(categoryToUpdate).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // Retorna 204 No Content, indicando sucesso
        }


        /// <summary>
        /// Apaga uma Categoria. Requer perfil de Administrador.
        /// </summary>
        /// <param name="id">ID da categoria a apagar</param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new { message = "Categoria não encontrada." });
            }

            // Verifica se existem tópicos associados que impediriam a exclusão
            bool hasTopics = await _context.Topics.AnyAsync(t => t.CategoryId == id);
            if (hasTopics)
            {
                // HTTP 409 Conflict é apropriado aqui
                return Conflict(new { message = "Não é possível apagar esta categoria pois existem tópicos associados a ela." });
            }

            // Apaga a imagem associada do servidor
            DeleteImage(category.CategoryImageUrl);

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent(); // Retorna 204 No Content, indicando sucesso
        }


        // Métodos auxiliares privados

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }

        private async Task<string> SaveImage(IFormFile imageFile)
        {
            string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "categories");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return "/images/categories/" + uniqueFileName;
        }

        private void DeleteImage(string imageUrl)
        {
            // Não apagar a imagem padrão
            if (string.IsNullOrEmpty(imageUrl) || imageUrl == "/images/categories/default_category_image.png")
            {
                return;
            }

            string imagePath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(imagePath))
            {
                try
                {
                    System.IO.File.Delete(imagePath);
                }
                catch (Exception ex)
                {
                    // Log do erro, mas não impedir a operação da API
                    Console.WriteLine($"Erro ao apagar imagem da categoria: {ex.Message}");
                }
            }
        }
    }
}