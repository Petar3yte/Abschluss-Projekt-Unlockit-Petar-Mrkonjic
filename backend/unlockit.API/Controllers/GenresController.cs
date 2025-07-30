using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using unlockit.API.Models.ProductContext;
using unlockit.API.Repositories;

namespace unlockit.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class GenresController : ControllerBase
    {
        //Dependency Injection 
        private readonly ProductRepository _repository;
        public GenresController(ProductRepository repository) 
        {
            _repository = repository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateGenre([FromBody] Genre genre)
        {
            //Validierung
            if (genre == null || string.IsNullOrWhiteSpace(genre.Name)) return BadRequest();
            //Auftrag & Ergebniss
            var createdGenre = await _repository.CreateGenreAsync(genre);
            return Ok(createdGenre);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            //Auftrag & Ergebniss
            var success = await _repository.DeleteGenreAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGenres()
        {
            //Auftrag & Ergebniss
            var items = await _repository.GetAllGenresAsync();
            return Ok(items);
        }
    }
}