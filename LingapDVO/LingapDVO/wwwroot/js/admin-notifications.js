// ═══════════════════════════════════════════════════════════════
// ADMIN NOTIFICATIONS - Real-time Bell Notifications
// Same design as user notifications in Homepage
// ═══════════════════════════════════════════════════════════════

(function () {
    'use strict';

    let adminNotifications = [];
    let adminSignalRConnection = null;

    // Initialize admin notification system
    function initializeAdminNotifications() {
        const notificationLink = document.getElementById('adminNotificationsLink');
        const notificationBadge = document.getElementById('adminNotificationBadge');
        const notificationDropdown = document.getElementById('adminNotificationDropdown');
        const notificationList = document.getElementById('adminNotificationList');
        const markAllReadBtn = document.getElementById('markAllAdminRead');

        if (!notificationLink || !notificationDropdown) {
            console.warn('Admin notification elements not found');
            return;
        }

        // Load initial notifications
        loadAdminNotifications();

        // Toggle dropdown on bell click
        notificationLink.addEventListener('click', function (e) {
            e.preventDefault();
            const isVisible = notificationDropdown.style.display === 'block';
            notificationDropdown.style.display = isVisible ? 'none' : 'block';

            // Load fresh data when opening
            if (!isVisible) {
                loadAdminNotifications();
            }
        });

        // Mark all as read
        if (markAllReadBtn) {
            markAllReadBtn.addEventListener('click', async function () {
                await markAllNotificationsAsRead();
            });
        }

        // Close dropdown when clicking outside
        document.addEventListener('click', function (e) {
            if (!notificationLink.contains(e.target) && !notificationDropdown.contains(e.target)) {
                notificationDropdown.style.display = 'none';
            }
        });

        // Initialize SignalR connection for real-time notifications
        initializeAdminSignalR();
    }

    // Load admin notifications from API
    async function loadAdminNotifications() {
        try {
            const response = await fetch('/Notifications/GetAdminNotifications');
            const data = await response.json();

            if (data.success && data.notifications) {
                adminNotifications = data.notifications;
                renderAdminNotifications();
                updateAdminNotificationBadge(data.unreadCount || 0);
            }
        } catch (error) {
            console.error('Error loading admin notifications:', error);
        }
    }

    // Render admin notifications in dropdown
    function renderAdminNotifications() {
        const notificationList = document.getElementById('adminNotificationList');
        if (!notificationList) return;

        if (adminNotifications.length === 0) {
            notificationList.innerHTML = `
                <div style="padding: 2rem; text-align: center; color: #adb5bd;">
                    <i class="bi bi-bell" style="font-size: 2rem; display: block; margin-bottom: 0.5rem; opacity: 0.3;"></i>
                    <p style="margin: 0;">No notifications</p>
                </div>
            `;
            return;
        }

        notificationList.innerHTML = adminNotifications.map(notification => {
            const icon = getNotificationIcon(notification.type);
            const priorityBadge = getPriorityBadge(notification.priority);
            const unreadClass = !notification.isRead ? 'unread' : '';
            const priorityClass = notification.priority ? `priority-${notification.priority}` : '';

            return `
                <div class="admin-notification-item ${unreadClass} ${priorityClass}"
                     data-id="${notification.id}"
                     onclick="handleAdminNotificationClick(${notification.id}, '${notification.link || '#'}')">
                    <div class="admin-notification-title">
                        ${icon}
                        <span class="notification-title-text">${escapeHtml(notification.title)}</span>
                        ${priorityBadge}
                    </div>
                    <div class="admin-notification-message">${escapeHtml(notification.message)}</div>
                    <div class="admin-notification-time">
                        <i class="bi bi-clock"></i>
                        ${formatTime(notification.createdAt)}
                    </div>
                </div>
            `;
        }).join('');
    }

    // Get icon based on notification type
    function getNotificationIcon(type) {
        const icons = {
            'application_submitted': '<i class="bi bi-file-earmark-plus"></i>',
            'retake_submitted': '<i class="bi bi-arrow-repeat"></i>',
            'feedback_submitted': '<i class="bi bi-chat-square-text"></i>',
            'priority_alert': '<i class="bi bi-exclamation-triangle"></i>'
        };
        return icons[type] || '<i class="bi bi-bell"></i>';
    }

    // Get priority badge HTML
    function getPriorityBadge(priority) {
        if (!priority || priority === 'normal') return '';

        const badges = {
            'high': '<span class="badge bg-danger">HIGH PRIORITY</span>',
            'medium': '<span class="badge bg-warning">MEDIUM PRIORITY</span>',
            'critical': '<span class="badge bg-danger">CRITICAL</span>'
        };
        return badges[priority] || '';
    }

    // Update notification badge count
    function updateAdminNotificationBadge(count) {
        const badge = document.getElementById('adminNotificationBadge');
        if (!badge) return;

        if (count > 0) {
            badge.textContent = count > 99 ? '99+' : count;
            badge.style.display = 'inline-block';
        } else {
            badge.style.display = 'none';
        }
    }

    // Handle notification click
    window.handleAdminNotificationClick = async function (notificationId, link) {
        // Mark as read
        await markNotificationAsRead(notificationId);

        // Navigate to link
        if (link && link !== '#') {
            window.location.href = link;
        }
    };

    // Mark single notification as read
    async function markNotificationAsRead(notificationId) {
        try {
            const response = await fetch('/Notifications/MarkAdminNotificationAsRead', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ notificationId })
            });

            if (response.ok) {
                const notification = adminNotifications.find(n => n.id === notificationId);
                if (notification) {
                    notification.isRead = true;
                }
                renderAdminNotifications();
                await loadAdminNotifications(); // Reload to get updated count
            }
        } catch (error) {
            console.error('Error marking notification as read:', error);
        }
    }

    // Mark all notifications as read
    async function markAllNotificationsAsRead() {
        try {
            const response = await fetch('/Notifications/MarkAllAdminNotificationsAsRead', {
                method: 'POST'
            });

            if (response.ok) {
                adminNotifications.forEach(n => n.isRead = true);
                renderAdminNotifications();
                updateAdminNotificationBadge(0);
            }
        } catch (error) {
            console.error('Error marking all notifications as read:', error);
        }
    }

    // Initialize SignalR for real-time admin notifications
    function initializeAdminSignalR() {
        // Check if SignalR is already initialized by another script
        if (window.notificationConnection) {
            console.log('Using existing SignalR connection for admin notifications');
            setupAdminSignalRHandlers(window.notificationConnection);
            return;
        }

        // Create new connection if SignalR is available
        if (typeof signalR === 'undefined') {
            console.warn('SignalR not loaded - admin notifications will not be real-time');
            return;
        }

        adminSignalRConnection = new signalR.HubConnectionBuilder()
            .withUrl('/notificationHub')
            .withAutomaticReconnect()
            .build();

        setupAdminSignalRHandlers(adminSignalRConnection);

        // Start connection
        adminSignalRConnection.start()
            .then(() => console.log('Admin notification SignalR connected'))
            .catch(err => console.error('Admin notification SignalR error:', err));
    }

    // Setup SignalR event handlers
    function setupAdminSignalRHandlers(connection) {
        // Receive new admin notification
        connection.on('ReceiveAdminNotification', function (data) {
            console.log('New admin notification:', data);

            // Add to notifications list at the top
            adminNotifications.unshift(data);

            // Update UI
            renderAdminNotifications();
            updateAdminNotificationBadge(data.unreadCount);

            // Show toast notification
            showAdminToast(data.title, data.message, data.type);

            // Play notification sound (optional)
            playNotificationSound();
        });

        // Update notification count
        connection.on('UpdateAdminNotificationCount', function (count) {
            updateAdminNotificationBadge(count);
        });
    }

    // Show toast notification (subtle popup)
    function showAdminToast(title, message, type) {
        // Create toast element if it doesn't exist
        let toast = document.getElementById('adminNotificationToast');
        if (!toast) {
            toast = document.createElement('div');
            toast.id = 'adminNotificationToast';
            toast.style.cssText = `
                position: fixed;
                top: 20px;
                right: 20px;
                background: white;
                border-radius: 8px;
                box-shadow: 0 4px 12px rgba(0,0,0,0.15);
                padding: 1rem 1.25rem;
                min-width: 300px;
                max-width: 400px;
                z-index: 10000;
                display: none;
                animation: slideInRight 0.3s ease;
            `;
            document.body.appendChild(toast);
        }

        const icon = getNotificationIcon(type);
        toast.innerHTML = `
            <div style="display: flex; align-items: start; gap: 0.75rem;">
                <div style="color: #4361ee; font-size: 1.5rem;">${icon}</div>
                <div style="flex: 1;">
                    <div style="font-weight: 600; color: #2c3e50; margin-bottom: 0.25rem;">${escapeHtml(title)}</div>
                    <div style="font-size: 0.875rem; color: #6c757d;">${escapeHtml(message)}</div>
                </div>
                <button onclick="this.parentElement.parentElement.style.display='none'" style="background: none; border: none; color: #adb5bd; cursor: pointer; font-size: 1.25rem; padding: 0; line-height: 1;">&times;</button>
            </div>
        `;

        toast.style.display = 'block';

        // Auto-hide after 5 seconds
        setTimeout(() => {
            toast.style.display = 'none';
        }, 5000);
    }

    // Play notification sound
    function playNotificationSound() {
        try {
            // Create a subtle notification sound using Web Audio API
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioContext.createOscillator();
            const gainNode = audioContext.createGain();

            oscillator.connect(gainNode);
            gainNode.connect(audioContext.destination);

            oscillator.frequency.value = 800;
            oscillator.type = 'sine';

            gainNode.gain.setValueAtTime(0.1, audioContext.currentTime);
            gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.1);

            oscillator.start(audioContext.currentTime);
            oscillator.stop(audioContext.currentTime + 0.1);
        } catch (error) {
            // Silently fail if audio not supported
        }
    }

    // Utility functions
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function formatTime(dateString) {
        const date = new Date(dateString);
        const now = new Date();
        const diffMs = now - date;
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 1) return 'Just now';
        if (diffMins < 60) return `${diffMins}m ago`;
        if (diffHours < 24) return `${diffHours}h ago`;
        if (diffDays < 7) return `${diffDays}d ago`;
        return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    }

    // Add CSS animation
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideInRight {
            from {
                transform: translateX(100%);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
    `;
    document.head.appendChild(style);

    // Initialize on DOM load
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeAdminNotifications);
    } else {
        initializeAdminNotifications();
    }
})();
