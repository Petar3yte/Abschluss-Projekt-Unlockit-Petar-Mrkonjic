namespace unlockit.API.Models
{
    public enum UserRole
    {
        Kunde,
        Mitarbeiter,
        Admin
    }
    public class User
    {
        public int UserId { get; set; }
        public Guid UserUUID { get; set; }
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public UserRole Role { get; set; }
        public string Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? Birthdate { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
