# Removed Scripts Documentation

## Overview
This document catalogs all JavaScript functions and scripts that were removed from form files during the uniform layout refactoring (commits `7613e19` and `bfa8834`). These scripts were removed when forms transitioned from individual HTML pages to using specialized layouts (`_InputFormsLayout`, `_EditFormsLayout`, `_ViewFormsLayout`, `_AdminFormsLayout`).

**Key Commits:**
- `7613e19` - Created `_FormsLayout` and applied to 6 form pages (Nov 5, 2025)
- `bfa8834` - Replaced generic `_FormsLayout` with specialized layouts (Nov 6, 2025)
- `c35dff2` - Added PDF viewer functionality with AES decryption (Nov 6, 2025)

---

## Table of Contents
1. [PDF Viewer Scripts (Admin Forms)](#1-pdf-viewer-scripts-admin-forms)
2. [Image Zoom Functionality](#2-image-zoom-functionality)
3. [File Upload Preview](#3-file-upload-preview)
4. [Phone Number Formatting](#4-phone-number-formatting)
5. [PhilHealth Number Formatting](#5-philhealth-number-formatting)
6. [Assistance Type Checkbox Handling](#6-assistance-type-checkbox-handling)
7. [Requestor Details Toggle](#7-requestor-details-toggle)
8. [Go Back Button Behavior](#8-go-back-button-behavior)
9. [Form Submission Handling](#9-form-submission-handling)
10. [Security Scripts (Disable Right-Click, Selection)](#10-security-scripts)
11. [Status Update Functions (Admin)](#11-status-update-functions)

---

## 1. PDF Viewer Scripts (Admin Forms)

### Description
Complete PDF.js-based viewer with navigation, zoom controls, and AES-256 decryption integration. Calls `Adminuser/ViewPDF` controller endpoint for secure document viewing.

### Affected Files
- All 15 admin status forms in `Views/Adminuser/`:
  - `FillupformHospitalBillapprovedstatus.cshtml`
  - `FillupformHospitalBillDisapprovedstatus.cshtml`
  - `FillupformHospitalBillUpdateprocessingstatus.cshtml`
  - `FillupformHospitalBillUpdatestatuClaimeddocs.cshtml`
  - `FillupformHospitalBillUpdatestatus.cshtml`
  - `Funeralburialapprovedstatus.cshtml`
  - `FuneralburialapprovedstatusUpdateClaimeddocs.cshtml`
  - `FuneralburialDisapprovedstatus.cshtml`
  - `Funeralburialformstatus.cshtml`
  - `FuneralburialformUpdateprocessingstatus.cshtml`
  - `Medicalandlabformapprovedsstatus.cshtml`
  - `MedicalandlabformDisapprovedstatus.cshtml`
  - `Medicalandlabformstatus.cshtml`
  - `MedicalandlabformstatusUpdateClaimeddocs.cshtml`
  - `MedicalandlabformUpdateprocessingstatus.cshtml`

### Code

```javascript
// ═══════════════════════════════════════════════════════════════════════════
// PDF.js Configuration and PDF Viewer Functions
// ═══════════════════════════════════════════════════════════════════════════

// Configure PDF.js worker
if (typeof pdfjsLib !== 'undefined') {
    pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.worker.min.js';
}

// PDF Viewer Variables
let currentPDF = null;
let currentPage = 1;
let totalPages = 1;
let currentScale = 1.2;

// PDF Viewer Function - CALLS ADMINUSER CONTROLLER
async function viewPDFInModal(fileName, fileType, displayName) {
    const modal = document.getElementById('pdfViewerModal');
    const title = document.getElementById('pdfViewerTitle');
    const loading = document.getElementById('pdfLoading');
    const errorDiv = document.getElementById('pdfError');
    const canvas = document.getElementById('pdfCanvas');
    const currentPageSpan = document.getElementById('currentPage');
    const totalPagesSpan = document.getElementById('totalPages');
    const zoomLevelSpan = document.getElementById('zoomLevel');

    if (!modal || !canvas) {
        console.error('PDF viewer elements not found');
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
        // Encode filename and build URL - IMPORTANT: Uses Adminuser controller
        const encodedFileName = encodeURIComponent(fileName);
        const url = '@Url.Action("ViewPDF", "Adminuser")?fileName=' + encodedFileName + '&fileType=' + fileType + '&t=' + Date.now();

        console.log('Fetching PDF from:', url);

        // Fetch PDF
        const response = await fetch(url);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const pdfData = await response.arrayBuffer();

        if (!pdfData || pdfData.byteLength === 0) {
            throw new Error('PDF data is empty');
        }

        console.log('PDF data loaded, size:', pdfData.byteLength);

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
        console.error('Error loading PDF:', error);
        if (loading) loading.style.display = 'none';
        if (errorDiv) {
            errorDiv.style.display = 'block';
            const errorMsg = errorDiv.querySelector('p');
            if (errorMsg) errorMsg.textContent = `Error loading PDF: ${error.message}`;
        }
    }
}

// Render specific page
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
        console.error('Error rendering page:', error);
    }
}

// Navigation functions
function nextPage() {
    if (currentPage < totalPages) {
        currentPage++;
        renderPage(currentPage);
        updateNavigationButtons();
    }
}

function prevPage() {
    if (currentPage > 1) {
        currentPage--;
        renderPage(currentPage);
        updateNavigationButtons();
    }
}

function zoomIn() {
    currentScale += 0.2;
    renderPage(currentPage);
    const zoomLevelSpan = document.getElementById('zoomLevel');
    if (zoomLevelSpan) zoomLevelSpan.textContent = Math.round(currentScale * 100) + '%';
}

function zoomOut() {
    if (currentScale > 0.5) {
        currentScale -= 0.2;
        renderPage(currentPage);
        const zoomLevelSpan = document.getElementById('zoomLevel');
        if (zoomLevelSpan) zoomLevelSpan.textContent = Math.round(currentScale * 100) + '%';
    }
}

function updateNavigationButtons() {
    const prevBtn = document.getElementById('prevBtn');
    const nextBtn = document.getElementById('nextBtn');

    if (prevBtn) prevBtn.disabled = currentPage <= 1;
    if (nextBtn) nextBtn.disabled = currentPage >= totalPages;
}

// Close PDF Viewer
function closePDFViewer() {
    const modal = document.getElementById('pdfViewerModal');
    if (modal) modal.style.display = 'none';

    // Clean up
    if (currentPDF) {
        currentPDF.destroy();
        currentPDF = null;
    }
}

// Close modal when clicking outside
window.onclick = function(event) {
    const pdfModal = document.getElementById('pdfViewerModal');
    const zoomModal = document.getElementById('zoomModal');

    if (event.target == pdfModal) {
        closePDFViewer();
    }
    if (event.target == zoomModal) {
        zoomModal.style.display = 'none';
    }
}
```

### HTML Elements Required

```html
<!-- PDF Viewer Modal -->
<div id="pdfViewerModal" class="pdf-viewer-modal">
    <div class="pdf-viewer-content">
        <div class="pdf-viewer-header">
            <span class="pdf-viewer-title" id="pdfViewerTitle">PDF Document</span>
            <div class="pdf-controls">
                <button class="btn btn-sm btn-light" onclick="prevPage()" id="prevBtn">
                    <i class="bi bi-chevron-left"></i> Previous
                </button>
                <span class="page-info" id="pageInfo">Page: <span id="currentPage">1</span> / <span id="totalPages">1</span></span>
                <button class="btn btn-sm btn-light" onclick="nextPage()" id="nextBtn">
                    Next <i class="bi bi-chevron-right"></i>
                </button>
                <button class="btn btn-sm btn-light" onclick="zoomOut()">
                    <i class="bi bi-zoom-out"></i>
                </button>
                <span class="zoom-level" id="zoomLevel">100%</span>
                <button class="btn btn-sm btn-light" onclick="zoomIn()">
                    <i class="bi bi-zoom-in"></i>
                </button>
            </div>
            <button class="pdf-viewer-close" onclick="closePDFViewer()">&times;</button>
        </div>
        <div class="pdf-viewer-body">
            <div id="pdfLoading" class="pdf-loading">
                <div class="spinner-border text-light" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <p>Loading PDF document...</p>
            </div>
            <div id="pdfError" class="pdf-error">
                <i class="bi bi-exclamation-triangle"></i>
                <p>Error loading PDF. Please try again.</p>
            </div>
            <div class="pdf-container">
                <canvas id="pdfCanvas"></canvas>
            </div>
        </div>
    </div>
</div>

<!-- PDF.js Script in <head> -->
<script src="https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.min.js"></script>
```

### Usage Example

```html
<!-- For PDF files -->
<div class="pdf-preview" onclick="viewPDFInModal('@ViewData["DoctorPrescription"]', 'doctorprescription', 'Doctor Prescription')">
    <div class="pdf-icon"><i class="bi bi-file-earmark-pdf"></i></div>
    <div class="pdf-info">
        <span class="pdf-filename">@ViewData["DoctorPrescription"]</span>
        <span class="pdf-label">Click to view PDF</span>
    </div>
</div>
```

---

## 2. Image Zoom Functionality

### Description
Allows users to click on images to view them in a full-screen modal with zoom capability.

### Affected Files
- All admin status forms
- All view forms (`Fillupformhospitalbillview.cshtml`, `Medicalandlabformview.cshtml`, `Funeralburialformview.cshtml`)

### Code

```javascript
// Image Zoom Functionality
function zoomImage(src) {
    const modal = document.getElementById("zoomModal");
    const modalImg = document.getElementById("zoomedImage");
    if (modal && modalImg) {
        modal.style.display = "block";
        modalImg.src = src;
    }
}

// Close zoom modal
const closeZoomBtn = document.querySelector(".close-zoom");
if (closeZoomBtn) {
    closeZoomBtn.onclick = function() {
        const modal = document.getElementById("zoomModal");
        if (modal) modal.style.display = "none";
    }
}
```

### HTML Required

```html
<!-- Image Zoom Modal -->
<div id="zoomModal" class="zoom-modal">
    <span class="close-zoom">&times;</span>
    <img class="zoom-modal-content" id="zoomedImage">
</div>
```

### CSS Required

```css
.zoom-modal {
    display: none;
    position: fixed;
    z-index: 2000;
    left: 0;
    top: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(0,0,0,0.9);
}

.zoom-modal-content {
    margin: auto;
    display: block;
    max-width: 90%;
    max-height: 90%;
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
}

.close-zoom {
    position: absolute;
    top: 15px;
    right: 35px;
    color: #f1f1f1;
    font-size: 40px;
    font-weight: bold;
    cursor: pointer;
}

.close-zoom:hover,
.close-zoom:focus {
    color: #bbb;
}
```

### Usage Example

```html
<!-- For non-PDF images -->
<div class="id-image-container" onclick="zoomImage('data:image/jpeg;base64,@ViewData["FrontIDBase64"]')">
    <img src="data:image/jpeg;base64,@ViewData["FrontIDBase64"]" alt="Front ID" class="id-image" />
</div>
```

---

## 3. File Upload Preview

### Description
Provides real-time preview for uploaded images and validation for file type/size in edit forms.

### Affected Files
- `FillupformHospitalBilledit.cshtml`
- `Medicalandlabformedit.cshtml`
- `Funeralburialformedit.cshtml`

### Code

```javascript
// File Upload Preview Function
function previewImage(input, previewId) {
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
            fileStatus.innerHTML = '<span class="file-error">Invalid file type. Please upload JPG, PNG, or PDF files only.</span>';
            input.value = '';
            return;
        }

        if (file.size > maxSize) {
            fileStatus.innerHTML = '<span class="file-error">File size too large. Maximum size is 10MB.</span>';
            input.value = '';
            return;
        }

        // Update UI for valid file
        fileStatus.innerHTML = '<span class="file-success">File selected: ' + file.name + '</span>';

        if (uploadText) {
            uploadText.textContent = file.name;
        }

        if (uploadBox) {
            uploadBox.style.borderColor = "var(--success-color)";
            uploadBox.style.backgroundColor = "rgba(40, 167, 69, 0.1)";
        }

        // Show preview for images only
        if (file.type.startsWith("image/")) {
            const reader = new FileReader();
            reader.onload = function (e) {
                preview.src = e.target.result;
                preview.style.display = "block";
            };
            reader.readAsDataURL(file);
        } else {
            // Hide preview for PDF files
            preview.style.display = "none";
        }
    } else {
        // Reset if no file
        preview.style.display = "none";
        fileStatus.innerHTML = '';
        if (uploadText) {
            uploadText.textContent = 'Click to upload new document';
        }
        if (uploadBox) {
            uploadBox.style.borderColor = "#ddd";
            uploadBox.style.backgroundColor = "#fafafa";
        }
    }
}
```

### HTML Usage

```html
<input type="file" id="receipt" name="Documentsrequired"
       accept="image/jpeg,image/jpg,image/png,application/pdf"
       onchange="previewImage(this, 'previewImg')" />
<img id="previewImg" class="img-preview" />
<div id="fileStatus"></div>
```

---

## 4. Phone Number Formatting

### Description
Auto-formats Philippine phone numbers (mobile and landline) with proper dashes and validates format.

### Affected Files
- All input forms (`FillupformHospitalBill.cshtml`, `Medicalandlabform.cshtml`, `Funeralburialform.cshtml`)
- All edit forms

### Code

```javascript
// Contact Number Formatting
const contactNoInput = document.getElementById('contactNoInput');
if (contactNoInput) {
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
        else if ((value.startsWith('02') || /^[1-9]\d{1}$/.test(value.substring(0,2)))) {
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

    contactNoInput.addEventListener('blur', function() {
        const regex = /^(09\d{2}-\d{3}-\d{4}|0[2-9]\d-\d{3}-\d{4})$/;
        if (this.value && !regex.test(this.value)) {
            alert("Please enter a valid Philippine number (e.g., 0912-345-6789 or 02-123-4567)");
            this.focus();
        }
    });
}
```

### HTML Usage

```html
<input type="text" id="contactNoInput" name="ContactNo"
       placeholder="09XX-XXX-XXXX or 0X-XXX-XXXX"
       maxlength="13" />
```

---

## 5. PhilHealth Number Formatting

### Description
Auto-formats PhilHealth numbers with dashes (XXXX-XXXX-XXXX format).

### Affected Files
- All input forms
- All edit forms

### Code

```javascript
// PhilHealth Input Formatting
const philHealthInput = document.getElementById('philHealthInput');
if (philHealthInput) {
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
}
```

### HTML Usage

```html
<input type="text" id="philHealthInput" name="PhilHealth"
       placeholder="XXXX-XXXX-XXXX"
       maxlength="14" />
```

---

## 6. Assistance Type Checkbox Handling

### Description
Single-selection checkbox system for assistance types with specification inputs. Only one assistance type can be selected at a time, with optional specification text field.

### Affected Files
- All input forms
- All edit forms

### Code

```javascript
// Assistance Type Checkbox Handling - SINGLE SELECTION ONLY (with deselect option)
const assistanceCheckboxes = document.querySelectorAll('.assistance-type');
const assistanceSpecInputs = document.querySelectorAll('.assistance-spec');

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

// Add event listeners to all specification inputs to update reflection in real-time
assistanceSpecInputs.forEach(input => {
    input.addEventListener('input', updateAssistanceReflection);
});

function updateAssistanceReflection() {
    const selected = Array.from(assistanceCheckboxes)
        .filter(cb => cb.checked)
        .map(cb => {
            const specInput = cb.parentNode.querySelector('.assistance-spec');
            if (specInput && specInput.value.trim() !== '') {
                return cb.value + ': ' + specInput.value.trim();
            }
            return cb.value;
        });

    const reflectionField = document.getElementById('assistanceReflection');
    if (reflectionField) {
        reflectionField.value = selected.join(', ');
    }
}

// Initialize the reflection on page load and set up initial state
document.addEventListener('DOMContentLoaded', function() {
    // Set initial state for specification inputs
    assistanceCheckboxes.forEach(checkbox => {
        const specInput = checkbox.parentNode.querySelector('.assistance-spec');
        if (specInput) {
            specInput.style.display = checkbox.checked ? 'block' : 'none';
        }
    });

    // Initial reflection update
    updateAssistanceReflection();
});
```

### HTML Usage

```html
<!-- Assistance Type Checkboxes -->
<div class="form-check">
    <input class="form-check-input assistance-type" type="checkbox"
           name="typeOfAssistance" value="Medical" id="medicalCheck">
    <label class="form-check-label" for="medicalCheck">Medical</label>
    <input type="text" class="form-control assistance-spec"
           placeholder="Specify medical assistance" style="display:none;">
</div>

<!-- Hidden field to store final selection -->
<input type="hidden" id="assistanceReflection" name="TypeOfAssistance" />
```

---

## 7. Requestor Details Toggle

### Description
Toggle visibility of requestor details section in forms.

### Affected Files
- All input forms
- All edit forms

### Code

```javascript
// Requestor Details Toggle
const toggleRequestorBtn = document.getElementById('toggleRequestorBtn');
const requestorSection = document.getElementById('requestorDetailsSection');

if (toggleRequestorBtn && requestorSection) {
    toggleRequestorBtn.addEventListener('click', function() {
        if (requestorSection.style.display === 'none') {
            requestorSection.style.display = 'block';
            this.textContent = 'Hide Requestor Details';
        } else {
            requestorSection.style.display = 'none';
            this.textContent = 'Show Requestor Details';
        }
    });
}
```

---

## 8. Go Back Button Behavior

### Description
Responsive go-back button that changes to compact mode on small screens.

### Affected Files
- All forms (before uniform go-back-button.js was created)

### Code

```javascript
const goBackBtn = document.getElementById('goBackBtn');

if (goBackBtn) {
    function updateButton() {
        const viewportWidth = window.innerWidth;
        if (viewportWidth < 480) {
            goBackBtn.classList.add('compact');
        } else {
            goBackBtn.classList.remove('compact');
        }
    }

    function handleViewportChange() {
        updateButton();
    }

    updateButton();
    window.addEventListener('resize', handleViewportChange);
    window.addEventListener('orientationchange', handleViewportChange);
}
```

**Note:** This was later replaced by the global `go-back-button.js` file.

---

## 9. Form Submission Handling

### Description
Validates file uploads before submission and shows loading state on submit button.

### Affected Files
- All edit forms

### Code

```javascript
// Form submission handling
document.addEventListener('DOMContentLoaded', function() {
    const form = document.getElementById('updateForm');
    const submitBtn = document.getElementById('submitBtn');

    if (form) {
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
    }
});
```

---

## 10. Security Scripts

### Description
Prevents right-click, text selection, drag-and-drop, and certain keyboard shortcuts to protect sensitive documents.

### Affected Files
- All admin status forms
- All view forms

### Code

```javascript
// Disable right-click context menu
document.addEventListener('contextmenu', function(e) {
    e.preventDefault();
    return false;
});

// Disable text selection
document.addEventListener('selectstart', function(e) {
    e.preventDefault();
    return false;
});

// Disable drag and drop for images
document.addEventListener('dragstart', function(e) {
    if (e.target.tagName === 'IMG') {
        e.preventDefault();
        return false;
    }
});

// Disable keyboard shortcuts for saving, printing, etc.
document.addEventListener('keydown', function(e) {
    // Disable F12 (Developer Tools)
    if (e.key === 'F12') {
        e.preventDefault();
        return false;
    }

    // Disable Ctrl+S (Save)
    if (e.ctrlKey && e.key === 's') {
        e.preventDefault();
        return false;
    }

    // Disable Ctrl+P (Print)
    if (e.ctrlKey && e.key === 'p') {
        e.preventDefault();
        return false;
    }
});

// Add CSS to prevent text selection
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
        -webkit-user-select: text;
        -moz-user-select: text;
        -ms-user-select: text;
        user-select: text;
    }
`;
document.head.appendChild(style);
```

---

## 11. Status Update Functions

### Description
Functions for admin status forms to update application status (Approve, Disapprove, Mark as Claimed, etc.).

### Affected Files
- All admin status forms

### Code

```javascript
// Update status to Approved
function updateStatusApproved() {
    if (confirm('Are you sure you want to approve this application?')) {
        document.getElementById('statusInput').value = 'Approved';
        document.getElementById('statusForm').submit();
    }
}

// Update status to Disapproved (with comments)
function updateStatusDisapproved() {
    const comments = prompt('Please enter reason for disapproval:');
    if (comments && comments.trim() !== '') {
        document.getElementById('statusInput').value = 'Disapproved';
        document.getElementById('commentsInput').value = comments;
        document.getElementById('statusForm').submit();
    } else if (comments !== null) {
        alert('Comments are required when disapproving an application.');
    }
}

// Update status to Processing
function updateStatusProcessing() {
    if (confirm('Mark this application as processing?')) {
        document.getElementById('statusInput').value = 'Processing';
        document.getElementById('statusForm').submit();
    }
}

// Mark as Claimed
function markAsClaimed() {
    if (confirm('Mark this application as claimed?')) {
        document.getElementById('statusInput').value = 'Claimed';
        document.getElementById('status3Input').value = 'Claimed';
        document.getElementById('statusForm').submit();
    }
}

// Comments Modal Functions (for more sophisticated UX)
function openCommentsModal() {
    document.getElementById('commentsModal').style.display = 'block';
}

function closeCommentsModal() {
    document.getElementById('commentsModal').style.display = 'none';
    document.getElementById('commentsTextarea').value = '';
}

function submitDisapproved() {
    const comments = document.getElementById('commentsTextarea').value.trim();
    if (comments === '') {
        alert('Please provide comments for disapproval.');
        return;
    }

    document.getElementById('statusInput').value = 'Disapproved';
    document.getElementById('commentsInput').value = comments;
    document.getElementById('statusForm').submit();
}
```

### HTML Required

```html
<form id="statusForm" method="post" asp-action="ActionName">
    <input type="hidden" id="statusInput" name="Status2" />
    <input type="hidden" id="commentsInput" name="Comments" />
    <input type="hidden" id="status3Input" name="Status3" />

    <!-- Status Buttons -->
    <button type="button" class="btn btn-success" onclick="updateStatusApproved()">
        <i class="bi bi-check-circle"></i> Approve
    </button>
    <button type="button" class="btn btn-danger" onclick="openCommentsModal()">
        <i class="bi bi-x-circle"></i> Disapprove
    </button>
    <button type="button" class="btn btn-primary" onclick="updateStatusProcessing()">
        <i class="bi bi-clock"></i> Mark as Processing
    </button>
    <button type="button" class="btn btn-info" onclick="markAsClaimed()">
        <i class="bi bi-hand-thumbs-up"></i> Mark as Claimed
    </button>
</form>

<!-- Comments Modal -->
<div id="commentsModal" class="comments-modal">
    <div class="comments-modal-content">
        <div class="comments-modal-header">
            <h5 class="comments-modal-title">Comments Required</h5>
            <button class="comments-modal-close" onclick="closeCommentsModal()">&times;</button>
        </div>
        <div class="mb-3">
            <label for="commentsTextarea" class="form-label">Reason for disapproval:</label>
            <textarea class="form-control" id="commentsTextarea" rows="4"
                      placeholder="Enter reason..." required></textarea>
        </div>
        <div class="d-flex justify-content-end gap-2">
            <button type="button" class="btn btn-secondary" onclick="closeCommentsModal()">Cancel</button>
            <button type="button" class="btn btn-danger" onclick="submitDisapproved()">Submit</button>
        </div>
    </div>
</div>
```

---

## Summary of Changes

### What Was Removed
When the uniform layouts (`_InputFormsLayout`, `_EditFormsLayout`, `_ViewFormsLayout`, `_AdminFormsLayout`) were implemented:

1. **All `<script>` tags** from individual form files
2. **PDF viewer modals and scripts** (should be in layouts)
3. **Image zoom modals and scripts**
4. **File upload preview functions**
5. **Phone/PhilHealth formatting scripts**
6. **Assistance checkbox handling**
7. **Form validation scripts**
8. **Security scripts** (right-click disable, etc.)
9. **Status update functions** (admin forms)

### What Should Be Restored

**In `_AdminFormsLayout.cshtml`:**
- ✅ PDF viewer modal and scripts (commit `7caf4f7` added these back)
- ✅ Image zoom modal and scripts
- ✅ Security scripts (disable right-click, selection)
- ❌ Status update functions (need to be restored per-page or in layout)

**In `_EditFormsLayout.cshtml`:**
- ❌ File upload preview function
- ❌ Form submission validation
- ❌ PDF viewer for viewing existing documents

**In `_InputFormsLayout.cshtml`:**
- ❌ Phone number formatting
- ❌ PhilHealth number formatting
- ❌ Assistance type checkbox handling
- ❌ Requestor details toggle
- ❌ File upload preview

**In `_ViewFormsLayout.cshtml`:**
- ❌ PDF viewer modal and scripts
- ❌ Image zoom functionality
- ❌ Security scripts

### Recommended Actions

1. **Add PDF viewer to _AdminFormsLayout**: ✅ Already done in commit `7caf4f7`
2. **Add form validation scripts to _InputFormsLayout**: Create a shared JS file
3. **Add file preview to _EditFormsLayout**: Include in layout's Scripts section
4. **Add security scripts to _ViewFormsLayout and _AdminFormsLayout**: Prevent document copying
5. **Create shared form-helpers.js**: Include phone/PhilHealth formatting, assistance handling

---

## File Locations

### Current Layouts
- `/Views/Shared/_InputFormsLayout.cshtml`
- `/Views/Shared/_EditFormsLayout.cshtml`
- `/Views/Shared/_ViewFormsLayout.cshtml`
- `/Views/Shared/_AdminFormsLayout.cshtml`

### Form Files
- **Dashboard Forms**: `/Views/Dashboard/`
- **Admin Status Forms**: `/Views/Adminuser/`

### Referenced Commits
- `7613e19` - Created _FormsLayout
- `bfa8834` - Created specialized layouts
- `c35dff2` - Added PDF viewer functionality
- `7caf4f7` - Added PDF viewer modals to admin forms (most recent)

---

*Generated on: 2025-11-07*
*Based on git commits: 7613e19, bfa8834, c35dff2, 7caf4f7*
