using System;

namespace unlockit.API.DTOs.Order
{
    public class OrderItemDto
    {
        public Guid ProductUUID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal => Quantity * UnitPrice; 
    }
}