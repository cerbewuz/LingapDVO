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
    const CHECK_INTERVAL = 30 * 1000; // Check session validity every 30 seconds

    let inactivityTimer = null;
    let lastActivityTime = Date.now();
    let confirmationShown = false;
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
        // Don't reset if confirmation is already shown
        if (confirmationShown) return;

        lastActivityTime = Date.now();

        // Clear existing timer
        if (inactivityTimer) {
            clearTimeout(inactivityTimer);
        }

        // Set inactivity timer (10 minutes)
        inactivityTimer = setTimeout(() => {
            showInactivityConfirmation();
        }, INACTIVITY_TIMEOUT);
    }

    // Show inactivity confirmation modal
    function showInactivityConfirmation() {
        if (confirmationShown) return;
        confirmationShown = true;

        console.log('Session Timeout: User inactive for 10 minutes - showing confirmation');

        // Blur and disable page content
        const mainContent = document.querySelector('body');
        if (mainContent) {
            mainContent.style.filter = 'blur(5px)';
            mainContent.style.pointerEvents = 'none';
            mainContent.style.userSelect = 'none';
        }

        // Create confirmation modal
        const modalHTML = `
            <div id="inactivity-confirmation-modal" class="fixed inset-0 z-[9999] flex items-center justify-center" style="background: rgba(0,0,0,0.8); pointer-events: auto;">
                <div class="bg-white rounded-2xl shadow-2xl max-w-lg w-full mx-4 p-8 modal-scale-in">
                    <div class="text-center">
                        <div class="mx-auto flex items-center justify-center h-20 w-20 rounded-full bg-red-100 mb-6">
                            <i class="fas fa-clock text-4xl text-red-600"></i>
                        </div>
                        <h3 class="text-2xl font-bold text-gray-900 mb-4">Session Timeout</h3>
                        <p class="text-gray-700 mb-2 text-lg">
                            You have been inactive for <strong>10 minutes</strong>.
                        </p>
                        <p class="text-gray-600 mb-8">
                            For your security, you will now be logged out and redirected to the homepage.
                        </p>
                        <button onclick="window.sessionTimeout.confirmLogout()"
                                class="w-full bg-crimson-500 hover:bg-crimson-600 text-white font-bold py-4 px-8 rounded-xl transition-all duration-300 transform hover:scale-105 shadow-lg">
                            <i class="fas fa-sign-out-alt mr-2"></i>
                            Confirm Logout
                        </button>
                    </div>
                </div>
            </div>
            <style>
                .bg-crimson-500 { background-color: #DC143C; }
                .bg-crimson-600 { background-color: #b01030; }
                .modal-scale-in {
                    opacity: 0;
                    transform: scale(0.9);
                    transition: opacity 0.3s ease-out, transform 0.3s ease-out;
                }
                #inactivity-confirmation-modal .modal-scale-in {
                    opacity: 1;
                    transform: scale(1);
                }
            </style>
        `;

        document.body.insertAdjacentHTML('beforeend', modalHTML);

        // Trigger animation
        setTimeout(() => {
            const modal = document.querySelector('#inactivity-confirmation-modal .modal-scale-in');
            if (modal) {
                modal.style.opacity = '1';
                modal.style.transform = 'scale(1)';
            }
        }, 10);

        // Stop session check
        if (sessionCheckInterval) {
            clearInterval(sessionCheckInterval);
        }
    }

    // Confirm logout and redirect
    function confirmLogout() {
        console.log('Session Timeout: Logout confirmed - redirecting to landing page');

        // Clear session storage
        sessionStorage.clear();

        // Redirect to landing page
        window.location.href = '/Landingpage';
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
                console.log('Session Timeout: Server session expired - showing confirmation');
                showInactivityConfirmation();
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
        confirmLogout: confirmLogout,
        manualLogout: function() {
            if (inactivityTimer) clearTimeout(inactivityTimer);
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
