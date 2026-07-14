window.cryptoViewLanguage = (function () {
    var VALID = { es: true, en: true };
    var KEY = 'cv_language';
    var DEFAULT = 'es';

    function get() {
        try {
            var v = localStorage.getItem(KEY);
            if (v && VALID[v]) return v;
        } catch (e) { /* localStorage blocked */ }
        return DEFAULT;
    }

    function set(lang) {
        if (!lang || !VALID[lang]) lang = DEFAULT;
        try { localStorage.setItem(KEY, lang); } catch (e) { /* no-op */ }
    }

    function applyToDocument(lang) {
        if (!lang || !VALID[lang]) lang = DEFAULT;
        document.documentElement.setAttribute('lang', lang);
    }

    return { get: get, set: set, applyToDocument: applyToDocument };
})();
