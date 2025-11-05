// ═══════════════════════════════════════════════════════════════════════════════
// VISUAL PROGRESS LOADER
// Shows step-by-step progress for OCR extraction and account verification
// ═══════════════════════════════════════════════════════════════════════════════

class VisualProgressLoader {
    constructor() {
        this.modal = null;
        this.currentStep = 0;
        this.totalSteps = 0;
        this.steps = [];
        this.isVisible = false;
    }

    /**
     * Initialize the loader with steps
     * @param {Array<string>} steps - Array of step descriptions
     */
    initialize(steps) {
        this.steps = steps;
        this.totalSteps = steps.length;
        this.currentStep = 0;
        this.createModal();
    }

    /**
     * Create the modal HTML
     */
    createModal() {
        // Remove existing modal if present
        const existingModal = document.getElementById('visual-progress-modal');
        if (existingModal) {
            existingModal.remove();
        }

        // Create modal element
        const modalHTML = `
            <div id="visual-progress-modal" class="fixed inset-0 z-50 hidden">
                <!-- Backdrop -->
                <div class="fixed inset-0 bg-black bg-opacity-50 backdrop-blur-sm transition-opacity"></div>

                <!-- Modal -->
                <div class="fixed inset-0 flex items-center justify-center p-4">
                    <div class="bg-white rounded-2xl shadow-2xl max-w-md w-full p-8 transform transition-all">
                        <!-- Header -->
                        <div class="text-center mb-6">
                            <div class="inline-flex items-center justify-center w-16 h-16 bg-crimson-100 rounded-full mb-4">
                                <i class="fas fa-cog fa-spin text-3xl text-crimson-600"></i>
                            </div>
                            <h3 class="text-2xl font-bold text-gray-800 mb-2" id="progress-modal-title">Processing...</h3>
                            <p class="text-gray-600" id="progress-modal-subtitle">Please wait while we verify your ID</p>
                        </div>

                        <!-- Progress Bar -->
                        <div class="mb-6">
                            <div class="flex justify-between text-sm text-gray-600 mb-2">
                                <span>Progress</span>
                                <span id="progress-percentage">0%</span>
                            </div>
                            <div class="w-full bg-gray-200 rounded-full h-3 overflow-hidden">
                                <div id="progress-bar-fill"
                                     class="bg-gradient-to-r from-crimson-500 to-crimson-600 h-3 rounded-full transition-all duration-500 ease-out"
                                     style="width: 0%"></div>
                            </div>
                        </div>

                        <!-- Steps List -->
                        <div class="space-y-3" id="progress-steps-list">
                            <!-- Steps will be dynamically added here -->
                        </div>

                        <!-- Current Step Message -->
                        <div class="mt-6 p-4 bg-blue-50 border-l-4 border-blue-500 rounded-r">
                            <div class="flex items-center">
                                <i class="fas fa-info-circle text-blue-500 mr-3"></i>
                                <p class="text-sm text-gray-700" id="current-step-message">Initializing...</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', modalHTML);
        this.modal = document.getElementById('visual-progress-modal');

        // Render steps
        this.renderSteps();
    }

    /**
     * Render the steps list
     */
    renderSteps() {
        const stepsList = document.getElementById('progress-steps-list');
        if (!stepsList) return;

        stepsList.innerHTML = '';

        this.steps.forEach((step, index) => {
            const stepHTML = `
                <div class="flex items-center gap-3 transition-all duration-300" data-step="${index}">
                    <div class="flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center transition-all duration-300
                                ${index < this.currentStep ? 'bg-green-500' : index === this.currentStep ? 'bg-blue-500' : 'bg-gray-300'}">
                        ${index < this.currentStep
                            ? '<i class="fas fa-check text-white text-sm"></i>'
                            : index === this.currentStep
                            ? '<i class="fas fa-spinner fa-spin text-white text-sm"></i>'
                            : `<span class="text-white text-xs font-semibold">${index + 1}</span>`
                        }
                    </div>
                    <div class="flex-1">
                        <p class="text-sm font-medium transition-all duration-300
                                  ${index < this.currentStep ? 'text-green-600' : index === this.currentStep ? 'text-blue-600' : 'text-gray-500'}">
                            ${step}
                        </p>
                    </div>
                </div>
            `;
            stepsList.insertAdjacentHTML('beforeend', stepHTML);
        });
    }

    /**
     * Show the loader
     */
    show() {
        if (this.modal) {
            this.modal.classList.remove('hidden');
            this.isVisible = true;
            document.body.style.overflow = 'hidden'; // Prevent scrolling
        }
    }

    /**
     * Hide the loader
     */
    hide() {
        if (this.modal) {
            this.modal.classList.add('hidden');
            this.isVisible = false;
            document.body.style.overflow = ''; // Restore scrolling
        }
    }

    /**
     * Update to next step
     * @param {string} message - Optional custom message for the step
     */
    nextStep(message = null) {
        if (this.currentStep < this.totalSteps) {
            this.currentStep++;
            this.updateProgress();

            if (message) {
                this.updateMessage(message);
            }
        }
    }

    /**
     * Go to a specific step
     * @param {number} stepIndex - Index of the step (0-based)
     * @param {string} message - Optional custom message
     */
    goToStep(stepIndex, message = null) {
        if (stepIndex >= 0 && stepIndex <= this.totalSteps) {
            this.currentStep = stepIndex;
            this.updateProgress();

            if (message) {
                this.updateMessage(message);
            }
        }
    }

    /**
     * Update the progress display
     */
    updateProgress() {
        const percentage = Math.round((this.currentStep / this.totalSteps) * 100);

        // Update progress bar
        const progressBar = document.getElementById('progress-bar-fill');
        if (progressBar) {
            progressBar.style.width = `${percentage}%`;
        }

        // Update percentage text
        const percentageText = document.getElementById('progress-percentage');
        if (percentageText) {
            percentageText.textContent = `${percentage}%`;
        }

        // Update steps visual
        this.renderSteps();

        // Update current step message
        if (this.currentStep > 0 && this.currentStep <= this.totalSteps) {
            const currentMessage = this.steps[this.currentStep - 1];
            this.updateMessage(currentMessage);
        }
    }

    /**
     * Update the current step message
     * @param {string} message - Message to display
     */
    updateMessage(message) {
        const messageElement = document.getElementById('current-step-message');
        if (messageElement) {
            messageElement.textContent = message;
        }
    }

    /**
     * Update the modal title
     * @param {string} title - New title
     */
    updateTitle(title) {
        const titleElement = document.getElementById('progress-modal-title');
        if (titleElement) {
            titleElement.textContent = title;
        }
    }

    /**
     * Update the modal subtitle
     * @param {string} subtitle - New subtitle
     */
    updateSubtitle(subtitle) {
        const subtitleElement = document.getElementById('progress-modal-subtitle');
        if (subtitleElement) {
            subtitleElement.textContent = subtitle;
        }
    }

    /**
     * Show completion state
     * @param {boolean} success - Whether the process completed successfully
     * @param {string} message - Completion message
     */
    showCompletion(success, message) {
        this.currentStep = this.totalSteps;
        this.updateProgress();

        // Update modal appearance
        const modalContent = this.modal.querySelector('.bg-white');
        const icon = this.modal.querySelector('.fa-cog');
        const iconContainer = icon.parentElement;

        if (success) {
            // Success state
            iconContainer.classList.remove('bg-crimson-100');
            iconContainer.classList.add('bg-green-100');
            icon.classList.remove('fa-cog', 'fa-spin', 'text-crimson-600');
            icon.classList.add('fa-check-circle', 'text-green-600');

            this.updateTitle('Verification Complete!');
            this.updateSubtitle('Your ID has been successfully verified');
            this.updateMessage(message || 'All steps completed successfully');

            // Update progress bar to green
            const progressBar = document.getElementById('progress-bar-fill');
            if (progressBar) {
                progressBar.classList.remove('from-crimson-500', 'to-crimson-600');
                progressBar.classList.add('from-green-500', 'to-green-600');
            }
        } else {
            // Error state
            iconContainer.classList.remove('bg-crimson-100');
            iconContainer.classList.add('bg-red-100');
            icon.classList.remove('fa-cog', 'fa-spin', 'text-crimson-600');
            icon.classList.add('fa-exclamation-circle', 'text-red-600');

            this.updateTitle('Verification Failed');
            this.updateSubtitle('There was an issue verifying your ID');
            this.updateMessage(message || 'Please try again or contact support');

            // Update progress bar to red
            const progressBar = document.getElementById('progress-bar-fill');
            if (progressBar) {
                progressBar.classList.remove('from-crimson-500', 'to-crimson-600');
                progressBar.classList.add('from-red-500', 'to-red-600');
            }
        }

        // Add close button
        const currentMessage = document.getElementById('current-step-message');
        if (currentMessage && currentMessage.parentElement) {
            const closeButton = document.createElement('button');
            closeButton.className = 'mt-4 w-full bg-gray-800 hover:bg-gray-700 text-white font-semibold py-3 px-6 rounded-xl transition-colors duration-300';
            closeButton.textContent = 'Close';
            closeButton.onclick = () => this.hide();
            currentMessage.parentElement.after(closeButton);
        }

        // Auto-hide after delay if successful
        if (success) {
            setTimeout(() => {
                this.hide();
            }, 3000);
        }
    }

    /**
     * Show error state
     * @param {string} errorMessage - Error message to display
     */
    showError(errorMessage) {
        this.showCompletion(false, errorMessage);
    }

    /**
     * Show success state
     * @param {string} successMessage - Success message to display
     */
    showSuccess(successMessage) {
        this.showCompletion(true, successMessage);
    }
}

// Create global instance
window.visualProgressLoader = new VisualProgressLoader();
