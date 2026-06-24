using CryptoView.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CryptoView.Services
{
    /// <summary>
    /// Service for managing user profile data.
    /// Handles validation and persistence to UserProfile table.
    /// </summary>
    public class ProfileService
    {
        private readonly CryptoDbContext _db;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(CryptoDbContext db, ILogger<ProfileService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the profile for a given user ID.
        /// Returns null if profile doesn't exist.
        /// </summary>
        public async Task<ProfileDto?> GetUserProfileAsync(int userId)
        {
            try
            {
                var profile = await _db.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile is null)
                {
                    _logger.LogInformation($"[ProfileService] No profile found for UserId {userId}");
                    return null;
                }

                _logger.LogInformation($"[ProfileService] Profile loaded for UserId {userId}");
                return new ProfileDto(
                    profile.FullName,
                    profile.Email,
                    profile.Bio,
                    profile.Country,
                    profile.FavoriteCryptoIds
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ProfileService] Error getting profile for UserId {userId}");
                throw;
            }
        }

        /// <summary>
        /// Saves or updates user profile.
        /// Validates required fields and email format.
        /// Returns (true, empty string) on success or (false, errorMessage) on validation failure.
        /// </summary>
        public async Task<(bool success, string errorMessage)> SaveUserProfileAsync(int userId, ProfileDto dto)
        {
            try
            {
                // Validate data
                var validationError = ValidateProfileData(dto);
                if (!string.IsNullOrEmpty(validationError))
                {
                    _logger.LogWarning($"[ProfileService] Validation error for UserId {userId}: {validationError}");
                    return (false, validationError);
                }

                // Get or create profile
                var profile = await _db.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile is null)
                {
                    profile = new UserProfile
                    {
                        UserId = userId,
                        FullName = dto.FullName ?? string.Empty,
                        Email = dto.Email ?? string.Empty,
                        Bio = dto.Bio,
                        Country = dto.Country,
                        FavoriteCryptoIds = dto.FavoriteCryptoIds,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.UserProfiles.Add(profile);
                    _logger.LogInformation($"[ProfileService] Creating new profile for UserId {userId}");
                }
                else
                {
                    profile.FullName = dto.FullName ?? string.Empty;
                    profile.Email = dto.Email ?? string.Empty;
                    profile.Bio = dto.Bio;
                    profile.Country = dto.Country;
                    profile.FavoriteCryptoIds = dto.FavoriteCryptoIds;
                    profile.UpdatedAt = DateTime.UtcNow;
                    _logger.LogInformation($"[ProfileService] Updating existing profile for UserId {userId}");
                }

                await _db.SaveChangesAsync();
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ProfileService] Error saving profile for UserId {userId}");
                return (false, $"Error al guardar el perfil: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates profile data.
        /// Returns error message if invalid, empty string if valid.
        /// </summary>
        private string ValidateProfileData(ProfileDto dto)
        {
            // FullName is required
            if (string.IsNullOrWhiteSpace(dto.FullName))
                return "El nombre es obligatorio.";

            if (dto.FullName.Length > 100)
                return "El nombre no puede exceder 100 caracteres.";

            // Email is required
            if (string.IsNullOrWhiteSpace(dto.Email))
                return "El email es obligatorio.";

            if (dto.Email.Length > 255)
                return "El email no puede exceder 255 caracteres.";

            // Validate email format using built-in EmailAddressAttribute
            var emailValidator = new EmailAddressAttribute();
            if (!emailValidator.IsValid(dto.Email))
                return "El formato del email no es válido.";

            // Bio optional but max 500
            if (!string.IsNullOrWhiteSpace(dto.Bio) && dto.Bio.Length > 500)
                return "La biografía no puede exceder 500 caracteres.";

            // Country optional but max 100
            if (!string.IsNullOrWhiteSpace(dto.Country) && dto.Country.Length > 100)
                return "El país no puede exceder 100 caracteres.";

            return string.Empty;
        }

        /// <summary>
        /// DTO for profile data transfer.
        /// </summary>
        public record ProfileDto(
            string? FullName,
            string? Email,
            string? Bio,
            string? Country,
            string? FavoriteCryptoIds
        );
    }
}
