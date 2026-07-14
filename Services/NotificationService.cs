using System.Text.Json.Serialization;

namespace CryptoView.Services
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Modelos
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Tipo visual/semántico de la notificación.</summary>
    public enum NotificationType { Success, Warning, Danger, Info }

    /// <summary>Prioridad de la notificación (informativa por ahora).</summary>
    public enum NotificationPriority { Low = 1, Normal = 2, High = 3 }

    /// <summary>
    /// Representa una notificación individual dentro de la app.
    /// Diseñado para ser inmutable en sus campos de identidad;
    /// sólo <see cref="IsRead"/> puede cambiar tras la creación.
    /// </summary>
    public class AppNotification
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public NotificationType Type { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string? TitleKey { get; set; }
        public string? MessageKey { get; set; }
        public string[]? MessageArgs { get; set; }
        public DateTime CreatedAt { get; init; } = DateTime.Now;
        public bool IsRead { get; set; }
        public NotificationPriority Priority { get; init; } = NotificationPriority.Normal;

        /// <summary>
        /// Nombre CSS en minúsculas, usado directamente por el componente de UI
        /// para aplicar clases dinámicas (notif-icon-success, etc.)
        /// </summary>
        [JsonIgnore]
        public string TypeName => Type switch
        {
            NotificationType.Success => "success",
            NotificationType.Warning => "warning",
            NotificationType.Danger => "danger",
            NotificationType.Info => "info",
            _ => "info"
        };

        /// <summary>Icono Bootstrap Icons correspondiente al tipo.</summary>
        [JsonIgnore]
        public string IconClass => Type switch
        {
            NotificationType.Success => "bi-check-circle-fill",
            NotificationType.Warning => "bi-exclamation-triangle-fill",
            NotificationType.Danger => "bi-x-circle-fill",
            NotificationType.Info => "bi-info-circle-fill",
            _ => "bi-bell-fill"
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Servicio
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Servicio central de notificaciones de CryptoView.
    ///
    /// Responsabilidades:
    ///   - Mantener la lista de notificaciones en memoria (por usuario/circuito,
    ///     al estar registrado como Scoped en Blazor Server).
    ///   - Exponer un evento <see cref="OnChanged"/> para que los componentes de
    ///     UI se suscriban y re-rendericen cuando la lista cambia.
    ///   - Proveer métodos de conveniencia para cada tipo de evento de la app.
    ///   - Evitar duplicados cercanos en el tiempo (deduplicación configurable).
    ///
    /// Para persistencia futura: reemplazar <c>_notifications</c> por llamadas
    /// a un repositorio o DbContext sin cambiar la interfaz pública.
    /// </summary>
    public class NotificationService
    {
        // ── Configuración ────────────────────────────────────────────────────

        /// <summary>
        /// Umbral de cambio de precio (%) a partir del cual se genera una
        /// notificación de movimiento importante. Modificable en runtime.
        /// </summary>
        public decimal PriceChangeThreshold { get; set; } = 5m;

        /// <summary>
        /// Ventana de tiempo dentro de la cual dos notificaciones con el mismo
        /// tipo + título se consideran duplicadas y la segunda se descarta.
        /// </summary>
        public TimeSpan DedupWindow { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Tiempo mínimo entre dos alertas de precio para la misma moneda y
        /// dirección (suba/baja). Evita spam cuando se refresca repetidamente
        /// sin un cambio real adicional.
        /// </summary>
        public TimeSpan PriceAlertCooldown { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Máximo de notificaciones que se retienen en memoria y en localStorage.
        /// Al superar el límite se eliminan las más antiguas.
        /// </summary>
        public int MaxNotifications { get; set; } = 50;

        // ── Estado interno ───────────────────────────────────────────────────

        private readonly List<AppNotification> _notifications = new();

        /// <summary>
        /// Historial de alertas de precio por moneda y dirección.
        /// Clave: "{coinName.ToLower()}:{up|down}" → hora del último alerta emitido.
        /// Permite deduplicar por moneda + dirección de forma independiente
        /// al mecanismo general de título+tipo.
        /// </summary>
        private readonly Dictionary<string, DateTime> _priceAlertHistory = new();

        // ── Evento de cambio ─────────────────────────────────────────────────

        /// <summary>
        /// Se dispara cada vez que la lista cambia (añadido, leído, limpiado).
        /// Los componentes deben suscribirse a este evento y llamar
        /// <c>await InvokeAsync(StateHasChanged)</c> para re-renderizar de forma
        /// segura en el hilo del circuito Blazor.
        /// </summary>
        public event Action? OnChanged;

        /// <summary>
        /// Se dispara cuando cambia la notificación seleccionada para el modal global.
        /// </summary>
        public event Action? OnModalChanged;

        /// <summary>Notificación actualmente mostrada en el modal global (null = cerrado).</summary>
        public AppNotification? SelectedNotification { get; private set; }

        /// <summary>Abre el modal global para la notificación indicada y la marca como leída.</summary>
        public void OpenModal(AppNotification n)
        {
            MarkAsRead(n.Id);
            SelectedNotification = n;
            OnModalChanged?.Invoke();
        }

        /// <summary>Cierra el modal global.</summary>
        public void CloseModal()
        {
            SelectedNotification = null;
            OnModalChanged?.Invoke();
        }

        // ── Consultas ────────────────────────────────────────────────────────

        /// <summary>Todas las notificaciones ordenadas de más reciente a más antigua.</summary>
        public IReadOnlyList<AppNotification> All
            => _notifications.OrderByDescending(n => n.CreatedAt).ToList();

        /// <summary>Número de notificaciones no leídas (badge).</summary>
        public int UnreadCount => _notifications.Count(n => !n.IsRead);

        // ── Comandos ─────────────────────────────────────────────────────────

        /// <summary>
        /// Agrega una nueva notificación respetando la ventana de deduplicación.
        /// </summary>
        public void Add(
            NotificationType type,
            string title,
            string message,
            NotificationPriority priority = NotificationPriority.Normal)
        {
            // Deduplicación: descartar si ya existe una notificación idéntica reciente
            bool isDuplicate = _notifications.Any(n =>
                n.Type == type &&
                n.Title == title &&
                (DateTime.Now - n.CreatedAt) < DedupWindow);

            if (isDuplicate) return;

            AddCore(type, title, message, priority);
        }

        /// <summary>
        /// Agrega una notificación con claves de localización.
        /// El título y mensaje se resolverán en tiempo de visualización.
        /// </summary>
        public void AddLocalized(
            NotificationType type,
            string titleKey,
            string messageKey,
            string[]? messageArgs = null,
            NotificationPriority priority = NotificationPriority.Normal,
            string? fallbackTitle = null,
            string? fallbackMessage = null)
        {
            // Deduplicación por TitleKey + MessageKey
            bool isDuplicate = _notifications.Any(n =>
                n.Type == type &&
                n.TitleKey == titleKey &&
                n.MessageKey == messageKey &&
                (DateTime.Now - n.CreatedAt) < DedupWindow);

            if (isDuplicate) return;

            AddCore(type, fallbackTitle ?? titleKey, fallbackMessage ?? messageKey, priority, titleKey, messageKey, messageArgs);
        }

        // ── Métodos de visualización localizada ──────────────────────────────

        /// <summary>Obtiene el título localizado para una notificación.</summary>
        public static string GetDisplayTitle(AppNotification n, LocalizacionService loc)
            => !string.IsNullOrEmpty(n.TitleKey) ? loc[n.TitleKey] : n.Title;

        /// <summary>Obtiene el mensaje localizado para una notificación.</summary>
        public static string GetDisplayMessage(AppNotification n, LocalizacionService loc)
        {
            if (!string.IsNullOrEmpty(n.MessageKey))
            {
                // [TEMP] Diagnóstico — eliminar antes de producción
                Console.WriteLine($"[NotifDebug] Key={n.MessageKey}; ArgsCount={n.MessageArgs?.Length ?? 0}; Args=[{string.Join(" | ", n.MessageArgs ?? Array.Empty<string>())}]");

                var result = n.MessageArgs is { Length: > 0 }
                    ? loc.Format(n.MessageKey, n.MessageArgs)
                    : loc[n.MessageKey];

                // Si el resultado contiene placeholders sin resolver, usar Message como fallback
                if (!string.IsNullOrEmpty(result) && System.Text.RegularExpressions.Regex.IsMatch(result, @"\{\d+[^}]*\}"))
                {
                    return !string.IsNullOrEmpty(n.Message) && !System.Text.RegularExpressions.Regex.IsMatch(n.Message, @"\{\d+[^}]*\}")
                        ? n.Message
                        : result; // último recurso: devolver el template con placeholders visibles
                }
                return result;
            }
            return n.Message;
        }

        // ── Método interno de inserción compartido ──────────────────────────

        private void AddCore(
            NotificationType type,
            string title,
            string message,
            NotificationPriority priority,
            string? titleKey = null,
            string? messageKey = null,
            string[]? messageArgs = null)
        {
            // Respetar el límite: eliminar las más antiguas si es necesario
            while (_notifications.Count >= MaxNotifications)
            {
                var oldest = _notifications.MinBy(n => n.CreatedAt);
                if (oldest != null) _notifications.Remove(oldest);
                else break;
            }

            _notifications.Add(new AppNotification
            {
                Type = type,
                Title = title,
                Message = message,
                TitleKey = titleKey,
                MessageKey = messageKey,
                MessageArgs = messageArgs,
                Priority = priority,
                CreatedAt = DateTime.Now
            });

            OnChanged?.Invoke();
        }

        /// <summary>Marca una notificación específica como leída.</summary>
        public void MarkAsRead(Guid id)
        {
            var notif = _notifications.FirstOrDefault(n => n.Id == id);
            if (notif is { IsRead: false })
            {
                notif.IsRead = true;
                OnChanged?.Invoke();
            }
        }

        /// <summary>Marca todas las notificaciones como leídas.</summary>
        public void MarkAllAsRead()
        {
            bool anyUnread = _notifications.Any(n => !n.IsRead);
            if (!anyUnread) return;

            foreach (var n in _notifications)
                n.IsRead = true;

            OnChanged?.Invoke();
        }

        /// <summary>
        /// Elimina todas las notificaciones de la lista en memoria.
        /// El componente de UI persiste el estado vacío en localStorage
        /// al recibir el evento <see cref="OnChanged"/>.
        /// </summary>
        public void ClearAll()
        {
            if (_notifications.Count == 0) return;
            _notifications.Clear();
            OnChanged?.Invoke();
        }

        /// <summary>
        /// Reemplaza la lista en memoria con notificaciones recuperadas de
        /// localStorage. No dispara <see cref="OnChanged"/> para evitar un
        /// ciclo de guardado innecesario; el componente llama
        /// <c>StateHasChanged</c> directamente tras invocar este método.
        /// </summary>
        public void LoadFromStorage(IEnumerable<AppNotification> stored)
        {
            _notifications.Clear();
            foreach (var n in stored.OrderByDescending(n => n.CreatedAt).Take(MaxNotifications))
            {
                MigrateHistoricalNotification(n);
                _notifications.Add(n);
            }
            // No se invoca OnChanged — el componente gestiona el re-render
        }

        // ── Métodos de conveniencia para eventos de la app ───────────────────

        /// <summary>Notificación de actualización de precios exitosa.</summary>
        public void NotifyPriceUpdateSuccess(int updatedCount)
        {
            string plural = updatedCount == 1 ? "criptomoneda" : "criptomonedas";
            AddLocalized(
                NotificationType.Success,
                "notifications.updateSuccess.title",
                "notifications.updateSuccess.message",
                messageArgs: new[] { updatedCount.ToString(), updatedCount == 1 ? "criptomoneda" : "criptomonedas" },
                fallbackTitle: "Precios actualizados",
                fallbackMessage: $"Se actualizaron los precios de {updatedCount} {plural} correctamente.");
        }

        /// <summary>Notificación de error al obtener datos de la API.</summary>
        public void NotifyPriceUpdateError()
        {
            AddLocalized(
                NotificationType.Danger,
                "notifications.updateError.title",
                "notifications.updateError.message",
                priority: NotificationPriority.High,
                fallbackTitle: "Error de actualización",
                fallbackMessage: "No se pudieron obtener los datos de una o más criptomonedas.");
        }

        /// <summary>
        /// Notificación de movimiento de precio significativo.
        /// Solo se llama cuando |change| >= <see cref="PriceChangeThreshold"/>.
        /// </summary>
        public void NotifyPriceMovement(string coinName, decimal changePercent)
        {
            string direction = changePercent > 0 ? "subió" : "bajó";
            string sign = changePercent > 0 ? "+" : "";
            string directionEn = changePercent > 0 ? "rose" : "fell";
            AddLocalized(
                NotificationType.Warning,
                "notifications.priceMovement.title",
                "notifications.priceMovement.message",
                messageArgs: new[] { coinName, direction, $"{sign}{changePercent:F2}%" },
                priority: NotificationPriority.High,
                fallbackTitle: "Movimiento importante detectado",
                fallbackMessage: $"{coinName} {direction} {sign}{changePercent:F2}% en la última actualización.");
        }

        /// <summary>
        /// Compara el precio anterior almacenado con el precio recién obtenido
        /// de la API y dispara una alerta si la variación supera
        /// <see cref="PriceChangeThreshold"/>.
        ///
        /// Fórmula: ((precioNuevo - precioAnterior) / precioAnterior) * 100
        ///
        /// La deduplicación es por moneda + dirección (suba/baja) con ventana
        /// configurable <see cref="PriceAlertCooldown"/>, más precisa que la
        /// deduplicación genérica por título.
        /// </summary>
        /// <param name="coinName">Nombre legible de la moneda (p.ej. "Bitcoin").</param>
        /// <param name="previousPrice">Precio almacenado antes de la actualización.</param>
        /// <param name="newPrice">Precio recién obtenido de la API.</param>
        public void NotifyPriceAlert(string coinName, decimal previousPrice, decimal newPrice)
        {
            // Sin precio base (primera carga) no hay delta real que comparar
            if (previousPrice <= 0) return;

            decimal delta = ((newPrice - previousPrice) / previousPrice) * 100m;

            if (Math.Abs(delta) < PriceChangeThreshold) return;

            bool isUp = delta > 0;
            string direction = isUp ? "up" : "down";
            string dedupKey = $"{coinName.ToLower()}:{direction}";

            // Cooldown por moneda + dirección: descarta si ya se alertó recientemente
            if (_priceAlertHistory.TryGetValue(dedupKey, out var lastAlert)
                && (DateTime.Now - lastAlert) < PriceAlertCooldown)
                return;

            _priceAlertHistory[dedupKey] = DateTime.Now;

            if (isUp)
            {
                AddLocalized(
                    NotificationType.Success,
                    "notifications.priceAlertUp.title",
                    "notifications.priceAlertUp.message",
                    messageArgs: new[] { coinName, $"{PriceChangeThreshold:F0}%", $"{delta:+0.00}%" },
                    priority: NotificationPriority.High,
                    fallbackTitle: "Suba importante detectada",
                    fallbackMessage: $"{coinName} subió más de {PriceChangeThreshold:F0}% desde la última actualización ({delta:+0.00}%).");
            }
            else
            {
                AddLocalized(
                    NotificationType.Warning,
                    "notifications.priceAlertDown.title",
                    "notifications.priceAlertDown.message",
                    messageArgs: new[] { coinName, $"{PriceChangeThreshold:F0}%", $"{delta:F2}%" },
                    priority: NotificationPriority.High,
                    fallbackTitle: "Baja importante detectada",
                    fallbackMessage: $"{coinName} bajó más de {PriceChangeThreshold:F0}% desde la última actualización ({delta:F2}%).");
            }
        }

        /// <summary>Notificación al agregar una criptomoneda a la lista.</summary>
        public void NotifyCryptoAdded(string coinName)
        {
            AddLocalized(
                NotificationType.Info,
                "notifications.cryptoAdded.title",
                "notifications.cryptoAdded.message",
                messageArgs: new[] { coinName },
                fallbackTitle: "Criptomoneda agregada",
                fallbackMessage: $"Se agregó {coinName} a tu lista de seguimiento.");
        }

        /// <summary>Notificación al eliminar una criptomoneda de la lista.</summary>
        public void NotifyCryptoDeleted(string coinName)
        {
            AddLocalized(
                NotificationType.Info,
                "notifications.cryptoDeleted.title",
                "notifications.cryptoDeleted.message",
                messageArgs: new[] { coinName },
                fallbackTitle: "Criptomoneda eliminada",
                fallbackMessage: $"{coinName} fue eliminada de tu lista de seguimiento.");
        }

        /// <summary>
        /// Migra notificaciones históricas guardadas con títulos hardcodeados
        /// para que tengan TitleKey/MessageKey y se muestren localizadas.
        /// No re-persiste a localStorage.
        /// </summary>
        private static void MigrateHistoricalNotification(AppNotification n)
        {
            if (!string.IsNullOrEmpty(n.TitleKey)) return; // ya migrada

            var msg = n.Message ?? string.Empty;

            switch (n.Title)
            {
                case "Precios actualizados":
                    n.TitleKey = "notifications.updateSuccess.title";
                    n.MessageKey = "notifications.updateSuccess.message";
                    // Extraer cantidad de monedas: "Se actualizaron los precios de X criptomonedas..."
                    var qtyMatch = System.Text.RegularExpressions.Regex.Match(msg, @"\d+");
                    if (qtyMatch.Success)
                        n.MessageArgs = new[] { qtyMatch.Value, qtyMatch.Value == "1" ? "criptomoneda" : "criptomonedas" };
                    break;
                case "Error de actualización":
                    n.TitleKey = "notifications.updateError.title";
                    n.MessageKey = "notifications.updateError.message";
                    break;
                case "Movimiento importante detectado":
                    n.TitleKey = "notifications.priceMovement.title";
                    n.MessageKey = "notifications.priceMovement.message";
                    // Formato: "{name} subió/bajó {sign}{change}%..."
                    var moveMatch = System.Text.RegularExpressions.Regex.Match(msg, @"^(.+?)\s+(subió|bajó|rose|fell)\s+(.+)\s+en la");
                    if (moveMatch.Success && moveMatch.Groups.Count >= 4)
                        n.MessageArgs = new[] { moveMatch.Groups[1].Value.Trim(), moveMatch.Groups[2].Value, moveMatch.Groups[3].Value.Trim() };
                    break;
                case "Suba importante detectada":
                    n.TitleKey = "notifications.priceAlertUp.title";
                    n.MessageKey = "notifications.priceAlertUp.message";
                    // Formato: "{name} subió más de {threshold}%...({delta}%)"
                    var upMatch = System.Text.RegularExpressions.Regex.Match(msg, @"^(.+?)\s+subió\s+más de\s+(.+?)%\s+desde la última actualización\s+\((.+?)\)\.");
                    if (upMatch.Success && upMatch.Groups.Count >= 4)
                        n.MessageArgs = new[] { upMatch.Groups[1].Value.Trim(), upMatch.Groups[2].Value.Trim() + "%", upMatch.Groups[3].Value.Trim() };
                    break;
                case "Baja importante detectada":
                    n.TitleKey = "notifications.priceAlertDown.title";
                    n.MessageKey = "notifications.priceAlertDown.message";
                    // Formato: "{name} bajó más de {threshold}%...({delta}%)"
                    var downMatch = System.Text.RegularExpressions.Regex.Match(msg, @"^(.+?)\s+bajó\s+más de\s+(.+?)%\s+desde la última actualización\s+\((.+?)\)\.");
                    if (downMatch.Success && downMatch.Groups.Count >= 4)
                        n.MessageArgs = new[] { downMatch.Groups[1].Value.Trim(), downMatch.Groups[2].Value.Trim() + "%", downMatch.Groups[3].Value.Trim() };
                    break;
                case "Criptomoneda agregada":
                    n.TitleKey = "notifications.cryptoAdded.title";
                    n.MessageKey = "notifications.cryptoAdded.message";
                    // Formato: "Se agregó {name} a tu lista..."
                    var addMatch = System.Text.RegularExpressions.Regex.Match(msg, @"^Se agregó\s+(.+?)\s+a tu");
                    if (!addMatch.Success)
                        addMatch = System.Text.RegularExpressions.Regex.Match(msg, @"^(.+?)\s+was added");
                    if (addMatch.Success)
                        n.MessageArgs = new[] { addMatch.Groups[1].Value.Trim() };
                    break;
                case "Criptomoneda eliminada":
                    n.TitleKey = "notifications.cryptoDeleted.title";
                    n.MessageKey = "notifications.cryptoDeleted.message";
                    // Formato: "{name} fue eliminada de tu lista..." o "{name} was removed..."
                    var delMatch = System.Text.RegularExpressions.Regex.Match(msg, @"^(.+?)\s+fue eliminada");
                    if (!delMatch.Success)
                        delMatch = System.Text.RegularExpressions.Regex.Match(msg, @"^(.+?)\s+was removed");
                    if (delMatch.Success)
                        n.MessageArgs = new[] { delMatch.Groups[1].Value.Trim() };
                    break;
            }
        }
    }
}
