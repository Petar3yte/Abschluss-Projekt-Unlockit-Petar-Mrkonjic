using System.ComponentModel.DataAnnotations;

namespace unlockit.API.DTOs.Address
{
    public class CreateAddressDto
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string AddressLine1 { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string PostalCode { get; set; }
        [Required]
        public string Country { get; set; }
    }
}