namespace CryptoView.Data
{
    /// <summary>
    /// Represents an application user.
    /// Role is either "Admin" (single, seeded at startup) or "Usuario" (created via public register).
    /// </summary>
    public class AppUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Email { get; set; }

        /// <summary>"Admin" or "Usuario" — never modified from the public register endpoint.</summary>
        public string Role { get; set; } = "Usuario";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
