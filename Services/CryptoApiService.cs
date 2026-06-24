using Newtonsoft.Json.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace CryptoView.Services
{
    /// <summary>
    /// Respuesta de la API de CoinGecko para precios de criptomonedas
    /// </summary>
    public class CoinPriceResponse
    {
        public string CoinId { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal MarketCap { get; set; }
        public decimal Volume24h { get; set; }
        public decimal Change24h { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Datos históricos de precio para gráficos
    /// </summary>
    public class HistoricalPriceData
    {
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
    }

    /// <summary>
    /// Respuesta de datos históricos reales desde CoinGecko:
    /// contiene precios y volúmenes por fecha, más un mensaje de error si ocurrió.
    /// </summary>
    public class HistoricalMarketData
    {
        public List<HistoricalPriceData> Prices { get; set; } = new();
        public List<(DateTime Date, decimal Volume)> Volumes { get; set; } = new();
        /// <summary>Mensaje de error de la API. Null si la consulta fue exitosa.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Servicio para interactuar con la API de Coinlore (100% GRATIS, sin API key).
    /// Proporciona métodos para obtener precios en tiempo real y datos históricos.
    /// </summary>
    public class CryptoApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CryptoApiService> _logger;
        private readonly IConfiguration _configuration;
        private const string BaseUrl = "https://api.coinlore.net";

        // Mapeo de nombres comunes a IDs de Coinlore
        private readonly Dictionary<string, string> _coinMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "bitcoin", "90" },
            { "btc", "90" },
            { "ethereum", "80" },
            { "eth", "80" },
            { "tether", "518" },
            { "usdt", "518" },
            { "binancecoin", "2710" },
            { "bnb", "2710" },
            { "cardano", "257" },
            { "ada", "257" },
            { "ripple", "58" },
            { "xrp", "58" },
            { "solana", "48543" },
            { "sol", "48543" },
            { "polkadot", "33536" },
            { "dot", "33536" },
            { "dogecoin", "2" },
            { "doge", "2" },
            { "avalanche", "44830" },
            { "avax", "44830" },
            { "litecoin", "1" },
            { "ltc", "1" },
            { "chainlink", "44791" },
            { "link", "44791" },
            { "stellar", "32726" },
            { "xlm", "32726" }
        };

        // Mapeo de símbolo → CoinGecko coin ID (para datos históricos reales)
        private static readonly Dictionary<string, string> _coinGeckoIdMap =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "BTC",  "bitcoin" },
            { "ETH",  "ethereum" },
            { "BNB",  "binancecoin" },
            { "SOL",  "solana" },
            { "ADA",  "cardano" },
            { "XRP",  "ripple" },
            { "DOGE", "dogecoin" },
            { "DOT",  "polkadot" },
            { "LTC",  "litecoin" },
            { "LINK", "chainlink" },
            { "XLM",  "stellar" },
            { "AVAX", "avalanche-2" },
            { "USDT", "tether" },
            { "TRX",  "tron" },
            { "XAUT", "tether-gold" },
        };

        /// <summary>
        /// Constructor del servicio
        /// </summary>
        public CryptoApiService(HttpClient httpClient, ILogger<CryptoApiService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;

            // Configurar el HttpClient para Coinlore API
            _httpClient.BaseAddress = new Uri(BaseUrl);
            if (!_httpClient.DefaultRequestHeaders.Contains("Accept"))
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Obtiene el precio actual de una criptomoneda específica usando Coinlore API
        /// </summary>
        /// <param name="coinId">ID o nombre de la moneda (ej: "bitcoin", "ethereum", "90")</param>
        /// <param name="vsCurrency">Moneda de referencia (siempre USD en Coinlore)</param>
        /// <returns>Información de precio actualizada</returns>
        public async Task<CoinPriceResponse?> GetCoinPriceAsync(string coinId, string vsCurrency = "usd")
        {
            try
            {
                _logger.LogInformation("Obteniendo precio para {CoinId} desde Coinlore API", coinId);

                // Convertir nombre a ID de Coinlore si es necesario
                string coinloreId = _coinMap.ContainsKey(coinId) ? _coinMap[coinId] : coinId;

                // Construir URL: https://api.coinlore.net/api/ticker/?id=90
                var url = $"/api/ticker/?id={coinloreId}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error en la API de Coinlore: {StatusCode}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var dataArray = JArray.Parse(json);

                // Coinlore devuelve un array con un solo objeto
                if (dataArray.Count == 0)
                {
                    _logger.LogWarning("Moneda {CoinId} no encontrada en Coinlore", coinId);
                    return null;
                }

                var coinData = dataArray[0] as JObject;
                if (coinData == null) return null;

                // Parsear la respuesta de Coinlore
                var priceResponse = new CoinPriceResponse
                {
                    CoinId = coinData["nameid"]?.Value<string>() ?? coinId.ToLower(),
                    Price = decimal.Parse(coinData["price_usd"]?.Value<string>() ?? "0"),
                    MarketCap = decimal.Parse(coinData["market_cap_usd"]?.Value<string>() ?? "0"),
                    Volume24h = coinData["volume24"]?.Value<decimal>() ?? 0,
                    Change24h = decimal.Parse(coinData["percent_change_24h"]?.Value<string>() ?? "0"),
                    LastUpdated = DateTime.UtcNow
                };

                _logger.LogInformation("Precio obtenido exitosamente para {CoinId}: ${Price} USD",
                    coinId, priceResponse.Price);

                return priceResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de red al obtener precio de {CoinId}", coinId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener precio de {CoinId}", coinId);
                return null;
            }
        }

        /// <summary>
        /// Obtiene los precios de múltiples criptomonedas usando Coinlore API
        /// </summary>
        /// <param name="coinIds">Lista de IDs o nombres de monedas</param>
        /// <param name="vsCurrency">Moneda de referencia (siempre USD en Coinlore)</param>
        /// <returns>Lista de respuestas de precio</returns>
        public async Task<List<CoinPriceResponse>> GetMultipleCoinPricesAsync(
            IEnumerable<string> coinIds,
            string vsCurrency = "usd")
        {
            var results = new List<CoinPriceResponse>();

            try
            {
                _logger.LogInformation("Obteniendo precios para múltiples monedas desde Coinlore");

                // Coinlore permite múltiples IDs separados por coma en el mismo endpoint
                var coinloreIds = coinIds
                    .Select(id => _coinMap.ContainsKey(id) ? _coinMap[id] : id)
                    .ToList();

                var idsParam = string.Join(",", coinloreIds);
                var url = $"/api/ticker/?id={idsParam}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error en la API de Coinlore: {StatusCode}", response.StatusCode);
                    return results;
                }

                var json = await response.Content.ReadAsStringAsync();
                var dataArray = JArray.Parse(json);

                // Procesar cada moneda en el array
                foreach (var item in dataArray)
                {
                    var coinData = item as JObject;
                    if (coinData != null)
                    {
                        results.Add(new CoinPriceResponse
                        {
                            CoinId = coinData["nameid"]?.Value<string>() ?? "",
                            Price = decimal.Parse(coinData["price_usd"]?.Value<string>() ?? "0"),
                            MarketCap = decimal.Parse(coinData["market_cap_usd"]?.Value<string>() ?? "0"),
                            Volume24h = coinData["volume24"]?.Value<decimal>() ?? 0,
                            Change24h = decimal.Parse(coinData["percent_change_24h"]?.Value<string>() ?? "0"),
                            LastUpdated = DateTime.UtcNow
                        });
                    }
                }

                _logger.LogInformation("Obtenidos {Count} precios exitosamente desde Coinlore", results.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener múltiples precios desde Coinlore");
            }

            return results;
        }

        /// <summary>
        /// Obtiene datos históricos de precio de Bitcoin (simulados con crecimiento exponencial)
        /// Para uso educativo y demostración de gráficos
        /// </summary>
        /// <param name="days">Número de días hacia atrás</param>
        /// <returns>Lista de datos históricos</returns>
        public Task<List<HistoricalPriceData>> GetBitcoinHistoricalDataAsync(int days = 365)
        {
            var historicalData = new List<HistoricalPriceData>();

            try
            {
                // En un escenario real, usaríamos el endpoint /coins/{id}/market_chart
                // Para este ejemplo educativo, generamos datos con crecimiento exponencial

                _logger.LogInformation("Generando datos históricos simulados para Bitcoin");

                var currentDate = DateTime.UtcNow.Date;
                var startingPrice = 10000m; // Precio inicial simulado
                var growthRate = 0.003m; // Tasa de crecimiento diario (~3%)

                for (int i = days; i >= 0; i--)
                {
                    var date = currentDate.AddDays(-i);
                    // Fórmula exponencial: P(t) = P0 * e^(rt)
                    var price = startingPrice * (decimal)Math.Pow((double)(1 + growthRate), days - i);

                    // Agregar variación aleatoria para simular volatilidad
                    var random = new Random(date.GetHashCode());
                    var volatility = (decimal)(random.NextDouble() * 0.1 - 0.05); // ±5%
                    price *= (1 + volatility);

                    historicalData.Add(new HistoricalPriceData
                    {
                        Date = date,
                        Price = Math.Round(price, 2)
                    });
                }

                // Opcional: Obtener datos reales de CoinGecko (comentado para evitar límites de API)
                /*
                var url = $"/coins/bitcoin/market_chart?vs_currency=usd&days={days}&interval=daily";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JObject.Parse(json);
                    var prices = data["prices"] as JArray;
                    
                    if (prices != null)
                    {
                        historicalData.Clear();
                        foreach (var pricePoint in prices)
                        {
                            var timestamp = pricePoint[0].Value<long>();
                            var price = pricePoint[1].Value<decimal>();
                            
                            historicalData.Add(new HistoricalPriceData
                            {
                                Date = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime,
                                Price = Math.Round(price, 2)
                            });
                        }
                    }
                }
                */

                _logger.LogInformation("Datos históricos generados: {Count} puntos", historicalData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos históricos");
            }

            return Task.FromResult(historicalData);
        }

        /// <summary>
        /// Busca criptomonedas por nombre o símbolo usando Coinlore API
        /// </summary>
        /// <param name="query">Término de búsqueda</param>
        /// <returns>Lista de monedas encontradas</returns>
        public async Task<List<(string Id, string Name, string Symbol)>> SearchCoinsAsync(string query)
        {
            var results = new List<(string Id, string Name, string Symbol)>();

            try
            {
                _logger.LogInformation("Buscando monedas en Coinlore: {Query}", query);

                // Coinlore no tiene endpoint de búsqueda, usamos /tickers/ y filtramos localmente
                // Obtenemos las primeras 100 monedas más populares
                var url = "/api/tickers/";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error en búsqueda de Coinlore: {StatusCode}", response.StatusCode);
                    return results;
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(json);
                var dataArray = data["data"] as JArray;

                if (dataArray != null)
                {
                    var queryLower = query.ToLower();

                    foreach (var item in dataArray)
                    {
                        var coin = item as JObject;
                        if (coin != null)
                        {
                            var name = coin["name"]?.Value<string>() ?? "";
                            var symbol = coin["symbol"]?.Value<string>() ?? "";
                            var nameid = coin["nameid"]?.Value<string>() ?? "";
                            var id = coin["id"]?.Value<string>() ?? "";

                            // Filtrar por coincidencia en nombre o símbolo
                            if (name.ToLower().Contains(queryLower) ||
                                symbol.ToLower().Contains(queryLower) ||
                                nameid.ToLower().Contains(queryLower))
                            {
                                results.Add((id, name, symbol));

                                if (results.Count >= 10) break; // Limitar a 10 resultados
                            }
                        }
                    }
                }

                _logger.LogInformation("Encontradas {Count} monedas en Coinlore", results.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en búsqueda de monedas en Coinlore");
            }

            return results;
        }

        /// <summary>
        /// Obtiene datos históricos simulados diferenciados por moneda.
        /// Cada activo tiene su propio perfil de precio base, crecimiento y volatilidad.
        /// </summary>
        /// <param name="symbol">Símbolo de la moneda (ej: BTC, ETH, BNB)</param>
        /// <param name="days">Número de días hacia atrás</param>
        public Task<List<HistoricalPriceData>> GetHistoricalDataAsync(string symbol, int days = 30)
        {
            // Per-coin profiles: (base price, daily growth rate, volatility)
            var profiles = new Dictionary<string, (decimal Base, decimal Growth, decimal Volatility)>(StringComparer.OrdinalIgnoreCase)
            {
                { "BTC",  (65000m, 0.0015m, 0.035m) },
                { "ETH",  (3200m,  0.0012m, 0.042m) },
                { "BNB",  (420m,   0.0008m, 0.038m) },
                { "AVAX", (36m,    0.0020m, 0.060m) },
                { "DOGE", (0.15m,  0.0030m, 0.080m) },
                { "SOL",  (170m,   0.0020m, 0.055m) },
                { "ADA",  (0.55m,  0.0010m, 0.050m) },
                { "XAUT", (2100m,  0.0003m, 0.015m) },
                { "LTC",  (85m,    0.0010m, 0.040m) },
                { "XRP",  (0.52m,  0.0010m, 0.050m) },
                { "DOT",  (8m,     0.0012m, 0.055m) },
                { "LINK", (14m,    0.0015m, 0.060m) },
                { "XLM",  (0.12m,  0.0008m, 0.045m) },
            };

            var (basePrice, growthRate, volatility) = profiles.ContainsKey(symbol)
                ? profiles[symbol]
                : (100m, 0.001m, 0.05m);

            int coinSeed = symbol.ToUpperInvariant().GetHashCode();
            var historicalData = new List<HistoricalPriceData>();
            var currentDate = DateTime.UtcNow.Date;

            for (int i = days; i >= 0; i--)
            {
                var date = currentDate.AddDays(-i);
                var price = basePrice * (decimal)Math.Pow((double)(1 + growthRate), days - i);
                var rng = new Random(coinSeed ^ date.GetHashCode());
                var noise = (decimal)(rng.NextDouble() * 2 - 1) * volatility;
                price *= (1 + noise);
                int decimalPlaces = price < 1m ? 6 : price < 100m ? 4 : 2;

                historicalData.Add(new HistoricalPriceData
                {
                    Date = date,
                    Price = Math.Round(price, decimalPlaces)
                });
            }

            _logger.LogInformation("Datos históricos generados para {Symbol}: {Count} puntos", symbol, historicalData.Count);
            return Task.FromResult(historicalData);
        }

        /// <summary>
        /// Calcula estadísticas básicas de una lista de precios
        /// Para cumplir con los requisitos de Probabilidad y Estadística
        /// </summary>
        /// <param name="prices">Lista de precios</param>
        /// <returns>Tupla con (media, desviación estándar, mínimo, máximo)</returns>
        public (decimal Mean, decimal StdDev, decimal Min, decimal Max) CalculateStatistics(List<decimal> prices)
        {
            if (prices == null || prices.Count == 0)
                return (0, 0, 0, 0);

            // Media
            var mean = prices.Average();

            // Desviación estándar
            var variance = prices.Select(p => Math.Pow((double)(p - mean), 2)).Average();
            var stdDev = (decimal)Math.Sqrt(variance);

            // Mínimo y máximo
            var min = prices.Min();
            var max = prices.Max();

            _logger.LogInformation("Estadísticas calculadas - Media: {Mean}, Desv: {StdDev}", mean, stdDev);

            return (mean, stdDev, min, max);
        }

        /// <summary>
        /// Obtiene datos históricos simulados para varias monedas en paralelo.
        /// </summary>
        public async Task<Dictionary<string, List<HistoricalPriceData>>> GetHistoricalDataForCoinsAsync(
            IEnumerable<string> symbols, int days = 30)
        {
            var result = new Dictionary<string, List<HistoricalPriceData>>(StringComparer.OrdinalIgnoreCase);
            foreach (var symbol in symbols)
            {
                var data = await GetHistoricalDataAsync(symbol, days);
                result[symbol.ToUpper()] = data;
            }
            return result;
        }

        /// <summary>
        /// Retorna capitalización de mercado simulada para hoy y ayer.
        /// Útil para la comparación "Market Cap hoy vs ayer" con una sola moneda.
        /// </summary>
        public Task<(decimal Today, decimal Yesterday)> GetYesterdayMarketCapAsync(string symbol)
        {
            var profiles = new Dictionary<string, (decimal Cap, decimal ChangePct)>(StringComparer.OrdinalIgnoreCase)
            {
                { "BTC", (1_350_000_000_000m,  1.85m) },
                { "ETH", (385_000_000_000m,     1.20m) },
                { "BNB", (85_000_000_000m,      0.95m) },
                { "SOL", (75_000_000_000m,      2.40m) },
                { "ADA", (22_000_000_000m,     -0.80m) },
                { "XRP", (55_000_000_000m,      1.10m) },
            };

            var (capToday, changePct) = profiles.TryGetValue(symbol, out var p)
                ? p : (10_000_000_000m, 0.50m);

            var yesterday = capToday / (1m + changePct / 100m);
            return Task.FromResult((capToday, Math.Round(yesterday, 2)));
        }

        /// <summary>
        /// Retorna variación simulada de precio en 24h para una moneda.
        /// Se usa cuando el dato real de la DB es cero o no está disponible.
        /// </summary>
        public decimal GetSimulated24hChange(string symbol)
        {
            var changes = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                { "BTC",  1.85m },
                { "ETH",  1.20m },
                { "BNB",  0.95m },
                { "SOL",  2.40m },
                { "ADA", -0.80m },
                { "XRP",  1.10m },
            };
            return changes.TryGetValue(symbol, out var v) ? v : 0.50m;
        }

        /// <summary>
        /// Descarga datos históricos reales desde CoinGecko.
        /// Endpoint: /coins/{id}/market_chart/range?vs_currency=usd&amp;from={unixFrom}&amp;to={unixTo}
        /// Los precios se devuelven sin ninguna conversión (TRX ≈ 0.08 USD, BTC ≈ 65000 USD).
        /// Sin API key, genera datos simulados realistas para el rango exacto solicitado.
        /// </summary>
        public async Task<HistoricalMarketData> GetCoinGeckoHistoricalDataAsync(
            string symbol, DateTime from, DateTime to)
        {
            var result = new HistoricalMarketData();
            try
            {
                // Normalizar fechas: eliminar componente de hora y capear 'to' a hoy
                var fromDate = from.Date;
                var toDate = to.Date > DateTime.UtcNow.Date ? DateTime.UtcNow.Date : to.Date;

                // Validación básica del rango antes de todo
                if (fromDate > toDate)
                {
                    result.ErrorMessage = "Rango de fechas inválido: 'Desde' debe ser anterior o igual a 'Hasta'.";
                    _logger.LogWarning("GetCoinGeckoHistoricalDataAsync: rango inválido {From} > {To}", fromDate, toDate);
                    return result;
                }

                // Resolución de ID: símbolo → CoinGecko coin ID
                if (!_coinGeckoIdMap.TryGetValue(symbol.ToUpper(), out var coinId))
                    coinId = symbol.ToLower();

                // Si no hay API key configurada → usar datos simulados directamente
                // (no necesitamos unix timestamps ni llamadas HTTP)
                var apiKey = _configuration["CoinGecko:ApiKey"];
                var hasKey = !string.IsNullOrWhiteSpace(apiKey);

                if (!hasKey)
                {
                    _logger.LogInformation(
                        "CoinGecko: sin API key — datos simulados para {Symbol} [{From:yyyy-MM-dd} – {To:yyyy-MM-dd}]",
                        symbol, fromDate, toDate);
                    return GenerateSimulatedRange(symbol, fromDate, toDate);
                }

                // ── A partir de aquí: tenemos API key, usamos CoinGecko real ──
                // Convertir fechas a UNIX segundos (UTC).
                var unixFrom = new DateTimeOffset(fromDate, TimeSpan.Zero).ToUnixTimeSeconds();
                var unixTo = new DateTimeOffset(toDate.AddDays(1).AddSeconds(-1), TimeSpan.Zero).ToUnixTimeSeconds();

                // Capear 'to' a ahora para no consultar fechas futuras
                var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (unixTo > nowSeconds)
                {
                    _logger.LogWarning(
                        "CoinGecko: fecha 'to' ({UnixTo}) es futura; ajustada a ahora ({Now})",
                        unixTo, nowSeconds);
                    unixTo = nowSeconds;
                }

                // Validación de coherencia del rango para la llamada HTTP
                if (unixFrom >= unixTo)
                {
                    _logger.LogWarning(
                        "CoinGecko: unixFrom ({From}) >= unixTo ({To}) — usando simulación de fallback",
                        unixFrom, unixTo);
                    return GenerateSimulatedRange(symbol, fromDate, toDate);
                }

                var baseUrl = _configuration["CoinGecko:BaseUrl"] ?? "https://api.coingecko.com/api/v3";
                var url = $"{baseUrl}/coins/{coinId}/market_chart/range" +
                          $"?vs_currency=usd&from={unixFrom}&to={unixTo}";

                _logger.LogInformation(
                    "CoinGecko → coinId={CoinId}, from={From:yyyy-MM-dd}({UnixFrom}), to={To:yyyy-MM-dd}({UnixTo})",
                    coinId, fromDate, unixFrom, toDate, unixTo);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                request.Headers.TryAddWithoutValidation(
                    "User-Agent",
                    "Mozilla/5.0 (compatible; CryptoView/1.0; Educational Dashboard)");
                request.Headers.TryAddWithoutValidation("x-cg-demo-api-key", apiKey);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var code = (int)response.StatusCode;
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "CoinGecko HTTP {Code} para {Symbol} — usando datos simulados como fallback",
                        code, symbol);

                    // Fallback: datos simulados cuando la API falla
                    if (code == 401 || code == 403 || code == 429 || code >= 500)
                        return GenerateSimulatedRange(symbol, fromDate, toDate);

                    result.ErrorMessage = $"CoinGecko respondió con error HTTP {code}.";
                    return result;
                }

                var json = await response.Content.ReadAsStringAsync();
                var jObj = Newtonsoft.Json.Linq.JObject.Parse(json);

                var pricesArr = jObj["prices"] as Newtonsoft.Json.Linq.JArray;
                var volumesArr = jObj["total_volumes"] as Newtonsoft.Json.Linq.JArray;

                if (pricesArr == null || pricesArr.Count == 0)
                {
                    _logger.LogWarning("CoinGecko devolvió 0 puntos para {Symbol} — usando datos simulados", symbol);
                    return GenerateSimulatedRange(symbol, fromDate, toDate);
                }

                foreach (var pt in pricesArr)
                {
                    var tsMs = pt[0]!.Value<long>();
                    var price = pt[1]!.Value<decimal>();
                    result.Prices.Add(new HistoricalPriceData
                    {
                        Date = DateTimeOffset.FromUnixTimeMilliseconds(tsMs).UtcDateTime,
                        Price = price
                    });
                }

                if (volumesArr != null)
                {
                    foreach (var pt in volumesArr)
                    {
                        var tsMs = pt[0]!.Value<long>();
                        var volume = pt[1]!.Value<decimal>();
                        result.Volumes.Add((DateTimeOffset.FromUnixTimeMilliseconds(tsMs).UtcDateTime, volume));
                    }
                }

                result.Prices = DownsampleHistoricalData(result.Prices, 150);
                _logger.LogInformation("CoinGecko: {Count} puntos para {Symbol}", result.Prices.Count, symbol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetCoinGeckoHistoricalDataAsync para {Symbol} — usando datos simulados", symbol);
                return GenerateSimulatedRange(symbol, from.Date, to.Date > DateTime.UtcNow.Date ? DateTime.UtcNow.Date : to.Date);
            }
            return result;
        }

        /// <summary>
        /// Genera datos históricos simulados con perfil realista por moneda
        /// para un rango de fechas específico. Usado como fallback cuando
        /// CoinGecko no está disponible (sin API key o error de red).
        /// </summary>
        private HistoricalMarketData GenerateSimulatedRange(string symbol, DateTime from, DateTime to)
        {
            var profiles = new Dictionary<string, (decimal Base, decimal Growth, decimal Volatility)>(StringComparer.OrdinalIgnoreCase)
            {
                { "BTC",  (65000m,  0.0015m, 0.035m) },
                { "ETH",  (3200m,   0.0012m, 0.042m) },
                { "BNB",  (420m,    0.0008m, 0.038m) },
                { "AVAX", (36m,     0.0020m, 0.060m) },
                { "DOGE", (0.15m,   0.0030m, 0.080m) },
                { "SOL",  (170m,    0.0020m, 0.055m) },
                { "ADA",  (0.55m,   0.0010m, 0.050m) },
                { "XRP",  (0.52m,   0.0010m, 0.050m) },
                { "LTC",  (85m,     0.0010m, 0.040m) },
                { "DOT",  (8m,      0.0012m, 0.055m) },
                { "LINK", (14m,     0.0015m, 0.060m) },
                { "XLM",  (0.12m,   0.0008m, 0.045m) },
                { "TRX",  (0.12m,   0.0005m, 0.040m) },
            };

            var (basePrice, growthRate, volatility) = profiles.TryGetValue(symbol, out var p)
                ? p : (100m, 0.001m, 0.05m);

            int coinSeed = symbol.ToUpperInvariant().GetHashCode();
            var result = new HistoricalMarketData();

            // Anclar el precio base al inicio del rango para que sea coherente
            var totalDays = (int)(to.Date - from.Date).TotalDays;
            for (int i = 0; i <= totalDays; i++)
            {
                var date = from.Date.AddDays(i);
                var price = basePrice * (decimal)Math.Pow((double)(1 + growthRate), i);
                var rng = new Random(coinSeed ^ date.GetHashCode());
                var noise = (decimal)(rng.NextDouble() * 2 - 1) * volatility;
                price *= (1 + noise);
                int decimals = price < 1m ? 6 : price < 100m ? 4 : 2;

                result.Prices.Add(new HistoricalPriceData
                {
                    Date = date,
                    Price = Math.Round(price, decimals)
                });

                // Volumen simulado: precio × circulación estimada con variación aleatoria
                var volRng = new Random((coinSeed ^ date.GetHashCode()) + 1);
                var volume = price * 50_000_000m * (decimal)(0.7 + volRng.NextDouble() * 0.6);
                result.Volumes.Add((date, Math.Round(volume, 0)));
            }

            result.Prices = DownsampleHistoricalData(result.Prices, 150);
            _logger.LogInformation(
                "Datos simulados generados para {Symbol}: {Count} puntos [{From:yyyy-MM-dd}–{To:yyyy-MM-dd}]",
                symbol, result.Prices.Count, from, to);
            return result;
        }

        /// <summary>
        /// Reduce una lista de datos históricos a un máximo de <paramref name="maxPoints"/> puntos,
        /// conservando el primer y el último punto para mantener el rango correcto.
        /// </summary>
        private static List<HistoricalPriceData> DownsampleHistoricalData(
            List<HistoricalPriceData> data, int maxPoints)
        {
            if (data.Count <= maxPoints) return data;
            var step = (double)(data.Count - 1) / (maxPoints - 1);
            var result = new List<HistoricalPriceData>(maxPoints);
            for (int i = 0; i < maxPoints; i++)
                result.Add(data[(int)Math.Round(i * step)]);
            return result;
        }
    }
}
