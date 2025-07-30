using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using unlockit.API.DTOs.Product;
using unlockit.API.Models.ProductContext;
using unlockit.API.Repositories;

namespace unlockit.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        //Dependency Injection 
        private readonly ProductRepository _productRepository;
        private readonly BillingRepository _billingRepository;

        public ProductsController(ProductRepository productRepository, BillingRepository billingRepository)
        {
            _productRepository = productRepository;
            _billingRepository = billingRepository;
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Mitarbeiter")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto createProductDto)
        {
            //Validierung
            if (createProductDto == null)
            {
                return BadRequest();
            }

            //Formular
            var productToCreate = new Product
            {
                Name = createProductDto.Name,
                Description = createProductDto.Description,
                Price = createProductDto.Price,
                StockQuantity = createProductDto.StockQuantity,
                CategoryId = createProductDto.CategoryId,
                BrandId = createProductDto.BrandId,
                IsVisible = true,
                LowStockThreshold = 10
            };

            //Auftrag an die Zentrale (Repository)
            var createdProductWithIds = await _productRepository.CreateProductAsync( 
                productToCreate,
                createProductDto.GenreIds,
                createProductDto.PlatformIds
            );

            //Produkt Details erneut abrufen
            var newProductDto = await _productRepository.GetProductByUuidAsync(createdProductWithIds.ProductUUID);

            if (newProductDto == null)
            {
                return StatusCode(500, "Fehler: Konnte das neu erstellte Produkt nicht abrufen.");
            }
            return CreatedAtAction(nameof(GetProductByUuid), new { productUuid = newProductDto.ProductUUID }, newProductDto);
        }

        [HttpGet("{productUuid}")]
        public async Task<IActionResult> GetProductByUuid(Guid productUuid)
        {
            //Auftrag & Ergebniss
            var product = await _productRepository.GetProductByUuidAsync(productUuid);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProducts([FromQuery] string? categoryName = null, [FromQuery] string? searchTerm = null, [FromQuery] string? platformName = null, [FromQuery] string? genreName = null)
        {
            //Auftrag & Ergebniss
            var products = await _productRepository.GetAllProductsAsync(categoryName, searchTerm, platformName, genreName);
            return Ok(products);
        }

        [HttpPut("{productUuid}")]
        [Authorize(Roles = "Admin, Mitarbeiter")]
        public async Task<IActionResult> UpdateProduct(Guid productUuid, [FromBody] UpdateProductDto updateProductDto)
        {
            //Validierung
            var productFromDb = await _productRepository.GetProductByUuidAsync(productUuid);
            if (productFromDb == null)
            {
                return NotFound("Produkt nicht gefunden.");
            }

            //Formular
            var productToUpdate = new Product
            {
                Name = updateProductDto.Name,
                Description = updateProductDto.Description,
                Price = updateProductDto.Price,
                StockQuantity = updateProductDto.StockQuantity,
                IsVisible = updateProductDto.IsVisible,
                CategoryId = updateProductDto.CategoryId,
                BrandId = updateProductDto.BrandId
            };

            //Auftrag & Ergebniss
            var success = await _productRepository.UpdateProductAsync(
                productFromDb.ProductId,
                productToUpdate,
                updateProductDto.GenreIds,
                updateProductDto.PlatformIds);

            if (success)
            {
                return NoContent();
            }

            return BadRequest("Produkt konnte nicht aktualisiert werden.");
        }

        [HttpDelete("{productUuid}")]
        [Authorize(Roles = "Admin, Mitarbeiter")]
        public async Task<IActionResult> DeleteProduct(Guid productUuid)
        {
            //Auftrag & Ergebniss
            var success = await _productRepository.DeleteProductAsync(productUuid);

            if (success)
            {
                return NoContent();
            }

            return NotFound("Produkt nicht gefunden.");
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetAllCategories()
        {
            //Auftrag & Ergebniss
            var categories = await _productRepository.GetAllCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("platforms")]
        public async Task<IActionResult> GetAllPlatforms()
        {
            //Auftrag & Ergebniss
            var platforms = await _productRepository.GetAllPlatformsAsync();
            return Ok(platforms);
        }

        [HttpGet("genres")]
        public async Task<IActionResult> GetAllGenres()
        {
            //Auftrag & Ergebniss
            var genres = await _productRepository.GetAllGenresAsync();
            return Ok(genres);
        }

        [HttpGet("{productId}/images")]
        public async Task<IActionResult> GetProductImages(int productId)
        {
            //Auftrag & Ergebniss
            var images = await _productRepository.GetImagesForProductAsync(productId);
            return Ok(images);
        }

        [HttpPost("{productId}/upload-image")]
        public async Task<IActionResult> UploadProductImage(int productId, IFormFile file)
        {
            //Validierung
            if (file == null || file.Length == 0)
            {
                return BadRequest("Es wurde keine Datei hochgeladen.");
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

            //Speicherort vorbereiten
            var uploadsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }
            var filePath = Path.Combine(uploadsFolderPath, fileName);

            //Datei speichern (Server)
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            //Pfad zum Bild speichern (DB)
            var imageUrl = $"/Uploads/{fileName}";
            var newImage = await _productRepository.AddImageToProductAsync(productId, imageUrl);

            return CreatedAtAction(nameof(GetProductImages), new { productId = newImage.ProductId }, newImage);
        }

        [HttpDelete("images/{imageId}")]
        public async Task<IActionResult> DeleteProductImage(int imageId)
        {
            //Auftrag & Ergebniss
            var success = await _productRepository.DeleteImageAsync(imageId);
            if (!success)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpPost("details")]
        public async Task<IActionResult> GetProductDetailsForCart([FromBody] List<Guid> uuids)
        {
            //Validierung
            if (uuids == null || !uuids.Any())
            {
                return Ok(new List<ProductSummaryDto>());
            }

            //Auftrag & Ergebniss
            var products = await _productRepository.GetProductsByUuidsAsync(uuids);
            return Ok(products);
        }

        [Authorize(Roles = "Admin,Mitarbeiter")]
        [HttpPost("images/{imageId}/set-main")]
        public async Task<IActionResult> SetMainProductImage(int imageId)
        {
            //Auftrag & Ergebniss
            var success = await _productRepository.SetMainImageAsync(imageId);
            if (!success)
            {
                return NotFound("Bild nicht gefunden.");
            }
            return NoContent();
        }

        [HttpPost("record-delivery")]
        [Authorize(Roles = "Admin,Mitarbeiter")]
        public async Task<IActionResult> RecordDelivery([FromBody] List<DeliveryItemDto> deliveryItems)
        {
            //Validierung
            if (deliveryItems == null || !deliveryItems.Any())
            {
                return BadRequest(new { message = "Keine Lieferpositionen angegeben." });
            }

            //Auftrag & Ergebniss
            try
            {
                var productUuids = deliveryItems.Select(item => item.ProductUuid).ToList();

                var products = await _productRepository.GetProductsByUuidsAsync(productUuids);

                var productNames = products.Select(p => p.Name).ToList();
                var description = "Wareneinkauf / Lieferung";
                if (productNames.Any())
                {
                    description += ": " + string.Join(", ", productNames);
                }

                decimal totalCost = await _productRepository.ProcessDeliveryAndGetTotalCostAsync(deliveryItems);

                await _billingRepository.CreateExpenseAsync(description, totalCost);

                return Ok(new { message = "Lieferung erfolgreich verbucht." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ein Fehler ist beim Verarbeiten der Lieferung aufgetreten.", error = ex.Message });
            }
        }

        [HttpGet("{productUuid}/recent-orders")]
        [Authorize(Roles = "Admin,Mitarbeiter")]
        public async Task<IActionResult> GetRecentOrdersForProduct(Guid productUuid)
        {
            //Auftrag & Ergebniss
            var orders = await _productRepository.GetRecentOrdersForProductAsync(productUuid);
            return Ok(orders);
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin,Mitarbeiter")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProductsForAdmin()
        {
            //Auftrag & Ergebniss
            var products = await _productRepository.GetAllForAdminAsync();
            return Ok(products);
        }

        [HttpPost("{productUuid}/reactivate")]
        [Authorize(Roles = "Admin, Mitarbeiter")]
        public async Task<IActionResult> ReactivateProduct(Guid productUuid)
        {
            //Auftrag & Ergebniss
            var success = await _productRepository.ReactivateProductAsync(productUuid);

            if (success)
            {
                return NoContent();
            }

            return NotFound("Produkt zum Reaktivieren nicht gefunden.");
        }
    }
}