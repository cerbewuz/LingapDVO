// ═══════════════════════════════════════════════════════════════════════════════
// IMAGE ZOOM FUNCTIONALITY
// ═══════════════════════════════════════════════════════════════════════════════
// Description: Full-screen modal zoom for images
// Used in: Admin status forms, view forms
// Dependencies: None
// ═══════════════════════════════════════════════════════════════════════════════

(function() {
    'use strict';

    // ═══════════════════════════════════════════════════════════════════════════
    // Image Zoom Function
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Opens an image in a full-screen zoom modal
     * @param {string} src - Image source URL or data URI
     */
    window.zoomImage = function(src) {
        const modal = document.getElementById("zoomModal");
        const modalImg = document.getElementById("zoomedImage");

        if (!modal || !modalImg) {
                        return;
        }

        modal.style.display = "block";
        modalImg.src = src;
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // Close Zoom Modal Function
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Closes the image zoom modal
     */
    window.closeZoomModal = function() {
        const modal = document.getElementById("zoomModal");
        if (modal) {
            modal.style.display = "none";
        }
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // Event Listeners
    // ═══════════════════════════════════════════════════════════════════════════

    document.addEventListener('DOMContentLoaded', function() {
        // Close button click
        const closeZoomBtn = document.querySelector(".close-zoom");
        if (closeZoomBtn) {
            closeZoomBtn.onclick = function() {
                window.closeZoomModal();
            };
        }

        // Close when clicking outside the image
        const zoomModal = document.getElementById("zoomModal");
        if (zoomModal) {
            zoomModal.onclick = function(event) {
                if (event.target === zoomModal) {
                    window.closeZoomModal();
                }
            };
        }

        // Close on ESC key
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape') {
                window.closeZoomModal();
            }
        });
    });

    })();
