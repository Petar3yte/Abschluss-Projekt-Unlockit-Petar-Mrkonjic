using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using unlockit.API.Models.ProductContext;
using unlockit.API.Repositories;

namespace unlockit.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class PlatformsController : ControllerBase
    {
        //Dependency Injection 
        private readonly ProductRepository _repository;
        public PlatformsController(ProductRepository repository) 
        {
            _repository = repository;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePlatform([FromBody] Platform platform)
        {
            //Validierung
            if (platform == null || string.IsNullOrWhiteSpace(platform.Name)) return BadRequest();
            //Auftrag & Ergebniss
            var createdPlatform = await _repository.CreatePlatformAsync(platform);
            return Ok(createdPlatform);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlatform(int id)
        {
            //Auftrag & Ergebniss
            var success = await _repository.DeletePlatformAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
        [HttpGet]
        public async Task<IActionResult> GetAllPlatforms()
        {
            //Auftrag & Ergebniss
            var items = await _repository.GetAllPlatformsAsync();
            return Ok(items);
        }
    }
}