using unlockit.API.Models.ProductContext;

namespace unlockit.API.Models.CartContext
{
    public class CartItem
    {
        public int CartItemId { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public Product Product { get; set; }
    }
}