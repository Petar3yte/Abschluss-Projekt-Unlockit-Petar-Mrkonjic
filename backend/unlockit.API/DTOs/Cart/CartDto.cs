using System;
using System.Collections.Generic;

namespace unlockit.API.DTOs.Cart
{
    public class CartDto
    {
        public Guid CartUuid { get; set; } 
        public List<CartItemDto> Items { get; set; } = new();
    }
}