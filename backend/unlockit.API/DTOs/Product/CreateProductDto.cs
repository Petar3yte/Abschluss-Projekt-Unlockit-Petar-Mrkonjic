using System.Collections.Generic;

namespace unlockit.API.DTOs.Product
{
    public class CreateProductDto
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public int? BrandId { get; set; }
        public List<int> GenreIds { get; set; } = new();
        public List<int> PlatformIds { get; set; } = new();
    }
}
