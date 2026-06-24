using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CryptoView.Data;
using CryptoView.Services;

namespace CryptoView.Controllers
{
    /// <summary>
    /// Controlador API RESTful para gestionar criptomonedas.
    /// Implementa operaciones CRUD completas: GET, POST, PUT, DELETE
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CryptoController : ControllerBase
    {
        private readonly CryptoDbContext _context;
        private readonly CryptoApiService _apiService;
        private readonly ILogger<CryptoController> _logger;

        /// <summary>
        /// Constructor del controlador
        /// </summary>
        public CryptoController(
            CryptoDbContext context,
            CryptoApiService apiService,
            ILogger<CryptoController> logger)
        {
            _context = context;
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/Crypto
        /// Obtiene todas las criptomonedas activas con sus notas
        /// </summary>
        /// <returns>Lista de criptomonedas</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CryptoCurrency>>> GetCryptoCurrencies()
        {
            _logger.LogInformation("GET: Obteniendo todas las criptomonedas");

            var cryptos = await _context.CryptoCurrencies
                .Include(c => c.Notes)
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(cryptos);
        }

        /// <summary>
        /// GET: api/Crypto/{id}
        /// Obtiene una criptomoneda específica por su ID
        /// </summary>
        /// <param name="id">ID de la criptomoneda</param>
        /// <returns>Criptomoneda solicitada</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CryptoCurrency>> GetCryptoCurrency(int id)
        {
            _logger.LogInformation("GET: Obteniendo criptomoneda con ID {Id}", id);

            var crypto = await _context.CryptoCurrencies
                .Include(c => c.Notes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (crypto == null)
            {
                _logger.LogWarning("Criptomoneda con ID {Id} no encontrada", id);
                return NotFound(new { message = $"Criptomoneda con ID {id} no encontrada" });
            }

            return Ok(crypto);
        }

        /// <summary>
        /// POST: api/Crypto
        /// Crea una nueva criptomoneda en favoritos
        /// </summary>
        /// <param name="crypto">Datos de la criptomoneda a crear</param>
        /// <returns>Criptomoneda creada</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<CryptoCurrency>> CreateCryptoCurrency(CryptoCurrency crypto)
        {
            _logger.LogInformation("POST: Creando nueva criptomoneda {Name}", crypto.Name);

            var coinIdLower = crypto.CoinId.ToLower().Trim();

            // Si existe activa, es duplicado real
            var activeExists = await _context.CryptoCurrencies
                .AnyAsync(c => c.CoinId.ToLower() == coinIdLower && c.IsActive);

            if (activeExists)
            {
                _logger.LogWarning("Ya existe una criptomoneda activa con CoinId {CoinId}", coinIdLower);
                return Conflict(new { message = $"Ya existe una criptomoneda con ID '{coinIdLower}'" });
            }

            // Si existe inactiva (soft-deleted), reactivarla
            var inactiveRecord = await _context.CryptoCurrencies
                .FirstOrDefaultAsync(c => c.CoinId.ToLower() == coinIdLower && !c.IsActive);

            if (inactiveRecord != null)
            {
                _logger.LogInformation("Reactivando criptomoneda con CoinId {CoinId} (Id={Id})", coinIdLower, inactiveRecord.Id);
                var priceData = await _apiService.GetCoinPriceAsync(coinIdLower);
                if (priceData != null)
                {
                    inactiveRecord.CurrentPrice = priceData.Price;
                    inactiveRecord.MarketCap = priceData.MarketCap;
                    inactiveRecord.Volume24h = priceData.Volume24h;
                    inactiveRecord.Change24h = priceData.Change24h;
                    inactiveRecord.LastUpdated = priceData.LastUpdated;
                }
                inactiveRecord.IsActive = true;
                inactiveRecord.DateAdded = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetCryptoCurrency), new { id = inactiveRecord.Id }, inactiveRecord);
            }

            // Obtener precio actual desde la API
            var newPriceData = await _apiService.GetCoinPriceAsync(coinIdLower);
            if (newPriceData != null)
            {
                crypto.CurrentPrice = newPriceData.Price;
                crypto.MarketCap = newPriceData.MarketCap;
                crypto.Volume24h = newPriceData.Volume24h;
                crypto.Change24h = newPriceData.Change24h;
                crypto.LastUpdated = newPriceData.LastUpdated;
            }

            crypto.DateAdded = DateTime.UtcNow;
            crypto.IsActive = true;

            _context.CryptoCurrencies.Add(crypto);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Criptomoneda {Name} creada con ID {Id}", crypto.Name, crypto.Id);

            return CreatedAtAction(nameof(GetCryptoCurrency), new { id = crypto.Id }, crypto);
        }

        /// <summary>
        /// PUT: api/Crypto/{id}
        /// Actualiza una criptomoneda existente
        /// </summary>
        /// <param name="id">ID de la criptomoneda</param>
        /// <param name="crypto">Nuevos datos de la criptomoneda</param>
        /// <returns>Sin contenido si es exitoso</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCryptoCurrency(int id, CryptoCurrency crypto)
        {
            _logger.LogInformation("PUT: Actualizando criptomoneda con ID {Id}", id);

            if (id != crypto.Id)
            {
                _logger.LogWarning("ID de ruta {RouteId} no coincide con ID de body {BodyId}", id, crypto.Id);
                return BadRequest(new { message = "El ID no coincide" });
            }

            var existingCrypto = await _context.CryptoCurrencies.FindAsync(id);
            if (existingCrypto == null)
            {
                _logger.LogWarning("Criptomoneda con ID {Id} no encontrada", id);
                return NotFound(new { message = $"Criptomoneda con ID {id} no encontrada" });
            }

            // Actualizar campos permitidos
            existingCrypto.Name = crypto.Name;
            existingCrypto.Symbol = crypto.Symbol;
            existingCrypto.PriceThreshold = crypto.PriceThreshold;
            existingCrypto.IsActive = crypto.IsActive;
            existingCrypto.LastUpdated = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Criptomoneda {Name} actualizada exitosamente", crypto.Name);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error de concurrencia al actualizar criptomoneda {Id}", id);
                return StatusCode(500, new { message = "Error al actualizar la criptomoneda" });
            }

