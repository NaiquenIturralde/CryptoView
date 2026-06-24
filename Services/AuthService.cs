using CryptoView.Data;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace CryptoView.Services
{
    /// <summary>
    /// Handles all authentication and user-management operations.
    ///
    /// Security guarantees:
    ///   - Passwords are NEVER stored in plain text (BCrypt, work factor 12).
    ///   - Public registration can only create Role = "Usuario".
    ///   - The admin account is managed exclusively via startup seed / config reset.
    ///   - There can only ever be ONE admin in the system.
    /// </summary>
    public class AuthService
    {
        private readonly CryptoDbContext _db;

        public AuthService(CryptoDbContext db) => _db = db;

        // ── Login ────────────────────────────────────────────────────────────

        /// <summary>
        /// Validates credentials. Returns the user on success, null on failure.
        /// Uses constant-time BCrypt verification to prevent timing attacks.
        /// </summary>
        public async Task<AppUser?> ValidateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            var user = await _db.AppUsers
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user is null) return null;

            return BC.Verify(password, user.PasswordHash) ? user : null;
        }

        // ── Public Register ──────────────────────────────────────────────────

        /// <summary>
        /// Registers a new user with role "Usuario".
        /// Returns (true, "") on success or (false, errorMessage) on failure.
        ///
        /// Role is hardcoded to "Usuario" — there is no parameter for it.
        /// </summary>
        public async Task<(bool ok, string error)> RegisterAsync(
            string username, string password, string? email)
        {
            username = username.Trim();

            if (username.Equals("admin", StringComparison.OrdinalIgnoreCase))
                return (false, "Ese nombre de usuario no está disponible.");

            if (await _db.AppUsers.AnyAsync(u => u.Username == username))
                return (false, "Ese nombre de usuario ya existe.");

            var user = new AppUser
            {
                Username = username,
                PasswordHash = BC.HashPassword(password, workFactor: 12),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                Role = "Usuario",   // ← ALWAYS "Usuario" — never elevated from here
                CreatedAt = DateTime.UtcNow
            };

            _db.AppUsers.Add(user);
            await _db.SaveChangesAsync();
            return (true, string.Empty);
        }

        // ── Admin Seed / Reset ───────────────────────────────────────────────

        /// <summary>
        /// Called once at startup (Program.cs).
        ///
        /// - If no admin exists → creates one with the supplied credentials.
        /// - If admin exists AND forceReset = true → updates only the password hash.
        ///   This is the recovery path: set AdminSeed:ForceReset = true in
        ///   appsettings.Development.json, restart, then set it back to false.
        /// - If admin exists AND forceReset = false → does nothing (normal startup).
        ///
        /// NEVER creates a second admin, regardless of forceReset value.
        /// </summary>
        public async Task EnsureAdminAsync(string username, string password, bool forceReset)
        {
            var admin = await _db.AppUsers
                .FirstOrDefaultAsync(u => u.Role == "Admin");

            if (admin is null)
            {
                _db.AppUsers.Add(new AppUser
                {
                    Username = username,
                    PasswordHash = BC.HashPassword(password, workFactor: 12),
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
            else if (forceReset)
            {
                // Reset the existing admin — never duplicate
                admin.PasswordHash = BC.HashPassword(password, workFactor: 12);
                await _db.SaveChangesAsync();
            }
        }
    }
}
