using CryptoView.Data;
using Microsoft.EntityFrameworkCore;

namespace CryptoView.Services
{
    /// <summary>
    /// Centralized authentication state management service.
    /// 
    /// Handles:
    /// - Real session validation against the database
    /// - Secure logout with complete cleanup
    /// - Prevents auto-login without valid session
    /// </summary>
    public class AuthStateService
    {
        private readonly CryptoDbContext _db;
        private AppUser? _cachedUser;

        public AuthStateService(CryptoDbContext db) => _db = db;

        // ── Initialize auth state at app startup ────────────────────────────

        /// <summary>
        /// Validates the current session against the database.
        /// Called once at app startup.
        /// 
        /// Returns the user if session is valid, null otherwise.
        /// This is the ONLY way to auto-restore a session after a page reload.
        /// </summary>
        public async Task<AppUser?> InitAuthStateAsync(int? userId, string? username)
        {
            _cachedUser = null;

            // Must have both userId and username to attempt validation
            if (!userId.HasValue || string.IsNullOrWhiteSpace(username))
                return null;

            try
            {
                // Query the database for the user
                var user = await _db.AppUsers
                    .FirstOrDefaultAsync(u => u.Id == userId.Value && u.Username == username);

                if (user is not null)
                {
                    _cachedUser = user;
                    return user;
                }
            }
            catch { /* DB error → invalid session */ }

            return null;
        }

        // ── Get current authenticated user ────────────────────────────────

        /// <summary>
        /// Returns the cached authenticated user, or null if not authenticated.
        /// Does NOT perform a database query (uses the cached value from InitAuthState).
        /// </summary>
        public AppUser? GetCurrentUser() => _cachedUser;

        // ── Check if user is authenticated ────────────────────────────────

        public bool IsAuthenticated() => _cachedUser is not null;

        // ── Get current user's ID ────────────────────────────────────────

        public int? GetCurrentUserId() => _cachedUser?.Id;

        // ── Get current username ─────────────────────────────────────────

        public string? GetCurrentUsername() => _cachedUser?.Username;

        // ── Set the current user after a successful login ────────────────

        /// <summary>
        /// Marks the given user as authenticated in-memory.
        /// Called directly after ValidateAsync returns a non-null user.
        /// No localStorage access — auth state lives only in this Scoped instance.
        /// </summary>
        public void Login(AppUser user)
        {
            Console.WriteLine($"[AuthStateService] Login en memoria: {user.Username} (Id={user.Id})");
            _cachedUser = user;
        }

        // ── Clear the session (logout) ───────────────────────────────

        /// <summary>
        /// Clears the cached user and any session state.
        /// Must be paired with JS cleanup of localStorage/sessionStorage.
        /// </summary>
        public void Logout()
        {
            Console.WriteLine("[AuthStateService] Sesión cerrada - _cachedUser = null");
            _cachedUser = null;
        }
    }
}