            return NoContent();
        }

        /// <summary>
        /// DELETE: api/Crypto/{id}
        /// Elimina una criptomoneda (soft delete)
        /// </summary>
        /// <param name="id">ID de la criptomoneda</param>
        /// <returns>Sin contenido si es exitoso</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCryptoCurrency(int id)
        {
            _logger.LogInformation("DELETE: Eliminando criptomoneda con ID {Id}", id);

            var crypto = await _context.CryptoCurrencies.FindAsync(id);
            if (crypto == null)
            {
                _logger.LogWarning("Criptomoneda con ID {Id} no encontrada", id);
                return NotFound(new { message = $"Criptomoneda con ID {id} no encontrada" });
            }

            // Soft delete: marcar como inactiva en lugar de eliminar físicamente
            crypto.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Criptomoneda {Name} marcada como inactiva", crypto.Name);

            return NoContent();
        }

        /// <summary>
        /// GET: api/Crypto/{id}/refresh
        /// Actualiza el precio de una criptomoneda desde la API externa
        /// </summary>
        /// <param name="id">ID de la criptomoneda</param>
        /// <returns>Criptomoneda con precio actualizado</returns>
        [HttpGet("{id}/refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<CryptoCurrency>> RefreshCryptoCurrency(int id)
        {
            _logger.LogInformation("GET: Actualizando precio de criptomoneda con ID {Id}", id);

            var crypto = await _context.CryptoCurrencies.FindAsync(id);
            if (crypto == null)
            {
                _logger.LogWarning("Criptomoneda con ID {Id} no encontrada", id);
                return NotFound(new { message = $"Criptomoneda con ID {id} no encontrada" });
            }

            // Obtener precio actualizado desde la API
            var priceData = await _apiService.GetCoinPriceAsync(crypto.CoinId.ToLower());
            if (priceData == null)
            {
                _logger.LogError("No se pudo obtener precio actualizado para {CoinId}", crypto.CoinId);
                return StatusCode(502, new { message = "No se pudo obtener el precio actualizado" });
            }

            // Actualizar los datos
            crypto.CurrentPrice = priceData.Price;
            crypto.MarketCap = priceData.MarketCap;
            crypto.Volume24h = priceData.Volume24h;
            crypto.Change24h = priceData.Change24h;
            crypto.LastUpdated = priceData.LastUpdated;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Precio actualizado para {Name}: ${Price}", crypto.Name, crypto.CurrentPrice);

            return Ok(crypto);
        }

        /// <summary>
        /// GET: api/Crypto/refresh-all
        /// Actualiza los precios de todas las criptomonedas activas
        /// </summary>
        /// <returns>Lista de criptomonedas actualizadas</returns>
        [HttpGet("refresh-all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CryptoCurrency>>> RefreshAllCryptoCurrencies()
        {
            _logger.LogInformation("GET: Actualizando todas las criptomonedas");

            var cryptos = await _context.CryptoCurrencies
                .Where(c => c.IsActive)
                .ToListAsync();

            if (cryptos.Count == 0)
            {
                return Ok(cryptos);
            }

            // Obtener todos los IDs de monedas
            var coinIds = cryptos.Select(c => c.CoinId.ToLower()).ToList();

            // Obtener precios en una sola llamada
            var pricesData = await _apiService.GetMultipleCoinPricesAsync(coinIds);

            // Actualizar cada criptomoneda con sus datos
            foreach (var crypto in cryptos)
            {
                var priceData = pricesData.FirstOrDefault(p =>
                    p.CoinId.Equals(crypto.CoinId, StringComparison.OrdinalIgnoreCase));

                if (priceData != null)
                {
                    crypto.CurrentPrice = priceData.Price;
                    crypto.MarketCap = priceData.MarketCap;
                    crypto.Volume24h = priceData.Volume24h;
                    crypto.Change24h = priceData.Change24h;
                    crypto.LastUpdated = priceData.LastUpdated;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Actualizadas {Count} criptomonedas", cryptos.Count);

            return Ok(cryptos);
        }

        /// <summary>
        /// GET: api/Crypto/search/{query}
        /// Busca criptomonedas en la API de CoinGecko
        /// </summary>
        /// <param name="query">Término de búsqueda</param>
        /// <returns>Lista de resultados de búsqueda</returns>
        [HttpGet("search/{query}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> SearchCryptoCurrencies(string query)
        {
            _logger.LogInformation("GET: Buscando criptomonedas con query '{Query}'", query);

            var results = await _apiService.SearchCoinsAsync(query);

            return Ok(results);
        }

        /// <summary>
        /// GET: api/Crypto/history?symbol={symbol}&amp;from={unixFrom}&amp;to={unixTo}
        /// Proxy backend hacia CoinGecko para obtener precios históricos.
        /// Protege la API key al centralizar la llamada en el servidor.
        /// </summary>
        /// <param name="symbol">Símbolo de la moneda (ej: BTC, TRX, ETH)</param>
        /// <param name="from">Timestamp UNIX en segundos del inicio del rango</param>
        /// <param name="to">Timestamp UNIX en segundos del fin del rango</param>
        [HttpGet("history")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GetHistoricalData(
            [FromQuery] string symbol,
            [FromQuery] long from,
            [FromQuery] long to)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest(new { error = "El parámetro 'symbol' es requerido." });

            if (from <= 0 || to <= 0 || from >= to)
                return BadRequest(new
                {
                    error = "Los parámetros 'from' y 'to' deben ser timestamps UNIX válidos con from < to."
                });

            var fromDate = DateTimeOffset.FromUnixTimeSeconds(from).UtcDateTime;
            var toDate = DateTimeOffset.FromUnixTimeSeconds(to).UtcDateTime;

            _logger.LogInformation(
                "GET /api/crypto/history → symbol={Symbol}, from={FromDate:yyyy-MM-dd}, to={ToDate:yyyy-MM-dd}",
                symbol, fromDate, toDate);

            var data = await _apiService.GetCoinGeckoHistoricalDataAsync(symbol, fromDate, toDate);

            if (data.ErrorMessage != null)
            {
                return Ok(new
                {
                    symbol = symbol.ToUpper(),
                    error = data.ErrorMessage,
                    count = 0,
                    prices = Array.Empty<object>(),
                    volumes = Array.Empty<object>()
                });
            }

            return Ok(new
            {
                symbol = symbol.ToUpper(),
                count = data.Prices.Count,
                prices = data.Prices.Select(p => new { date = p.Date, price = p.Price }),
                volumes = data.Volumes.Select(v => new { date = v.Date, volume = v.Volume })
            });
        }
    }
}
