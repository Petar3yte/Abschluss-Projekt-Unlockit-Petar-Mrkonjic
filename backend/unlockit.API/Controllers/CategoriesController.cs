using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using unlockit.API.Repositories;
using unlockit.API.Models.ProductContext; 

namespace unlockit.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Mitarbeiter")] 
    public class CategoriesController : ControllerBase
    {
        //Dependency Injection 
        private readonly ProductRepository _productRepository;

        public CategoriesController(ProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            //Kategorien aus DB laden
            var categories = await _productRepository.GetAllCategoriesAsync();
            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] Category newCategory)
        {
            //Validierung
            if (newCategory == null || string.IsNullOrWhiteSpace(newCategory.Name))
            {
                return BadRequest("Kategoriename darf nicht leer sein.");
            }

            //Auftrag & Ergebniss
            var createdCategory = await _productRepository.CreateCategoryAsync(newCategory);
            return CreatedAtAction(nameof(GetAllCategories), new { id = createdCategory.CategoryId }, createdCategory);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            //Auftrag & Ergebniss
            var success = await _productRepository.DeleteCategoryAsync(id);
            if (!success)
            {
                return NotFound("Kategorie nicht gefunden.");
            }
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
        {
            //Validierung
            if (id != category.CategoryId && category.CategoryId != 0)
            {
                return BadRequest("ID-Konflikt.");
            }

            //Auftrag & Ergebniss
            var success = await _productRepository.UpdateCategoryAsync(id, category);
            if (!success)
            {
                return NotFound("Kategorie nicht gefunden.");
            }
            return NoContent();
        }
    }
}