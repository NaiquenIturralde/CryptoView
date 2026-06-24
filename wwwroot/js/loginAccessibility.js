// ── Login Modal Accessibility: Focus Trap, Escape, Focus Return ──
window.loginAccessibility = (function () {
    let modalEl = null;
    let triggerEl = null;
    let focusableSelector = 'a[href], button:not([tabindex="-1"]):not([disabled]), input:not([tabindex="-1"]):not([disabled]):not([type="hidden"]), textarea, select, [tabindex]:not([tabindex="-1"])';

    function getFocusableElements() {
        if (!modalEl) return [];
        return Array.from(modalEl.querySelectorAll(focusableSelector));
    }

    function handleKeyDown(e) {
        if (!modalEl) return;

        // Escape → close (delegate to Blazor via click on close/overlay button)
        if (e.key === 'Escape') {
            e.preventDefault();
            e.stopPropagation();
            // Click the close button inside the modal to trigger Blazor's @onclick
            var closeBtn = modalEl.querySelector('.login-modal-close');
            if (closeBtn) closeBtn.click();
            return;
        }

        // Tab trap
        if (e.key === 'Tab') {
            const focusable = getFocusableElements();
            if (focusable.length === 0) {
                e.preventDefault();
                return;
            }

            const first = focusable[0];
            const last = focusable[focusable.length - 1];

            if (e.shiftKey) {
                if (document.activeElement === first) {
                    e.preventDefault();
                    last.focus();
                }
            } else {
                if (document.activeElement === last) {
                    e.preventDefault();
                    first.focus();
                }
            }
        }
    }

    function openModal(modalId, triggerId) {
        modalEl = document.getElementById(modalId);
        triggerEl = document.getElementById(triggerId);

        document.addEventListener('keydown', handleKeyDown, true);

        // Focus first interactive element in modal
        setTimeout(function () {
            const focusable = getFocusableElements();
            if (focusable.length > 0) {
                focusable[0].focus();
            }
        }, 100);
    }

    function closeModal() {
        document.removeEventListener('keydown', handleKeyDown, true);

        // Return focus to trigger element
        if (triggerEl) {
            triggerEl.focus();
        }

        modalEl = null;
        triggerEl = null;
    }

    function cleanup() {
        document.removeEventListener('keydown', handleKeyDown, true);
        modalEl = null;
        triggerEl = null;
    }

    return {
        open: openModal,
        close: closeModal,
        cleanup: cleanup
    };
})();
