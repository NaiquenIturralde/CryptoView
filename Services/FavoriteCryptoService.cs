using CryptoView.Data;
using Microsoft.EntityFrameworkCore;

namespace CryptoView.Services
{
    /// <summary>
    /// Servicio central para gestionar las criptomonedas favoritas/seguidas por usuario.
    ///
    /// FUENTE DE VERDAD (Fase 2): tabla relacional UserFavorites.
    /// La columna JSON UserProfiles.FavoriteCryptoIds queda únicamente como
    /// respaldo histórico — este servicio NO la lee ni la escribe.
    ///
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
        /// Obtiene la lista de IDs de criptomonedas favoritas del usuario
        /// directamente desde UserFavorites.
        /// </summary>
        public async Task<List<int>> GetFavoriteCryptoIdsAsync(int userId)
        {
            try
            {
                var ids = await _db.UserFavorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.CryptoCurrencyId)
                    .OrderBy(id => id)
                    .ToListAsync();

                _logger.LogDebug("[FavoriteCrypto] UserId={UserId}, favoritos en UserFavorites: {Count}", userId, ids.Count);
                return ids;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FavoriteCrypto] Error leyendo favoritos para UserId={UserId}", userId);
                return new List<int>();
            }
        }

        /// <summary>
        /// Obtiene las criptomonedas completas (con Notas incluidas) que el usuario sigue,
        /// haciendo join desde UserFavorites hacia el catálogo CryptoCurrencies.
        /// </summary>
        public async Task<List<CryptoCurrency>> GetFavoriteCryptosAsync(int userId)
        {
            try
            {
                var cryptos = await _db.UserFavorites
                    .Where(f => f.UserId == userId)
                    .Include(f => f.CryptoCurrency)
                        .ThenInclude(c => c.Notes)
                    .OrderByDescending(f => f.CryptoCurrency.MarketCap)
                    .Select(f => f.CryptoCurrency)
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
        /// Agrega una criptomoneda a favoritos del usuario.
        /// No crea duplicados (PK compuesta + chequeo previo).
        /// Retorna true si se agregó, false si ya existía o la moneda no existe.
        /// </summary>
        public async Task<bool> AddFavoriteCryptoAsync(int userId, int cryptoId)
        {
            try
            {
                // No permite favoritos de monedas inexistentes en el catálogo
                var cryptoExists = await _db.CryptoCurrencies.AnyAsync(c => c.Id == cryptoId);
                if (!cryptoExists)
                {
                    _logger.LogWarning("[FavoriteCrypto] cryptoId={CryptoId} no existe en catálogo (UserId={UserId})", cryptoId, userId);
                    return false;
                }

                // No crear duplicados
                var alreadyExists = await _db.UserFavorites
                    .AnyAsync(f => f.UserId == userId && f.CryptoCurrencyId == cryptoId);
                if (alreadyExists)
                {
                    _logger.LogDebug("[FavoriteCrypto] UserId={UserId} ya tiene cryptoId={CryptoId}", userId, cryptoId);
                    return false;
                }

                _db.UserFavorites.Add(new UserFavorite
                {
                    UserId = userId,
                    CryptoCurrencyId = cryptoId,
                    AddedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
                _logger.LogInformation("[FavoriteCrypto] UserId={UserId} agregó cryptoId={CryptoId}", userId, cryptoId);
                return true;
            }
            catch (DbUpdateException ex)
            {
                // Carrera: otra petición insertó la misma PK compuesta primero
                _logger.LogWarning(ex, "[FavoriteCrypto] Duplicado concurrente UserId={UserId} cryptoId={CryptoId}", userId, cryptoId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FavoriteCrypto] Error agregando cryptoId={CryptoId} para UserId={UserId}", cryptoId, userId);
                return false;
            }
        }

        /// <summary>
        /// Quita una criptomoneda de favoritos del usuario.
        /// Elimina únicamente la fila (UserId, CryptoCurrencyId).
        /// NO modifica CryptoCurrencies (catálogo global intacto).
        /// Retorna true si se quitó, false si no existía.
        /// </summary>
        public async Task<bool> RemoveFavoriteCryptoAsync(int userId, int cryptoId)
        {
            try
            {
                var favorite = await _db.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.CryptoCurrencyId == cryptoId);

                if (favorite is null)
                {
                    _logger.LogDebug("[FavoriteCrypto] UserId={UserId} no tiene cryptoId={CryptoId}", userId, cryptoId);
                    return false;
                }

                _db.UserFavorites.Remove(favorite);
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
            return await _db.UserFavorites
                .AnyAsync(f => f.UserId == userId && f.CryptoCurrencyId == cryptoId);
        }

        /// <summary>
        /// Reemplaza el conjunto completo de favoritos del usuario por los IDs dados.
        /// Operación atómica en una sola transacción: borra los que sobran,
        /// inserta los que faltan. Usado por EditarPerfil (checkboxes).
        /// IDs inválidos (que no existen en el catálogo) se ignoran.
        /// </summary>
        public async Task<bool> SetFavoriteCryptosAsync(int userId, IEnumerable<int> cryptoIds)
        {
            try
            {
                var desired = cryptoIds.Distinct().ToList();

                // Solo IDs que realmente existen en el catálogo
                var validIds = await _db.CryptoCurrencies
                    .Where(c => desired.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync();

                var currentIds = await _db.UserFavorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.CryptoCurrencyId)
                    .ToListAsync();

                // Eliminar los que ya no están seleccionados
                var toRemove = currentIds.Except(validIds).ToList();
                if (toRemove.Count > 0)
                {
                    var rowsToRemove = await _db.UserFavorites
                        .Where(f => f.UserId == userId && toRemove.Contains(f.CryptoCurrencyId))
                        .ToListAsync();
                    _db.UserFavorites.RemoveRange(rowsToRemove);
                }

                // Insertar los nuevos
                var toAdd = validIds.Except(currentIds).ToList();
                foreach (var id in toAdd)
                {
                    _db.UserFavorites.Add(new UserFavorite
                    {
                        UserId = userId,
                        CryptoCurrencyId = id,
                        AddedAt = DateTime.UtcNow
                    });
                }

                if (toRemove.Count > 0 || toAdd.Count > 0)
                {
                    await _db.SaveChangesAsync();
                    _logger.LogInformation(
                        "[FavoriteCrypto] Sync UserId={UserId}: +{Added} / -{Removed}",
                        userId, toAdd.Count, toRemove.Count);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FavoriteCrypto] Error sincronizando favoritos UserId={UserId}", userId);
                return false;
            }
        }
    }
}
