namespace unlockit.API.DTOs.Product
{
    public class DeliveryItemDto
    {
        public Guid ProductUuid { get; set; }
        public int Quantity { get; set; }
        public decimal CostPerItem { get; set; }
    }
}
