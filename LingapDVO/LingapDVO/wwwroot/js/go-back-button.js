// ═══════════════════════════════════════════════════════════════════════════════
// UNIFORM GO BACK BUTTON WITH MOBILE SCROLL BEHAVIOR
// ═══════════════════════════════════════════════════════════════════════════════
// Features:
// - Uniform styling across all pages
// - Hides when scrolling down on mobile screens
// - Shows when scrolling up on mobile screens
// - Always visible on desktop screens
// ═══════════════════════════════════════════════════════════════════════════════

(function() {
    'use strict';

    // Wait for DOM to be ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initGoBackButton);
    } else {
        initGoBackButton();
    }

    function initGoBackButton() {
        const goBackBtn = document.getElementById('goBackBtn');

        if (!goBackBtn) {
            console.warn('Go Back button not found on this page');
            return;
        }

        console.log('Go Back Button: Initialized');

        // ═══════════════════════════════════════════════════════════════════════
        // MOBILE SCROLL BEHAVIOR
        // ═══════════════════════════════════════════════════════════════════════
        let lastScrollTop = 0;
        let scrollThreshold = 10; // Minimum scroll distance to trigger hide/show
        let ticking = false;

        function isMobile() {
            return window.innerWidth <= 768; // Mobile breakpoint
        }

        function handleScroll() {
            if (!ticking) {
                window.requestAnimationFrame(() => {
                    const currentScrollTop = window.pageYOffset || document.documentElement.scrollTop;

                    // Only apply scroll behavior on mobile screens
                    if (isMobile()) {
                        // Scrolling down - hide button
                        if (currentScrollTop > lastScrollTop && currentScrollTop > scrollThreshold) {
                            goBackBtn.classList.add('go-back-hidden');
                            goBackBtn.classList.remove('go-back-visible');
                        }
                        // Scrolling up - show button
                        else if (currentScrollTop < lastScrollTop) {
                            goBackBtn.classList.remove('go-back-hidden');
                            goBackBtn.classList.add('go-back-visible');
                        }
                    } else {
                        // On desktop, always show
                        goBackBtn.classList.remove('go-back-hidden');
                        goBackBtn.classList.add('go-back-visible');
                    }

                    lastScrollTop = currentScrollTop <= 0 ? 0 : currentScrollTop;
                    ticking = false;
                });

                ticking = true;
            }
        }

        // Add scroll event listener
        window.addEventListener('scroll', handleScroll, { passive: true });

        // Handle window resize (mobile <-> desktop transition)
        window.addEventListener('resize', () => {
            if (!isMobile()) {
                // On desktop, always show
                goBackBtn.classList.remove('go-back-hidden');
                goBackBtn.classList.add('go-back-visible');
            }
        });

        // Initial state - show button
        goBackBtn.classList.add('go-back-visible');

        // ═══════════════════════════════════════════════════════════════════════
        // BUTTON CLICK BEHAVIOR - CUSTOMIZABLE
        // ═══════════════════════════════════════════════════════════════════════
        // You can customize the back button behavior using data attributes:
        // - data-back-url: Custom URL to navigate to (e.g., data-back-url="/Dashboard")
        // - data-back-action: Custom action ("history", "url", or "custom")
        // - data-back-custom: Custom JavaScript function name to call
        //
        // Examples:
        // <button id="goBackBtn" data-back-url="/Dashboard">Go Back</button>
        // <button id="goBackBtn" data-back-action="history">Go Back</button>
        // <button id="goBackBtn" data-back-custom="myCustomFunction">Go Back</button>
        // ═══════════════════════════════════════════════════════════════════════

        goBackBtn.addEventListener('click', function(e) {
            e.preventDefault();

            // Get custom settings from data attributes
            const backAction = goBackBtn.getAttribute('data-back-action') || 'auto';
            const backUrl = goBackBtn.getAttribute('data-back-url');
            const customFunctionName = goBackBtn.getAttribute('data-back-custom');

            // Custom function - highest priority
            if (customFunctionName && typeof window[customFunctionName] === 'function') {
                console.log('Go Back Button: Calling custom function -', customFunctionName);
                window[customFunctionName]();
                return;
            }

            // Custom URL
            if (backUrl) {
                console.log('Go Back Button: Navigating to custom URL -', backUrl);
                window.location.href = backUrl;
                return;
            }

            // Action-based behavior
            switch (backAction) {
                case 'history':
                    // Always use browser history, fallback to homepage
                    if (window.history.length > 1) {
                        window.history.back();
                        console.log('Go Back Button: Navigating to previous page');
                    } else {
                        window.location.href = '/Homepage';
                        console.log('Go Back Button: No history, redirecting to homepage');
                    }
                    break;

                case 'url':
                    // Must have data-back-url set
                    console.warn('Go Back Button: data-back-action="url" requires data-back-url attribute');
                    break;

                case 'auto':
                default:
                    // Default behavior: use history, fallback to homepage
                    if (window.history.length > 1) {
                        window.history.back();
                        console.log('Go Back Button: Navigating to previous page');
                    } else {
                        window.location.href = '/Homepage';
                        console.log('Go Back Button: No history, redirecting to homepage');
                    }
                    break;
            }
        });

        console.log('Go Back Button: Scroll behavior and click handler attached');
    }
})();
