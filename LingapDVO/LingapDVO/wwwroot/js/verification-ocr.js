document.addEventListener('DOMContentLoaded', function () {
    // Initialize animations
    setTimeout(() => {
        document.querySelectorAll('.fade-in').forEach(el => {
            el.classList.add('loaded');
        });
        document.querySelectorAll('.scale-in').forEach(el => {
            el.classList.add('visible');
        });
        document.querySelectorAll('.slide-down').forEach(el => {
            el.classList.add('visible');
        });
    }, 100);

    // Get DOM elements
    const documentType = document.getElementById('document-type');
    const documentWarning = document.getElementById('document-warning');
    const uploadFront = document.getElementById('upload-area-front');
    const uploadBack = document.getElementById('upload-area-back');
    const fileFront = document.getElementById('file-input-front');
    const fileBack = document.getElementById('file-input-back');
    const imagePreviewFront = document.getElementById('image-preview-front');
    const imagePreviewBack = document.getElementById('image-preview-back');
    const status = document.getElementById('ocr-status');
    const resultBox = document.getElementById('ocr-result');
    const progressBar = document.getElementById('progress-bar');
    const progress = document.getElementById('progress');
    const terms = document.getElementById('terms');

    // Davao City Verification Elements
    const davaoVerification = document.getElementById('davao-verification');
    const davaoResultIcon = document.getElementById('davao-result-icon');
    const davaoResultTitle = document.getElementById('davao-result-title');
    const davaoResultMessage = document.getElementById('davao-result-message');
    const davaoStatusBadge = document.getElementById('davao-status-badge');

    // Wrong ID Type Warning Element
    const wrongIdTypeWarning = document.getElementById('wrong-id-warning');

    // Form fields to disable when license is invalid
    const formFields = [
        'idnumber', 'phonenumber', 'lastname', 'firstname', 'middlename',
        'birthdate', 'BlkLotStreet', 'SubVill', 'Barangay', 'District'
    ];

    // Track if OCR is enabled
    let ocrEnabled = false;

    // Go Back button functionality
    const goBackBtn = document.getElementById('goBackBtn');
    goBackBtn.addEventListener('click', function () {
        window.history.back();
    });

    // Hide wrong ID type warning
    function hideWrongIdTypeWarning() {
        if (wrongIdTypeWarning) {
            wrongIdTypeWarning.classList.add('hidden');
        }
    }

    // Show wrong ID type warning
    function showWrongIdTypeWarning(expectedType, detectedType) {
        if (wrongIdTypeWarning) {
            const message = document.getElementById('wrong-id-message');
            message.textContent = `You selected ${expectedType} but uploaded a ${detectedType}. Please upload the correct document type.`;
            wrongIdTypeWarning.classList.remove('hidden');
        }

        // Clear uploaded files and reset
        clearUploadedFiles();
        resultBox.classList.add('hidden');
        davaoVerification.classList.add('hidden');
        status.innerHTML = '<i class="fas fa-exclamation-triangle mr-2"></i>Wrong document type detected. Please upload the correct document.';
    }

    // Handle document type selection
    documentType.addEventListener('change', function () {
        const selectedValue = this.value;

        // Hide any existing wrong ID warning when changing document type
        hideWrongIdTypeWarning();

        if (selectedValue === 'phil-id' || selectedValue === 'driver-license') {
            ocrEnabled = true;
            uploadFront.classList.remove('disabled');
            uploadBack.classList.remove('disabled');
            documentWarning.classList.add('hidden');
            status.innerHTML = '<i class="fas fa-info-circle mr-2"></i>You can now upload ID images';

            // Show Davao verification for both driver's license and national ID
            davaoVerification.classList.add('hidden');
            enableFormFields();
        } else {
            ocrEnabled = false;
            uploadFront.classList.add('disabled');
            uploadBack.classList.add('disabled');
            if (selectedValue) {
                documentWarning.classList.remove('hidden');
                status.innerHTML = '<i class="fas fa-info-circle mr-2"></i>OCR not available for selected document type';
            } else {
                documentWarning.classList.add('hidden');
                status.innerHTML = '<i class="fas fa-info-circle mr-2"></i>Select a document to begin';
            }
            resultBox.classList.add('hidden');
            davaoVerification.classList.add('hidden');
            enableFormFields();
        }
    });

    // Setup uploaders for both front and back
    setupUploader(uploadFront, fileFront, imagePreviewFront, false);
    setupUploader(uploadBack, fileBack, imagePreviewBack, true);

    function setupUploader(uploadArea, fileInput, preview, isBack = false) {
        // Click handler
        uploadArea.addEventListener('click', () => {
            if (!ocrEnabled) return;
            fileInput.click();
        });

        // File input change handler
        fileInput.addEventListener('change', (event) => {
            if (!ocrEnabled) return;
            if (event.target.files.length) {
                handleFileUpload(event.target.files[0], preview, uploadArea, isBack);
            }
        });

        // Drag and drop handlers
        uploadArea.addEventListener('dragover', (e) => {
            if (!ocrEnabled) return;
            e.preventDefault();
            uploadArea.classList.add('dragover');
        });

        uploadArea.addEventListener('dragleave', () => {
            if (!ocrEnabled) return;
            uploadArea.classList.remove('dragover');
        });

        uploadArea.addEventListener('drop', (e) => {
            if (!ocrEnabled) return;
            e.preventDefault();
            uploadArea.classList.remove('dragover');

            if (e.dataTransfer.files.length) {
                fileInput.files = e.dataTransfer.files;
                handleFileUpload(e.dataTransfer.files[0], preview, uploadArea, isBack);
            }
        });
    }

    // Handle file upload and processing
    function handleFileUpload(file, preview, section, isBack) {
        if (!validateFile(file)) return;

        const reader = new FileReader();
        reader.onload = function (e) {
            // Show image preview
            preview.src = e.target.result;
            preview.classList.remove('hidden');
            section.classList.add('active');

            // Process with OCR if enabled
            if (ocrEnabled) {
                processImageWithOCR(preview, isBack);
            }
        };
        reader.readAsDataURL(file);
    }

    // Process image with OCR
    function processImageWithOCR(preview, isBack) {
        status.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Processing...';
        progressBar.classList.remove('hidden');
        progress.style.width = '0%';

        // Hide any previous wrong ID warning
        hideWrongIdTypeWarning();

        Tesseract.recognize(preview.src, 'eng', {
            logger: m => {
                if (m.status === 'recognizing text') {
                    progress.style.width = `${Math.round(m.progress * 100)}%`;
                }
            }
        }).then(({ data: { text } }) => {
            status.innerHTML = '<i class="fas fa-check-circle mr-2"></i>OCR completed!';
            progressBar.classList.add('hidden');

            // First, detect what type of ID was actually uploaded
            const detectedIdType = detectIdType(text);
            const selectedIdType = documentType.value;

            // Check if the uploaded ID matches the selected type
            if (detectedIdType && detectedIdType !== selectedIdType) {
                const expectedName = selectedIdType === 'driver-license' ? 'Driver\'s License' : 'National ID';
                const detectedName = detectedIdType === 'driver-license' ? 'Driver\'s License' : 'National ID';
                showWrongIdTypeWarning(expectedName, detectedName);
                return;
            }

            // If ID type matches or couldn't be detected, proceed with parsing
            if (selectedIdType === "driver-license") {
                if (!isBack) {
                    parseDriverLicenseFront(text);
                }
            } else if (selectedIdType === "phil-id") {
                if (isBack) {
                    parsePhilSysBack(text);
                } else {
                    parsePhilSysFront(text);
                }
            }

        }).catch(err => {
            console.error('OCR Error:', err);
            progressBar.classList.add('hidden');
            status.innerHTML = '<i class="fas fa-exclamation-triangle mr-2"></i>An error occurred while processing the ID. Please try again.';
        });
    }

    // Detect what type of ID was uploaded based on text content
    function detectIdType(text) {
        const upperText = text.toUpperCase();

        // Driver's License indicators
        const driverLicenseIndicators = [
            'DRIVER\'S LICENSE',
            'DRIVER LICENSE',
            'LICENSE TO DRIVE',
            'NON-PROFESSIONAL',
            'PROFESSIONAL',
            'LAND TRANSPORTATION OFFICE',
            'LTO',
            'RESTRICTION',
            'CONDITIONS'
        ];

        // National ID indicators
        const nationalIdIndicators = [
            'PHILSYS',
            'PHILIPPINE IDENTIFICATION',
            'NATIONAL ID',
            'PAG-ASA',
            'PHILIPPINE ID',
            'PSA',
            'PHILSYS NUMBER',
            'UNIQUE REFERENCE NUMBER'
        ];

        let driverLicenseScore = 0;
        let nationalIdScore = 0;

        // Score for Driver's License
        driverLicenseIndicators.forEach(indicator => {
            if (upperText.includes(indicator)) {
                driverLicenseScore++;
            }
        });

        // Score for National ID
        nationalIdIndicators.forEach(indicator => {
            if (upperText.includes(indicator)) {
                nationalIdScore++;
            }
        });

        // Determine the type based on scores
        if (driverLicenseScore > nationalIdScore && driverLicenseScore >= 2) {
            return 'driver-license';
        } else if (nationalIdScore > driverLicenseScore && nationalIdScore >= 2) {
            return 'phil-id';
        }

        // If scores are equal or unclear, try additional heuristics
        if (driverLicenseScore === nationalIdScore) {
            // Check for specific patterns unique to each type
            if (upperText.includes('LICENSE NO') || upperText.includes('EXPIRY') || upperText.includes('RESTRICTION')) {
                return 'driver-license';
            } else if (upperText.includes('PHILSYS') || upperText.includes('UNIQUE REFERENCE') || upperText.includes('PSA')) {
                return 'phil-id';
            }
        }

        // If still unclear, return null
        return null;
    }

    // Parse Driver's License Front
    function parseDriverLicenseFront(text) {
        const lines = text.split('\n').map(line => line.trim()).filter(line => line);

        let idNumber = "", lastName = "", firstName = "", middleName = "", birthdate = "", city = "";

        // Find license number (format: X00-00-000000)
        for (const line of lines) {
            const licenseMatch = line.match(/[A-Z0-9]{1,3}-[A-Z0-9]{2}-\d{6}/);
            if (licenseMatch) {
                idNumber = licenseMatch[0];
                break;
            }
        }

        // Find name (Lastname, Firstname MiddleName format)
        for (let i = 0; i < lines.length; i++) {
            if (lines[i].includes(',')) {
                const nameParts = lines[i].split(',');
                if (nameParts.length >= 2) {
                    lastName = cleanName(nameParts[0]);
                    const firstMiddleParts = nameParts[1].trim().split(' ');
                    if (firstMiddleParts.length > 0) firstName = cleanName(firstMiddleParts[0]);
                    if (firstMiddleParts.length > 1) middleName = cleanName(firstMiddleParts.slice(1).join(' '));
                }
                break;
            }
        }

        // Find birthdate
        for (const line of lines) {
            const dateMatch = line.match(/\b\d{4}\/\d{2}\/\d{2}\b/);
            if (dateMatch) {
                const [year, month, day] = dateMatch[0].split('/');
                birthdate = `${year}-${month}-${day}`;
                break;
            }
        }

        // Find city for Davao verification
        city = extractCity(lines);

        updateFormFields(idNumber, firstName, middleName, lastName, birthdate);

        // Check if it's Davao City
        const isDavaoCity = checkIfDavaoCity(city);

        // Show Davao verification result
        showDavaoVerificationResult(isDavaoCity, city, 'driver-license');
    }

    // Parse PhilSys ID Front - Extract name and birthdate only
    function parsePhilSysFront(text) {
        const lines = text.split('\n').map(line => line.trim()).filter(line => line);
        const linesUpper = lines.map(line => line.toUpperCase());

        let idNumber = "", lastName = "", firstName = "", middleName = "", birthdate = "", city = "";

        // Find ID number (format: 1234-5678-9012-3456)
        for (const line of lines) {
            const idMatch = line.match(/\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}/);
            if (idMatch) {
                idNumber = idMatch[0].replace(/\s+/g, "").replace(/-/g, "");
                break;
            }
        }

        // Find names
        for (let i = 0; i < linesUpper.length; i++) {
            const line = linesUpper[i];
            if (line.includes("APELYIDO") && !line.includes("GITNANG")) {
                lastName = cleanName(lines[i + 1] || "");
            }
            if (line.includes("MGA PANGALAN") || line.includes("GIVEN NAMES")) {
                firstName = cleanName(lines[i + 1] || "");
            }
            if (line.includes("GITNANG") || line.includes("MIDDLE NAME")) {
                middleName = cleanName(lines[i + 1] || "");
            }
            // Look for birthdate in various formats
            if (line.includes("KAPANGANAKAN") || line.includes("PETSA") || line.includes("BIRTHDATE") || line.includes("DATE OF BIRTH")) {
                // Try to extract birthdate from current line or next line
                let dateText = line;
                if (!containsDate(line) && i + 1 < lines.length) {
                    dateText = lines[i + 1];
                }
                birthdate = extractBirthdateFromText(dateText);
            }
        }

        // If birthdate not found with indicators, search all lines for date patterns
        if (!birthdate) {
            birthdate = findBirthdateInText(lines);
        }

        // Extract city from address for National ID Davao verification
        city = extractCityFromAddress(lines);

        updateFormFields(idNumber, firstName, middleName, lastName, birthdate);

        // Check if it's Davao City for National ID
        const isDavaoCity = checkIfDavaoCity(city);

        // Show Davao verification result for National ID
        showDavaoVerificationResult(isDavaoCity, city, 'national-id');
    }

    // Parse PhilSys ID Back - No gender extraction
    function parsePhilSysBack(text) {
        // No functionality needed for back side since gender extraction is removed
        resultBox.classList.remove('hidden');
        resultBox.textContent = "Back side processed. Please verify the information extracted from the front side.";
    }

    // Extract city from address for National ID
    function extractCityFromAddress(lines) {
        // Look for address patterns and extract city
        for (let i = 0; i < lines.length; i++) {
            const line = lines[i].toUpperCase();

            // Look for address indicators
            if (line.includes('TIRAHAN') || line.includes('ADDRESS') || line.includes('MANILA') ||
                line.includes('DAVAO') || line.includes('CEBU') || line.includes('QUEZON')) {

                // Check current line and next few lines for city information
                for (let j = i; j < Math.min(i + 3, lines.length); j++) {
                    const addressLine = lines[j].toUpperCase();

                    // Look for city patterns in the address
                    if (addressLine.includes('DAVAO CITY') || addressLine.includes('CITY OF DAVAO')) {
                        return "Davao City";
                    } else if (addressLine.includes('DAVAO')) {
                        return "Davao";
                    } else if (addressLine.includes('MANILA')) {
                        return "Manila";
                    } else if (addressLine.includes('CEBU')) {
                        return "Cebu";
                    } else if (addressLine.includes('QUEZON')) {
                        return "Quezon";
                    } else if (addressLine.includes('METRO MANILA')) {
                        return "Metro Manila";
                    }
                }
            }
        }

        // Alternative: Look for city patterns in any line
        return extractCity(lines);
    }

    // Enhanced birthdate extraction for PhilSys
    function extractBirthdateFromText(text) {
        if (!text) return "";

        // Try various date formats commonly found in PhilSys
        const datePatterns = [
            /\b(\d{1,2})[\/\-\.](\d{1,2})[\/\-\.](\d{4})\b/, // DD/MM/YYYY or DD-MM-YYYY
            /\b(\d{4})[\/\-\.](\d{1,2})[\/\-\.](\d{1,2})\b/, // YYYY/MM/DD or YYYY-MM-DD
            /\b(\d{1,2})\s+(January|February|March|April|May|June|July|August|September|October|November|December)\s+(\d{4})\b/i, // DD Month YYYY
            /\b(January|February|March|April|May|June|July|August|September|October|November|December)\s+(\d{1,2}),?\s+(\d{4})\b/i // Month DD, YYYY
        ];

        for (const pattern of datePatterns) {
            const match = text.match(pattern);
            if (match) {
                if (pattern === datePatterns[0]) {
                    // DD/MM/YYYY format
                    const [, day, month, year] = match;
                    return `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
                } else if (pattern === datePatterns[1]) {
                    // YYYY/MM/DD format
                    const [, year, month, day] = match;
                    return `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
                } else if (pattern === datePatterns[2] || pattern === datePatterns[3]) {
                    // Text month format
                    const monthNames = {
                        'january': '01', 'february': '02', 'march': '03', 'april': '04',
                        'may': '05', 'june': '06', 'july': '07', 'august': '08',
                        'september': '09', 'october': '10', 'november': '11', 'december': '12'
                    };
                    const month = match[1] || match[2];
                    const day = match[2] || match[3];
                    const year = match[3] || match[4];
                    const monthNum = monthNames[month.toLowerCase()];
                    if (monthNum) {
                        return `${year}-${monthNum}-${day.padStart(2, '0')}`;
                    }
                }
            }
        }

        return "";
    }

    // Search all lines for birthdate patterns
    function findBirthdateInText(lines) {
        for (const line of lines) {
            const birthdate = extractBirthdateFromText(line);
            if (birthdate) {
                return birthdate;
            }
        }
        return "";
    }

    // Check if text contains a date pattern
    function containsDate(text) {
        const datePatterns = [
            /\b\d{1,2}[\/\-\.]\d{1,2}[\/\-\.]\d{4}\b/,
            /\b\d{4}[\/\-\.]\d{1,2}[\/\-\.]\d{1,2}\b/,
            /\b\d{1,2}\s+(January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{4}\b/i,
            /\b(January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{1,2},?\s+\d{4}\b/i
        ];

        return datePatterns.some(pattern => pattern.test(text));
    }

    // Extract city from driver's license text
    function extractCity(lines) {
        // Look for city patterns
        const cityIndicators = ['DAVAO', 'CAGAYAN', 'CEBU', 'MANILA', 'QUEZON', 'MAKATI', 'TAGUM', 'DIGOS', 'PANABO'];

        for (const line of lines) {
            for (const indicator of cityIndicators) {
                if (line.toUpperCase().includes(indicator)) {
                    // Check if it's specifically Davao City
                    if (line.toUpperCase().includes('DAVAO')) {
                        if (line.toUpperCase().includes('CITY') || line.toUpperCase().includes('DAVAO CITY')) {
                            return "Davao City";
                        }
                    }
                    return cleanText(line);
                }
            }
        }

        // Alternative: Look for text that might be a city
        for (const line of lines) {
            if (line.length > 3 && line.length < 30 &&
                /^[A-Z][a-zA-Z\s]+$/.test(line) &&
                !line.includes('REPUBLIC') && !line.includes('PHILIPPINES') &&
                !line.includes('LICENSE') && !line.includes('DRIVER')) {
                return cleanText(line);
            }
        }

        return "Not detected";
    }

    // Check if city is Davao City
    function checkIfDavaoCity(city) {
        if (!city || city === "Not detected") return false;

        const davaoPatterns = [
            'DAVAO CITY',
            'CITY OF DAVAO',
            'DAVAO',
            'DVO CITY',
            'D.C.'
        ];

        const normalizedCity = city.toUpperCase().trim();

        for (const pattern of davaoPatterns) {
            if (normalizedCity.includes(pattern) || pattern.includes(normalizedCity)) {
                return true;
            }
        }

        return false;
    }

    // Show Davao City verification result
    function showDavaoVerificationResult(isDavaoCity, detectedCity, idType) {
        // Show for both driver's license and national ID
        davaoVerification.classList.remove('hidden');

        if (isDavaoCity) {
            // Valid Davao City ID
            davaoVerification.className = 'p-6 rounded-xl border-l-4 bg-green-50 border-green-200 slide-down';
            davaoResultIcon.className = 'fas fa-check-circle text-2xl mt-1 text-green-500';
            davaoResultTitle.textContent = idType === 'driver-license' ? 'Valid Davao City License' : 'Valid Davao City National ID';
            davaoResultMessage.textContent = idType === 'driver-license'
                ? 'Your driver\'s license has been verified as issued in Davao City.'
                : 'Your National ID has been verified as registered in Davao City.';
            davaoStatusBadge.className = 'px-3 py-1 rounded-full text-sm font-semibold bg-green-500 text-white';
            davaoStatusBadge.textContent = 'Valid';

            // Enable form fields for valid ID
            enableFormFields();
        } else {
            // Invalid (not Davao City) ID
            davaoVerification.className = 'p-6 rounded-xl border-l-4 bg-red-50 border-red-200 slide-down';
            davaoResultIcon.className = 'fas fa-times-circle text-2xl mt-1 text-red-500';
            davaoResultTitle.textContent = idType === 'driver-license' ? 'License Not From Davao City' : 'National ID Not From Davao City';
            davaoResultMessage.textContent = `This service is only available for Davao City residents, or your picture may be blurry. Please try again.`;
            davaoStatusBadge.className = 'px-3 py-1 rounded-full text-sm font-semibold bg-red-500 text-white';
            davaoStatusBadge.textContent = 'Invalid';

            // Disable form fields and clear uploaded files for invalid ID
            disableFormFields();
            clearUploadedFiles();
        }
    }

    // Disable all form fields
    function disableFormFields() {
        formFields.forEach(fieldId => {
            const field = document.getElementById(fieldId);
            if (field) {
                field.disabled = true;
                field.classList.add('disabled-field');
            }
        });

        // Also disable the submit button
        const submitButton = document.querySelector('button[type="submit"]');
        if (submitButton) {
            submitButton.disabled = true;
            submitButton.classList.add('opacity-50', 'cursor-not-allowed');
        }
    }

    // Enable all form fields
    function enableFormFields() {
        formFields.forEach(fieldId => {
            const field = document.getElementById(fieldId);
            if (field) {
                field.disabled = false;
                field.classList.remove('disabled-field');
            }
        });

        // Also enable the submit button
        const submitButton = document.querySelector('button[type="submit"]');
        if (submitButton) {
            submitButton.disabled = false;
            submitButton.classList.remove('opacity-50', 'cursor-not-allowed');
        }
    }

    // Clear uploaded files and reset upload areas
    function clearUploadedFiles() {
        // Clear file inputs
        fileFront.value = '';
        fileBack.value = '';

        // Hide preview images
        imagePreviewFront.classList.add('hidden');
        imagePreviewBack.classList.add('hidden');

        // Remove active class from upload areas
        uploadFront.classList.remove('active');
        uploadBack.classList.remove('active');

        // Clear any extracted data from form fields
        document.getElementById('idnumber').value = '';
        document.getElementById('firstname').value = '';
        document.getElementById('middlename').value = '';
        document.getElementById('lastname').value = '';
        document.getElementById('birthdate').value = '';
    }

    // Helper functions
    function cleanName(text) {
        if (!text) return "";
        return text
            .replace(/[^a-z\s]/gi, "")
            .replace(/\s+/g, " ")
            .trim()
            .replace(/^[a-z]\s+/, "")
            .replace(/^[a-z]\s+/, "")
            .replace(/\s+[a-z]$/, "")
            .replace(/^\s*[a-z]\s*$/, "");
    }

    function cleanText(text) {
        return text
            .replace(/[^a-zA-Z0-9\s\-,\.]/g, '')
            .replace(/\s+/g, ' ')
            .trim();
    }

    function cleanBirthdate(text) {
        if (!text) return "";
        const dateMatch = text.match(/(\d{1,2})[\/\-\.](\d{1,2})[\/\-\.](\d{4})/);
        if (dateMatch) {
            const [, month, day, year] = dateMatch;
            return `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
        }
        return "";
    }

    // Update form fields with extracted data (no gender parameter)
    function updateFormFields(idNumber, firstName, middleName, lastName, birthdate) {
        if (idNumber) document.getElementById('idnumber').value = idNumber;
        if (firstName) document.getElementById('firstname').value = firstName.toUpperCase();
        if (middleName) document.getElementById('middlename').value = middleName.toUpperCase();
        if (lastName) document.getElementById('lastname').value = lastName.toUpperCase();
        if (birthdate) document.getElementById('birthdate').value = birthdate;

        // Show success message
        resultBox.classList.remove('hidden');
        resultBox.textContent = "Information extracted from ID. Please verify the details.";
    }

    function validateFile(file) {
        const validTypes = ['image/jpeg', 'image/png', 'image/jpg'];
        const maxSize = 5 * 1024 * 1024; // 5MB

        if (!validTypes.includes(file.type)) {
            alert('Please upload a valid image file (JPEG or PNG).');
            return false;
        }

        if (file.size > maxSize) {
            alert('File size exceeds 5MB limit.');
            return false;
        }

        return true;
    }

    // Form validation
    document.getElementById('registrationForm').addEventListener('submit', function (e) {
        if (!terms.checked) {
            alert('You must agree to the terms and conditions.');
            e.preventDefault();
            return;
        }
    });
});      