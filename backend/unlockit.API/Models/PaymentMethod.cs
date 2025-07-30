using System;

namespace unlockit.API.Models
{
    public class PaymentMethod
    {
        public int PaymentMethodId { get; set; }

        public string Name { get; set; }

        public bool IsEnabled { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}