using CryptoView.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CryptoView.Services
{
    /// <summary>
    /// Servicio central para gestionar las criptomonedas favoritas/seguidas por usuario.
    /// Fuente de verdad: UserProfiles.FavoriteCryptoIds (JSON array de enteros).
    /// CryptoCurrencies funciona como catálogo global — nunca se modifica desde aquí.
    /// </summary>
    public class FavoriteCryptoService
    {
        private readonly CryptoDbContext _db;
        private readonly ILogger<FavoriteCryptoService> _logger;

        public FavoriteCryptoService(CryptoDbContext db, ILogger<FavoriteCryptoService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene la lista de IDs de criptomonedas favoritas del usuario.
        /// Retorna lista vacía si no hay perfil, si FavoriteCryptoIds es null/vacío, o si el JSON es inválido.
        /// </summary>
        public async Task<List<int>> GetFavoriteCryptoIdsAsync(int userId)
        {
            try
            {
                var profile = await _db.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile?.FavoriteCryptoIds is null)
                    return new List<int>();

                var ids = ParseFavoriteIds(profile.FavoriteCryptoIds);
                _logger.LogDebug("[FavoriteCrypto] UserId={UserId}, IDs parseados: {Count}", userId, ids.Count);
                return ids;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FavoriteCrypto] Error leyendo favoritos para UserId={UserId}", userId);
                return new List<int>();
            }
        }

        /// <summary>
        /// Obtiene las criptomonedas completas (con Notas incluidas) que el usuario sigue.
        /// Filtra contra CryptoCurrencies para devolver solo monedas existentes.
        /// </summary>
        public async Task<List<CryptoCurrency>> GetFavoriteCryptosAsync(int userId)
        {
            try
            {
                var favoriteIds = await GetFavoriteCryptoIdsAsync(userId);
                if (!favoriteIds.Any())
                    return new List<CryptoCurrency>();

                var cryptos = await _db.CryptoCurrencies
                    .Include(c => c.Notes)
                    .Where(c => favoriteIds.Contains(c.Id))
                    .OrderByDescending(c => c.MarketCap)
                    .ToListAsync();

                return cryptos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FavoriteCrypto] Error cargando cryptos favoritas para UserId={UserId}", userId);
                return new List<CryptoCurrency>();
            }
        }

        /// <summary>
        /// Agrega una criptomoneda a la lista de favoritas del usuario.
        /// Evita duplicados. Si el usuario no tiene perfil, crea uno básico.
        /// Retorna true si se agregó, false si ya existía.
        /// </summary>
        public async Task<bool> AddFavoriteCryptoAsync(int userId, int cryptoId)
        {
            try
            {
                var profile = await _db.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                var currentIds = profile?.FavoriteCryptoIds is not null
                    ? ParseFavoriteIds(profile.FavoriteCryptoIds)
                    : new List<int>();

                if (currentIds.Contains(cryptoId))
                {
                    _logger.LogDebug("[FavoriteCrypto] UserId={UserId} ya tiene cryptoId={CryptoId}", userId, cryptoId);
                    return false;
                }

                currentIds.Add(cryptoId);

                if (profile is null)
                {
                    profile = new UserProfile
                    {
                        UserId = userId,
                        FullName = string.Empty,
                        Email = string.Empty,
                        FavoriteCryptoIds = JsonSerializer.Serialize(currentIds),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.UserProfiles.Add(profile);
                    _logger.LogInformation("[FavoriteCrypto] Creado perfil nuevo para UserId={UserId}", userId);
                }
                else
                {
                    profile.FavoriteCryptoIds = JsonSerializer.Serialize(currentIds);
                    profile.UpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                _logger.LogInformation("[FavoriteCrypto] UserId={UserId} agregó cryptoId={CryptoId}", userId, cryptoId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FavoriteCrypto] Error agregando cryptoId={CryptoId} para UserId={UserId}", cryptoId, userId);
                return false;
            }
        }

        /// <summary>
        /// Quita una criptomoneda de la lista de favoritas del usuario.
        /// NO modifica CryptoCurrencies (catálogo global intacto).
        /// Retorna true si se quitó, false si no existía.
        /// </summary>
        public async Task<bool> RemoveFavoriteCryptoAsync(int userId, int cryptoId)
        {
            try
            {
                var profile = await _db.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile?.FavoriteCryptoIds is null)
                    return false;

                var currentIds = ParseFavoriteIds(profile.FavoriteCryptoIds);
                if (!currentIds.Contains(cryptoId))
                {
                    _logger.LogDebug("[FavoriteCrypto] UserId={UserId} no tiene cryptoId={CryptoId}", userId, cryptoId);
                    return false;
                }

                currentIds.Remove(cryptoId);
                profile.FavoriteCryptoIds = currentIds.Any()
                    ? JsonSerializer.Serialize(currentIds)
                    : null;
                profile.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                _logger.LogInformation("[FavoriteCrypto] UserId={UserId} quitó cryptoId={CryptoId}", userId, cryptoId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FavoriteCrypto] Error quitando cryptoId={CryptoId} para UserId={UserId}", cryptoId, userId);
                return false;
            }
        }

        /// <summary>
        /// Verifica si una criptomoneda es favorita del usuario.
        /// </summary>
        public async Task<bool> IsFavoriteCryptoAsync(int userId, int cryptoId)
        {
            var ids = await GetFavoriteCryptoIdsAsync(userId);
            return ids.Contains(cryptoId);
        }

        // ── Helpers privados ──────────────────────────────────────────────────

        /// <summary>
        /// Parsea un string JSON "[1,2,5]" de forma segura.
        /// Acepta null, vacío, JSON inválido, y IDs duplicados.
        /// </summary>
        private static List<int> ParseFavoriteIds(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<int>();

            try
            {
                var ids = JsonSerializer.Deserialize<List<int>>(json);
                if (ids is null)
                    return new List<int>();

                // Eliminar duplicados manteniendo orden
                return ids.Distinct().ToList();
            }
            catch
            {
                // JSON inválido → tratar como lista vacía
                return new List<int>();
            }
        }
    }
}
