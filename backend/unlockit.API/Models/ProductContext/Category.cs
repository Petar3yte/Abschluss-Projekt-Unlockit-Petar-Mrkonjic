namespace unlockit.API.Models.ProductContext
{
    public class Category
    {
        public int CategoryId { get; set; }
        public Guid CategoryUUID { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
