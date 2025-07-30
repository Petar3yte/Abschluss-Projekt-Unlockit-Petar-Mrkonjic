using unlockit.API.Models.ProductContext;

namespace unlockit.API.Models.OrderContext
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; } 
        public int ProductId { get; set; } 
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; } 
        public Product Product { get; set; }
    }
}