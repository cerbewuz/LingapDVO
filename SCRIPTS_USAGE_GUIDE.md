# JavaScript Files Usage Guide

## Overview
This guide explains how to use the newly created JavaScript files that contain functionality extracted from the form files during layout refactoring.

---

## Created Files

### 1. `pdf-viewer.js`
**Location:** `/wwwroot/js/pdf-viewer.js`
**Purpose:** PDF viewing with AES-256 decryption, navigation, and zoom controls
**Size:** ~12 KB
**Dependencies:** PDF.js 3.11.174

### 2. `image-zoom.js`
**Location:** `/wwwroot/js/image-zoom.js`
**Purpose:** Full-screen image zoom functionality
**Size:** ~2 KB
**Dependencies:** None

### 3. `form-utilities.js`
**Location:** `/wwwroot/js/form-utilities.js`
**Purpose:** Form field formatting, validation, file preview
**Size:** ~10 KB
**Dependencies:** None

### 4. `security.js`
**Location:** `/wwwroot/js/security.js`
**Purpose:** Document protection (disable copy, right-click, etc.)
**Size:** ~5 KB
**Dependencies:** None

### 5. `admin-status-functions.js`
**Location:** `/wwwroot/js/admin-status-functions.js`
**Purpose:** Admin status update functions (Approve, Disapprove, etc.)
**Size:** ~6 KB
**Dependencies:** Bootstrap 5 (for modals)

---

## How to Include in Layouts

### For `_AdminFormsLayout.cshtml`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <!-- PDF.js Library (REQUIRED for pdf-viewer.js) -->
    <script src="https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.min.js"></script>

    <!-- Other head content... -->
</head>
<body>
    <!-- Body content... -->

    <!-- Bootstrap 5 (if not already included) -->
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>

    <!-- Admin Form Scripts -->
    <script src="~/js/security.js"></script>
    <script src="~/js/pdf-viewer.js"></script>
    <script src="~/js/image-zoom.js"></script>
    <script src="~/js/admin-status-functions.js"></script>

    @RenderSection("Scripts", required: false)
</body>
</html>
```

### For `_EditFormsLayout.cshtml`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <!-- Head content... -->
</head>
<body>
    <!-- Body content... -->

    <!-- jQuery (if not already included) -->
    <script src="https://code.jquery.com/jquery-3.7.1.min.js"></script>

    <!-- jQuery Validation -->
    <script src="https://cdn.jsdelivr.net/npm/jquery-validation@1.19.5/dist/jquery.validate.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/jquery-validation-unobtrusive@4.0.0/dist/jquery.validate.unobtrusive.min.js"></script>

    <!-- Form Utilities -->
    <script src="~/js/form-utilities.js"></script>

    <!-- Optional: PDF Viewer for viewing existing documents -->
    <script src="https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.min.js"></script>
    <script src="~/js/pdf-viewer.js"></script>
    <script src="~/js/image-zoom.js"></script>

    @RenderSection("Scripts", required: false)
</body>
</html>
```

### For `_InputFormsLayout.cshtml`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <!-- Head content... -->
</head>
<body>
    <!-- Body content... -->

    <!-- jQuery (if not already included) -->
    <script src="https://code.jquery.com/jquery-3.7.1.min.js"></script>

    <!-- jQuery Validation -->
    <script src="https://cdn.jsdelivr.net/npm/jquery-validation@1.19.5/dist/jquery.validate.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/jquery-validation-unobtrusive@4.0.0/dist/jquery.validate.unobtrusive.min.js"></script>

    <!-- Form Utilities -->
    <script src="~/js/form-utilities.js"></script>

    @RenderSection("Scripts", required: false)
</body>
</html>
```

### For `_ViewFormsLayout.cshtml`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <!-- PDF.js Library -->
    <script src="https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.min.js"></script>

    <!-- Head content... -->
</head>
<body>
    <!-- Body content... -->

    <!-- View Form Scripts -->
    <script src="~/js/security.js"></script>
    <script src="~/js/pdf-viewer.js"></script>
    <script src="~/js/image-zoom.js"></script>

    @RenderSection("Scripts", required: false)
</body>
</html>
```

---

## Required HTML Elements

