using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.JSInterop;

namespace CryptoView.Services
{
    /// <summary>
    /// Servicio central de localización para CryptoView.
    /// Carga diccionarios JSON desde wwwroot/i18n/ en disco.
    /// Scoped = una instancia por circuito Blazor Server.
    /// </summary>
    public class LocalizacionService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IJSRuntime _js;
        private readonly ILogger<LocalizacionService> _logger;

        private string _language = "es";
        private Dictionary<string, string> _current = new();
        private Dictionary<string, string> _fallback = new();
        private bool _initialized;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private static readonly HashSet<string> ValidLanguages = new() { "es", "en" };

        /// <summary>Idioma activo actual ("es" o "en").</summary>
        public string Language => _language;

        /// <summary>Se dispara cuando el idioma cambia. Los componentes deben desuscribirse en Dispose().</summary>
        public event Action? OnLanguageChanged;

        /// <summary>Indexador unificado para obtener traducciones: Loc["clave"].</summary>
        public string this[string key] => T(key);

        /// <summary>
        /// Constructor. Carga español desde disco como fallback.
        /// No necesita JS Interop — IWebHostEnvironment lee del filesystem del servidor.
        /// </summary>
        public LocalizacionService(
            IWebHostEnvironment env,
            IJSRuntime js,
            ILogger<LocalizacionService> logger)
        {
            _env = env;
            _js = js;
            _logger = logger;

            _fallback = LoadFromDisk("es") ?? new Dictionary<string, string>();
            _current = new Dictionary<string, string>(_fallback);
        }

        /// <summary>
        /// Inicialización post-prerender. Llamar SOLO desde OnAfterRenderAsync(firstRender=true).
        /// Lee localStorage, carga el diccionario correspondiente si es necesario,
        /// y dispara OnLanguageChanged si hubo un cambio de idioma.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_initialized) return;

            bool languageChanged = false;

            await _lock.WaitAsync();
            try
            {
                if (_initialized) return;

                var lang = await ReadLanguageFromStorage();

                if (lang != _language)
                {
                    var dict = LoadFromDisk(lang);
                    if (dict is not null)
                    {
                        _current = dict;
                        _language = lang;
                        await ApplyToDocument(lang);
                        languageChanged = true;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[Localizacion] No se pudo cargar {Lang}.json, manteniendo español.", lang);
                        _language = "es";
                        _current = new Dictionary<string, string>(_fallback);
                        await ApplyToDocument("es");
                    }
                }

                _initialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Localizacion] Error durante inicialización");
                _language = "es";
                _current = new Dictionary<string, string>(_fallback);
                _initialized = true;
            }
            finally
            {
                _lock.Release();
            }

            if (languageChanged)
            {
                OnLanguageChanged?.Invoke();
            }
        }

        /// <summary>
        /// Cambia el idioma activo, persiste en localStorage, actualiza el DOM y notifica.
        /// </summary>
        public async Task SetLanguageAsync(string lang)
        {
            if (!ValidLanguages.Contains(lang)) return;

            bool languageChanged = false;

            await _lock.WaitAsync();
            try
            {
                if (lang == _language) return;

                var dict = LoadFromDisk(lang);
                if (dict is null)
                {
                    _logger.LogWarning(
                        "[Localizacion] No se pudo cargar {Lang}.json para cambio de idioma.", lang);
                    return;
                }

                _current = dict;
                _language = lang;

                await PersistLanguage(lang);
                await ApplyToDocument(lang);

                languageChanged = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Localizacion] Error cambiando a {Lang}", lang);
            }
            finally
            {
                _lock.Release();
            }

            if (languageChanged)
            {
                OnLanguageChanged?.Invoke();
            }
        }

        /// <summary>
        /// Lookup de traducción con fallback: idioma actual → español → [clave].
        /// </summary>
        public string T(string key)
        {
            if (_current.TryGetValue(key, out var val)) return val;
            if (_fallback.TryGetValue(key, out var fb)) return fb;
            return $"[{key}]";
        }

        /// <summary>
        /// Resuelve la clave de traducción y aplica string.Format con los argumentos.
        /// Cultura: en-US para inglés, es-AR para español.
        /// </summary>
        public string Format(string key, params object[] args)
        {
            var template = T(key);
            if (args.Length == 0) return template;

            var culture = _language == "en"
                ? CultureInfo.GetCultureInfo("en-US")
                : CultureInfo.GetCultureInfo("es-AR");

            try
            {
                return string.Format(culture, template, args);
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "[Localizacion] Error de formato en clave '{Key}': {Template}", key, template);
                return template;
            }
        }

        private Dictionary<string, string>? LoadFromDisk(string lang)
        {
            if (!ValidLanguages.Contains(lang)) return null;

            try
            {
                var path = Path.Combine(_env.WebRootPath, "i18n", $"{lang}.json");

                if (!File.Exists(path))
                {
                    _logger.LogWarning("[Localizacion] Archivo no encontrado: {Path}", path);
                    return null;
                }

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Localizacion] Error leyendo {Lang}.json desde disco", lang);
                return null;
            }
        }

        private async Task<string> ReadLanguageFromStorage()
        {
            try
            {
                var lang = await _js.InvokeAsync<string>("cryptoViewLanguage.get");
                return ValidLanguages.Contains(lang) ? lang : "es";
            }
            catch
            {
                return "es";
            }
        }

        private async Task PersistLanguage(string lang)
        {
            try
            {
                await _js.InvokeVoidAsync("cryptoViewLanguage.set", lang);
            }
            catch
            {
            }
        }

        private async Task ApplyToDocument(string lang)
        {
            try
            {
                await _js.InvokeVoidAsync("cryptoViewLanguage.applyToDocument", lang);
            }
            catch
            {
            }
        }
    }
}
