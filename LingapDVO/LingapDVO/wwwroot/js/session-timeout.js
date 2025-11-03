// ═══════════════════════════════════════════════════════════════════════════════
// SESSION TIMEOUT AND INACTIVITY DETECTION - ASYNCHRONOUS WITH WARNING
// ═══════════════════════════════════════════════════════════════════════════════
// Shows warning at 9 minutes, gives user option to continue or logout
// Tracks mouse movement, keyboard input, clicks, scrolling, and touch events
// Automatically logs out if no response after warning timeout
// ═══════════════════════════════════════════════════════════════════════════════

(function() {
    'use strict';

    // Configuration
    const WARNING_TIMEOUT = 9 * 60 * 1000; // Show warning at 9 minutes
    const FINAL_TIMEOUT = 10 * 60 * 1000; // Force logout at 10 minutes
    const CHECK_INTERVAL = 30 * 1000; // Check session validity every 30 seconds
    const COUNTDOWN_INTERVAL = 1000; // Update countdown every second

    let inactivityTimer = null;
    let warningTimer = null;
    let countdownTimer = null;
    let lastActivityTime = Date.now();
    let warningShown = false;
    let sessionCheckInterval = null;

    // Initialize session timeout tracker
    function initSessionTimeout() {
        console.log('Session Timeout: Initialized with meaningful activity tracking');
        console.log('Session Timeout: Tracks transactions, navigation, form submissions only');

        // Track MEANINGFUL user activities only (no passive scrolling/mouse movement)
        trackMeaningfulActivities();

        // Start the inactivity timer
        resetInactivityTimer();

        // Start periodic session check
        startSessionCheck();

        console.log('Session Timeout: Meaningful activity tracking started');
    }

    // Track only meaningful user activities (transactions, navigation, forms)
    function trackMeaningfulActivities() {
        console.log('===========================================');
        console.log('MEANINGFUL ACTIVITY TRACKING ENABLED');
        console.log('===========================================');
        console.log('Tracked Activities:');
        console.log('  ✓ Page Navigation (clicking links)');
        console.log('  ✓ Form Submissions');
        console.log('  ✓ Button Clicks (submit, save, etc.)');
        console.log('  ✓ Input Field Interactions (typing in forms)');
        console.log('  ✓ File Uploads');
        console.log('  ✓ AJAX Requests (API calls)');
        console.log('-------------------------------------------');
        console.log('NOT Tracked (Passive):');
        console.log('  ✗ Mouse movements');
        console.log('  ✗ Scrolling');
        console.log('  ✗ Hovering');
        console.log('  ✗ Reading without interaction');
        console.log('===========================================\n');

        // ═══════════════════════════════════════════════════════════════════════
        // 1. TRACK PAGE NAVIGATION (Link Clicks)
        // ═══════════════════════════════════════════════════════════════════════
        document.addEventListener('click', function(e) {
            // Skip if warning is shown
            if (warningShown) return;

            const target = e.target.closest('a, button[type="button"], button[type="submit"]');

            if (target) {
                // Check if it's a navigation link or meaningful button
                if (target.tagName === 'A' && target.href && !target.href.startsWith('javascript:')) {
                    console.log('📍 Activity: Page navigation → ' + target.href);
                    resetInactivityTimer();
                } else if (target.tagName === 'BUTTON') {
                    const buttonText = target.textContent.trim().toLowerCase();
                    // Only count meaningful buttons (not close/cancel/etc)
                    const meaningfulButtons = ['submit', 'save', 'update', 'delete', 'send', 'confirm', 'proceed', 'apply', 'upload', 'download'];
                    if (meaningfulButtons.some(keyword => buttonText.includes(keyword))) {
                        console.log('🔘 Activity: Button clicked → ' + target.textContent.trim());
                        resetInactivityTimer();
                    }
                }
            }
        }, true);

        // ═══════════════════════════════════════════════════════════════════════
        // 2. TRACK FORM SUBMISSIONS
        // ═══════════════════════════════════════════════════════════════════════
        document.addEventListener('submit', function(e) {
            if (warningShown) return;

            const form = e.target;
            const formName = form.name || form.id || 'unnamed form';
            console.log('📝 Activity: Form submitted → ' + formName);
            resetInactivityTimer();
        }, true);

        // ═══════════════════════════════════════════════════════════════════════
        // 3. TRACK INPUT FIELD INTERACTIONS (Typing in forms)
        // ═══════════════════════════════════════════════════════════════════════
        let inputTimer = null;
        document.addEventListener('input', function(e) {
            if (warningShown) return;

            const target = e.target;
            // Only track meaningful inputs (text fields, textareas, selects)
            if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.tagName === 'SELECT') {
                // Debounce: only reset timer if user has been typing for 3 seconds
                clearTimeout(inputTimer);
                inputTimer = setTimeout(() => {
                    const fieldName = target.name || target.id || target.placeholder || 'input field';
                    console.log('⌨️  Activity: User typing in → ' + fieldName);
                    resetInactivityTimer();
                }, 3000); // 3-second debounce
            }
        }, true);

        // ═══════════════════════════════════════════════════════════════════════
        // 4. TRACK FILE UPLOADS
        // ═══════════════════════════════════════════════════════════════════════
        document.addEventListener('change', function(e) {
            if (warningShown) return;

            const target = e.target;
            if (target.tagName === 'INPUT' && target.type === 'file' && target.files.length > 0) {
                console.log('📤 Activity: File uploaded → ' + target.files[0].name);
                resetInactivityTimer();
            }
        }, true);

        // ═══════════════════════════════════════════════════════════════════════
        // 5. TRACK AJAX/FETCH REQUESTS (API Calls)
        // ═══════════════════════════════════════════════════════════════════════
        // Intercept fetch API
        const originalFetch = window.fetch;
        window.fetch = function(...args) {
            if (!warningShown) {
                const url = typeof args[0] === 'string' ? args[0] : args[0].url;
                console.log('🌐 Activity: API request → ' + url);
                resetInactivityTimer();
            }
            return originalFetch.apply(this, args);
        };

        // Intercept XMLHttpRequest
        const originalXHROpen = XMLHttpRequest.prototype.open;
        XMLHttpRequest.prototype.open = function(method, url) {
            if (!warningShown) {
                this.addEventListener('loadstart', function() {
                    console.log('🌐 Activity: XHR request → ' + url);
                    resetInactivityTimer();
                });
            }
            return originalXHROpen.apply(this, arguments);
        };

        // ═══════════════════════════════════════════════════════════════════════
        // 6. TRACK VISIBILITY CHANGES (Tab switching back to page)
        // ═══════════════════════════════════════════════════════════════════════
        document.addEventListener('visibilitychange', function() {
            if (!warningShown && document.visibilityState === 'visible') {
                console.log('👁️  Activity: User returned to tab');
                resetInactivityTimer();
            }
        });

        console.log('✅ All meaningful activity trackers initialized');
    }

    // Reset the inactivity timer
    function resetInactivityTimer() {
        // Don't reset if warning is already shown (user must decide)
        if (warningShown) return;

        lastActivityTime = Date.now();

        // Clear existing timers
        if (warningTimer) {
            clearTimeout(warningTimer);
        }
        if (inactivityTimer) {
            clearTimeout(inactivityTimer);
        }

        // Set warning timer (9 minutes)
        warningTimer = setTimeout(() => {
            showSessionWarning();
        }, WARNING_TIMEOUT);

        // Set final logout timer (10 minutes)
        inactivityTimer = setTimeout(() => {
            forceLogout();
        }, FINAL_TIMEOUT);
    }

    // Show session timeout warning (at 9 minutes)
    function showSessionWarning() {
        if (warningShown) return;
        warningShown = true;

        console.log('Session Timeout: Showing warning at 9 minutes - user has 60 seconds to respond');

        // Blur and disable page content
        const mainContent = document.querySelector('body');
        if (mainContent) {
            mainContent.style.filter = 'blur(5px)';
            mainContent.style.pointerEvents = 'none';
            mainContent.style.userSelect = 'none';
        }

        // Calculate remaining time
        let remainingSeconds = 60; // 1 minute remaining

        // Create warning modal
        const modalHTML = `
            <div id="session-warning-modal" class="fixed inset-0 z-[9999] flex items-center justify-center" style="background: rgba(0,0,0,0.8); pointer-events: auto;">
                <div class="bg-white rounded-2xl shadow-2xl max-w-lg w-full mx-4 p-8 modal-scale-in">
                    <div class="text-center">
                        <div class="mx-auto flex items-center justify-center h-20 w-20 rounded-full bg-yellow-100 mb-6">
                            <i class="fas fa-exclamation-triangle text-4xl text-yellow-600"></i>
                        </div>
                        <h3 class="text-2xl font-bold text-gray-900 mb-4">Session Timeout Warning</h3>
                        <p class="text-gray-700 mb-4 text-lg leading-relaxed">
                            Your session is about to expire due to inactivity.<br>
                            Would you like to stay signed in?
                        </p>
                        <div class="bg-gray-100 rounded-lg p-4 mb-6">
                            <p class="text-sm text-gray-600 mb-1">Time remaining:</p>
                            <p id="countdown-timer" class="text-3xl font-bold text-crimson-600">60s</p>
                        </div>
                        <div class="flex gap-3">
                            <button onclick="window.sessionTimeout.staySignedIn()"
                                    class="flex-1 bg-green-600 hover:bg-green-700 text-white font-bold py-4 px-6 rounded-xl transition-all duration-300 transform hover:scale-105 shadow-lg">
                                <i class="fas fa-check mr-2"></i>
                                Yes
                            </button>
                            <button onclick="window.sessionTimeout.logout()"
                                    class="flex-1 bg-red-600 hover:bg-red-700 text-white font-bold py-4 px-6 rounded-xl transition-all duration-300 transform hover:scale-105 shadow-lg">
                                <i class="fas fa-times mr-2"></i>
                                No
                            </button>
                        </div>
                    </div>
                </div>
            </div>
            <style>
                .modal-scale-in {
                    opacity: 0;
                    transform: scale(0.9);
                    transition: opacity 0.3s ease-out, transform 0.3s ease-out;
                }
                #session-warning-modal .modal-scale-in {
                    opacity: 1;
                    transform: scale(1);
                }
                .text-crimson-600 {
                    color: #DC143C;
                }
            </style>
        `;

        document.body.insertAdjacentHTML('beforeend', modalHTML);

        // Trigger animation
        setTimeout(() => {
            const modal = document.querySelector('#session-warning-modal .modal-scale-in');
            if (modal) {
                modal.style.opacity = '1';
                modal.style.transform = 'scale(1)';
            }
        }, 10);

        // Start countdown
        const countdownElement = document.getElementById('countdown-timer');
        countdownTimer = setInterval(() => {
            remainingSeconds--;
            if (countdownElement) {
                countdownElement.textContent = remainingSeconds + 's';

                // Change color as time runs out
                if (remainingSeconds <= 10) {
                    countdownElement.style.color = '#DC143C'; // Crimson
                } else if (remainingSeconds <= 30) {
                    countdownElement.style.color = '#F59E0B'; // Amber
                }
            }

            if (remainingSeconds <= 0) {
                clearInterval(countdownTimer);
            }
        }, COUNTDOWN_INTERVAL);
    }

    // Force logout (called at 10 minutes if no response)
    function forceLogout() {
        console.log('Session Timeout: Force logout - no response to warning');

        // Clear countdown if still running
        if (countdownTimer) {
            clearInterval(countdownTimer);
        }

        // Remove warning modal if present
        const warningModal = document.getElementById('session-warning-modal');
        if (warningModal) {
            warningModal.remove();
        }

        // Clear session storage
        sessionStorage.clear();

        // Redirect to landing page
        window.location.href = '/Landingpage';
    }

    // User clicked "Yes" - Stay signed in
    function staySignedIn() {
        console.log('Session Timeout: User chose to stay signed in - extending session');

        // Clear countdown timer
        if (countdownTimer) {
            clearInterval(countdownTimer);
        }

        // Remove warning modal
        const warningModal = document.getElementById('session-warning-modal');
        if (warningModal) {
            warningModal.remove();
        }

        // Restore page interaction
        const mainContent = document.querySelector('body');
        if (mainContent) {
            mainContent.style.filter = 'none';
            mainContent.style.pointerEvents = 'auto';
            mainContent.style.userSelect = 'auto';
        }

        // Reset warning flag
        warningShown = false;

        // Reset all timers - this extends the session
        resetInactivityTimer();

        // Show brief confirmation
        showBriefConfirmation('Session extended!', 'success');
    }

    // User clicked "No" - Logout
    function logout() {
        console.log('Session Timeout: User chose to logout - redirecting to landing page');

        // Clear countdown timer
        if (countdownTimer) {
            clearInterval(countdownTimer);
        }

        // Clear all timers
        if (warningTimer) clearTimeout(warningTimer);
        if (inactivityTimer) clearTimeout(inactivityTimer);
        if (sessionCheckInterval) clearInterval(sessionCheckInterval);

        // Clear session storage
        sessionStorage.clear();

        // Redirect to landing page
        window.location.href = '/Landingpage';
    }

    // Show brief confirmation message
    function showBriefConfirmation(message, type) {
        const bgColor = type === 'success' ? '#10B981' : '#3B82F6';
        const icon = type === 'success' ? 'fa-check-circle' : 'fa-info-circle';

        const confirmationHTML = `
            <div id="session-confirmation" style="position: fixed; top: 20px; right: 20px; z-index: 10000; background: ${bgColor}; color: white; padding: 16px 24px; border-radius: 12px; box-shadow: 0 10px 25px rgba(0,0,0,0.2); display: flex; align-items: center; gap: 12px; animation: slideInRight 0.3s ease-out;">
                <i class="fas ${icon} text-2xl"></i>
                <span class="font-semibold text-lg">${message}</span>
            </div>
            <style>
                @keyframes slideInRight {
                    from {
                        transform: translateX(400px);
                        opacity: 0;
                    }
                    to {
                        transform: translateX(0);
                        opacity: 1;
                    }
                }
                @keyframes slideOutRight {
                    from {
                        transform: translateX(0);
                        opacity: 1;
                    }
                    to {
                        transform: translateX(400px);
                        opacity: 0;
                    }
                }
            </style>
        `;

        document.body.insertAdjacentHTML('beforeend', confirmationHTML);

        // Auto-remove after 3 seconds
        setTimeout(() => {
            const confirmation = document.getElementById('session-confirmation');
            if (confirmation) {
                confirmation.style.animation = 'slideOutRight 0.3s ease-out';
                setTimeout(() => confirmation.remove(), 300);
            }
        }, 3000);
    }

    // Check session validity with server
    function checkSessionValidity() {
        // Don't check if warning is already shown
        if (warningShown) return;

        fetch('/Login/CheckSession', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        })
        .then(response => response.json())
        .then(data => {
            if (!data.isValid) {
                console.log('Session Timeout: Server session expired - showing warning');
                showSessionWarning();
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
        staySignedIn: staySignedIn,
        logout: logout,
        manualLogout: function() {
            if (warningTimer) clearTimeout(warningTimer);
            if (inactivityTimer) clearTimeout(inactivityTimer);
            if (countdownTimer) clearInterval(countdownTimer);
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
        if (warningTimer) clearTimeout(warningTimer);
        if (inactivityTimer) clearTimeout(inactivityTimer);
        if (countdownTimer) clearInterval(countdownTimer);
        if (sessionCheckInterval) clearInterval(sessionCheckInterval);
    });

    console.log('Session Timeout: Module loaded successfully');
})();
