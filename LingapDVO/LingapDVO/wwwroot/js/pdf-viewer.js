// ═══════════════════════════════════════════════════════════════════════════════
// PDF VIEWER WITH AES-256 DECRYPTION
// ═══════════════════════════════════════════════════════════════════════════════
// Description: Complete PDF.js viewer with navigation, zoom, and secure decryption
// Used in: Admin status forms, view forms
// Dependencies: PDF.js 3.11.174, Adminuser/ViewPDF controller endpoint
// ═══════════════════════════════════════════════════════════════════════════════

(function() {
    'use strict';

    // ═══════════════════════════════════════════════════════════════════════════
    // PDF.js Configuration
    // ═══════════════════════════════════════════════════════════════════════════

    // Configure PDF.js worker
    if (typeof pdfjsLib !== 'undefined') {
        pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.worker.min.js';
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Global Variables
    // ═══════════════════════════════════════════════════════════════════════════

    let currentPDF = null;
    let currentPage = 1;
    let totalPages = 1;
    let currentScale = 1.2;

    // ═══════════════════════════════════════════════════════════════════════════
    // Main PDF Viewer Function
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Opens PDF in modal viewer with secure decryption
     * @param {string} fileName - The encrypted PDF filename
     * @param {string} fileType - Type of file (doctorprescription, deathcertificate, medicalcertificate)
     * @param {string} displayName - Display name for modal title
     */
    window.viewPDFInModal = async function(fileName, fileType, displayName) {
        const modal = document.getElementById('pdfViewerModal');
        const title = document.getElementById('pdfViewerTitle');
        const loading = document.getElementById('pdfLoading');
        const errorDiv = document.getElementById('pdfError');
        const canvas = document.getElementById('pdfCanvas');
        const currentPageSpan = document.getElementById('currentPage');
        const totalPagesSpan = document.getElementById('totalPages');
        const zoomLevelSpan = document.getElementById('zoomLevel');

        if (!modal || !canvas) {
                        return;
        }

        const context = canvas.getContext('2d');

        // Reset state
        if (title) title.textContent = displayName || 'PDF Document';
        modal.style.display = 'block';
        if (loading) loading.style.display = 'flex';
        if (errorDiv) errorDiv.style.display = 'none';
        canvas.style.display = 'none';

        currentPDF = null;
        currentPage = 1;
        currentScale = 1.2;

        try {
            // Build URL for ViewPDF endpoint (uses Adminuser controller)
            const encodedFileName = encodeURIComponent(fileName);
            const baseUrl = window.location.origin;
            const url = `${baseUrl}/Adminuser/ViewPDF?fileName=${encodedFileName}&fileType=${fileType}&t=${Date.now()}`;

                        // Fetch PDF data
            const response = await fetch(url);

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const pdfData = await response.arrayBuffer();

            if (!pdfData || pdfData.byteLength === 0) {
                throw new Error('PDF data is empty');
            }

                        // Load PDF using PDF.js
            const loadingTask = pdfjsLib.getDocument({ data: pdfData });
            currentPDF = await loadingTask.promise;

            totalPages = currentPDF.numPages;
            if (totalPagesSpan) totalPagesSpan.textContent = totalPages;
            if (currentPageSpan) currentPageSpan.textContent = currentPage;

            // Update control buttons
            updateNavigationButtons();

            // Render first page
            await renderPage(currentPage);

            // Hide loading, show canvas
            if (loading) loading.style.display = 'none';
            canvas.style.display = 'block';
            if (zoomLevelSpan) zoomLevelSpan.textContent = Math.round(currentScale * 100) + '%';

        } catch (error) {
                        if (loading) loading.style.display = 'none';
            if (errorDiv) {
                errorDiv.style.display = 'block';
                const errorMsg = errorDiv.querySelector('p');
                if (errorMsg) errorMsg.textContent = `Error loading PDF: ${error.message}`;
            }
        }
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // PDF Rendering
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Renders a specific page of the PDF
     * @param {number} pageNum - Page number to render (1-indexed)
     */
    async function renderPage(pageNum) {
        if (!currentPDF) return;

        const canvas = document.getElementById('pdfCanvas');
        const context = canvas.getContext('2d');
        const currentPageSpan = document.getElementById('currentPage');

        try {
            const page = await currentPDF.getPage(pageNum);
            const viewport = page.getViewport({ scale: currentScale });

            // Set canvas dimensions
            canvas.height = viewport.height;
            canvas.width = viewport.width;

            // Render PDF page
            const renderContext = {
                canvasContext: context,
                viewport: viewport
            };

            await page.render(renderContext).promise;
            if (currentPageSpan) currentPageSpan.textContent = pageNum;

        } catch (error) {
                    }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Navigation Functions
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Navigate to next page
     */
    window.nextPage = function() {
        if (currentPage < totalPages) {
            currentPage++;
            renderPage(currentPage);
            updateNavigationButtons();
        }
    };

    /**
     * Navigate to previous page
     */
    window.prevPage = function() {
        if (currentPage > 1) {
            currentPage--;
            renderPage(currentPage);
            updateNavigationButtons();
        }
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // Zoom Functions
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Zoom in (increase scale by 20%)
     */
    window.zoomIn = function() {
        currentScale += 0.2;
        renderPage(currentPage);
        const zoomLevelSpan = document.getElementById('zoomLevel');
        if (zoomLevelSpan) zoomLevelSpan.textContent = Math.round(currentScale * 100) + '%';
    };

    /**
     * Zoom out (decrease scale by 20%, minimum 50%)
     */
    window.zoomOut = function() {
        if (currentScale > 0.5) {
            currentScale -= 0.2;
            renderPage(currentPage);
            const zoomLevelSpan = document.getElementById('zoomLevel');
            if (zoomLevelSpan) zoomLevelSpan.textContent = Math.round(currentScale * 100) + '%';
        }
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // UI Update Functions
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Updates the state of navigation buttons (enable/disable)
     */
    function updateNavigationButtons() {
        const prevBtn = document.getElementById('prevBtn');
        const nextBtn = document.getElementById('nextBtn');

        if (prevBtn) prevBtn.disabled = currentPage <= 1;
        if (nextBtn) nextBtn.disabled = currentPage >= totalPages;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Modal Control Functions
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Closes the PDF viewer modal and cleans up resources
     */
    window.closePDFViewer = function() {
        const modal = document.getElementById('pdfViewerModal');
        if (modal) modal.style.display = 'none';

        // Clean up PDF.js resources
        if (currentPDF) {
            currentPDF.destroy();
            currentPDF = null;
        }
    };

    /**
     * Close modal when clicking outside
     */
    window.addEventListener('click', function(event) {
        const pdfModal = document.getElementById('pdfViewerModal');
        if (event.target === pdfModal) {
            window.closePDFViewer();
        }
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // Initialization
    // ═══════════════════════════════════════════════════════════════════════════

    })();
