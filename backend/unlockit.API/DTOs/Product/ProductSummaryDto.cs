using System;

namespace unlockit.API.DTOs.Product
{
    public class ProductSummaryDto
    {
        public Guid ProductUUID { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? CategoryName { get; set; }
        public string? BrandName { get; set; }

        public string? MainImageUrl { get; set; }

        public List<string> Platforms { get; set; } = new();
        public List<string> Genres { get; set; } = new();
    }
}
