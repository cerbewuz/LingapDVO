// ═══════════════════════════════════════════════════════════════════════════════
// ADMIN STATUS UPDATE FUNCTIONS
// ═══════════════════════════════════════════════════════════════════════════════
// Description: Functions for updating application status (Approve, Disapprove, etc.)
// Used in: Admin status forms
// Dependencies: Bootstrap 5 (for modals)
// ═══════════════════════════════════════════════════════════════════════════════

(function() {
    'use strict';

    // ═══════════════════════════════════════════════════════════════════════════
    // STATUS UPDATE FUNCTIONS
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Updates application status to Approved
     */
    window.updateStatusApproved = function() {
        if (confirm('Are you sure you want to approve this application?')) {
            const statusInput = document.getElementById('statusInput');
            const statusForm = document.getElementById('statusForm');

            if (statusInput && statusForm) {
                statusInput.value = 'Approved';
                statusForm.submit();
            } else {
                console.error('Status form elements not found');
            }
        }
    };

    /**
     * Updates application status to Disapproved (with comments modal)
     */
    window.updateStatusDisapproved = function() {
        openCommentsModal();
    };

    /**
     * Simple disapproval with prompt (fallback if modal not available)
     */
    window.updateStatusDisapprovedSimple = function() {
        const comments = prompt('Please enter reason for disapproval:');
        if (comments && comments.trim() !== '') {
            const statusInput = document.getElementById('statusInput');
            const commentsInput = document.getElementById('commentsInput');
            const statusForm = document.getElementById('statusForm');

            if (statusInput && commentsInput && statusForm) {
                statusInput.value = 'Disapproved';
                commentsInput.value = comments;
                statusForm.submit();
            } else {
                console.error('Status form elements not found');
            }
        } else if (comments !== null) {
            alert('Comments are required when disapproving an application.');
        }
    };

    /**
     * Updates application status to Processing
     */
    window.updateStatusProcessing = function() {
        if (confirm('Mark this application as processing?')) {
            const statusInput = document.getElementById('statusInput');
            const statusForm = document.getElementById('statusForm');

            if (statusInput && statusForm) {
                statusInput.value = 'Processing';
                statusForm.submit();
            } else {
                console.error('Status form elements not found');
            }
        }
    };

    /**
     * Marks application as Claimed
     */
    window.markAsClaimed = function() {
        if (confirm('Mark this application as claimed?')) {
            const statusInput = document.getElementById('statusInput');
            const status3Input = document.getElementById('status3Input');
            const statusForm = document.getElementById('statusForm');

            if (statusForm) {
                if (statusInput) statusInput.value = 'Claimed';
                if (status3Input) status3Input.value = 'Claimed';
                statusForm.submit();
            } else {
                console.error('Status form not found');
            }
        }
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // COMMENTS MODAL FUNCTIONS
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Opens the comments modal for disapproval
     */
    window.openCommentsModal = function() {
        const modal = document.getElementById('commentsModal');
        if (modal) {
            modal.style.display = 'block';

            // Focus on textarea
            const textarea = document.getElementById('commentsTextarea');
            if (textarea) {
                textarea.focus();
            }
        } else {
            // Fallback to simple prompt if modal not available
            window.updateStatusDisapprovedSimple();
        }
    };

    /**
     * Closes the comments modal
     */
    window.closeCommentsModal = function() {
        const modal = document.getElementById('commentsModal');
        const textarea = document.getElementById('commentsTextarea');

        if (modal) {
            modal.style.display = 'none';
        }

        if (textarea) {
            textarea.value = '';
            textarea.classList.remove('is-invalid');
        }
    };

    /**
     * Submits disapproval with comments from modal
     */
    window.submitDisapproved = function() {
        const textarea = document.getElementById('commentsTextarea');
        const comments = textarea ? textarea.value.trim() : '';

        if (comments === '') {
            alert('Please provide comments for disapproval.');
            if (textarea) {
                textarea.classList.add('is-invalid');
                textarea.focus();
            }
            return;
        }

        const statusInput = document.getElementById('statusInput');
        const commentsInput = document.getElementById('commentsInput');
        const statusForm = document.getElementById('statusForm');

        if (statusInput && commentsInput && statusForm) {
            statusInput.value = 'Disapproved';
            commentsInput.value = comments;
            statusForm.submit();
        } else {
            console.error('Status form elements not found');
            alert('Error: Could not submit form. Please try again.');
        }
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // BULK ACTIONS (Optional - for future use)
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Validates status change before submission
     * @param {string} newStatus - The new status to set
     * @returns {boolean} - True if valid, false otherwise
     */
    function validateStatusChange(newStatus) {
        const currentStatus = document.getElementById('currentStatus');
        if (!currentStatus) return true; // No validation needed

        const current = currentStatus.value;

        // Define valid status transitions
        const validTransitions = {
            'Pending': ['Processing', 'Approved', 'Disapproved'],
            'Processing': ['Approved', 'Disapproved'],
            'Approved': ['Claimed'],
            'Disapproved': [], // Cannot change from disapproved
            'Claimed': [] // Final state
        };

        if (validTransitions[current] && !validTransitions[current].includes(newStatus)) {
            alert(`Invalid status transition from ${current} to ${newStatus}`);
            return false;
        }

        return true;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EVENT LISTENERS
    // ═══════════════════════════════════════════════════════════════════════════

    document.addEventListener('DOMContentLoaded', function() {
        // Close comments modal when clicking outside
        const commentsModal = document.getElementById('commentsModal');
        if (commentsModal) {
            window.addEventListener('click', function(event) {
                if (event.target === commentsModal) {
                    window.closeCommentsModal();
                }
            });
        }

        // Enter key submits comments modal
        const commentsTextarea = document.getElementById('commentsTextarea');
        if (commentsTextarea) {
            commentsTextarea.addEventListener('keydown', function(e) {
                if (e.ctrlKey && e.key === 'Enter') {
                    window.submitDisapproved();
                }
            });
        }

        // ESC key closes modal
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape') {
                window.closeCommentsModal();
            }
        });

        console.log('Admin Status Functions initialized');
    });

})();
