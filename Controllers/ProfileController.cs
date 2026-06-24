using CryptoView.Services;
using Microsoft.AspNetCore.Mvc;

namespace CryptoView.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly ProfileService _profileService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(ProfileService profileService, ILogger<ProfileController> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/profile
        /// Retrieves the authenticated user's profile.
        /// Expects X-User-Id header containing the UserId.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ProfileResponseDto>> GetProfile()
        {
            try
            {
                // Get authenticated user ID from header
                var userIdHeader = HttpContext.Request.Headers["X-User-Id"].ToString();
                _logger.LogInformation($"[ProfileController] GetProfile called with header: {userIdHeader}");

                if (!int.TryParse(userIdHeader, out var userId) || userId <= 0)
                {
                    _logger.LogWarning("[ProfileController] Invalid or missing X-User-Id header");
                    return Unauthorized(new { error = "Sesión inválida." });
                }

                var profile = await _profileService.GetUserProfileAsync(userId);

                // If no profile exists, return empty DTO (user can create one)
                if (profile is null)
                {
                    _logger.LogInformation($"[ProfileController] No profile found for UserId {userId}, returning empty");
                    return Ok(new ProfileResponseDto(
                        FullName: string.Empty,
                        Email: string.Empty,
                        Bio: null,
                        Country: null,
                        FavoriteCryptoIds: null
                    ));
                }

                return Ok(new ProfileResponseDto(
                    FullName: profile.FullName,
                    Email: profile.Email,
                    Bio: profile.Bio,
                    Country: profile.Country,
                    FavoriteCryptoIds: profile.FavoriteCryptoIds
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProfileController] Error en GetProfile");
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

        /// <summary>
        /// PUT /api/profile
        /// Updates the authenticated user's profile.
        /// Expects X-User-Id header containing the UserId.
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<ProfileResponseDto>> UpdateProfile([FromBody] ProfileUpdateDto request)
        {
            try
            {
                // Get authenticated user ID from header
                var userIdHeader = HttpContext.Request.Headers["X-User-Id"].ToString();
                _logger.LogInformation($"[ProfileController] UpdateProfile called for header: {userIdHeader}");

                if (!int.TryParse(userIdHeader, out var userId) || userId <= 0)
                {
                    _logger.LogWarning("[ProfileController] Invalid or missing X-User-Id header on PUT");
                    return Unauthorized(new { error = "Sesión inválida." });
                }

                _logger.LogInformation($"[ProfileController] Updating profile for UserId {userId}");

                // Validate and save
                var dto = new ProfileService.ProfileDto(
                    request.FullName,
                    request.Email,
                    request.Bio,
                    request.Country,
                    request.FavoriteCryptoIds
                );

                var (success, error) = await _profileService.SaveUserProfileAsync(userId, dto);

                if (!success)
                {
                    _logger.LogWarning($"[ProfileController] Validation error: {error}");
                    return BadRequest(new { error });
                }

                // Return updated profile
                var updated = await _profileService.GetUserProfileAsync(userId);
                return Ok(new ProfileResponseDto(
                    FullName: updated!.FullName,
                    Email: updated.Email,
                    Bio: updated.Bio,
                    Country: updated.Country,
                    FavoriteCryptoIds: updated.FavoriteCryptoIds
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProfileController] Error en UpdateProfile");
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }
    }

    /// <summary>
    /// DTO for PUT /api/profile request.
    /// </summary>
    public class ProfileUpdateDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Bio { get; set; }
        public string? Country { get; set; }
        public string? FavoriteCryptoIds { get; set; }
    }

    /// <summary>
    /// DTO for profile response.
    /// </summary>
    public record ProfileResponseDto(
        string? FullName,
        string? Email,
        string? Bio,
        string? Country,
        string? FavoriteCryptoIds
    );
}
