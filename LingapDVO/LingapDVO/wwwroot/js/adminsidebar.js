// ═══════════════════════════════════════════════════════════════════════════════
// ADMIN SIDEBAR AND PRIORITY ALERTS MODULE
// ═══════════════════════════════════════════════════════════════════════════════
// Provides shared functionality for admin pages:
// - Pull arrow toggle (desktop) - Manual control
// - Burger menu toggle (mobile)
// - Sliding sidebar behavior
// - Priority alerts in header
// - Application priority calculation
// - Active page highlighting
// ═══════════════════════════════════════════════════════════════════════════════

(function() {
    'use strict';

    // ═══════════════════════════════════════════════════════════════════════
    // SIDEBAR TOGGLE FUNCTIONALITY
    // ═══════════════════════════════════════════════════════════════════════

    let isMobile = false;
    let sidebarOpen = false; // Sidebar hidden by default

    /**
     * Check if current viewport is mobile
     */
    function checkMobile() {
        isMobile = window.innerWidth <= 768;
        return isMobile;
    }

    /**
     * Initialize sidebar toggle
     */
    function initSidebarToggle() {
        const pullArrow = document.getElementById('sidebarPullArrow');
        const burgerBtn = document.getElementById('burgerMenuBtn');
        const sidebar = document.getElementById('adminSidebar');
        const overlay = document.getElementById('sidebarOverlay');
        const mainContent = document.querySelector('.main-content-with-sidebar');

        if (!sidebar) {
            console.warn('Admin Sidebar: Sidebar elements not found');
            return;
        }

        // Set initial state
        checkMobile();
        updateSidebarState();

        // Pull arrow click (desktop manual toggle)
        if (pullArrow) {
            pullArrow.addEventListener('click', toggleSidebar);
        }

        // Burger button click (mobile)
        if (burgerBtn) {
            burgerBtn.addEventListener('click', toggleSidebar);
        }

        // Overlay click (close sidebar on mobile)
        if (overlay) {
            overlay.addEventListener('click', closeSidebar);
        }

        // Prevent sidebar from closing when clicking inside it
        if (sidebar) {
            sidebar.addEventListener('click', function(e) {
                // Stop event from bubbling to overlay or other handlers
                e.stopPropagation();
            });
        }

        // Window resize handler
        window.addEventListener('resize', handleResize);

        // Handle nav link clicks
        const navLinks = sidebar.querySelectorAll('.nav-link');
        navLinks.forEach(link => {
            link.addEventListener('click', function(e) {
                // Only handle active state for links with data-page attribute
                const page = link.getAttribute('data-page');
                if (page) {
                    // Remove active from all links
                    navLinks.forEach(l => l.classList.remove('active'));
                    // Add active to clicked link
                    link.classList.add('active');

                    console.log('Admin Sidebar: Navigation link clicked, sidebar remains open');
                }

                // IMPORTANT: Don't auto-close sidebar - user controls it manually
                // Sidebar will only close when user clicks the pull arrow (desktop) or burger button (mobile)
                // This ensures the sidebar stays in its current state after navigation
            });
        });

        console.log('Admin Sidebar: Pull arrow toggle functionality initialized');
    }

    /**
     * Toggle sidebar open/close
     */
    function toggleSidebar() {
        sidebarOpen = !sidebarOpen;
        updateSidebarState();
    }

    /**
     * Open sidebar
     */
    function openSidebar() {
        sidebarOpen = true;
        updateSidebarState();
    }

    /**
     * Close sidebar
     */
    function closeSidebar() {
        sidebarOpen = false;
        updateSidebarState();
    }

    /**
     * Update sidebar visibility state
     */
    function updateSidebarState() {
        const pullArrow = document.getElementById('sidebarPullArrow');
        const burgerBtn = document.getElementById('burgerMenuBtn');
        const sidebar = document.getElementById('adminSidebar');
        const overlay = document.getElementById('sidebarOverlay');
        const mainContent = document.querySelector('.main-content-with-sidebar');

        if (!sidebar) return;

        checkMobile();

        if (isMobile) {
            // Mobile behavior
            if (sidebarOpen) {
                sidebar.classList.add('visible');
                sidebar.classList.remove('mobile-hidden');
                overlay?.classList.add('active');
                burgerBtn?.classList.add('active');
                document.body.style.overflow = 'hidden';
            } else {
                sidebar.classList.remove('visible');
                sidebar.classList.add('mobile-hidden');
                overlay?.classList.remove('active');
                burgerBtn?.classList.remove('active');
                document.body.style.overflow = '';
            }

            // Show burger button, hide pull arrow
            if (burgerBtn) {
                burgerBtn.style.display = 'flex';
            }
            if (pullArrow) {
                pullArrow.classList.add('mobile-hidden');
            }

            // Remove desktop margins
            if (mainContent) {
                mainContent.classList.add('mobile-view');
            }
        } else {
            // Desktop behavior - Manual control with pull arrow
            if (sidebarOpen) {
                sidebar.classList.add('visible');
                pullArrow?.classList.add('open');
                // Remove sidebar-hidden class when sidebar is open
                if (mainContent) {
                    mainContent.classList.remove('sidebar-hidden');
                }
            } else {
                sidebar.classList.remove('visible');
                pullArrow?.classList.remove('open');
                // Add sidebar-hidden class when sidebar is closed
                if (mainContent) {
                    mainContent.classList.add('sidebar-hidden');
                }
            }

            // Hide mobile elements
            sidebar.classList.remove('mobile-hidden');
            overlay?.classList.remove('active');
            burgerBtn?.classList.remove('active');
            document.body.style.overflow = '';

            // Show pull arrow, hide burger button
            if (burgerBtn) {
                burgerBtn.style.display = 'none';
            }
            if (pullArrow) {
                pullArrow.classList.remove('mobile-hidden');
            }

            // Desktop margins handled by user control
            if (mainContent) {
                mainContent.classList.remove('mobile-view');
            }
        }
    }

    /**
     * Handle window resize
     */
    function handleResize() {
        const wasMobile = isMobile;
        checkMobile();

        // If switching from mobile to desktop
        if (wasMobile && !isMobile) {
            sidebarOpen = false;
            updateSidebarState();
        }
        // If switching from desktop to mobile
        else if (!wasMobile && isMobile) {
            sidebarOpen = false;
            updateSidebarState();
        }
    }

    /**
     * Set active navigation item based on current page
     */
    function setActiveNavItem() {
        const currentPath = window.location.pathname.toLowerCase();
        const navLinks = document.querySelectorAll('.nav-link');

        // Remove active class from all links first
        navLinks.forEach(link => {
            link.classList.remove('active');
        });

        // Find and set the matching link as active
        navLinks.forEach(link => {
            const page = link.getAttribute('data-page');
            if (!page) return; // Skip links without data-page attribute

            let isActive = false;

            // Match specific pages exactly to prevent false positives
            switch(page) {
                case 'admin':
                    // Only match exact admin page, not analytics or priorities
                    isActive = currentPath === '/adminuser/admin' ||
                               currentPath.endsWith('/adminuser/admin');
                    break;
                case 'analytics':
                    // Only match analytics dashboard
                    isActive = currentPath.includes('/analyticsdashboard');
                    break;
                case 'priorities':
                    // Only match priorities page
                    isActive = currentPath.includes('/priorities');
                    break;
                default:
                    // For any other pages, check if path contains the page name
                    isActive = currentPath.includes('/' + page);
                    break;
            }

            if (isActive) {
                link.classList.add('active');
            }
        });

        console.log('Admin Sidebar: Active navigation item set for path:', currentPath);
    }

    /**
     * Update priority badge count
     */
    function updatePriorityBadge(count) {
        const badge = document.getElementById('priorityBadge');
        if (badge) {
            if (count > 0) {
                badge.textContent = count;
                badge.style.display = 'inline-block';
            } else {
                badge.style.display = 'none';
            }
        }
    }

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
        // Sidebar toggle functions
        initSidebarToggle: initSidebarToggle,
        toggleSidebar: toggleSidebar,
        openSidebar: openSidebar,
        closeSidebar: closeSidebar,
        setActiveNavItem: setActiveNavItem,
        updatePriorityBadge: updatePriorityBadge,

        // Priority alert functions
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
            initSidebarToggle();
            setActiveNavItem();
            console.log('Admin Sidebar Module: Loaded and initialized successfully');
        });
    } else {
        initSidebarToggle();
        setActiveNavItem();
        console.log('Admin Sidebar Module: Loaded and initialized successfully');
    }

})();