### For PDF Viewer (`pdf-viewer.js`)

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
                <span class="page-info">Page: <span id="currentPage">1</span> / <span id="totalPages">1</span></span>
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
                <div class="spinner-border text-light" role="status"></div>
                <p>Loading PDF document...</p>
            </div>
            <div id="pdfError" class="pdf-error" style="display: none;">
                <i class="bi bi-exclamation-triangle"></i>
                <p>Error loading PDF. Please try again.</p>
            </div>
            <div class="pdf-container">
                <canvas id="pdfCanvas"></canvas>
            </div>
        </div>
    </div>
</div>
```

### For Image Zoom (`image-zoom.js`)

```html
<!-- Image Zoom Modal -->
<div id="zoomModal" class="zoom-modal">
    <span class="close-zoom">&times;</span>
    <img class="zoom-modal-content" id="zoomedImage">
</div>
```

### For Admin Status Functions (`admin-status-functions.js`)

```html
<!-- Status Form -->
<form id="statusForm" method="post" asp-action="ActionName">
    <input type="hidden" id="statusInput" name="Status2" />
    <input type="hidden" id="commentsInput" name="Comments" />
    <input type="hidden" id="status3Input" name="Status3" />

    <!-- Status Buttons -->
    <button type="button" class="btn btn-success" onclick="updateStatusApproved()">
        <i class="bi bi-check-circle"></i> Approve
    </button>
    <button type="button" class="btn btn-danger" onclick="updateStatusDisapproved()">
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

### For Form Utilities (`form-utilities.js`)

```html
<!-- Phone Number Input -->
<input type="text" id="contactNoInput" name="ContactNo"
       placeholder="09XX-XXX-XXXX or 0X-XXX-XXXX"
       maxlength="13" class="form-control" />

<!-- PhilHealth Input -->
<input type="text" id="philHealthInput" name="PhilHealth"
       placeholder="XXXX-XXXX-XXXX"
       maxlength="14" class="form-control" />

<!-- File Upload with Preview -->
<input type="file" id="receipt" name="Documentsrequired"
       accept="image/jpeg,image/jpg,image/png,application/pdf"
       onchange="previewImage(this, 'previewImg')" class="form-control" />
<img id="previewImg" class="img-preview" style="display: none;" />
<div id="fileStatus"></div>

<!-- Assistance Type Checkboxes -->
<div class="form-check">
    <input class="form-check-input assistance-type" type="checkbox"
           name="typeOfAssistance" value="Medical" id="medicalCheck">
    <label class="form-check-label" for="medicalCheck">Medical</label>
    <input type="text" class="form-control assistance-spec"
           placeholder="Specify medical assistance" style="display:none;">
</div>
<input type="hidden" id="assistanceReflection" name="TypeOfAssistance" />

<!-- Requestor Details Toggle -->
<button type="button" id="toggleRequestorBtn" class="btn btn-secondary">
    Show Requestor Details
</button>
<div id="requestorDetailsSection" style="display: none;">
    <!-- Requestor fields... -->
</div>

<!-- Form with Submit Button -->
<form id="updateForm" method="post">
    <!-- Form fields... -->
    <button type="submit" id="submitBtn" class="btn btn-primary">Update</button>
</form>
```

---

## Usage Examples

### Example 1: View PDF Document

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

### Example 2: Zoom Image

```html
<!-- For images -->
<div class="id-image-container" onclick="zoomImage('data:image/jpeg;base64,@ViewData["FrontIDBase64"]')">
    <img src="data:image/jpeg;base64,@ViewData["FrontIDBase64"]" alt="Front ID" class="id-image" />
</div>
```

### Example 3: Approve Application

```html
<button type="button" class="btn btn-success" onclick="updateStatusApproved()">
    <i class="bi bi-check-circle"></i> Approve Application
</button>
```

---

## CSS Requirements

### PDF Viewer Modal Styles

```css
.pdf-viewer-modal {
    display: none;
    position: fixed;
    z-index: 2000;
    left: 0;
    top: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(0,0,0,0.9);
}

.pdf-viewer-content {
    position: relative;
    margin: 0;
    width: 100%;
    height: 100%;
    display: flex;
    flex-direction: column;
}

.pdf-viewer-header {
    background-color: #333;
    color: white;
    padding: 10px 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.pdf-viewer-body {
    flex: 1;
    overflow: auto;
    display: flex;
    justify-content: center;
    align-items: center;
    background-color: #525252;
}

#pdfCanvas {
    max-width: 100%;
    height: auto;
}
```

