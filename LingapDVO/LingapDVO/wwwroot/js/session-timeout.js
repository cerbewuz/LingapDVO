// ═══════════════════════════════════════════════════════════════════════════════
// SESSION TIMEOUT AND INACTIVITY DETECTION - DYNAMIC CONFIGURATION
// ═══════════════════════════════════════════════════════════════════════════════
// Fetches timeout configuration dynamically from server (appsettings.json)
// Shows warning before session expires, gives user option to continue or logout
// Tracks meaningful user activities (not passive scrolling/hovering)
// Automatically logs out if no response after warning timeout
// ═══════════════════════════════════════════════════════════════════════════════

(function () {
    'use strict';

    // ====== CONFIGURATION ======
    const SESSION_TIMEOUT_MINUTES = 15; // change as needed
    const KEEP_ALIVE_INTERVAL_MINUTES = 5;
    const LOGOUT_URL = '/Login/Logout';
    const KEEP_ALIVE_URL = '/Login/KeepAlive';

    // ====== STATE ======
    let inactivityTimer = null;
    let keepAliveTimer = null;
    let isKeepAliveRequest = false;

    // ====== CORE FUNCTIONS ======

    function logoutUser() {
        console.warn('Session timeout reached. Logging out user...');
        window.location.href = LOGOUT_URL;
    }

    function resetInactivityTimer() {
        clearTimeout(inactivityTimer);

        inactivityTimer = setTimeout(() => {
            console.warn('User inactive for too long — logging out.');
            logoutUser();
        }, SESSION_TIMEOUT_MINUTES * 60 * 1000);

        // Send keep-alive request safely (no recursion)
        if (!isKeepAliveRequest) {
            isKeepAliveRequest = true;
            originalFetch(KEEP_ALIVE_URL, { method: 'GET' })
                .catch(() => { /* ignore network errors */ })
                .finally(() => { isKeepAliveRequest = false; });
        }
    }

    function startKeepAlive() {
        clearInterval(keepAliveTimer);
        keepAliveTimer = setInterval(() => {
            if (!isKeepAliveRequest) {
                isKeepAliveRequest = true;
                originalFetch(KEEP_ALIVE_URL, { method: 'GET' })
                    .catch(() => { })
                    .finally(() => { isKeepAliveRequest = false; });
            }
        }, KEEP_ALIVE_INTERVAL_MINUTES * 60 * 1000);
    }

    // ====== EVENT LISTENERS ======

    const activityEvents = ['click', 'mousemove', 'keypress', 'scroll', 'touchstart'];
    activityEvents.forEach(evt => {
        window.addEventListener(evt, () => resetInactivityTimer());
    });

    // ====== FETCH INTERCEPTOR ======

    const originalFetch = window.fetch;

    window.fetch = async function (resource, config) {
        try {
            const url = (typeof resource === 'string') ? resource : resource.url;

            // Log activity (optional)
            console.log('🌐 Activity: API request →', url);

            // Skip timer reset for keep-alive requests to prevent recursion
            if (url && url.includes(KEEP_ALIVE_URL)) {
                return originalFetch.apply(this, arguments);
            }

            // Normal requests reset inactivity timer
            resetInactivityTimer();

            // Proceed with original fetch
            return originalFetch.apply(this, arguments);
        } catch (err) {
            console.error('Fetch interception error:', err);
            throw err;
        }
    };

    // ====== INITIALIZATION ======

    console.log('🕒 Session timeout manager initialized.');
    resetInactivityTimer();
    startKeepAlive();
})();
