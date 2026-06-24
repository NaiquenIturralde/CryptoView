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
    /// Session expiry time in milliseconds (24 hours)
    _sessionTimeout: 24 * 60 * 60 * 1000,

    /// Persists the authenticated user as JSON and keeps legacy keys in sync.
    /// Includes timestamp for session validation.
    setUser: function (userObj) {
        try {
            // Ensure timestamp is set
            if (!userObj.timestamp) {
                userObj.timestamp = Date.now();
            }
            localStorage.setItem("currentUser", JSON.stringify(userObj));
            localStorage.setItem("user",     userObj.username || "");
            localStorage.setItem("userRole", userObj.role     || "");
            localStorage.setItem("sessionCreated", userObj.timestamp.toString());
        } catch (e) {
            console.warn("[sessionManager] No se pudo guardar usuario:", e);
        }
    },

    /// Returns the full user object, or null if not logged in or session expired.
    getUser: function () {
        try {
            var json = localStorage.getItem("currentUser");
            if (!json) return null;
            
            var obj = JSON.parse(json);
            
            // Validate session hasn't expired
            if (obj.timestamp) {
                var now = Date.now();
                var sessionAge = now - obj.timestamp;
                if (sessionAge > this._sessionTimeout) {
                    // Session expired
                    this.logout();
                    return null;
                }
            }
            
            return obj;
        } catch (e) {
            console.warn("[sessionManager] Error parsing user:", e);
            return null;
        }
    },

    /// Returns just the username string for easy C# interop.
    /// Falls back to the legacy "user" key so sessions created before
    /// this update are still recognised after a page refresh.
    getUsername: function () {
        try {
            var json = localStorage.getItem("currentUser");
            if (json) {
                var obj = JSON.parse(json);
                if (obj.username) {
                    // Validate session hasn't expired
                    if (obj.timestamp) {
                        var now = Date.now();
                        var sessionAge = now - obj.timestamp;
                        if (sessionAge > this._sessionTimeout) {
                            // Session expired
                            this.logout();
                            return "";
                        }
                    }
                    return obj.username;
                }
            }
            return localStorage.getItem("user") || "";
        } catch (e) {
            console.warn("[sessionManager] Error getting username:", e);
            return localStorage.getItem("user") || "";
        }
    },

    /// Returns whether there is a valid, non-expired session
    isSessionValid: function () {
        try {
            var user = this.getUser();
            return user !== null && user.id && user.username;
        } catch (e) {
            return false;
        }
    },

    /// Checks session age and warns if close to expiry
    getSessionAge: function () {
        try {
            var created = localStorage.getItem("sessionCreated");
            if (!created) return -1;
            
            var createdTime = parseInt(created, 10);
            return Date.now() - createdTime;
        } catch (e) {
            return -1;
        }
    },

    /// Completely clears all session data from storage
    logout: function () {
        console.log("Logout ejecutado - limpiando sesión");
        localStorage.removeItem("token");
        localStorage.removeItem("user");
        localStorage.removeItem("userRole");
        localStorage.removeItem("currentUser");
        localStorage.removeItem("sessionCreated");
        sessionStorage.clear();
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
