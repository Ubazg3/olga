// Smart auto-refresh for the admin pages. The desktop client polls
// the server every 5 s; the web side reloads less aggressively (15 s)
// so admins don't see the page flash while reading.
//
// Skipped when:
//   * any text input / textarea / select has focus  — don't interrupt typing
//   * window.__noAutoRefresh is set                 — replay view opts out
//   * the page is hidden (background tab)           — saves work
//
// On every reload the URL is preserved including query string, so the
// "Search" box state on /Admin/Users survives an auto-refresh.
(function () {
    const INTERVAL_MS = 15000;

    function isTypingTarget(el) {
        if (!el) return false;
        const tag = (el.tagName || '').toUpperCase();
        if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return true;
        if (el.isContentEditable) return true;
        return false;
    }

    function tick() {
        if (window.__noAutoRefresh) return;
        if (document.hidden) return;
        if (isTypingTarget(document.activeElement)) return;
        // Preserve full URL — query string included.
        window.location.reload();
    }

    setInterval(tick, INTERVAL_MS);
})();
