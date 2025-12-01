// Real-time Priority System with Animated Counters
(function () {
    'use strict';

    let connection = null;
    let currentHighCount = 0;
    let currentMediumCount = 0;

    // Initialize SignalR connection for priority updates
    function initializePriorityConnection() {
        if (typeof signalR === 'undefined') {
                        return;
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl("/notificationHub")
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Handle priority count updates (real-time animated counters)
        connection.on("ReceivePriorityCountUpdate", function (data) {
                        // Animate counter increments
            animateCounter('high-priority-count', currentHighCount, data.highPriority);
            animateCounter('medium-priority-count', currentMediumCount, data.mediumPriority);

            // Update current counts
            currentHighCount = data.highPriority;
            currentMediumCount = data.mediumPriority;

            // Show visual notification
            showPriorityUpdateNotification(data);
        });

        // Handle new priority application notification
        connection.on("ReceiveNewPriorityApplication", function (data) {
                        // Show toast notification
            showNewPriorityToast(data);

            // Play notification sound
            playNotificationSound();

            // Increment appropriate counter with animation
            if (data.priority === 'high') {
                currentHighCount++;
                animateCounter('high-priority-count', currentHighCount - 1, currentHighCount);
                pulseElement('high-priority-count');
            } else if (data.priority === 'medium') {
                currentMediumCount++;
                animateCounter('medium-priority-count', currentMediumCount - 1, currentMediumCount);
                pulseElement('medium-priority-count');
            }

            // ⚡ PERFORMANCE: Update DOM instead of full page reload
            // If renderPriorities function exists, call it to refresh the list
            // Otherwise, the counter animation is sufficient for real-time feedback
            setTimeout(() => {
                if (typeof renderPriorities === 'function') {
                    renderPriorities(); // Call function to update DOM dynamically
                }
                // Note: Removed window.location.reload() - was wasting bandwidth and creating poor UX
            }, 1500);
        });

        // Handle admin broadcast messages
        connection.on("ReceiveAdminBroadcast", function (data) {
                        showAdminBroadcast(data.message, data.type);
        });

        // Connection lifecycle events
        connection.onreconnecting((error) => {
                        updateConnectionStatus('reconnecting');
        });

        connection.onreconnected((connectionId) => {
                        updateConnectionStatus('connected');

            // ⚡ PERFORMANCE: Update DOM instead of full page reload after reconnection
            setTimeout(() => {
                if (typeof renderPriorities === 'function') {
                    renderPriorities(); // Refresh data dynamically
                }
                // Note: Removed window.location.reload() - DOM update is more efficient
            }, 1000);
        });

        connection.onclose((error) => {
                        updateConnectionStatus('disconnected');
        });

        // Start connection
        startConnection();
    }

    // Start SignalR connection with retry logic
    function startConnection() {
        connection.start()
            .then(function () {
                                updateConnectionStatus('connected');

                // Initialize current counts from DOM
                const highEl = document.getElementById('high-priority-count');
                const mediumEl = document.getElementById('medium-priority-count');

                if (highEl) currentHighCount = parseInt(highEl.textContent) || 0;
                if (mediumEl) currentMediumCount = parseInt(mediumEl.textContent) || 0;
            })
            .catch(function (error) {
                                updateConnectionStatus('error');

                // Retry after 5 seconds
                setTimeout(startConnection, 5000);
            });
    }

    // Animate counter from old value to new value
    function animateCounter(elementId, startValue, endValue) {
        const element = document.getElementById(elementId);
        if (!element) return;

        const duration = 1000; // 1 second animation
        const startTime = Date.now();
        const difference = endValue - startValue;

        function updateCounter() {
            const currentTime = Date.now();
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);

            // Easing function (ease-out)
            const easedProgress = 1 - Math.pow(1 - progress, 3);

            const currentValue = Math.floor(startValue + (difference * easedProgress));
            element.textContent = currentValue;

            if (progress < 1) {
                requestAnimationFrame(updateCounter);
            } else {
                element.textContent = endValue;
            }
        }

        requestAnimationFrame(updateCounter);
    }

    // Pulse animation for element
    function pulseElement(elementId) {
        const element = document.getElementById(elementId);
        if (!element) return;

        // Use CSS class-based animation for smoother effect
        element.classList.add('pulse');

        setTimeout(() => {
            element.classList.remove('pulse');
        }, 500);
    }

    // Show priority update notification
    function showPriorityUpdateNotification(data) {
        const totalChange = (data.highPriority - currentHighCount) + (data.mediumPriority - currentMediumCount);

        if (totalChange > 0) {
            showToast(
                'Priority Update',
                `${totalChange} new priority application(s) require attention`,
                'warning'
            );
        }
    }

    // Show new priority toast notification
    function showNewPriorityToast(data) {
        const priorityIcon = data.priority === 'high'
            ? '<i class="bi bi-exclamation-triangle-fill"></i>'
            : '<i class="bi bi-exclamation-circle-fill"></i>';

        const priorityColor = data.priority === 'high' ? '#e74c3c' : '#f39c12';

        const toastHtml = `
            <div class="toast priority-toast align-items-center border-0" role="alert" aria-live="assertive" aria-atomic="true"
                 style="border-left: 4px solid ${priorityColor};">
                <div class="d-flex">
                    <div class="toast-body">
                        <div class="d-flex align-items-center gap-2 mb-1">
                            <span style="color: ${priorityColor}; font-size: 1.2rem;">${priorityIcon}</span>
                            <strong>${data.priority.charAt(0).toUpperCase() + data.priority.slice(1)} Priority Application</strong>
                        </div>
                        <div><strong>${data.applicationType}</strong></div>
                        <div class="text-muted small">${data.applicantName}</div>
                    </div>
                    <button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;

        showToastHtml(toastHtml, 7000);
    }

    // Show admin broadcast notification
    function showAdminBroadcast(message, type) {
        const typeClass = {
            'info': 'bg-info text-white',
            'success': 'bg-success text-white',
            'warning': 'bg-warning text-dark',
            'danger': 'bg-danger text-white'
        }[type] || 'bg-light';

        showToast('Admin Message', message, type);
    }

    // Generic toast function
    function showToast(title, message, type = 'info') {
        const typeClass = {
            'info': 'bg-info text-white',
            'success': 'bg-success text-white',
            'warning': 'bg-warning text-dark',
            'danger': 'bg-danger text-white'
        }[type] || 'bg-light';

        const toastHtml = `
            <div class="toast align-items-center ${typeClass} border-0" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">
                        <strong>${title}</strong><br>
                        ${message}
                    </div>
                    <button type="button" class="btn-close ${type === 'warning' ? '' : 'btn-close-white'} me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;

        showToastHtml(toastHtml, 5000);
    }

    // Show toast with custom HTML
    function showToastHtml(toastHtml, duration = 5000) {
        // Get or create toast container
        let toastContainer = document.getElementById('toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'toast-container';
            toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
            toastContainer.style.zIndex = '9999';
            document.body.appendChild(toastContainer);
        }

        // Create toast element
        const toastWrapper = document.createElement('div');
        toastWrapper.innerHTML = toastHtml;
        const toastElement = toastWrapper.firstElementChild;
        toastContainer.appendChild(toastElement);

        // Show toast
        const toast = new bootstrap.Toast(toastElement, { delay: duration });
        toast.show();

        // Remove after hidden
        toastElement.addEventListener('hidden.bs.toast', function () {
            toastElement.remove();
        });
    }

    // Update connection status indicator
    function updateConnectionStatus(status) {
        const indicator = document.querySelector('.page-title p');
        if (!indicator) return;

        const statusMessages = {
            'connected': 'Real-time monitoring active <i class="bi bi-circle-fill text-success" style="font-size: 0.5rem; animation: pulse 2s infinite;"></i>',
            'reconnecting': 'Reconnecting... <i class="bi bi-arrow-repeat text-warning" style="animation: spin 1s linear infinite;"></i>',
            'disconnected': 'Connection lost - using fallback updates <i class="bi bi-exclamation-triangle-fill text-danger"></i>',
            'error': 'Connection error - retrying... <i class="bi bi-x-circle-fill text-danger"></i>'
        };

        indicator.innerHTML = statusMessages[status] || statusMessages['disconnected'];
    }

    // Play notification sound
    function playNotificationSound() {
        try {
            // Create a simple notification beep using Web Audio API
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioContext.createOscillator();
            const gainNode = audioContext.createGain();

            oscillator.connect(gainNode);
            gainNode.connect(audioContext.destination);

            oscillator.frequency.value = 800;
            oscillator.type = 'sine';

            gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
            gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.1);

            oscillator.start(audioContext.currentTime);
            oscillator.stop(audioContext.currentTime + 0.1);
        } catch (error) {
                    }
    }

    // Add CSS animations
    function addAnimationStyles() {
        if (document.getElementById('priority-animations')) return;

        const style = document.createElement('style');
        style.id = 'priority-animations';
        style.textContent = `
            @keyframes pulse {
                0%, 100% { opacity: 1; }
                50% { opacity: 0.5; }
            }
            @keyframes spin {
                from { transform: rotate(0deg); }
                to { transform: rotate(360deg); }
            }
            .priority-toast {
                min-width: 300px;
                box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            }
        `;
        document.head.appendChild(style);
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            addAnimationStyles();
            initializePriorityConnection();
        });
    } else {
        addAnimationStyles();
        initializePriorityConnection();
    }

    // Expose for external use
    window.prioritySystem = {
        connection: connection,
        animateCounter: animateCounter,
        showToast: showToast
    };

})();
