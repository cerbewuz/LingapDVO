// ═══════════════════════════════════════════════════════════════════════════════
// ADMIN SIDEBAR AND PRIORITY ALERTS MODULE
// ═══════════════════════════════════════════════════════════════════════════════
// Provides shared functionality for admin pages:
// - Priority alerts in header
// - Application priority calculation
// - Responsive without @media queries
// ═══════════════════════════════════════════════════════════════════════════════

(function() {
    'use strict';

    // ═══════════════════════════════════════════════════════════════════════
    // PRIORITY ALERTS FUNCTIONALITY
    // ═══════════════════════════════════════════════════════════════════════

    /**
     * Update priority alerts in the page header
     * @param {Array} applications - Array of application objects with createdAt dates
     */
    function updateHeaderPriorityAlerts(applications) {
        const now = new Date();
        let highPriorityCount = 0;
        let mediumPriorityCount = 0;

        // Calculate priority counts
        applications.forEach(app => {
            const hoursSinceSubmission = (now - app.createdAt) / (1000 * 60 * 60);
            if (hoursSinceSubmission >= 2) {
                highPriorityCount++;
            } else if (hoursSinceSubmission >= 1) {
                mediumPriorityCount++;
            }
        });

        // Find the alerts container
        const alertsContainer = document.getElementById('headerPriorityAlerts');
        if (!alertsContainer) return;

        let alertsHTML = '';

        // Add high priority alert
        if (highPriorityCount > 0) {
            alertsHTML += `
                <a href="/Adminuser/Priorities?filter=high" class="priority-alert high">
                    <i class="bi bi-exclamation-triangle-fill"></i>
                    <span>High Priority</span>
                    <span class="priority-count">${highPriorityCount}</span>
                </a>
            `;
        }

        // Add medium priority alert
        if (mediumPriorityCount > 0) {
            alertsHTML += `
                <a href="/Adminuser/Priorities?filter=medium" class="priority-alert medium">
                    <i class="bi bi-exclamation-circle-fill"></i>
                    <span>Medium Priority</span>
                    <span class="priority-count">${mediumPriorityCount}</span>
                </a>
            `;
        }

        alertsContainer.innerHTML = alertsHTML;
    }

    /**
     * Calculate priority level for an application
     * @param {Date} createdAt - Application creation date
     * @returns {String} Priority level: 'high', 'medium', or 'normal'
     */
    function calculatePriority(createdAt) {
        const now = new Date();
        const hoursSinceSubmission = (now - createdAt) / (1000 * 60 * 60);

        if (hoursSinceSubmission >= 2) {
            return 'high';
        } else if (hoursSinceSubmission >= 1) {
            return 'medium';
        } else {
            return 'normal';
        }
    }

    /**
     * Get priority color class
     * @param {String} priority - Priority level
     * @returns {String} CSS class for priority color
     */
    function getPriorityColorClass(priority) {
        switch(priority) {
            case 'high':
                return 'text-danger fw-bold';
            case 'medium':
                return 'text-warning fw-semibold';
            default:
                return 'text-success';
        }
    }

    /**
     * Get priority icon
     * @param {String} priority - Priority level
     * @returns {String} HTML for priority icon
     */
    function getPriorityIcon(priority) {
        switch(priority) {
            case 'high':
                return '<i class="bi bi-exclamation-triangle-fill text-danger"></i>';
            case 'medium':
                return '<i class="bi bi-exclamation-circle-fill text-warning"></i>';
            default:
                return '<i class="bi bi-check-circle-fill text-success"></i>';
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════════════════════════════════════

    // Expose functions globally for use in admin pages
    window.AdminSidebar = {
        updateHeaderPriorityAlerts: updateHeaderPriorityAlerts,
        calculatePriority: calculatePriority,
        getPriorityColorClass: getPriorityColorClass,
        getPriorityIcon: getPriorityIcon
    };

    // ═══════════════════════════════════════════════════════════════════════
    // AUTO-INITIALIZATION
    // ═══════════════════════════════════════════════════════════════════════

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            console.log('Admin Sidebar Module: Loaded successfully');
        });
    } else {
        console.log('Admin Sidebar Module: Loaded successfully');
    }

})();