### Image Zoom Modal Styles

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

### Comments Modal Styles

```css
.comments-modal {
    display: none;
    position: fixed;
    z-index: 3000;
    left: 0;
    top: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(0,0,0,0.5);
}

.comments-modal-content {
    background-color: white;
    margin: 10% auto;
    padding: 2rem;
    border-radius: 8px;
    width: 90%;
    max-width: 500px;
    box-shadow: 0 4px 6px rgba(0,0,0,0.1);
}
```

---

## Function Reference

### PDF Viewer Functions (`pdf-viewer.js`)

| Function | Parameters | Description |
|----------|-----------|-------------|
| `viewPDFInModal()` | `fileName, fileType, displayName` | Opens PDF in modal with decryption |
| `nextPage()` | None | Navigate to next page |
| `prevPage()` | None | Navigate to previous page |
| `zoomIn()` | None | Zoom in by 20% |
| `zoomOut()` | None | Zoom out by 20% |
| `closePDFViewer()` | None | Close PDF modal |

### Image Zoom Functions (`image-zoom.js`)

| Function | Parameters | Description |
|----------|-----------|-------------|
| `zoomImage()` | `src` | Open image in zoom modal |
| `closeZoomModal()` | None | Close zoom modal |

### Form Utilities Functions (`form-utilities.js`)

| Function | Parameters | Description |
|----------|-----------|-------------|
| `previewImage()` | `input, previewId` | Preview uploaded file |
| Auto-initialized | N/A | Phone number formatting |
| Auto-initialized | N/A | PhilHealth formatting |
| Auto-initialized | N/A | Assistance checkbox handling |

### Admin Status Functions (`admin-status-functions.js`)

| Function | Parameters | Description |
|----------|-----------|-------------|
| `updateStatusApproved()` | None | Mark application as approved |
| `updateStatusDisapproved()` | None | Open disapproval modal |
| `updateStatusProcessing()` | None | Mark as processing |
| `markAsClaimed()` | None | Mark as claimed |
| `openCommentsModal()` | None | Open comments modal |
| `closeCommentsModal()` | None | Close comments modal |
| `submitDisapproved()` | None | Submit disapproval with comments |

---

## Testing

### Test Checklist

- [ ] PDF viewer opens and displays PDF correctly
- [ ] PDF navigation (next/prev page) works
- [ ] PDF zoom in/out works
- [ ] Image zoom modal opens on click
- [ ] Phone number auto-formats correctly (09XX-XXX-XXXX)
- [ ] PhilHealth number auto-formats correctly (XXXX-XXXX-XXXX)
- [ ] File upload preview shows image
- [ ] File upload validates file type and size
- [ ] Assistance checkboxes allow only one selection
- [ ] Requestor details toggle works
- [ ] Admin status buttons update application status
- [ ] Disapproval modal requires comments
- [ ] Security features prevent right-click, copy, etc.

---

## Troubleshooting

### PDF Viewer Not Working

**Issue:** PDF doesn't load or shows error

**Solutions:**
1. Verify PDF.js is loaded: `<script src="https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.min.js"></script>`
2. Check console for errors
3. Verify ViewPDF controller endpoint exists and works
4. Check that modal HTML elements exist with correct IDs

### Form Utilities Not Working

**Issue:** Phone/PhilHealth formatting doesn't apply

**Solutions:**
1. Verify input elements have correct IDs: `contactNoInput`, `philHealthInput`
2. Check that form-utilities.js is loaded after jQuery
3. Verify no JavaScript errors in console

### Security Scripts Too Restrictive

**Issue:** Users can't copy text from input fields

**Solution:**
The security.js already allows text selection in input/textarea fields. If issues persist, check CSS overrides.

---

## Notes

- **Performance:** All scripts use IIFE pattern to avoid global scope pollution
- **Compatibility:** Works with modern browsers (Chrome, Firefox, Edge, Safari)
- **Security:** security.js provides basic protection but cannot prevent all screenshot/copy methods
- **Maintenance:** Update PDF.js version by changing CDN link in both HTML and pdf-viewer.js

---

*Generated on: 2025-11-07*
*For use with: LingapDVO Application*
