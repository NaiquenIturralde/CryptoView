using CryptoView.Models;
using Microsoft.JSInterop;

namespace CryptoView.Services
{
    /// <summary>
    /// Centralized service for managing contextual tutorials across CryptoView.
    ///
    /// Responsibilities:
    ///   - Register all tutorial definitions (ID, version, steps).
    ///   - Persist/restore seen-state per user in localStorage via JS interop.
    ///   - Provide methods to check, mark, and reset tutorials.
    ///   - Resolve localized step content via LocalizacionService.
    ///
    /// Scoped = one instance per Blazor Server circuit.
    /// </summary>
    public class TutorialService
    {
        private readonly IJSRuntime _js;
        private readonly LocalizacionService _loc;
        private readonly ILogger<TutorialService> _logger;

        private readonly List<TutorialDefinition> _tutorials = new();

        /// <summary>
        /// Fires when a tutorial's seen-state changes (e.g. after MarkAsSeen or Reset).
        /// Components can subscribe to refresh their state.
        /// </summary>
        public event Action? OnStateChanged;

        public TutorialService(
            IJSRuntime js,
            LocalizacionService loc,
            ILogger<TutorialService> logger)
        {
            _js = js;
            _loc = loc;
            _logger = logger;
            RegisterTutorials();
        }

        // ── Tutorial Registration ──────────────────────────────────────────

        private void RegisterTutorials()
        {
            // Single guided tour through the entire application
            _tutorials.Add(new TutorialDefinition
            {
                Id = "main-tour",
                Version = 1,
                PageRoute = "/",
                Steps = new()
                {
                    // Step 1: Welcome (centered modal, no navigation)
                    new TutorialStep
                    {
                        TitleKey = "tutorial.main.welcome.title",
                        DescriptionKey = "tutorial.main.welcome.desc",
                        Position = TutorialPosition.Center
                    },
                    // Step 2: Dashboard
                    new TutorialStep
                    {
                        TitleKey = "tutorial.main.dashboard.title",
                        DescriptionKey = "tutorial.main.dashboard.desc",
                        Route = "/",
                        TargetSelector = "[data-tutorial='nav-dashboard']",
                        Position = TutorialPosition.Right
                    },
                    // Step 3: Mis Criptomonedas
                    new TutorialStep
                    {
                        TitleKey = "tutorial.main.cryptos.title",
                        DescriptionKey = "tutorial.main.cryptos.desc",
                        Route = "/cryptolist",
                        TargetSelector = "[data-tutorial='nav-cryptos']",
                        Position = TutorialPosition.Right
                    },
                    // Step 4: Estadísticas
                    new TutorialStep
                    {
                        TitleKey = "tutorial.main.stats.title",
                        DescriptionKey = "tutorial.main.stats.desc",
                        Route = "/stats",
                        TargetSelector = "[data-tutorial='nav-stats']",
                        Position = TutorialPosition.Right
                    },
                    // Step 5: Mi Perfil
                    new TutorialStep
                    {
                        TitleKey = "tutorial.main.profile.title",
                        DescriptionKey = "tutorial.main.profile.desc",
                        Route = "/perfil",
                        TargetSelector = "[data-tutorial='nav-profile']",
                        Position = TutorialPosition.Bottom
                    },
                    // Step 6: Preferencias
                    new TutorialStep
                    {
                        TitleKey = "tutorial.main.preferences.title",
                        DescriptionKey = "tutorial.main.preferences.desc",
                        Route = "/preferencias",
                        TargetSelector = "[data-tutorial='nav-preferences']",
                        Position = TutorialPosition.Bottom
                    },
                    // Step 7: Ayuda
                    new TutorialStep
                    {
                        TitleKey = "tutorial.main.help.title",
                        DescriptionKey = "tutorial.main.help.desc",
                        Route = "/ayuda",
                        TargetSelector = "[data-tutorial='nav-help']",
                        Position = TutorialPosition.Bottom
                    },
                    // Step 8: Language selector
                    new TutorialStep
                    {
                        TitleKey = "tutorial.main.language.title",
                        DescriptionKey = "tutorial.main.language.desc",
                        TargetSelector = "[data-tutorial='language-selector']",
                        Position = TutorialPosition.Bottom
                    },
                    // Step 9: Notifications
                    new TutorialStep
                    {
                        TitleKey = "tutorial.main.notifications.title",
                        DescriptionKey = "tutorial.main.notifications.desc",
                        TargetSelector = "[data-tutorial='notifications']",
                        Position = TutorialPosition.Bottom
                    },
                    // Step 10: Finish (centered modal, no navigation)
                    new TutorialStep
                    {
                        TitleKey = "tutorial.main.finish.title",
                        DescriptionKey = "tutorial.main.finish.desc",
                        Route = "/",
                        Position = TutorialPosition.Center
                    }
                }
            });
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns all registered tutorial definitions (for Ayuda.razor listing).
        /// </summary>
        public IReadOnlyList<TutorialDefinition> GetAll() => _tutorials.AsReadOnly();

        /// <summary>
        /// Gets a specific tutorial by ID.
        /// </summary>
        public TutorialDefinition? GetTutorial(string id)
            => _tutorials.FirstOrDefault(t => t.Id == id);

        /// <summary>
        /// Checks whether the current user should see this tutorial.
        /// Returns true if the tutorial has NOT been seen yet (or a new version exists).
        /// </summary>
        public async Task<bool> ShouldShowAsync(string tutorialId)
        {
            var tutorial = GetTutorial(tutorialId);
            if (tutorial is null) return false;

            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId is null) return false;

                var key = BuildStorageKey(tutorialId, tutorial.Version, userId.Value);
                var seen = await _js.InvokeAsync<bool?>("tutorialStorage.hasSeen", key);
                return seen != true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[TutorialService] Error checking tutorial state for {Id}", tutorialId);
                return false;
            }
        }

