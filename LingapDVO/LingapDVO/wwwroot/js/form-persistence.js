// ═══════════════════════════════════════════════════════════════════════════════
// FORM FIELD PERSISTENCE
// ═══════════════════════════════════════════════════════════════════════════════
// Automatically saves and restores form field values using localStorage
// Ensures auto-populated fields remain even after errors or page refresh
// ═══════════════════════════════════════════════════════════════════════════════

(function() {
    'use strict';

    // ⚡ PERFORMANCE: Debounce helper function to limit localStorage writes
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    // Get unique form identifier based on current page
    function getFormKey() {
        const path = window.location.pathname;
        const formName = path.split('/').pop() || 'form';
        return `lingap_form_${formName}`;
    }

    // Save form field value to localStorage
    function saveFieldValue(fieldName, value) {
        try {
            const formKey = getFormKey();
            let formData = JSON.parse(localStorage.getItem(formKey) || '{}');
            formData[fieldName] = value;
            localStorage.setItem(formKey, JSON.stringify(formData));
        } catch (error) {
                    }
    }

    // Debounced version of saveFieldValue (500ms delay to reduce I/O)
    // Previous: Saved on every keystroke, causing excessive localStorage operations
    const debouncedSaveFieldValue = debounce(saveFieldValue, 500);

    // Get saved form field value from localStorage
    function getSavedFieldValue(fieldName) {
        try {
            const formKey = getFormKey();
            let formData = JSON.parse(localStorage.getItem(formKey) || '{}');
            return formData[fieldName] || null;
        } catch (error) {
                        return null;
        }
    }

    // Clear saved form data
    function clearSavedFormData() {
        try {
            const formKey = getFormKey();
            localStorage.removeItem(formKey);
        } catch (error) {
                    }
    }

    // Restore form field values from localStorage
    function restoreFormValues(form) {
        if (!form) return;

        const formKey = getFormKey();
        let formData = {};

        try {
            formData = JSON.parse(localStorage.getItem(formKey) || '{}');
        } catch (error) {
                        return;
        }

        // Restore text inputs, textareas, and selects
        form.querySelectorAll('input[type="text"], input[type="email"], input[type="tel"], input[type="number"], input[type="date"], textarea, select').forEach(field => {
            const fieldName = field.name || field.id;
            if (!fieldName) return;

            // Only restore if field is empty (don't override server-side values)
            if (!field.value || field.value === '') {
                const savedValue = formData[fieldName];
                if (savedValue !== null && savedValue !== undefined) {
                    field.value = savedValue;
                }
            }
        });

        // Restore radio buttons
        form.querySelectorAll('input[type="radio"]').forEach(radio => {
            const fieldName = radio.name;
            if (!fieldName) return;

            const savedValue = formData[fieldName];
            if (savedValue === radio.value) {
                radio.checked = true;
            }
        });

        // Restore checkboxes
        form.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
            const fieldName = checkbox.name || checkbox.id;
            if (!fieldName) return;

            const savedValue = formData[fieldName];
            if (savedValue === 'true' || savedValue === true) {
                checkbox.checked = true;
            }
        });
    }

    // Save form field on change
    function setupFieldPersistence(form) {
        if (!form) return;

        // Save text inputs, textareas, and selects on change
        // Use debounced save for 'input' events to avoid excessive writes
        form.querySelectorAll('input[type="text"], input[type="email"], input[type="tel"], input[type="number"], input[type="date"], textarea, select').forEach(field => {
            // Debounced save on input (typing) - 500ms delay
            field.addEventListener('input', function() {
                const fieldName = this.name || this.id;
                if (fieldName) {
                    debouncedSaveFieldValue(fieldName, this.value);
                }
            });

            // Immediate save on change (blur, selection change)
            field.addEventListener('change', function() {
                const fieldName = this.name || this.id;
                if (fieldName) {
                    saveFieldValue(fieldName, this.value);
                }
            });
        });

        // Save radio buttons on change
        form.querySelectorAll('input[type="radio"]').forEach(radio => {
            radio.addEventListener('change', function() {
                const fieldName = this.name;
                if (fieldName && this.checked) {
                    saveFieldValue(fieldName, this.value);
                }
            });
        });

        // Save checkboxes on change
        form.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
            checkbox.addEventListener('change', function() {
                const fieldName = this.name || this.id;
                if (fieldName) {
                    saveFieldValue(fieldName, this.checked);
                }
            });
        });
    }

    // Clear form data on successful submission
    function clearOnSubmit(form) {
        if (!form) return;

        // Listen for form submit event
        form.addEventListener('submit', function(e) {
            // Don't clear immediately - instead, set a flag to clear on next page load
            // This ensures data is only cleared if form actually submits successfully
            // If there's a validation error (client-side or server-side), the data remains
            
            // Use a small delay to check if the form submission was NOT prevented
            setTimeout(() => {
                // If we're still on the same page after 200ms, it means form submission was prevented
                // So we don't clear the data
                // If the page navigates away, this timeout won't run anyway
            }, 200);
        });

        // Clear data only when we detect a successful submission (page is unloading after form submit)
        let formSubmitting = false;
        form.addEventListener('submit', function() {
            formSubmitting = true;
        });

        window.addEventListener('beforeunload', function() {
            if (formSubmitting) {
                // Form was submitted and page is navigating - clear the saved data
                clearSavedFormData();
            }
        });
    }

    // Initialize form persistence
    function initFormPersistence() {
        const forms = document.querySelectorAll('form');

        forms.forEach(form => {
            // Restore saved values on page load
            restoreFormValues(form);

            // Setup field persistence (save on change)
            setupFieldPersistence(form);

            // Clear saved data on successful submission
            clearOnSubmit(form);
        });

        ');
    }

    // Auto-initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initFormPersistence);
    } else {
        initFormPersistence();
    }

    // Expose API for manual control if needed
    window.FormPersistence = {
        save: saveFieldValue,
        get: getSavedFieldValue,
        clear: clearSavedFormData,
        restore: restoreFormValues
    };

})();
