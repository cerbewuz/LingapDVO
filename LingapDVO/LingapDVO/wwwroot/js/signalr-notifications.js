// SignalR Real-time Notifications
(function() {
    'use strict';

    // Check if SignalR is available
    if (typeof signalR === 'undefined') {
        console.error('SignalR library not loaded. Please include SignalR client library.');
        return;
    }

    // Build connection to NotificationHub
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Handle incoming notifications
    connection.on("ReceiveNotification", function (notification) {
        console.log('Received notification:', notification);

        // Show toast/alert notification
        showNotificationToast(notification);

        // Update notification badge count
        updateNotificationBadge();

        // Trigger custom event for other parts of the app
        const event = new CustomEvent('notificationReceived', { detail: notification });
        document.dispatchEvent(event);
    });

    // Connection lifecycle events
    connection.onreconnecting((error) => {
        console.warn('SignalR reconnecting:', error);
        showConnectionStatus('Reconnecting to notification service...');
    });

    connection.onreconnected((connectionId) => {
        console.log('SignalR reconnected:', connectionId);
        showConnectionStatus('Connected to notification service', 'success');
    });

    connection.onclose((error) => {
        console.error('SignalR connection closed:', error);
        showConnectionStatus('Disconnected from notification service', 'error');
    });

    // Start connection
    function startConnection() {
        connection.start()
            .then(function () {
                console.log('SignalR connected successfully');
            })
            .catch(function (error) {
                console.error('SignalR connection error:', error);
                // Retry connection after 5 seconds
                setTimeout(startConnection, 5000);
            });
    }

    // Show notification toast
    function showNotificationToast(notification) {
        // Create toast element
        const toastHtml = `
            <div class="toast notification-toast align-items-center ${getNotificationClass(notification.type)}" role="alert" aria-live="assertive" aria-atomic="true" data-bs-autohide="true" data-bs-delay="5000">
                <div class="d-flex">
                    <div class="toast-body">
                        <strong>${notification.title}</strong>
                        <p class="mb-0">${notification.message}</p>
                    </div>
                    <button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;

        // Get or create toast container
        let toastContainer = document.getElementById('toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'toast-container';
            toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
            toastContainer.style.zIndex = '9999';
            document.body.appendChild(toastContainer);
        }

        // Add toast to container
        const toastWrapper = document.createElement('div');
        toastWrapper.innerHTML = toastHtml;
        const toastElement = toastWrapper.firstElementChild;
        toastContainer.appendChild(toastElement);

        // Show toast
        const toast = new bootstrap.Toast(toastElement);
        toast.show();

        // Remove toast element after it's hidden
        toastElement.addEventListener('hidden.bs.toast', function () {
            toastElement.remove();
        });

        // Play notification sound (optional)
        playNotificationSound();
    }

    // Get notification class based on type
    function getNotificationClass(type) {
        switch (type) {
            case 'application_approved':
                return 'bg-success text-white';
            case 'application_disapproved':
                return 'bg-danger text-white';
            case 'application_processing':
                return 'bg-info text-white';
            case 'application_claimed':
                return 'bg-primary text-white';
            case 'application_submitted':
            default:
                return 'bg-light border';
        }
    }

    // Update notification badge count
    function updateNotificationBadge() {
        fetch('/Notifications/GetUserNotifications')
            .then(response => response.json())
            .then(data => {
                if (Array.isArray(data)) {
                    const unreadCount = data.filter(n => !n.isRead).length;

                    // Update badge elements
                    const badges = document.querySelectorAll('.notification-badge, .notif-count');
                    badges.forEach(badge => {
                        if (unreadCount > 0) {
                            badge.textContent = unreadCount > 99 ? '99+' : unreadCount;
                            badge.style.display = 'block';
                        } else {
                            badge.textContent = '0';
                            badge.style.display = 'none';
                        }
                    });
                }
            })
            .catch(error => {
                console.error('Error updating notification badge:', error);
            });
    }

    // Show connection status message
    function showConnectionStatus(message, type = 'info') {
        console.log(`[${type.toUpperCase()}] ${message}`);
        // You can enhance this to show a visual indicator if needed
    }

    // Play notification sound (optional)
    function playNotificationSound() {
        // Uncomment to enable sound
        // const audio = new Audio('/sounds/notification.mp3');
        // audio.play().catch(err => console.log('Could not play notification sound:', err));
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', startConnection);
    } else {
        startConnection();
    }

    // Expose connection for external use
    window.notificationConnection = connection;

    // Listen for custom events to send notifications
    document.addEventListener('sendNotification', function(event) {
        const { userId, title, message, type, link } = event.detail;

        connection.invoke("SendNotificationToUser", userId, title, message, type, link)
            .catch(function (error) {
                console.error('Error sending notification:', error);
            });
    });

})();
