// Client-Side Upload/Application Tracking with Delay Notifications
(function () {
    'use strict';

    let connection = null;
    let userId = null;
    let applications = [];

    // Initialize tracking system
    function initializeUploadTracking() {
        // Get userId from page (should be set by the server)
        userId = window.currentUserId || document.querySelector('[data-user-id]')?.dataset.userId;

        if (!userId) {
            console.warn('[Upload Tracking] User ID not found, tracking disabled');
            return;
        }

        // Initialize SignalR connection
        initializeSignalRConnection();

        // Load user applications
        loadUserApplications();

        // Check for delays periodically (every 30 seconds)
        setInterval(checkForDelays, 30000);
    }

    // Initialize SignalR connection
    function initializeSignalRConnection() {
        if (typeof signalR === 'undefined') {
            console.error('[Upload Tracking] SignalR library not loaded');
            return;
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl("/notificationHub")
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Handle delay notifications from server
        connection.on("ReceiveDelayNotification", function (data) {
            console.log('[Upload Tracking] Delay notification received:', data);
            showDelayNotification(data);
        });

        // Handle real-time status updates
        connection.on("ReceiveStatusUpdate", function (data) {
            console.log('[Upload Tracking] Status update received:', data);
            updateApplicationStatus(data);
            showStatusUpdateNotification(data);
        });

        // Handle general notifications
        connection.on("ReceiveNotification", function (data) {
            console.log('[Upload Tracking] Notification received:', data);
            showGeneralNotification(data);
        });

        // Connection lifecycle
        connection.onreconnecting((error) => {
            console.warn('[Upload Tracking] Reconnecting...', error);
            updateConnectionIndicator('reconnecting');
        });

        connection.onreconnected((connectionId) => {
            console.log('[Upload Tracking] Reconnected:', connectionId);
            updateConnectionIndicator('connected');
            loadUserApplications(); // Refresh data
        });

        connection.onclose((error) => {
            console.error('[Upload Tracking] Connection closed:', error);
            updateConnectionIndicator('disconnected');
        });

        // Start connection
        startConnection();
    }

    // Start SignalR connection
    function startConnection() {
        connection.start()
            .then(function () {
                console.log('[Upload Tracking] Connected to real-time tracking');
                updateConnectionIndicator('connected');
            })
            .catch(function (error) {
                console.error('[Upload Tracking] Connection error:', error);
                updateConnectionIndicator('error');
                setTimeout(startConnection, 5000);
            });
    }

    // Load user applications from page data
    function loadUserApplications() {
        // Applications should be loaded from the page model
        // This function can be customized based on how data is passed to the page
        applications = window.userApplications || [];

        console.log('[Upload Tracking] Loaded applications:', applications);

        // Initial delay check
        checkForDelays();
    }

    // Check for application delays
    function checkForDelays() {
        const now = new Date();

        applications.forEach(app => {
            if (app.status && app.status.toLowerCase() === 'pending') {
                const createdAt = new Date(app.createdAt);
                const hoursElapsed = (now - createdAt) / (1000 * 60 * 60);

                // Determine priority level
                let priority = null;
                if (hoursElapsed >= 2) {
                    priority = 'high';
                } else if (hoursElapsed >= 1) {
                    priority = 'medium';
                }

                // Show notification if medium or high priority
                if (priority && !app.delayNotificationShown) {
                    showDelayNotificationLocal(app, priority, hoursElapsed);
                    app.delayNotificationShown = true;
                }
            }
        });
    }

    // Show delay notification (from server)
    function showDelayNotification(data) {
        const icon = data.priority === 'high'
            ? '<i class="bi bi-exclamation-triangle-fill text-danger"></i>'
            : '<i class="bi bi-clock-history text-warning"></i>';

        const title = data.priority === 'high'
            ? 'Application Delayed - High Priority'
            : 'Processing Update';

        const alertHtml = `
            <div class="alert alert-dismissible fade show shadow-sm border-0" role="alert"
                 style="border-left: 4px solid ${data.priority === 'high' ? '#e74c3c' : '#f39c12'}; background-color: ${data.priority === 'high' ? '#ffebee' : '#fff3cd'};">
                <div class="d-flex align-items-start gap-3">
                    <div style="font-size: 2rem;">${icon}</div>
                    <div class="flex-grow-1">
                        <h5 class="alert-heading mb-2">${title}</h5>
                        <p class="mb-2">${data.message}</p>
                        <small class="text-muted">
                            <i class="bi bi-tag"></i> ${data.applicationType}
                            <span class="ms-2"><i class="bi bi-clock"></i> ${Math.floor(data.hoursElapsed)} hours elapsed</span>
                        </small>
                        ${data.priority === 'high' ? '<hr class="my-2"><p class="mb-0 small"><strong>Action:</strong> Our team has been notified and is prioritizing your application.</p>' : ''}
                    </div>
                    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                </div>
            </div>
        `;

        showAlert(alertHtml);
        playNotificationSound(data.priority);
    }

    // Show local delay notification - NOW INTEGRATED INTO TIMELINE
    function showDelayNotificationLocal(app, priority, hoursElapsed) {
        const message = priority === 'high'
            ? `Your application has been pending for ${Math.floor(hoursElapsed)} hours. We apologize for the delay. Our team is working on it with high priority and will process it ASAP.`
            : `Your application has been pending for over an hour. Don't worry! It's in our queue and will be reviewed shortly.`;

        // Add delay timeline item to the tracking card
        addDelayTimelineItem(app, priority, hoursElapsed, message);
    }

    // Add delay notification as timeline item in tracking card
    function addDelayTimelineItem(app, priority, hoursElapsed, message) {
        // Find all tracking cards and locate the one matching this application
        const trackingCards = document.querySelectorAll('.tracking-card');

        trackingCards.forEach(card => {
            // Check if this card matches the application by checking the form ID in action buttons
            const viewButton = card.querySelector('.btn-view');
            if (!viewButton) return;

            const href = viewButton.getAttribute('href');
            if (!href || !href.includes('/' + app.formId)) return;

            // Check if delay notification already exists for this card
            const existingDelay = card.querySelector('.timeline-item[data-delay-notification="true"]');
            if (existingDelay) return; // Already shown

            // Find the timeline container
            const timeline = card.querySelector('.timeline');
            if (!timeline) return;

            // Find the "Pending Review" timeline item (should be the 2nd item)
            const pendingItem = timeline.querySelector('.timeline-item:nth-child(2)');
            if (!pendingItem) return;

            // Create delay notification timeline item
            const delayItem = document.createElement('div');
            delayItem.className = 'timeline-item';
            delayItem.setAttribute('data-delay-notification', 'true');

            const priorityColor = priority === 'high' ? 'var(--danger-color)' : 'var(--warning-color)';
            const priorityBg = priority === 'high' ? '#fee2e2' : '#fef3c7';
            const priorityIcon = priority === 'high' ? 'fa-exclamation-triangle' : 'fa-clock';
            const priorityLabel = priority === 'high' ? 'High Priority Delay' : 'Processing Delay Notice';

            delayItem.innerHTML = `
                <div class="timeline-marker" style="background: ${priorityColor};">
                    <i class="fas ${priorityIcon}"></i>
                </div>
                <div class="timeline-content" style="background: ${priorityBg}; border-left: 3px solid ${priorityColor};">
                    <h6 style="color: ${priorityColor};">
                        <i class="fas fa-bell me-1"></i>${priorityLabel}
                    </h6>
                    <span class="timeline-date">${new Date().toLocaleString('en-US', {
                        day: '2-digit',
                        month: 'short',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit',
                        hour12: true
                    })}</span>
                    <p class="timeline-description" style="color: #4b5563; margin-top: 0.5rem;">
                        ${message}
                    </p>
                    <div class="timeline-description mt-2" style="font-size: 0.75rem; background: white; padding: 0.5rem; border-radius: 4px;">
                        <i class="fas fa-info-circle" style="color: ${priorityColor};"></i>
                        <strong>Time Elapsed:</strong> ${Math.floor(hoursElapsed)} hour(s) ${Math.floor((hoursElapsed % 1) * 60)} minute(s)
                    </div>
                </div>
            `;

            // Insert after the pending item (makes it the 3rd item)
            if (pendingItem.nextSibling) {
                timeline.insertBefore(delayItem, pendingItem.nextSibling);
            } else {
                timeline.appendChild(delayItem);
            }

            // Add subtle animation
            delayItem.style.opacity = '0';
            delayItem.style.transform = 'translateX(-10px)';
            setTimeout(() => {
                delayItem.style.transition = 'all 0.4s ease-out';
                delayItem.style.opacity = '1';
                delayItem.style.transform = 'translateX(0)';
            }, 50);

            // Play notification sound
            playNotificationSound(priority);
        });
    }

    // Update application status in real-time
    function updateApplicationStatus(data) {
        const application = applications.find(app => app.formId === data.formId);

        if (application) {
            application.status = data.status;
            console.log('[Upload Tracking] Application status updated:', application);

            // Refresh UI if there's a render function
            if (typeof window.refreshApplicationList === 'function') {
                window.refreshApplicationList();
            }
        }
    }

    // Show status update notification
    function showStatusUpdateNotification(data) {
        const statusMessages = {
            'processing': {
                icon: '<i class="bi bi-hourglass-split text-info"></i>',
                title: 'Application Processing',
                color: '#3498db',
                bg: '#e3f2fd'
            },
            'approve': {
                icon: '<i class="bi bi-check-circle-fill text-success"></i>',
                title: 'Application Approved',
                color: '#10b981',
                bg: '#e8f5e9'
            },
            'disapprove': {
                icon: '<i class="bi bi-x-circle-fill text-danger"></i>',
                title: 'Application Status Update',
                color: '#ef4444',
                bg: '#ffebee'
            },
            'claimed': {
                icon: '<i class="bi bi-trophy-fill text-primary"></i>',
                title: 'Application Claimed',
                color: '#4361ee',
                bg: '#eef2ff'
            }
        };

        const statusInfo = statusMessages[data.status.toLowerCase()] || statusMessages['processing'];

        const alertHtml = `
            <div class="alert alert-dismissible fade show shadow-sm border-0" role="alert"
                 style="border-left: 4px solid ${statusInfo.color}; background-color: ${statusInfo.bg};">
                <div class="d-flex align-items-start gap-3">
                    <div style="font-size: 2rem;">${statusInfo.icon}</div>
                    <div class="flex-grow-1">
                        <h5 class="alert-heading mb-2">${statusInfo.title}</h5>
                        <p class="mb-2">Your <strong>${data.applicationType}</strong> has been updated to: <strong>${data.status}</strong></p>
                        <small class="text-muted"><i class="bi bi-clock"></i> Just now</small>
                    </div>
                    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                </div>
            </div>
        `;

        showAlert(alertHtml);
        playNotificationSound('status');
    }

    // Show general notification
    function showGeneralNotification(data) {
        const toastHtml = `
            <div class="toast align-items-center border-0 shadow-sm" role="alert">
                <div class="d-flex">
                    <div class="toast-body">
                        <strong>${data.title}</strong><br>
                        ${data.message}
                    </div>
                    <button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast"></button>
                </div>
            </div>
        `;

        showToastHtml(toastHtml);
    }

    // Show alert in page
    function showAlert(alertHtml) {
        let alertContainer = document.getElementById('alert-container');

        if (!alertContainer) {
            alertContainer = document.createElement('div');
            alertContainer.id = 'alert-container';
            alertContainer.className = 'container mt-3';

            // Insert at the top of main content
            const mainContent = document.querySelector('.container, main, .main-content');
            if (mainContent) {
                mainContent.insertBefore(alertContainer, mainContent.firstChild);
            } else {
                document.body.insertBefore(alertContainer, document.body.firstChild);
            }
        }

        const alertWrapper = document.createElement('div');
        alertWrapper.innerHTML = alertHtml;
        const alertElement = alertWrapper.firstElementChild;
        alertContainer.insertBefore(alertElement, alertContainer.firstChild);

        // Auto-remove after 30 seconds if not manually closed
        setTimeout(() => {
            if (alertElement.parentNode) {
                alertElement.classList.remove('show');
                setTimeout(() => alertElement.remove(), 150);
            }
        }, 30000);
    }

    // Show toast notification
    function showToastHtml(toastHtml) {
        let toastContainer = document.getElementById('toast-container');

        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'toast-container';
            toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
            toastContainer.style.zIndex = '9999';
            document.body.appendChild(toastContainer);
        }

        const toastWrapper = document.createElement('div');
        toastWrapper.innerHTML = toastHtml;
        const toastElement = toastWrapper.firstElementChild;
        toastContainer.appendChild(toastElement);

        const toast = new bootstrap.Toast(toastElement, { delay: 5000 });
        toast.show();

        toastElement.addEventListener('hidden.bs.toast', () => toastElement.remove());
    }

    // Update connection indicator
    function updateConnectionIndicator(status) {
        const indicator = document.getElementById('connection-status');
        if (!indicator) return;

        const statusConfig = {
            'connected': {
                text: 'Real-time tracking active',
                icon: '<i class="bi bi-circle-fill text-success"></i>',
                class: 'text-success'
            },
            'reconnecting': {
                text: 'Reconnecting...',
                icon: '<i class="bi bi-arrow-repeat text-warning"></i>',
                class: 'text-warning'
            },
            'disconnected': {
                text: 'Disconnected',
                icon: '<i class="bi bi-x-circle-fill text-danger"></i>',
                class: 'text-danger'
            },
            'error': {
                text: 'Connection error',
                icon: '<i class="bi bi-exclamation-triangle-fill text-danger"></i>',
                class: 'text-danger'
            }
        };

        const config = statusConfig[status] || statusConfig['disconnected'];
        indicator.innerHTML = `<small class="${config.class}">${config.icon} ${config.text}</small>`;
    }

    // Play notification sound
    function playNotificationSound(type = 'info') {
        try {
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioContext.createOscillator();
            const gainNode = audioContext.createGain();

            oscillator.connect(gainNode);
            gainNode.connect(audioContext.destination);

            // Different frequencies for different priorities
            const frequencies = {
                'high': 1000,
                'medium': 800,
                'status': 600,
                'info': 700
            };

            oscillator.frequency.value = frequencies[type] || frequencies['info'];
            oscillator.type = 'sine';

            gainNode.gain.setValueAtTime(0.2, audioContext.currentTime);
            gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.15);

            oscillator.start(audioContext.currentTime);
            oscillator.stop(audioContext.currentTime + 0.15);
        } catch (error) {
            console.warn('[Upload Tracking] Could not play notification sound:', error);
        }
    }

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeUploadTracking);
    } else {
        initializeUploadTracking();
    }

    // Expose for external use
    window.uploadTracking = {
        connection: connection,
        loadApplications: loadUserApplications,
        checkDelays: checkForDelays
    };

})();
