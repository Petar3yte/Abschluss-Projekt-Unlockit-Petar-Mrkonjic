namespace unlockit.API.DTOs.User
{
    public class UpdateUserDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public DateTime? Birthdate { get; set; }

        public string? Password { get; set; }

        public string? Role { get; set; }
    }
}