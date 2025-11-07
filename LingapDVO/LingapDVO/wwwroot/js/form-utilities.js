// ═══════════════════════════════════════════════════════════════════════════════
// FORM UTILITIES
// ═══════════════════════════════════════════════════════════════════════════════
// Description: Form field formatting, validation, and helper functions
// Used in: Input forms, edit forms
// Dependencies: None
// ═══════════════════════════════════════════════════════════════════════════════

(function() {
    'use strict';

    // ═══════════════════════════════════════════════════════════════════════════
    // PHONE NUMBER FORMATTING
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Initializes Philippine phone number formatting
     * Formats: 09XX-XXX-XXXX (mobile) or 0X-XXX-XXXX (landline)
     */
    function initPhoneNumberFormatting() {
        const contactNoInput = document.getElementById('contactNoInput');
        if (!contactNoInput) return;

        contactNoInput.addEventListener('input', function(e) {
            let value = this.value.replace(/\D/g, '');

            // Mobile: 09XX-XXX-XXXX (13 chars with dashes)
            if (value.startsWith('09') && value.length <= 11) {
                if (value.length > 4) {
                    value = value.substring(0, 4) + '-' + value.substring(4);
                }
                if (value.length > 8) {
                    value = value.substring(0, 8) + '-' + value.substring(8);
                }
            }
            // Landline: 0X-XXX-XXXX (11 chars with dashes)
            else if ((value.startsWith('02') || /^[1-9]\d{1}$/.test(value.substring(0, 2)))) {
                if (value.length > 2) {
                    value = value.substring(0, 2) + '-' + value.substring(2);
                }
                if (value.length > 6) {
                    value = value.substring(0, 6) + '-' + value.substring(6);
                }
            }

            const maxLength = value.startsWith('09') ? 13 : 11;
            if (value.length > maxLength) {
                value = value.substring(0, maxLength);
            }

            this.value = value;
        });

        // Validation on blur
        contactNoInput.addEventListener('blur', function() {
            const regex = /^(09\d{2}-\d{3}-\d{4}|0[2-9]\d-\d{3}-\d{4})$/;
            if (this.value && !regex.test(this.value)) {
                alert("Please enter a valid Philippine number (e.g., 0912-345-6789 or 02-123-4567)");
                this.focus();
            }
        });

        console.log('Phone number formatting initialized');
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PHILHEALTH NUMBER FORMATTING
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Initializes PhilHealth number formatting
     * Format: XXXX-XXXX-XXXX
     */
    function initPhilHealthFormatting() {
        const philHealthInput = document.getElementById('philHealthInput');
        if (!philHealthInput) return;

        philHealthInput.addEventListener('input', function(e) {
            let value = this.value.replace(/\D/g, '');

            if (value.length > 4) {
                value = value.substring(0, 4) + '-' + value.substring(4);
            }
            if (value.length > 9) {
                value = value.substring(0, 9) + '-' + value.substring(9);
            }
            if (value.length > 14) {
                value = value.substring(0, 14);
            }

            this.value = value;
        });

        console.log('PhilHealth formatting initialized');
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FILE UPLOAD PREVIEW
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Previews uploaded file (image or PDF)
     * @param {HTMLInputElement} input - File input element
     * @param {string} previewId - ID of preview image element
     */
    window.previewImage = function(input, previewId) {
        const file = input.files[0];
        const preview = document.getElementById(previewId);
        const uploadBox = input.previousElementSibling;
        const uploadText = document.getElementById('uploadText');
        const fileStatus = document.getElementById('fileStatus');

        if (file) {
            // File validation
            const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'application/pdf'];
            const maxSize = 10 * 1024 * 1024; // 10MB

            if (!validTypes.includes(file.type)) {
                if (fileStatus) {
                    fileStatus.innerHTML = '<span class="file-error">Invalid file type. Please upload JPG, PNG, or PDF files only.</span>';
                }
                input.value = '';
                return;
            }

            if (file.size > maxSize) {
                if (fileStatus) {
                    fileStatus.innerHTML = '<span class="file-error">File size too large. Maximum size is 10MB.</span>';
                }
                input.value = '';
                return;
            }

            // Update UI for valid file
            if (fileStatus) {
                fileStatus.innerHTML = '<span class="file-success">File selected: ' + file.name + '</span>';
            }

            if (uploadText) {
                uploadText.textContent = file.name;
            }

            if (uploadBox) {
                uploadBox.style.borderColor = "var(--success-color, #28a745)";
                uploadBox.style.backgroundColor = "rgba(40, 167, 69, 0.1)";
            }

            // Show preview for images only
            if (file.type.startsWith("image/") && preview) {
                const reader = new FileReader();
                reader.onload = function(e) {
                    preview.src = e.target.result;
                    preview.style.display = "block";
                };
                reader.readAsDataURL(file);
            } else if (preview) {
                // Hide preview for PDF files
                preview.style.display = "none";
            }
        } else {
            // Reset if no file
            if (preview) preview.style.display = "none";
            if (fileStatus) fileStatus.innerHTML = '';
            if (uploadText) {
                uploadText.textContent = 'Click to upload new document';
            }
            if (uploadBox) {
                uploadBox.style.borderColor = "#ddd";
                uploadBox.style.backgroundColor = "#fafafa";
            }
        }
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // ASSISTANCE TYPE CHECKBOX HANDLING
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Initializes single-selection checkbox behavior for assistance types
     * Only one assistance type can be selected at a time
     */
    function initAssistanceCheckboxes() {
        const assistanceCheckboxes = document.querySelectorAll('.assistance-type');
        const assistanceSpecInputs = document.querySelectorAll('.assistance-spec');

        if (assistanceCheckboxes.length === 0) return;

        // Add event listeners to all assistance checkboxes
        assistanceCheckboxes.forEach(checkbox => {
            checkbox.addEventListener('change', function() {
                // If this checkbox is being checked, uncheck all others
                if (this.checked) {
                    assistanceCheckboxes.forEach(otherCheckbox => {
                        if (otherCheckbox !== this) {
                            otherCheckbox.checked = false;
                            // Hide and clear specification inputs for unselected checkboxes
                            const specInput = otherCheckbox.parentNode.querySelector('.assistance-spec');
                            if (specInput) {
                                specInput.style.display = 'none';
                                specInput.value = '';
                            }
                        }
                    });
                }

                // Handle specification input for the current checkbox
                const specInput = this.parentNode.querySelector('.assistance-spec');
                if (specInput) {
                    specInput.style.display = this.checked ? 'block' : 'none';
                    if (!this.checked) {
                        specInput.value = '';
                    }
                }

                updateAssistanceReflection();
            });
        });

        // Add event listeners to all specification inputs
        assistanceSpecInputs.forEach(input => {
            input.addEventListener('input', updateAssistanceReflection);
        });

        // Initialize state
        assistanceCheckboxes.forEach(checkbox => {
            const specInput = checkbox.parentNode.querySelector('.assistance-spec');
            if (specInput) {
                specInput.style.display = checkbox.checked ? 'block' : 'none';
            }
        });

        updateAssistanceReflection();
        console.log('Assistance checkboxes initialized');
    }

    /**
     * Updates the hidden reflection field with selected assistance types
     */
    function updateAssistanceReflection() {
        const assistanceCheckboxes = document.querySelectorAll('.assistance-type');
        const reflectionField = document.getElementById('assistanceReflection');

        if (!reflectionField) return;

        const selected = Array.from(assistanceCheckboxes)
            .filter(cb => cb.checked)
            .map(cb => {
                const specInput = cb.parentNode.querySelector('.assistance-spec');
                if (specInput && specInput.value.trim() !== '') {
                    return cb.value + ': ' + specInput.value.trim();
                }
                return cb.value;
            });

        reflectionField.value = selected.join(', ');
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // REQUESTOR DETAILS TOGGLE
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Initializes toggle button for requestor details section
     */
    function initRequestorToggle() {
        const toggleRequestorBtn = document.getElementById('toggleRequestorBtn');
        const requestorSection = document.getElementById('requestorDetailsSection');

        if (!toggleRequestorBtn || !requestorSection) return;

        toggleRequestorBtn.addEventListener('click', function() {
            if (requestorSection.style.display === 'none') {
                requestorSection.style.display = 'block';
                this.textContent = 'Hide Requestor Details';
            } else {
                requestorSection.style.display = 'none';
                this.textContent = 'Show Requestor Details';
            }
        });

        console.log('Requestor toggle initialized');
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FORM SUBMISSION HANDLING
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Initializes form submission validation and loading state
     */
    function initFormSubmission() {
        const form = document.getElementById('updateForm');
        const submitBtn = document.getElementById('submitBtn');

        if (!form) return;

        form.addEventListener('submit', function(e) {
            // Validate file upload
            const fileInput = document.getElementById('receipt');
            if (fileInput && fileInput.files.length > 0) {
                const file = fileInput.files[0];
                const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'application/pdf'];
                const maxSize = 10 * 1024 * 1024;

                if (!validTypes.includes(file.type)) {
                    e.preventDefault();
                    alert('Invalid file type. Please upload JPG, PNG, or PDF files only.');
                    return;
                }

                if (file.size > maxSize) {
                    e.preventDefault();
                    alert('File size too large. Maximum size is 10MB.');
                    return;
                }
            }

            // Show loading state
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Updating...';
            }
        });

        console.log('Form submission handling initialized');
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // INITIALIZATION
    // ═══════════════════════════════════════════════════════════════════════════

    document.addEventListener('DOMContentLoaded', function() {
        initPhoneNumberFormatting();
        initPhilHealthFormatting();
        initAssistanceCheckboxes();
        initRequestorToggle();
        initFormSubmission();

        console.log('Form Utilities initialized');
    });

})();
