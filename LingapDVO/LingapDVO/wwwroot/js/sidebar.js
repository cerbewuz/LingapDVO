// ═══════════════════════════════════════════════════════════════════════════════
// RESPONSIVE SIDEBAR WITH BURGER MENU
// ═══════════════════════════════════════════════════════════════════════════════
// Provides collapsible sidebar navigation for admin pages
// Includes notification badge for priority applications
// ═══════════════════════════════════════════════════════════════════════════════

(function() {
    'use strict';

    // Sidebar HTML template
    const sidebarHTML = `
        <div class="sidebar" id="sidebar">
            <div class="sidebar-header">
                <div class="sidebar-title">
                    <i class="bi bi-heart-pulse"></i>
                    <span>LingapDVO</span>
                </div>
            </div>
            <ul class="sidebar-nav">
                <li>
                    <a href="/Admin" id="nav-admin">
                        <i class="bi bi-house-door"></i>
                        <span>Application Dashboard</span>
                    </a>
                </li>
                <li>
                    <a href="/AdminDashboard" id="nav-analytics">
                        <i class="bi bi-bar-chart"></i>
                        <span>Analytics Dashboard</span>
                    </a>
                </li>
                <li>
                    <a href="/DelayedApplications" id="nav-delayed">
                        <i class="bi bi-bell"></i>
                        <span>Delayed Applications</span>
                        <span class="notification-badge" id="notification-badge" style="display: none;">0</span>
                    </a>
                </li>
                <li>
                    <a href="/Login/Logout">
                        <i class="bi bi-box-arrow-right"></i>
                        <span>Logout</span>
                    </a>
                </li>
            </ul>
        </div>
    `;

    // Sidebar styles (will be injected if not present)
    const sidebarStyles = `
        <style id="sidebar-styles">
            .sidebar {
                width: 260px;
                min-width: 260px;
                background: linear-gradient(180deg, var(--dark) 0%, #2d2d44 100%);
                color: white;
                padding: 1.5rem 0;
                box-shadow: 0 0 20px rgba(0, 0, 0, 0.1);
                position: fixed;
                left: 0;
                top: 0;
                height: 100vh;
                z-index: 1000;
                transition: transform 0.3s ease, width 0.3s ease;
                flex-shrink: 0;
                overflow-y: auto;
            }

            .sidebar-collapsed {
                transform: translateX(-100%);
            }

            .sidebar-header {
                padding: 0 1.5rem 1.5rem;
                border-bottom: 1px solid rgba(255, 255, 255, 0.1);
                margin-bottom: 1rem;
            }

            .sidebar-title {
                display: flex;
                align-items: center;
                font-weight: 700;
                font-size: 1.4rem;
                color: white;
            }

            .sidebar-title i {
                color: var(--lingap-red);
                margin-right: 0.75rem;
                font-size: 1.6rem;
            }

            .sidebar-nav {
                list-style: none;
                padding: 0;
                margin: 0;
            }

            .sidebar-nav li {
                margin-bottom: 0.5rem;
            }

            .sidebar-nav a {
                display: flex;
                align-items: center;
                color: rgba(255, 255, 255, 0.8);
                text-decoration: none;
                padding: 0.85rem 1.5rem;
                border-radius: 0.5rem;
                transition: all 0.2s ease;
                font-weight: 500;
                font-size: 1rem;
                position: relative;
            }

            .sidebar-nav a:hover,
            .sidebar-nav a.active {
                background: rgba(255, 255, 255, 0.1);
                color: white;
            }

            .sidebar-nav i {
                margin-right: 0.75rem;
                font-size: 1.2rem;
                min-width: 1.2rem;
            }

            .notification-badge {
                background: var(--danger);
                color: white;
                border-radius: 50%;
                padding: 0.15rem 0.5rem;
                font-size: 0.75rem;
                font-weight: 700;
                margin-left: auto;
                min-width: 1.5rem;
                text-align: center;
                animation: pulse 2s infinite;
            }

            @keyframes pulse {
                0%, 100% {
                    opacity: 1;
                }
                50% {
                    opacity: 0.7;
                }
            }

            .burger-menu {
                position: fixed;
                top: 1rem;
                left: 1rem;
                z-index: 1001;
                background: white;
                border: none;
                border-radius: 0.5rem;
                padding: 0.75rem;
                box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
                cursor: pointer;
                transition: all 0.2s ease;
                /* Show on mobile, hide on desktop */
                display: block;
            }

            .burger-menu:hover {
                background: var(--gray-200);
            }

            .burger-menu i {
                font-size: 1.5rem;
                color: var(--dark);
            }

            .sidebar-overlay {
                display: none;
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: rgba(0, 0, 0, 0.5);
                z-index: 999;
            }

            .sidebar-overlay.active {
                display: block;
            }

            /* Responsive adjustments without @media queries */
            /* Mobile: Hide sidebar by default on small screens (viewport < 768px) */
            .sidebar {
                /* If viewport < 768px: translateX(-100%), else translateX(0) */
                transform: translateX(clamp(-100%, calc((100vw - 768px) * 999), 0%));
            }

            .sidebar.sidebar-open {
                transform: translateX(0) !important;
            }

            /* Show burger menu only on mobile (viewport < 768px) */
            /* Using scale and opacity for smooth responsive behavior */
            .burger-menu {
                transform: scale(clamp(0, calc((768px - 100vw) * 999), 1));
                opacity: clamp(0, calc((768px - 100vw) * 999), 1);
                transform-origin: top left;
            }

            /* Desktop sidebar always visible */
            .sidebar-collapsed {
                transform: translateX(0);
            }
        </style>
    `;

    // Initialize sidebar
    function initSidebar() {
        // Check if sidebar styles already exist
        if (!document.getElementById('sidebar-styles')) {
            document.head.insertAdjacentHTML('beforeend', sidebarStyles);
        }

        // Check if sidebar container exists
        let sidebarContainer = document.getElementById('sidebar-container');

        if (sidebarContainer) {
            // Inject sidebar HTML
            sidebarContainer.innerHTML = sidebarHTML;
        } else {
            // If no container, inject at the beginning of body
            document.body.insertAdjacentHTML('afterbegin', sidebarHTML);
        }

        // Create burger menu button
        createBurgerMenu();

        // Create overlay
        createOverlay();

        // Set active menu item based on current page
        setActiveMenuItem();

        // Load notification count
        loadNotificationCount();

        // Setup event listeners
        setupEventListeners();

            }

    // Create burger menu button
    function createBurgerMenu() {
        const burgerHTML = `
            <button class="burger-menu" id="burger-menu">
                <i class="bi bi-list"></i>
            </button>
        `;

        document.body.insertAdjacentHTML('afterbegin', burgerHTML);
    }

    // Create overlay for mobile
    function createOverlay() {
        const overlayHTML = `
            <div class="sidebar-overlay" id="sidebar-overlay"></div>
        `;

        document.body.insertAdjacentHTML('afterbegin', overlayHTML);
    }

    // Set active menu item based on current URL
    function setActiveMenuItem() {
        const currentPath = window.location.pathname.toLowerCase();

        // Remove all active classes
        document.querySelectorAll('.sidebar-nav a').forEach(link => {
            link.classList.remove('active');
        });

        // Set active based on path
        if (currentPath.includes('/admindashboard')) {
            document.getElementById('nav-analytics')?.classList.add('active');
        } else if (currentPath.includes('/delayedapplications')) {
            document.getElementById('nav-delayed')?.classList.add('active');
        } else if (currentPath.includes('/admin')) {
            document.getElementById('nav-admin')?.classList.add('active');
        }
    }

    // Load notification count from server
    function loadNotificationCount() {
        fetch('/Adminuser/GetNotificationCount')
            .then(response => response.json())
            .then(data => {
                updateNotificationBadge(data.count || 0);
            })
            .catch(error => {
                            });
    }

    // Update notification badge
    function updateNotificationBadge(count) {
        const badge = document.getElementById('notification-badge');
        if (badge) {
            if (count > 0) {
                badge.textContent = count > 99 ? '99+' : count;
                badge.style.display = 'inline-block';
            } else {
                badge.style.display = 'none';
            }
        }
    }

    // Setup event listeners
    function setupEventListeners() {
        const burger = document.getElementById('burger-menu');
        const sidebar = document.getElementById('sidebar');
        const overlay = document.getElementById('sidebar-overlay');

        if (burger && sidebar && overlay) {
            // Toggle sidebar on burger click
            burger.addEventListener('click', () => {
                toggleSidebar();
            });

            // Close sidebar on overlay click
            overlay.addEventListener('click', () => {
                closeSidebar();
            });

            // Close sidebar when clicking a link on mobile
            const navLinks = sidebar.querySelectorAll('.sidebar-nav a');
            navLinks.forEach(link => {
                link.addEventListener('click', () => {
                    if (window.innerWidth <= 768) {
                        closeSidebar();
                    }
                });
            });
        }

        // Handle window resize
        window.addEventListener('resize', handleResize);
    }

    // Toggle sidebar
    function toggleSidebar() {
        const sidebar = document.getElementById('sidebar');
        const overlay = document.getElementById('sidebar-overlay');

        if (sidebar && overlay) {
            sidebar.classList.toggle('sidebar-open');
            overlay.classList.toggle('active');
        }
    }

    // Close sidebar
    function closeSidebar() {
        const sidebar = document.getElementById('sidebar');
        const overlay = document.getElementById('sidebar-overlay');

        if (sidebar && overlay) {
            sidebar.classList.remove('sidebar-open');
            overlay.classList.remove('active');
        }
    }

    // Handle window resize
    function handleResize() {
        if (window.innerWidth > 768) {
            closeSidebar();
        }
    }

    // Public API
    window.sidebar = {
        refresh: loadNotificationCount,
        close: closeSidebar,
        toggle: toggleSidebar
    };

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initSidebar);
    } else {
        initSidebar();
    }

    // ⚡ PERFORMANCE: Removed polling for notification count (previously every 2 minutes)
    // SignalR push notifications handle real-time updates more efficiently
    // This eliminates 300+ unnecessary requests per hour with 10 active admins
    // setInterval(loadNotificationCount, 120000); // REMOVED - Use SignalR instead

    })();
