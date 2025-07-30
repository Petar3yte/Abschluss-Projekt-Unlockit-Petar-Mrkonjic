using System;

namespace unlockit.API.Models.ProductContext
{
    public class ProductImage
    {
        public int ProductImageId { get; set; }
        public Guid ProductImageUUID { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; }
        public bool IsMainImage { get; set; }
    }
}