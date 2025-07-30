using unlockit.API.Models;
using unlockit_API.DTOs.Order;

namespace unlockit.API.DTOs.User
{
    public class UserDto
    {
        public Guid UserUUID { get; set; }
        public string UserName { get; set; }
        public UserRole Role { get; set; }
        public string Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? Birthdate { get; set; }
        public string? ProfilePictureUrl { get; set; }

        public List<OrderSummaryDto> RecentOrders { get; set; }
    }
}