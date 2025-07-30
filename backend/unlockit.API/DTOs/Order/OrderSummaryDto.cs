namespace unlockit_API.DTOs.Order
{
    public class OrderSummaryDto
    {
        public Guid OrderUUID { get; set; }
        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; }
    }
}