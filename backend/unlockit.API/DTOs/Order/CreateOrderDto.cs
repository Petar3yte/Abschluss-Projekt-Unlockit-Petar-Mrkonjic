using System;
using System.Collections.Generic;

namespace unlockit.API.DTOs.Order
{
    public class CreateOrderDto
    {
        public Guid ShippingAddressUUID { get; set; }
        public List<CreateOrderItemDto> Items { get; set; } = new();
        public string PaymentMethodName { get; set; }
    }
}