namespace CryptoView.Data
{
    /// <summary>
    /// Represents extended user profile data persisted in the database.
    /// Linked to AppUser via UserId (one-to-one relationship).
    /// </summary>
    public class UserProfile
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to AppUser. Unique constraint ensures one-to-one relationship.
        /// </summary>
        public int UserId { get; set; }

        public AppUser AppUser { get; set; } = null!;

        /// <summary>
        /// Full name (first + last). Required field. Max 100 characters.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Email address. Required field. Max 255 characters.
        /// Stored separately for profile customization (distinct from AppUser.Email).
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Optional biography/description. Max 500 characters.
        /// </summary>
        public string? Bio { get; set; }

        /// <summary>
        /// Optional country/location. Max 100 characters.
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// JSON array of favorite cryptocurrency IDs. Stored as string for simplicity.
        /// Example: "[1, 2, 5]"
        /// </summary>
        public string? FavoriteCryptoIds { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
