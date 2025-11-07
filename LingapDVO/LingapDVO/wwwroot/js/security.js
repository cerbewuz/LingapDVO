// ═══════════════════════════════════════════════════════════════════════════════
// SECURITY SCRIPTS
// ═══════════════════════════════════════════════════════════════════════════════
// Description: Document protection - prevents copying, downloading, and unauthorized access
// Used in: Admin status forms, view forms
// Dependencies: None
// ═══════════════════════════════════════════════════════════════════════════════

(function() {
    'use strict';

    // ═══════════════════════════════════════════════════════════════════════════
    // DISABLE RIGHT-CLICK CONTEXT MENU
    // ═══════════════════════════════════════════════════════════════════════════

    document.addEventListener('contextmenu', function(e) {
        e.preventDefault();
        return false;
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // DISABLE TEXT SELECTION
    // ═══════════════════════════════════════════════════════════════════════════

    document.addEventListener('selectstart', function(e) {
        e.preventDefault();
        return false;
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // DISABLE DRAG AND DROP FOR IMAGES
    // ═══════════════════════════════════════════════════════════════════════════

    document.addEventListener('dragstart', function(e) {
        if (e.target.tagName === 'IMG') {
            e.preventDefault();
            return false;
        }
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // DISABLE KEYBOARD SHORTCUTS
    // ═══════════════════════════════════════════════════════════════════════════

    document.addEventListener('keydown', function(e) {
        // Disable F12 (Developer Tools)
        if (e.key === 'F12') {
            e.preventDefault();
            return false;
        }

        // Disable Ctrl+Shift+I (Developer Tools)
        if (e.ctrlKey && e.shiftKey && e.key === 'I') {
            e.preventDefault();
            return false;
        }

        // Disable Ctrl+Shift+J (Console)
        if (e.ctrlKey && e.shiftKey && e.key === 'J') {
            e.preventDefault();
            return false;
        }

        // Disable Ctrl+U (View Source)
        if (e.ctrlKey && e.key === 'u') {
            e.preventDefault();
            return false;
        }

        // Disable Ctrl+S (Save)
        if (e.ctrlKey && e.key === 's') {
            e.preventDefault();
            return false;
        }

        // Disable Ctrl+P (Print) - Allow only through designated print buttons
        if (e.ctrlKey && e.key === 'p') {
            e.preventDefault();
            return false;
        }

        // Disable Ctrl+C (Copy) on images and sensitive content
        if (e.ctrlKey && e.key === 'c') {
            const selection = window.getSelection();
            if (selection && selection.toString().length > 0) {
                // Allow copying from input/textarea fields
                const activeElement = document.activeElement;
                if (activeElement.tagName === 'INPUT' || activeElement.tagName === 'TEXTAREA') {
                    return true;
                }
                e.preventDefault();
                return false;
            }
        }
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // ADD CSS TO PREVENT TEXT SELECTION
    // ═══════════════════════════════════════════════════════════════════════════

    const style = document.createElement('style');
    style.textContent = `
        body {
            -webkit-user-select: none;
            -moz-user-select: none;
            -ms-user-select: none;
            user-select: none;
        }

        /* Allow selection in form fields for editing */
        input, textarea, select {
            -webkit-user-select: text !important;
            -moz-user-select: text !important;
            -ms-user-select: text !important;
            user-select: text !important;
        }

        /* Prevent image dragging */
        img {
            -webkit-user-drag: none;
            -khtml-user-drag: none;
            -moz-user-drag: none;
            -o-user-drag: none;
            user-drag: none;
            pointer-events: auto;
        }

        /* Prevent copying via CSS */
        .no-copy {
            -webkit-user-select: none;
            -moz-user-select: none;
            -ms-user-select: none;
            user-select: none;
        }
    `;
    document.head.appendChild(style);

    // ═══════════════════════════════════════════════════════════════════════════
    // DISABLE SCREENSHOT TOOLS (Limited Effectiveness)
    // ═══════════════════════════════════════════════════════════════════════════

    // Detect PrintScreen key (Windows)
    document.addEventListener('keyup', function(e) {
        if (e.key === 'PrintScreen') {
            console.warn('Screenshot attempt detected');
            // Note: Cannot actually prevent screenshots, but can log/warn
        }
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // CONSOLE WARNING MESSAGE
    // ═══════════════════════════════════════════════════════════════════════════

    console.log('%c⚠️ SECURITY WARNING', 'color: red; font-size: 20px; font-weight: bold;');
    console.log('%cThis is a secure area. Unauthorized access, copying, or distribution of documents is prohibited and may be subject to legal action.', 'color: red; font-size: 14px;');
    console.log('%cIf you are not an authorized administrator, please close this window immediately.', 'color: red; font-size: 14px;');

    // ═══════════════════════════════════════════════════════════════════════════
    // BLUR DETECTION (Tab/Window Switch)
    // ═══════════════════════════════════════════════════════════════════════════

    let blurCount = 0;
    window.addEventListener('blur', function() {
        blurCount++;
        if (blurCount > 5) {
            console.warn('Multiple window switches detected. Monitoring for security.');
        }
    });

    console.log('Security measures initialized');

})();
