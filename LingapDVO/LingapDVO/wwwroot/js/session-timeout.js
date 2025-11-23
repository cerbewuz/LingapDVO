// ═══════════════════════════════════════════════════════════════════════════════
// SESSION TIMEOUT AND INACTIVITY DETECTION
// ═══════════════════════════════════════════════════════════════════════════════
// Configuration is loaded from server (appsettings.json) via /Login/GetSessionConfig
// Default behavior: Warning modal at 10 minutes, Force logout at 12 minutes
// User can extend session by clicking "Continue Session" or by any activity
// ═══════════════════════════════════════════════════════════════════════════════

(function () {
    'use strict';

    // ====== CONFIGURATION (will be loaded from server) ======
    let WARNING_TIME_MINUTES = 10;      // Will be updated from server config
    let FORCE_LOGOUT_MINUTES = 12;      // Will be updated from server config
    let COUNTDOWN_DURATION_SECONDS = 120; // Will be calculated from server config
    let KEEP_ALIVE_INTERVAL_MINUTES = 5;  // Will be updated from server config
    let LOGOUT_URL = '/Login/Logout';
    let KEEP_ALIVE_URL = '/Login/KeepAlive';
    let CONFIG_LOADED = false;

    // ====== STATE ======
    let warningTimer = null;
    let logoutTimer = null;
    let keepAliveTimer = null;
    let countdownInterval = null;
    let isKeepAliveRequest = false;
    let modalShown = false;

    // ====== DOM ELEMENTS ======
    const modal = document.getElementById('sessionWarningModal');
    const countdownElement = document.getElementById('sessionCountdown');
    const continueBtn = document.getElementById('continueSessionBtn');
    const logoutBtn = document.getElementById('logoutNowBtn');

    // ====== CONFIGURATION LOADING ======

    async function loadServerConfiguration() {
        try {
            const response = await fetch('/Login/GetSessionConfig', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error(`Server returned ${response.status}`);
            }

            const config = await response.json();

            // Update configuration from server
            WARNING_TIME_MINUTES = config.warningTimeMinutes || 10;
            FORCE_LOGOUT_MINUTES = config.forceLogoutMinutes || 12;
            COUNTDOWN_DURATION_SECONDS = config.countdownDurationSeconds || 120;
            KEEP_ALIVE_INTERVAL_MINUTES = config.keepAliveIntervalMinutes || 5;
            LOGOUT_URL = config.logoutUrl || '/Login/Logout';
            KEEP_ALIVE_URL = config.keepAliveUrl || '/Login/KeepAlive';

            CONFIG_LOADED = true;


            return true;
        } catch (error) {
            CONFIG_LOADED = true; // Mark as loaded to continue with defaults
            return false;
        }
    }

    // ====== CORE FUNCTIONS ======

    function logoutUser(showToast = false) {

        // Store flag to show toast on login page
        if (showToast) {
            sessionStorage.setItem('sessionTimedOut', 'true');
        }

        window.location.href = LOGOUT_URL;
    }

    function showWarningModal() {
        if (modalShown) return;

        modalShown = true;
        modal.style.display = 'flex';
        startCountdown();

        // Set logout timer for remaining time
        logoutTimer = setTimeout(() => {
            hideWarningModal();
            logoutUser(true);
        }, COUNTDOWN_DURATION_SECONDS * 1000);
    }

    function hideWarningModal() {
        modalShown = false;
        modal.style.display = 'none';
        stopCountdown();
        clearTimeout(logoutTimer);
    }

    function startCountdown() {
        let remainingSeconds = COUNTDOWN_DURATION_SECONDS;
        updateCountdownDisplay(remainingSeconds);

        countdownInterval = setInterval(() => {
            remainingSeconds--;
            updateCountdownDisplay(remainingSeconds);

            if (remainingSeconds <= 0) {
                stopCountdown();
            }
        }, 1000);
    }

    function stopCountdown() {
        if (countdownInterval) {
            clearInterval(countdownInterval);
            countdownInterval = null;
        }
    }

    function updateCountdownDisplay(seconds) {
        const minutes = Math.floor(seconds / 60);
        const secs = seconds % 60;
        countdownElement.textContent = `${minutes}:${secs.toString().padStart(2, '0')}`;
    }

    function resetInactivityTimer() {
        // Clear all timers
        clearTimeout(warningTimer);
        clearTimeout(logoutTimer);

        // Hide modal if shown
        if (modalShown) {
            hideWarningModal();
        }

        // Set new warning timer
        warningTimer = setTimeout(() => {
            showWarningModal();
        }, WARNING_TIME_MINUTES * 60 * 1000);

        // Send keep-alive request safely (no recursion)
        if (!isKeepAliveRequest) {
            isKeepAliveRequest = true;
            originalFetch(KEEP_ALIVE_URL, { method: 'GET' })
                .then(() => {
                })
                .catch((err) => {
                })
                .finally(() => {
                    isKeepAliveRequest = false;
                });
        }
    }

    function continueSession() {
        hideWarningModal();
        resetInactivityTimer();
    }

    function logoutNow() {
        hideWarningModal();
        logoutUser(false);
    }

    function startKeepAlive() {
        clearInterval(keepAliveTimer);
        keepAliveTimer = setInterval(() => {
            if (!isKeepAliveRequest && !modalShown) {
                isKeepAliveRequest = true;
                originalFetch(KEEP_ALIVE_URL, { method: 'GET' })
                    .then(() => {
                    })
                    .catch((err) => {
                    })
                    .finally(() => {
                        isKeepAliveRequest = false;
                    });
            }
        }, KEEP_ALIVE_INTERVAL_MINUTES * 60 * 1000);
    }

    // ====== EVENT LISTENERS ======

    // ⚡ PERFORMANCE: Debounce helper function to limit event handler calls
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    // User activity tracking with debouncing (1 second delay to reduce overhead)
    // Previous: Every mousemove triggered handler, causing performance issues
    const activityEvents = ['click', 'mousemove', 'keypress', 'scroll', 'touchstart'];
    const debouncedResetTimer = debounce(() => {
        if (!modalShown && CONFIG_LOADED) {
            resetInactivityTimer();
        }
    }, 1000); // Debounce by 1 second

    activityEvents.forEach(evt => {
        window.addEventListener(evt, debouncedResetTimer, { passive: true });
    });

    // Modal button handlers
    if (continueBtn) {
        continueBtn.addEventListener('click', continueSession);
    }

    if (logoutBtn) {
        logoutBtn.addEventListener('click', logoutNow);
    }

    // ====== FETCH INTERCEPTOR ======

    const originalFetch = window.fetch;

    window.fetch = async function (resource, config) {
        try {
            const url = (typeof resource === 'string') ? resource : resource.url;

            // Skip timer reset for keep-alive and config requests to prevent recursion
            if (url && (url.includes(KEEP_ALIVE_URL) || url.includes('/GetSessionConfig'))) {
                return originalFetch.apply(this, arguments);
            }

            // Normal requests reset inactivity timer
            if (!modalShown && CONFIG_LOADED) {
                resetInactivityTimer();
            }

            // Proceed with original fetch
            return originalFetch.apply(this, arguments);
        } catch (err) {
            throw err;
        }
    };

    // ====== INITIALIZATION ======

    async function initialize() {

        // Load configuration from server first
        const configLoaded = await loadServerConfiguration();

        if (!configLoaded) {
        }


        resetInactivityTimer();
        startKeepAlive();

        // Show toast on login page if session timed out
        if (window.location.pathname === '/Login/Login' || window.location.pathname === '/') {
            const timedOut = sessionStorage.getItem('sessionTimedOut');
            if (timedOut === 'true') {
                sessionStorage.removeItem('sessionTimedOut');
                setTimeout(() => {
                    showSessionTimeoutToast();
                }, 500);
            }
        }
    }

    function showSessionTimeoutToast() {
        // Create toast container if it doesn't exist
        let toastContainer = document.getElementById('toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'toast-container';
            toastContainer.style.cssText = `
                position: fixed;
                top: 20px;
                right: 20px;
                z-index: 10000;
                display: flex;
                flex-direction: column;
                gap: 10px;
            `;
            document.body.appendChild(toastContainer);
        }

        // Create toast element
        const toast = document.createElement('div');
        toast.className = 'session-timeout-toast';
        toast.innerHTML = `
            <div style="
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                border-radius: 12px;
                padding: 20px;
                box-shadow: 0 10px 40px rgba(102, 126, 234, 0.4);
                color: white;
                animation: slideInRight 0.5s ease-out;
                min-width: 350px;
            ">
                <div style="display: flex; align-items: start; gap: 15px;">
                    <div style="
                        background: rgba(255, 255, 255, 0.2);
                        border-radius: 50%;
                        width: 48px;
                        height: 48px;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        flex-shrink: 0;
                    ">
                        <i class="fas fa-clock" style="font-size: 24px;"></i>
                    </div>
                    <div style="flex: 1;">
                        <div style="font-weight: 700; font-size: 16px; margin-bottom: 5px;">
                            <strong>Session Expired</strong>
                        </div>
                        <div style="font-size: 14px; opacity: 0.95; line-height: 1.4;">
                            You have been logged out due to inactivity.
                        </div>
                    </div>
                    <button onclick="this.closest('.session-timeout-toast').remove()"
                            style="
                                background: rgba(255, 255, 255, 0.2);
                                border: none;
                                color: white;
                                width: 28px;
                                height: 28px;
                                border-radius: 50%;
                                cursor: pointer;
                                display: flex;
                                align-items: center;
                                justify-content: center;
                                transition: background 0.2s;
                                flex-shrink: 0;
                            "
                            onmouseover="this.style.background='rgba(255, 255, 255, 0.3)'"
                            onmouseout="this.style.background='rgba(255, 255, 255, 0.2)'">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
            </div>
        `;

        toastContainer.appendChild(toast);

        // Auto-remove toast after 5 seconds
        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transition = 'opacity 0.5s ease';
            setTimeout(() => toast.remove(), 500);
        }, 5000);
    }

    // Add CSS for slideInRight animation
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideInRight {
            from {
                opacity: 0;
                transform: translateX(100%);
            }
            to {
                opacity: 1;
                transform: translateX(0);
            }
        }
    `;
    document.head.appendChild(style);

    // Start initialization when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }
})();
