using unlockit.API.DTOs.User;

namespace unlockit.API.DTOs.Auth
{
    public class LoginResponseDto
    {
        public UserDto User { get; set; }
        public string Token { get; set; }
    }
}