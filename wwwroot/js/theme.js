window.scrollToTop = function () {
    window.scrollTo({ top: 0, behavior: 'instant' });
};

window.scrollIntoViewById = function (id) {
    const el = document.getElementById(id);
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
};

window.themeManager = {
    applyTheme: function (theme) {
        const cls = theme === "light" ? "theme-light" : "theme-dark";
        // Apply to <html> (drives CSS custom properties)
        document.documentElement.classList.remove("theme-dark", "theme-light");
        document.documentElement.classList.add(cls);
        // Apply to <body> as well (drives body.theme-light selectors and !important overrides)
        document.body.classList.remove("theme-dark", "theme-light");
        document.body.classList.add(cls);
        localStorage.setItem("theme", theme);

        // Update logo images dynamically
        const logoDark  = document.getElementById("logoCryptoViewDark");
        const logoLight = document.getElementById("logoCryptoViewLight");
        if (logoDark && logoLight) {
            logoDark.style.display  = theme === "dark"  ? "block" : "none";
            logoLight.style.display = theme === "light" ? "block" : "none";
        }

        console.log("Tema aplicado:", theme);
    },

    getTheme: function () {
        return localStorage.getItem("theme") || "dark";
    }
};

window.sessionManager = {
    /// Persists the authenticated user in sessionStorage.
    /// Session expires when the browser tab/window closes.
    setUser: function (userObj) {
        try {
            if (!userObj.timestamp) {
                userObj.timestamp = Date.now();
            }
            sessionStorage.setItem("currentUser", JSON.stringify(userObj));

            // Transition cleanup: remove legacy keys from localStorage
            try {
                localStorage.removeItem("currentUser");
                localStorage.removeItem("user");
                localStorage.removeItem("userRole");
                localStorage.removeItem("sessionCreated");
                localStorage.removeItem("token");
            } catch (e) { /* ignore cleanup errors */ }
        } catch (e) {
            console.warn("[sessionManager] No se pudo guardar usuario:", e);
        }
    },

    /// Returns the full user object, or null if no session exists.
    getUser: function () {
        try {
            var json = sessionStorage.getItem("currentUser");
            if (!json) return null;
            return JSON.parse(json);
        } catch (e) {
            console.warn("[sessionManager] Error parsing user:", e);
            return null;
        }
    },

    /// Returns just the username string for easy C# interop.
    getUsername: function () {
        try {
            var json = sessionStorage.getItem("currentUser");
            if (json) {
                var obj = JSON.parse(json);
                if (obj.username) return obj.username;
            }
        } catch (e) {
            console.warn("[sessionManager] Error getting username:", e);
        }
        return "";
    },

    /// Returns whether there is a valid session
    isSessionValid: function () {
        try {
            var user = this.getUser();
            return user !== null && user.id && user.username;
        } catch (e) {
            return false;
        }
    },

    /// Returns the raw user JSON string for C# interop (used by MainLayout to restore session)
    getUserJson: function () {
        try {
            var json = sessionStorage.getItem("currentUser");
            if (!json) return null;
            var obj = JSON.parse(json);
            if (obj.id && obj.username) return json;
            return null;
        } catch (e) {
            return null;
        }
    },

    /// Completely clears all session data from storage
    logout: function () {
        sessionStorage.removeItem("currentUser");
        try {
            localStorage.removeItem("token");
            localStorage.removeItem("user");
            localStorage.removeItem("userRole");
            localStorage.removeItem("sessionCreated");
            localStorage.removeItem("currentUser");
        } catch (e) { /* ignore cleanup errors */ }
    }
};

// Toggles body.modal-open to lock scroll while a modal is open.
window.setBodyModalOpen = function (open) {
    document.body.classList.toggle("modal-open", !!open);
};

// ─── Notification persistence ────────────────────────────────────────────────
// Guarda y recupera la lista de notificaciones en localStorage.
// Usado por NotificationDropdown.razor vía IJSRuntime.
window.notificationStore = {
    _key: "cryptoview_notifications",

    // Persiste el JSON serializado de las notificaciones.
    save: function (json) {
        try {
            localStorage.setItem(this._key, json);
        } catch (e) {
            console.warn("[notificationStore] No se pudo guardar en localStorage:", e);
        }
    },

    // Devuelve el JSON almacenado, o "[]" si no existe o hay error.
    load: function () {
        try {
            return localStorage.getItem(this._key) || "[]";
        } catch (e) {
            console.warn("[notificationStore] No se pudo leer de localStorage:", e);
            return "[]";
        }
    },

    // Elimina la entrada de localStorage.
    clear: function () {
        try {
            localStorage.removeItem(this._key);
        } catch (e) {
            console.warn("[notificationStore] No se pudo limpiar localStorage:", e);
        }
    }
};

// ─── Sidebar mobile toggle ──────────────────────────────────────────────────
// Muestra/oculta el sidebar y el overlay en modo móvil.
window.sidebarManager = {
    toggle: function (open) {
        var sidebar = document.querySelector('.sidebar');
        var overlay = document.querySelector('.sidebar-overlay');
        if (sidebar) sidebar.classList.toggle('show', !!open);
        if (overlay) overlay.classList.toggle('visible', !!open);
    }
};
