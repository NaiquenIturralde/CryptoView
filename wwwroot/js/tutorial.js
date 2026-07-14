// ── Tutorial Storage: localStorage persistence for tutorial seen-state ──
window.tutorialStorage = {
    /// Checks if a key exists in localStorage.
    hasSeen: function (key) {
        try {
            return localStorage.getItem(key) === "1";
        } catch (e) {
            return false;
        }
    },

    /// Marks a tutorial as seen.
    markSeen: function (key) {
        try {
            localStorage.setItem(key, "1");
        } catch (e) {
            console.warn("[tutorialStorage] Error marking seen:", e);
        }
    },

    /// Removes a single tutorial key.
    remove: function (key) {
        try {
            localStorage.removeItem(key);
        } catch (e) {
            console.warn("[tutorialStorage] Error removing key:", e);
        }
    },

    /// Returns the bounding rect of a DOM element, or null if not found.
    getTargetRect: function (selector) {
        try {
            var el = document.querySelector(selector);
            if (!el) return null;
            var rect = el.getBoundingClientRect();
            return {
                top: rect.top,
                left: rect.left,
                width: rect.width,
                height: rect.height,
                bottom: rect.bottom,
                right: rect.right
            };
        } catch (e) {
            return null;
        }
    },

    /// Scrolls an element into view smoothly and returns its rect after scroll.
    scrollIntoView: function (selector) {
        try {
            var el = document.querySelector(selector);
            if (!el) return null;
            el.scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'nearest' });
            // Return rect after a short delay for scroll to settle
            return new Promise(function (resolve) {
                setTimeout(function () {
                    var rect = el.getBoundingClientRect();
                    resolve({
                        top: rect.top,
                        left: rect.left,
                        width: rect.width,
                        height: rect.height,
                        bottom: rect.bottom,
                        right: rect.right
                    });
                }, 350);
            });
        } catch (e) {
            return null;
        }
    },

    /// Sets focus on an element for accessibility.
    focusElement: function (selector) {
        try {
            var el = document.querySelector(selector);
            if (el) {
                el.setAttribute('tabindex', '-1');
                el.focus({ preventScroll: true });
            }
        } catch (e) { /* non-critical */ }
    },

    /// Returns viewport dimensions.
    getViewport: function () {
        return {
            width: window.innerWidth,
            height: window.innerHeight,
            isMobile: window.innerWidth < 768
        };
    }
};