        /// <summary>
        /// Marks a tutorial as seen for the current user.
        /// </summary>
        public async Task MarkAsSeenAsync(string tutorialId)
        {
            var tutorial = GetTutorial(tutorialId);
            if (tutorial is null) return;

            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId is null) return;

                var key = BuildStorageKey(tutorialId, tutorial.Version, userId.Value);
                await _js.InvokeVoidAsync("tutorialStorage.markSeen", key);
                OnStateChanged?.Invoke();
                _logger.LogDebug("[TutorialService] Marked tutorial {Id} v{Version} as seen for user {UserId}",
                    tutorialId, tutorial.Version, userId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[TutorialService] Error marking tutorial {Id}", tutorialId);
            }
        }

        /// <summary>
        /// Resets a specific tutorial so the user will see it again.
        /// </summary>
        public async Task ResetTutorialAsync(string tutorialId)
        {
            var tutorial = GetTutorial(tutorialId);
            if (tutorial is null) return;

            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId is null) return;

                var key = BuildStorageKey(tutorialId, tutorial.Version, userId.Value);
                await _js.InvokeVoidAsync("tutorialStorage.remove", key);
                OnStateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[TutorialService] Error resetting tutorial {Id}", tutorialId);
            }
        }

        /// <summary>
        /// Resets ALL tutorials for the current user.
        /// </summary>
        public async Task ResetAllTutorialsAsync()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId is null) return;

                foreach (var tutorial in _tutorials)
                {
                    var key = BuildStorageKey(tutorial.Id, tutorial.Version, userId.Value);
                    await _js.InvokeVoidAsync("tutorialStorage.remove", key);
                }
                OnStateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[TutorialService] Error resetting all tutorials");
            }
        }

        // ── Localized Content ──────────────────────────────────────────────

        public string GetStepTitle(TutorialStep step) => _loc[step.TitleKey];
        public string GetStepDescription(TutorialStep step) => _loc[step.DescriptionKey];

        public string GetStepIndicator(int current, int total)
            => _loc.Format("tutorial.stepOf", current + 1, total);

        public string GetGotItLabel() => _loc["tutorial.main.gotIt"];
        public string GetSkipLabel() => _loc["tutorial.main.skip"];

        // ── Helpers ────────────────────────────────────────────────────────

        private string BuildStorageKey(string tutorialId, int version, int userId)
            => $"cv_tutorial_{tutorialId}_v{version}_{userId}";

        private async Task<int?> GetCurrentUserIdAsync()
        {
            try
            {
                var userJson = await _js.InvokeAsync<string?>("sessionManager.getUserJson");
                if (string.IsNullOrEmpty(userJson)) return null;

                var userObj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(userJson);
                if (userObj.TryGetProperty("id", out var idProp) && idProp.TryGetInt32(out var userId))
                    return userId;
            }
            catch { /* JS not available or parse error */ }
            return null;
        }
    }
}
