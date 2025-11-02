// ═══════════════════════════════════════════════════════════════════════════════
// SESSION TIMEOUT AND INACTIVITY DETECTION
// ═══════════════════════════════════════════════════════════════════════════════
// Automatically logs out user after 10 minutes of inactivity
// Tracks mouse movement, keyboard input, clicks, scrolling, and touch events
// Shows warning before logout and redirects to login with message
// ═══════════════════════════════════════════════════════════════════════════════

(function() {
    'use strict';

    // Configuration
    const INACTIVITY_TIMEOUT = 10 * 60 * 1000; // 10 minutes in milliseconds
    const WARNING_TIME = 2 * 60 * 1000; // Show warning 2 minutes before timeout
    const CHECK_INTERVAL = 30 * 1000; // Check session validity every 30 seconds

    let inactivityTimer = null;
    let warningTimer = null;
    let lastActivityTime = Date.now();
    let warningShown = false;
    let sessionCheckInterval = null;

    // Initialize session timeout tracker
    function initSessionTimeout() {
        console.log('Session Timeout: Initialized with 10-minute inactivity detection');

        // Reset activity on user interaction
        const activityEvents = [
            'mousedown',
            'mousemove',
            'keypress',
            'scroll',
            'touchstart',
            'click',
            'keydown'
        ];

        // Add event listeners for all activity types
        activityEvents.forEach(event => {
            document.addEventListener(event, resetInactivityTimer, true);
        });

        // Start the inactivity timer
        resetInactivityTimer();

        // Start periodic session check
        startSessionCheck();

        console.log('Session Timeout: Activity tracking started');
    }

    // Reset the inactivity timer
    function resetInactivityTimer() {
        lastActivityTime = Date.now();

        // Clear existing timers
        if (inactivityTimer) {
            clearTimeout(inactivityTimer);
        }
        if (warningTimer) {
            clearTimeout(warningTimer);
        }

        // Hide warning if shown
        if (warningShown) {
            hideWarning();
            warningShown = false;
        }

        // Set warning timer (8 minutes - shows warning 2 minutes before logout)
        warningTimer = setTimeout(() => {
            showWarning();
        }, INACTIVITY_TIMEOUT - WARNING_TIME);

        // Set inactivity timer (10 minutes)
        inactivityTimer = setTimeout(() => {
            logoutDueToInactivity();
        }, INACTIVITY_TIMEOUT);
    }

    // Show warning modal before logout
    function showWarning() {
        if (warningShown) return;
        warningShown = true;

        console.log('Session Timeout: Showing inactivity warning');

        // Create warning modal
        const modalHTML = `
            <div id="inactivity-warning-modal" class="fixed inset-0 z-[9999] flex items-center justify-center" style="background: rgba(0,0,0,0.5);">
                <div class="bg-white rounded-lg shadow-2xl max-w-md w-full mx-4 p-6 animate-fade-in">
                    <div class="text-center">
                        <div class="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-yellow-100 mb-4">
                            <i class="fas fa-exclamation-triangle text-3xl text-yellow-600"></i>
                        </div>
                        <h3 class="text-xl font-bold text-gray-900 mb-2">Session Expiring Soon</h3>
                        <p class="text-gray-600 mb-6">
                            You will be logged out in <strong id="warning-countdown">2:00</strong> due to inactivity.
                        </p>
                        <p class="text-sm text-gray-500 mb-6">
                            Move your mouse or press any key to stay logged in.
                        </p>
                        <button onclick="window.sessionTimeout.dismiss()"
                                class="w-full bg-crimson-500 hover:bg-crimson-600 text-white font-semibold py-3 px-6 rounded-lg transition-colors duration-300">
                            Stay Logged In
                        </button>
                    </div>
                </div>
            </div>
            <style>
                .bg-crimson-500 { background-color: #DC143C; }
                .bg-crimson-600 { background-color: #b01030; }
                @keyframes fade-in {
                    from { opacity: 0; transform: scale(0.95); }
                    to { opacity: 1; transform: scale(1); }
                }
                .animate-fade-in {
                    animation: fade-in 0.3s ease-out;
                }
            </style>
        `;

        document.body.insertAdjacentHTML('beforeend', modalHTML);

        // Start countdown
        startWarningCountdown();
    }

    // Hide warning modal
    function hideWarning() {
        const modal = document.getElementById('inactivity-warning-modal');
        if (modal) {
            modal.remove();
        }
        warningShown = false;
        console.log('Session Timeout: Warning dismissed');
    }

    // Start countdown in warning modal
    function startWarningCountdown() {
        const countdownElement = document.getElementById('warning-countdown');
        if (!countdownElement) return;

        let remainingSeconds = 120; // 2 minutes

        const countdownInterval = setInterval(() => {
            remainingSeconds--;

            if (remainingSeconds <= 0 || !warningShown) {
                clearInterval(countdownInterval);
                return;
            }

            const minutes = Math.floor(remainingSeconds / 60);
            const seconds = remainingSeconds % 60;
            countdownElement.textContent = `${minutes}:${seconds.toString().padStart(2, '0')}`;
        }, 1000);
    }

    // Logout user due to inactivity
    function logoutDueToInactivity() {
        console.log('Session Timeout: User inactive for 10 minutes - logging out');

        // Stop session check
        if (sessionCheckInterval) {
            clearInterval(sessionCheckInterval);
        }

        // Show loading message
        showLogoutMessage();

        // Perform logout
        setTimeout(() => {
            // Clear session storage
            sessionStorage.clear();

            // Redirect to login with timeout message
            window.location.href = '/Login?timeout=true';
        }, 1000);
    }

    // Show logout message
    function showLogoutMessage() {
        const messageHTML = `
            <div id="logout-message" class="fixed inset-0 z-[9999] flex items-center justify-center" style="background: rgba(0,0,0,0.7);">
                <div class="bg-white rounded-lg shadow-2xl max-w-md w-full mx-4 p-8 text-center">
                    <div class="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-red-100 mb-4">
                        <i class="fas fa-sign-out-alt text-3xl text-red-600"></i>
                    </div>
                    <h3 class="text-2xl font-bold text-gray-900 mb-3">Logging Out...</h3>
                    <p class="text-gray-600">
                        You have been logged out due to inactivity.
                    </p>
                    <div class="mt-4">
                        <i class="fas fa-spinner fa-spin text-3xl text-gray-400"></i>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', messageHTML);
    }

    // Check session validity with server
    function checkSessionValidity() {
        fetch('/Login/CheckSession', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        })
        .then(response => response.json())
        .then(data => {
            if (!data.isValid) {
                console.log('Session Timeout: Server session expired - logging out');
                logoutDueToInactivity();
            }
        })
        .catch(error => {
            console.error('Session Timeout: Error checking session:', error);
        });
    }

    // Start periodic session checking
    function startSessionCheck() {
        sessionCheckInterval = setInterval(() => {
            checkSessionValidity();
        }, CHECK_INTERVAL);

        console.log('Session Timeout: Periodic session check started (every 30 seconds)');
    }

    // Public API
    window.sessionTimeout = {
        dismiss: function() {
            resetInactivityTimer();
            hideWarning();
        },
        manualLogout: function() {
            if (inactivityTimer) clearTimeout(inactivityTimer);
            if (warningTimer) clearTimeout(warningTimer);
            if (sessionCheckInterval) clearInterval(sessionCheckInterval);
            console.log('Session Timeout: Manual logout initiated');
        }
    };

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initSessionTimeout);
    } else {
        initSessionTimeout();
    }

    // Log when page is about to unload
    window.addEventListener('beforeunload', () => {
        if (inactivityTimer) clearTimeout(inactivityTimer);
        if (warningTimer) clearTimeout(warningTimer);
        if (sessionCheckInterval) clearInterval(sessionCheckInterval);
    });

    console.log('Session Timeout: Module loaded successfully');
})();
