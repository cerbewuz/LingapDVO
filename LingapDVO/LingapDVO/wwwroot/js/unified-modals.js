/**
 * ═══════════════════════════════════════════════════════════════════════════════
 * UNIFIED MODAL SYSTEM - LingapDVO
 * ═══════════════════════════════════════════════════════════════════════════════
 * Provides consistent success, error, and warning modals across the entire system.
 * Design based on the ID Verification Tab animations and styling.
 *
 * Usage:
 *   LingapModals.success('Title', 'Message', options);
 *   LingapModals.error('Title', 'Message', options);
 *   LingapModals.warning('Title', 'Message', options);
 *   LingapModals.confirm('Title', 'Message', options);
 *
 * Note: For pages with _StandardModals partial, you can also use:
 *   StandardModals.success('Title', 'Message', options);
 * ═══════════════════════════════════════════════════════════════════════════════
 */

(function(window) {
    'use strict';

    // Modal container ID
    const MODAL_CONTAINER_ID = 'lingap-unified-modal';

    // Inject CSS for animations
    function injectModalStyles() {
        if (document.getElementById('lingap-modal-styles')) return;

        const style = document.createElement('style');
        style.id = 'lingap-modal-styles';
        style.textContent = `
            @keyframes modalIconScaleIn {
                0% {
                    transform: scale(0);
                    opacity: 0;
                }
                50% {
                    transform: scale(1.1);
                }
                100% {
                    transform: scale(1);
                    opacity: 1;
                }
            }

            @keyframes modalSlideIn {
                from {
                    opacity: 0;
                    transform: scale(0.95) translateY(-20px);
                }
                to {
                    opacity: 1;
                    transform: scale(1) translateY(0);
                }
            }

            .lingap-modal-icon {
                animation: modalIconScaleIn 0.5s ease-out;
            }

            #${MODAL_CONTAINER_ID}.show .modal-dialog {
                animation: modalSlideIn 0.3s ease-out;
            }

            #${MODAL_CONTAINER_ID} .modal-content {
                border-radius: 1.5rem !important;
                box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25) !important;
            }
        `;
        document.head.appendChild(style);
    }

    /**
     * Create or get the modal container element
     */
    function getOrCreateModalContainer() {
        injectModalStyles();

        let container = document.getElementById(MODAL_CONTAINER_ID);
        if (!container) {
            container = document.createElement('div');
            container.id = MODAL_CONTAINER_ID;
            container.className = 'modal fade';
            container.setAttribute('tabindex', '-1');
            container.setAttribute('aria-hidden', 'true');
            container.innerHTML = `
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content border-0 overflow-hidden" id="lingap-modal-content">
                        <!-- Content injected dynamically -->
                    </div>
                </div>
            `;
            document.body.appendChild(container);
        }
        return container;
    }

    /**
     * Generate suggestions list HTML
     */
    function generateSuggestionsList(suggestions) {
        if (!suggestions || suggestions.length === 0) return '';

        let html = '<ul class="text-sm space-y-1 mt-3 pl-4">';
        suggestions.forEach(s => {
            html += `<li class="list-disc">${s}</li>`;
        });
        html += '</ul>';
        return html;
    }

    /**
     * Show SUCCESS modal - ID Verification Tab Design
     */
    function showSuccess(title, message, options = {}) {
        const container = getOrCreateModalContainer();
        const content = document.getElementById('lingap-modal-content');

        const buttonText = options.buttonText || 'Continue';

        content.innerHTML = `
            <div style="background: linear-gradient(to bottom right, #f0fdf4, #ffffff, #ecfdf5);">
                <div class="p-8 text-center">
                    <!-- Success Icon with Animation -->
                    <div class="lingap-modal-icon" style="width: 5rem; height: 5rem; background: linear-gradient(135deg, #22c55e 0%, #059669 100%); border-radius: 9999px; display: flex; align-items: center; justify-content: center; margin: 0 auto 1rem auto; box-shadow: 0 10px 25px -5px rgba(34, 197, 94, 0.4);">
                        <i class="fas fa-check text-white" style="font-size: 1.875rem;"></i>
                    </div>

                    <!-- Success Message -->
                    <h4 style="font-weight: 700; color: #1f2937; font-size: 1.25rem; margin-bottom: 0.5rem;">${title}</h4>
                    <p style="color: #4b5563; font-size: 0.875rem; margin-bottom: 1.5rem;">${message}</p>

                    <!-- Action Button -->
                    <button type="button" onclick="LingapModals.close()" style="width: 100%; background: linear-gradient(to right, #16a34a, #059669); color: white; padding: 0.75rem 1.5rem; border-radius: 0.75rem; font-weight: 600; border: none; cursor: pointer; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1); transition: all 0.2s;" onmouseover="this.style.background='linear-gradient(to right, #15803d, #047857)'" onmouseout="this.style.background='linear-gradient(to right, #16a34a, #059669)'">
                        ${buttonText}
                    </button>
                </div>
            </div>
        `;

        showModal(container, options);
    }

    /**
     * Show ERROR modal - ID Verification Tab Design
     */
    function showError(title, message, options = {}) {
        const container = getOrCreateModalContainer();
        const content = document.getElementById('lingap-modal-content');

        const buttonText = options.buttonText || 'OK';
        const headerTitle = options.headerTitle || 'Error';
        const suggestions = options.suggestions || [];
        const details = options.details || '';
        const icon = options.icon || 'fa-times';

        let detailsHtml = '';
        if (details || suggestions.length > 0) {
            detailsHtml = `
                <div style="background: #fef2f2; border-left: 4px solid #dc2626; border-radius: 0.5rem; padding: 1rem; text-align: left; margin-bottom: 1rem;">
                    <div style="display: flex; align-items: flex-start; gap: 0.75rem;">
                        <i class="fas fa-info-circle" style="color: #dc2626; font-size: 1.25rem; margin-top: 0.125rem;"></i>
                        <div style="flex: 1;">
                            <h6 style="font-size: 0.875rem; font-weight: 600; color: #991b1b; margin-bottom: 0.5rem;">Additional Information</h6>
                            <p style="font-size: 0.875rem; color: #374151; line-height: 1.625;">${details}</p>
                            ${generateSuggestionsList(suggestions)}
                        </div>
                    </div>
                </div>
            `;
        }

        content.innerHTML = `
            <!-- Header -->
            <div style="background: linear-gradient(to right, #dc2626, #e11d48); color: white; padding: 1rem 1.5rem; display: flex; align-items: center; justify-content: space-between;">
                <div style="display: flex; align-items: center; gap: 0.75rem;">
                    <div style="width: 2.5rem; height: 2.5rem; background: rgba(255,255,255,0.2); border-radius: 9999px; display: flex; align-items: center; justify-content: center;">
                        <i class="fas fa-exclamation-triangle text-white" style="font-size: 1.125rem;"></i>
                    </div>
                    <h5 style="font-weight: 700; font-size: 1.125rem; margin: 0;">${headerTitle}</h5>
                </div>
                <button type="button" onclick="LingapModals.close()" style="background: none; border: none; color: white; cursor: pointer; font-size: 1.25rem; opacity: 0.8;" onmouseover="this.style.opacity='1'" onmouseout="this.style.opacity='0.8'">
                    <i class="fas fa-times"></i>
                </button>
            </div>

            <!-- Body -->
            <div class="p-6 text-center">
                <div class="lingap-modal-icon" style="width: 5rem; height: 5rem; background: #fee2e2; border-radius: 9999px; display: flex; align-items: center; justify-content: center; margin: 0 auto 1rem auto;">
                    <i class="fas ${icon}" style="color: #dc2626; font-size: 1.875rem;"></i>
                </div>
                <h4 style="font-weight: 700; color: #1f2937; font-size: 1.25rem; margin-bottom: 0.75rem;">${title}</h4>
                <p style="color: #4b5563; font-size: 0.875rem; margin-bottom: 1rem;">${message}</p>

                ${detailsHtml}
            </div>

            <!-- Footer -->
            <div style="background: #f9fafb; padding: 0.75rem 1.5rem; border-top: none;">
                <button type="button" onclick="LingapModals.close()" style="width: 100%; background: linear-gradient(to right, #dc2626, #e11d48); color: white; padding: 0.75rem; border-radius: 0.75rem; font-weight: 600; border: none; cursor: pointer; transition: all 0.2s;" onmouseover="this.style.background='linear-gradient(to right, #b91c1c, #be123c)'" onmouseout="this.style.background='linear-gradient(to right, #dc2626, #e11d48)'">
                    ${buttonText}
                </button>
            </div>
        `;

        showModal(container, options);
    }

    /**
     * Show WARNING modal - ID Verification Tab Design
     */
    function showWarning(title, message, options = {}) {
        const container = getOrCreateModalContainer();
        const content = document.getElementById('lingap-modal-content');

        const buttonText = options.buttonText || 'OK';
        const headerTitle = options.headerTitle || 'Warning';
        const suggestions = options.suggestions || [];

        let suggestionsHtml = '';
        if (suggestions.length > 0) {
            suggestionsHtml = `
                <div style="background: #fffbeb; border-left: 4px solid #f59e0b; border-radius: 0.5rem; padding: 1rem; text-align: left; margin-bottom: 1rem;">
                    <div style="display: flex; align-items: flex-start; gap: 0.75rem;">
                        <i class="fas fa-lightbulb" style="color: #f59e0b; font-size: 1.25rem; margin-top: 0.125rem;"></i>
                        <div style="flex: 1;">
                            ${generateSuggestionsList(suggestions)}
                        </div>
                    </div>
                </div>
            `;
        }

        content.innerHTML = `
            <!-- Header -->
            <div style="background: linear-gradient(to right, #f59e0b, #f97316); color: white; padding: 1rem 1.5rem; display: flex; align-items: center; justify-content: space-between;">
                <div style="display: flex; align-items: center; gap: 0.75rem;">
                    <div style="width: 2.5rem; height: 2.5rem; background: rgba(255,255,255,0.2); border-radius: 9999px; display: flex; align-items: center; justify-content: center;">
                        <i class="fas fa-exclamation-circle text-white" style="font-size: 1.125rem;"></i>
                    </div>
                    <h5 style="font-weight: 700; font-size: 1.125rem; margin: 0;">${headerTitle}</h5>
                </div>
                <button type="button" onclick="LingapModals.close()" style="background: none; border: none; color: white; cursor: pointer; font-size: 1.25rem; opacity: 0.8;" onmouseover="this.style.opacity='1'" onmouseout="this.style.opacity='0.8'">
                    <i class="fas fa-times"></i>
                </button>
            </div>

            <!-- Body -->
            <div class="p-6 text-center" style="background: linear-gradient(to bottom right, #fffbeb, #ffffff, #fff7ed);">
                <div class="lingap-modal-icon" style="width: 5rem; height: 5rem; background: #fef3c7; border-radius: 9999px; display: flex; align-items: center; justify-content: center; margin: 0 auto 1rem auto;">
                    <i class="fas fa-exclamation" style="color: #d97706; font-size: 1.875rem;"></i>
                </div>
                <h4 style="font-weight: 700; color: #1f2937; font-size: 1.25rem; margin-bottom: 0.75rem;">${title}</h4>
                <p style="color: #4b5563; font-size: 0.875rem; margin-bottom: 1rem;">${message}</p>

                ${suggestionsHtml}
            </div>

            <!-- Footer -->
            <div style="background: #f9fafb; padding: 0.75rem 1.5rem; border-top: none;">
                <button type="button" onclick="LingapModals.close()" style="width: 100%; background: linear-gradient(to right, #f59e0b, #f97316); color: white; padding: 0.75rem; border-radius: 0.75rem; font-weight: 600; border: none; cursor: pointer; transition: all 0.2s;" onmouseover="this.style.background='linear-gradient(to right, #d97706, #ea580c)'" onmouseout="this.style.background='linear-gradient(to right, #f59e0b, #f97316)'">
                    ${buttonText}
                </button>
            </div>
        `;

        showModal(container, options);
    }

    /**
     * Show INFO modal - ID Verification Tab Design
     */
    function showInfo(title, message, options = {}) {
        const container = getOrCreateModalContainer();
        const content = document.getElementById('lingap-modal-content');

        const buttonText = options.buttonText || 'Got It';
        const headerTitle = options.headerTitle || 'Information';

        content.innerHTML = `
            <!-- Header -->
            <div style="background: linear-gradient(to right, #2563eb, #4f46e5); color: white; padding: 1rem 1.5rem; display: flex; align-items: center; justify-content: space-between;">
                <div style="display: flex; align-items: center; gap: 0.75rem;">
                    <div style="width: 2.5rem; height: 2.5rem; background: rgba(255,255,255,0.2); border-radius: 9999px; display: flex; align-items: center; justify-content: center;">
                        <i class="fas fa-info-circle text-white" style="font-size: 1.125rem;"></i>
                    </div>
                    <h5 style="font-weight: 700; font-size: 1.125rem; margin: 0;">${headerTitle}</h5>
                </div>
                <button type="button" onclick="LingapModals.close()" style="background: none; border: none; color: white; cursor: pointer; font-size: 1.25rem; opacity: 0.8;" onmouseover="this.style.opacity='1'" onmouseout="this.style.opacity='0.8'">
                    <i class="fas fa-times"></i>
                </button>
            </div>

            <!-- Body -->
            <div class="p-6 text-center" style="background: linear-gradient(to bottom right, #eff6ff, #ffffff, #eef2ff);">
                <div class="lingap-modal-icon" style="width: 5rem; height: 5rem; background: #dbeafe; border-radius: 9999px; display: flex; align-items: center; justify-content: center; margin: 0 auto 1rem auto;">
                    <i class="fas fa-info" style="color: #2563eb; font-size: 1.875rem;"></i>
                </div>
                <h4 style="font-weight: 700; color: #1f2937; font-size: 1.25rem; margin-bottom: 0.75rem;">${title}</h4>
                <p style="color: #4b5563; font-size: 0.875rem; margin-bottom: 1rem;">${message}</p>
            </div>

            <!-- Footer -->
            <div style="background: #f9fafb; padding: 0.75rem 1.5rem; border-top: none;">
                <button type="button" onclick="LingapModals.close()" style="width: 100%; background: linear-gradient(to right, #2563eb, #4f46e5); color: white; padding: 0.75rem; border-radius: 0.75rem; font-weight: 600; border: none; cursor: pointer; transition: all 0.2s;" onmouseover="this.style.background='linear-gradient(to right, #1d4ed8, #4338ca)'" onmouseout="this.style.background='linear-gradient(to right, #2563eb, #4f46e5)'">
                    ${buttonText}
                </button>
            </div>
        `;

        showModal(container, options);
    }

    /**
     * Show CONFIRM modal - ID Verification Tab Design
     */
    function showConfirm(title, message, options = {}) {
        const container = getOrCreateModalContainer();
        const content = document.getElementById('lingap-modal-content');

        const confirmText = options.confirmText || 'Confirm';
        const cancelText = options.cancelText || 'Cancel';
        const headerTitle = options.headerTitle || 'Confirm Action';
        const isDangerous = options.dangerous || false;

        const headerBg = isDangerous
            ? 'linear-gradient(to right, #dc2626, #e11d48)'
            : 'linear-gradient(to right, #2563eb, #4f46e5)';
        const bodyBg = isDangerous
            ? 'linear-gradient(to bottom right, #fef2f2, #ffffff, #fff1f2)'
            : 'linear-gradient(to bottom right, #eff6ff, #ffffff, #eef2ff)';
        const iconBg = isDangerous ? '#fee2e2' : '#dbeafe';
        const iconColor = isDangerous ? '#dc2626' : '#2563eb';
        const confirmBg = isDangerous
            ? 'linear-gradient(to right, #dc2626, #e11d48)'
            : 'linear-gradient(to right, #2563eb, #4f46e5)';
        const confirmBgHover = isDangerous
            ? 'linear-gradient(to right, #b91c1c, #be123c)'
            : 'linear-gradient(to right, #1d4ed8, #4338ca)';

        // Store callbacks in window for onclick access
        window._lingapConfirmCallback = options.onConfirm;
        window._lingapCancelCallback = options.onCancel;

        content.innerHTML = `
            <!-- Header -->
            <div style="background: ${headerBg}; color: white; padding: 1rem 1.5rem; display: flex; align-items: center;">
                <div style="display: flex; align-items: center; gap: 0.75rem;">
                    <div style="width: 2.5rem; height: 2.5rem; background: rgba(255,255,255,0.2); border-radius: 9999px; display: flex; align-items: center; justify-content: center;">
                        <i class="fas fa-question-circle text-white" style="font-size: 1.125rem;"></i>
                    </div>
                    <h5 style="font-weight: 700; font-size: 1.125rem; margin: 0;">${headerTitle}</h5>
                </div>
            </div>

            <!-- Body -->
            <div class="p-6 text-center" style="background: ${bodyBg};">
                <div class="lingap-modal-icon" style="width: 5rem; height: 5rem; background: ${iconBg}; border-radius: 9999px; display: flex; align-items: center; justify-content: center; margin: 0 auto 1rem auto;">
                    <i class="fas fa-question" style="color: ${iconColor}; font-size: 1.875rem;"></i>
                </div>
                <h4 style="font-weight: 700; color: #1f2937; font-size: 1.25rem; margin-bottom: 0.75rem;">${title}</h4>
                <p style="color: #4b5563; font-size: 0.875rem; margin-bottom: 1rem;">${message}</p>
            </div>

            <!-- Footer -->
            <div style="background: #f9fafb; padding: 0.75rem 1.5rem; border-top: none; display: flex; gap: 0.75rem;">
                <button type="button" onclick="LingapModals._handleCancel()" style="flex: 1; background: #e5e7eb; color: #374151; padding: 0.75rem; border-radius: 0.75rem; font-weight: 600; border: none; cursor: pointer; transition: all 0.2s;" onmouseover="this.style.background='#d1d5db'" onmouseout="this.style.background='#e5e7eb'">
                    ${cancelText}
                </button>
                <button type="button" onclick="LingapModals._handleConfirm()" id="lingap-confirm-btn" style="flex: 1; background: ${confirmBg}; color: white; padding: 0.75rem; border-radius: 0.75rem; font-weight: 600; border: none; cursor: pointer; transition: all 0.2s;" onmouseover="this.style.background='${confirmBgHover}'" onmouseout="this.style.background='${confirmBg}'">
                    ${confirmText}
                </button>
            </div>
        `;

        showModal(container, { ...options, backdrop: 'static' });
    }

    /**
     * Show LOADING modal with spinner
     */
    function showLoading(message = 'Please wait...', options = {}) {
        const container = getOrCreateModalContainer();
        const content = document.getElementById('lingap-modal-content');

        content.innerHTML = `
            <div style="background: linear-gradient(to bottom right, #1f2937, #374151, #1f2937); padding: 2rem;">
                <div style="display: flex; flex-direction: column; align-items: center; text-align: center;">
                    <!-- Animated Spinner -->
                    <div style="margin-bottom: 1.25rem;">
                        <div style="width: 5rem; height: 5rem; display: flex; align-items: center; justify-content: center;">
                            <div style="width: 4rem; height: 4rem; border: 4px solid rgba(255,255,255,0.3); border-top-color: white; border-radius: 9999px; animation: spin 1s linear infinite;"></div>
                        </div>
                    </div>

                    <!-- Message -->
                    <p style="color: white; font-size: 1.25rem; font-weight: 500; margin: 0;">${message}</p>
                    <p style="color: rgba(255,255,255,0.6); font-size: 0.875rem; margin-top: 0.5rem;">This may take a moment...</p>
                </div>
            </div>
            <style>
                @keyframes spin {
                    to { transform: rotate(360deg); }
                }
            </style>
        `;

        showModal(container, { ...options, backdrop: 'static', keyboard: false });
    }

    /**
     * Internal: Show the modal using Bootstrap
     */
    function showModal(container, options = {}) {
        try {
            // Remove any existing modal instance
            const existingInstance = bootstrap.Modal.getInstance(container);
            if (existingInstance) {
                existingInstance.dispose();
            }

            const modalOptions = {
                backdrop: options.backdrop || true,
                keyboard: options.keyboard !== false
            };

            const bsModal = new bootstrap.Modal(container, modalOptions);

            // Handle close callback
            if (options.onClose) {
                container.addEventListener('hidden.bs.modal', function handler() {
                    options.onClose();
                    container.removeEventListener('hidden.bs.modal', handler);
                }, { once: true });
            }

            // Handle redirect on close
            if (options.redirectUrl) {
                container.addEventListener('hidden.bs.modal', function handler() {
                    window.location.href = options.redirectUrl;
                    container.removeEventListener('hidden.bs.modal', handler);
                }, { once: true });
            }

            // Auto-close if specified
            if (options.autoClose && options.autoClose > 0) {
                setTimeout(() => {
                    closeModal();
                }, options.autoClose);
            }

            bsModal.show();
        } catch (e) {
            console.error('Error showing modal:', e);
            // Fallback to alert
            alert(document.getElementById('lingap-modal-content')?.textContent || 'Modal error');
        }
    }

    /**
     * Close the modal
     */
    function closeModal() {
        const container = document.getElementById(MODAL_CONTAINER_ID);
        if (container) {
            const bsModal = bootstrap.Modal.getInstance(container);
            if (bsModal) {
                bsModal.hide();
            }
        }
    }

    /**
     * Internal: Handle confirm button click
     */
    function handleConfirm() {
        closeModal();
        if (typeof window._lingapConfirmCallback === 'function') {
            window._lingapConfirmCallback();
        }
    }

    /**
     * Internal: Handle cancel button click
     */
    function handleCancel() {
        closeModal();
        if (typeof window._lingapCancelCallback === 'function') {
            window._lingapCancelCallback();
        }
    }

    // Expose the API globally
    window.LingapModals = {
        success: showSuccess,
        error: showError,
        warning: showWarning,
        info: showInfo,
        confirm: showConfirm,
        loading: showLoading,
        close: closeModal,
        _handleConfirm: handleConfirm,
        _handleCancel: handleCancel
    };

    // Also provide legacy function names for backwards compatibility
    window.showUnifiedSuccessModal = showSuccess;
    window.showUnifiedErrorModal = showError;
    window.showUnifiedWarningModal = showWarning;
    window.showUnifiedInfoModal = showInfo;
    window.showUnifiedConfirmModal = showConfirm;

})(window);
