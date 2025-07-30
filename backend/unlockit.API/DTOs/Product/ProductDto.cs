using System;
using System.Collections.Generic;

namespace unlockit.API.DTOs.Product
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public Guid ProductUUID { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }


        public string? CategoryName { get; set; }
        public string? BrandName { get; set; }


        public List<string> Genres { get; set; } = new();
        public List<string> Platforms { get; set; } = new();
        public List<ProductImageDto> Images { get; set; } = new();
        public bool IsActive { get; set; }


    }
}