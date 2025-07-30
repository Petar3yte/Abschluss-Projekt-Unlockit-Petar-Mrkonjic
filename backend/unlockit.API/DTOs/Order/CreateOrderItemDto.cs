using System;

namespace unlockit.API.DTOs.Order
{
    public class CreateOrderItemDto
    {
        public Guid ProductUUID { get; set; }
        public int Quantity { get; set; }
    }
}