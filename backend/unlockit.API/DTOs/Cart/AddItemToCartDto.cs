using System;

namespace unlockit.API.DTOs.Cart
{
    public class AddItemToCartDto
    {
        public Guid ProductUuid { get; set; }
        public int Quantity { get; set; }
    }
}