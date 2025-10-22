document.addEventListener('DOMContentLoaded', function () {
    const dataEl = document.getElementById('registration-data');

    // ═══════════════════════════════════════════════════════════════
    // 🔍 DEBUG: Log registered user info for verification
    // ═══════════════════════════════════════════════════════════════
    console.log('=== ACCOUNT VERIFICATION PAGE LOADED ===');

    // Get registered user names from server-side variables
    const serverData = document.getElementById('server-data');
    const registeredFirstName = serverData?.dataset?.firstName || '';
    const registeredMiddleName = serverData?.dataset?.middleName || '';
    const registeredLastName = serverData?.dataset?.lastName || '';
    const registeredSuffix = serverData?.dataset?.suffix || '';

    console.log('Registered names:', {
        first: registeredFirstName,
        middle: registeredMiddleName,
        last: registeredLastName,
        suffix: registeredSuffix
    });

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

    // Get DOM elements with null checks
    const documentType = document.getElementById('document-type');
    const documentWarning = document.getElementById('document-warning');
    const uploadFront = document.getElementById('upload-area-front');
    const uploadBack = document.getElementById('upload-area-back');
    const fileFront = document.getElementById('file-input-front');
    const fileBack = document.getElementById('file-input-back');
    const imagePreview = document.getElementById('image-preview');
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
        'sex', 'birthdate', 'BlkLotStreet', 'SubVill', 'Barangay', 'District',
        'SecurityQuestions', 'Securityanswer'
    ];

    // Track if OCR is enabled
    let ocrEnabled = false;

    // Check if required elements exist
    if (!documentType || !uploadFront || !uploadBack) {
        console.error('Required DOM elements not found');
        return;
    }

    // Go Back button functionality
    const goBackBtn = document.getElementById('goBackBtn');
    if (goBackBtn) {
        goBackBtn.addEventListener('click', function () {
            window.history.back();
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // 🔍 NAME VERIFICATION SYSTEM - Compare OCR data with registered user
    // ═══════════════════════════════════════════════════════════════

    // Normalize name for comparison (same as backend)
    function normalizePhilippineName(name) {
        if (!name) return "";

        let normalized = name.toLowerCase().trim();

        // Remove periods and extra spaces
        normalized = normalized.replace(/\./g, "").replace(/\s+/g, " ").trim();

        // Normalize Filipino compound name particles
        const normalizationMap = {
            'dela cruz': 'delacruz',
            'de la cruz': 'delacruz',
            'delos santos': 'delossantos',
            'de los santos': 'delossantos',
            'delos reyes': 'delosreyes',
            'de los reyes': 'delosreyes',
            'dela rosa': 'delarosa',
            'de la rosa': 'delarosa',
            'dela paz': 'delapaz',
            'de la paz': 'delapaz',
            'del rosario': 'delrosario',
            'de guzman': 'deguzman',
            'san jose': 'sanjose',
            'san juan': 'sanjuan',
            'santa maria': 'santamaria',
            'santa cruz': 'santacruz'
        };

        // Apply normalization
        Object.keys(normalizationMap).forEach(key => {
            const regex = new RegExp(key, 'gi');
            normalized = normalized.replace(regex, normalizationMap[key]);
        });

        return normalized;
    }

    // Enhanced name validation function
    function validateNameMatch(extractedFirstName, extractedLastName, extractedMiddleName = '') {
        if (!registeredFirstName || !registeredLastName) {
            console.warn('Registered name information not available');
            return { isValid: true, message: 'Registered name not available for comparison' };
        }

        // Check if extracted names are empty
        if (!extractedFirstName || !extractedLastName) {
            return {
                isValid: false,
                message: 'Could not extract complete name from ID'
            };
        }

        // Normalize names for comparison
        const normalizedIdFirstName = normalizePhilippineName(extractedFirstName);
        const normalizedIdLastName = normalizePhilippineName(extractedLastName);
        const normalizedIdMiddleName = normalizePhilippineName(extractedMiddleName);
        const normalizedRegFirstName = normalizePhilippineName(registeredFirstName);
        const normalizedRegLastName = normalizePhilippineName(registeredLastName);
        const normalizedRegMiddleName = normalizePhilippineName(registeredMiddleName);

        // Debug logging
        console.log('=== NAME VERIFICATION ===');
        console.log('Extracted from ID:', {
            first: extractedFirstName,
            middle: extractedMiddleName,
            last: extractedLastName
        });
        console.log('Registered user:', {
            first: registeredFirstName,
            middle: registeredMiddleName,
            last: registeredLastName,
            suffix: registeredSuffix
        });
        console.log('Normalized ID names:', {
            first: normalizedIdFirstName,
            last: normalizedIdLastName,
            middle: normalizedIdMiddleName
        });
        console.log('Normalized registered names:', {
            first: normalizedRegFirstName,
            last: normalizedRegLastName,
            middle: normalizedRegMiddleName
        });

        // Check for exact match
        const firstNameMatches = normalizedRegFirstName === normalizedIdFirstName;
        const lastNameMatches = normalizedRegLastName === normalizedIdLastName;

        // Check middle name if available (optional match)
        const middleNameMatches = !normalizedRegMiddleName ||
            !normalizedIdMiddleName ||
            normalizedRegMiddleName === normalizedIdMiddleName;

        if (firstNameMatches && lastNameMatches) {
            console.log('✅ NAME VERIFICATION PASSED');
            return {
                isValid: true,
                message: 'Name matches registered user',
                details: {
                    firstNameMatch: true,
                    lastNameMatch: true,
                    middleNameMatch: middleNameMatches
                }
            };
        } else {
            console.log('❌ NAME VERIFICATION FAILED');
            return {
                isValid: false,
                message: 'Name does not match registered user',
                details: {
                    firstNameMatch: firstNameMatches,
                    lastNameMatch: lastNameMatches,
                    middleNameMatch: middleNameMatches,
                    mismatchedFields: {
                        firstName: !firstNameMatches,
                        lastName: !lastNameMatches,
                        middleName: !middleNameMatches
                    }
                }
            };
        }
    }

    // Show name verification result
    function showNameVerificationResult(verificationResult, extractedData) {
        if (!resultBox) return;

        resultBox.classList.remove('hidden');

        if (verificationResult.isValid) {
            resultBox.className = 'p-4 rounded-lg bg-green-50 border border-green-200 text-green-800';
            resultBox.innerHTML = `
                <div class="flex items-center">
                    <i class="fas fa-check-circle text-green-500 mr-2"></i>
                    <strong>Name Verified:</strong> ID matches registered user
                </div>
                <div class="mt-2 text-sm">
                    <div>Extracted: ${extractedData.first} ${extractedData.middle} ${extractedData.last}</div>
                    <div>Registered: ${registeredFirstName} ${registeredMiddleName} ${registeredLastName}</div>
                </div>
            `;
        } else {
            resultBox.className = 'p-4 rounded-lg bg-red-50 border border-red-200 text-red-800';
            resultBox.innerHTML = `
                <div class="flex items-center">
                    <i class="fas fa-exclamation-triangle text-red-500 mr-2"></i>
                    <strong>Name Mismatch:</strong> ${verificationResult.message}
                </div>
                <div class="mt-2 text-sm">
                    <div>ID Name: ${extractedData.first} ${extractedData.middle} ${extractedData.last}</div>
                    <div>Registered: ${registeredFirstName} ${registeredMiddleName} ${registeredLastName}</div>
                    <div class="mt-2 text-red-600">
                        Please ensure you're using your own valid ID
                    </div>
                </div>
            `;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 🧹 FIXED OCR TEXT PREPROCESSING FUNCTIONS
    // ═══════════════════════════════════════════════════════════════

    // Fixed OCR text preprocessing - ensure it always returns an array
    function preprocessOCRLines(text) {
        if (!text || typeof text !== 'string') {
            console.warn('Invalid text provided to preprocessOCRLines:', text);
            return [];
        }

        try {
            const sanitized = sanitizeOCRText(text);
            const lines = sanitized.split('\n')
                .map(line => line.trim())
                .filter(line => line.length > 0);

            // Remove likely header/footer garbage
            const filtered = lines.filter(line => {
                const upper = line.toUpperCase();
                // Skip lines that are pure numbers or single characters
                if (line.length < 2) return false;
                // Skip common header/footer text
                if (upper.includes('REPUBLIC') && upper.includes('PHILIPPINES')) return false;
                if (upper.match(/^PAGE\s*\d+/)) return false;
                return true;
            });

            console.log('📄 Preprocessed OCR lines:', filtered);
            return filtered;
        } catch (error) {
            console.error('Error in preprocessOCRLines:', error);
            return [];
        }
    }

    // Enhanced sanitizeOCRText with better error handling
    function sanitizeOCRText(text) {
        if (!text || typeof text !== 'string') {
            console.warn('Invalid text provided to sanitizeOCRText:', text);
            return "";
        }

        try {
            let cleaned = text
                // Remove common OCR garbage symbols
                .replace(/[|¦\[\]{}©®™°«»¬§¶†‡]/g, "")
                // Remove control characters and non-printable chars
                .replace(/[\x00-\x1F\x7F-\x9F]/g, "")
                // Remove multiple dots/periods that aren't part of names
                .replace(/\.{2,}/g, ".")
                // Normalize dashes and hyphens
                .replace(/[—–]/g, "-")
                // Remove underscores, tildes, backticks
                .replace(/[_~`]/g, "")
                // Fix common OCR misreads of letters
                .replace(/\b0(?=[A-Za-z])/g, "O") // 0 followed by letter -> O
                .replace(/\b1(?=[A-Za-z]{2,})/g, "I") // 1 at start of word -> I
                .replace(/5(?=[A-Za-z]{2,})/g, "S") // 5 in middle of text -> S
                // Normalize whitespace
                .replace(/\s+/g, " ")
                .trim();

            return cleaned;
        } catch (error) {
            console.error('Error in sanitizeOCRText:', error);
            return text || "";
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 🪪 FIXED PHILSYS FRONT PARSING WITH BETTER ERROR HANDLING
    // ═══════════════════════════════════════════════════════════════

    function parsePhilSysFront(text) {
        console.log("=== PHILSYS FRONT PARSING START ===");
        
        // Ensure we have valid text input
        if (!text || typeof text !== 'string') {
            console.warn("⚠️ Invalid text provided to parsePhilSysFront:", text);
            return null;
        }

        const idData = {
            first: "",
            middle: "",
            last: "",
            suffix: "",
            sex: "",
            birthDate: "",
            address: "",
            city: "",
            barangay: ""
        };

        try {
            // Use the fixed preprocessing function
            const lines = preprocessOCRLines(text);
            
            // Check if we have valid lines
            if (!lines || !Array.isArray(lines) || lines.length === 0) {
                console.warn("⚠️ No valid lines extracted from OCR text");
                return null;
            }

            console.log("Processing PhilSys front with lines:", lines);

            // Normalize all text for easier matching
            const text = lines.join(" ").toUpperCase();

            // ──────────────── Extract Name ────────────────
            // Common OCR patterns from PhilSys IDs
            const namePattern = /NAME\s*[:\-]?\s*([A-ZÑ\s]+)\s*/;
            const matchName = text.match(namePattern);
            if (matchName) {
                const fullName = matchName[1].trim().replace(/\s{2,}/g, " ");
                const parts = fullName.split(" ");
                if (parts.length >= 2) {
                    idData.last = parts[0];
                    idData.first = parts[1];
                    idData.middle = parts.length > 2 ? parts.slice(2).join(" ") : "";
                }
            }

            // ──────────────── Extract Sex ────────────────
            const sexMatch = text.match(/\b(SEX|GENDER)\s*[:\-]?\s*(MALE|FEMALE|M|F)\b/);
            if (sexMatch) idData.sex = sexMatch[2].toUpperCase();

            // ──────────────── Extract Birth Date ────────────────
            const dobMatch = text.match(/\bBIRTH\s*DATE\s*[:\-]?\s*([0-9]{2}[-\/][0-9]{2}[-\/][0-9]{4})\b/);
            if (dobMatch) idData.birthDate = dobMatch[1];

            // ──────────────── Extract Address ────────────────
            const addrMatch = text.match(/\bADDRESS\s*[:\-]?\s*([A-Z0-9\s,.-]+)/);
            if (addrMatch) idData.address = addrMatch[1].trim();

            // Optional fallback: detect city or barangay by keywords
            const cityMatch = text.match(/\b(CITY|MUNICIPALITY)\s*[:\-]?\s*([A-Z\s]+)/);
            const brgyMatch = text.match(/\b(BARANGAY|BRGY)\s*[:\-]?\s*([A-Z0-9\s]+)/);
            if (cityMatch) idData.city = cityMatch[2].trim();
            if (brgyMatch) idData.barangay = brgyMatch[2].trim();

            console.log("✅ Parsed ID Data:", idData);

            // ═══════════════════════════════════════════════════════════════
            // 🔍 PERFORM NAME VERIFICATION AFTER EXTRACTION
            // ═══════════════════════════════════════════════════════════════
            if (idData.first && idData.last) {
                const verificationResult = validateNameMatch(idData.first, idData.last, idData.middle);
                showNameVerificationResult(verificationResult, idData);

                // Update form fields only if verification passed
                if (verificationResult.isValid) {
                    updateFormFields(idData);
                } else {
                    // Show warning but still populate fields for manual verification
                    updateFormFields(idData);
                    console.warn('Name mismatch detected - fields populated but require manual verification');
                }
            }

            return idData;

        } catch (error) {
            console.error('❌ Error in parsePhilSysFront:', error);
            return null;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 🚗 FIXED DRIVER'S LICENSE PARSING WITH BETTER ERROR HANDLING
    // ═══════════════════════════════════════════════════════════════

    function parseDriverLicenseFront(text) {
        console.log("=== DRIVER'S LICENSE PARSING START ===");

        // Validate input
        if (!text || typeof text !== 'string') {
            console.warn("⚠️ Invalid text provided to parseDriverLicenseFront:", text);
            return;
        }

        try {
            // Advanced preprocessing with fixed function
            const lines = preprocessOCRLines(text);
            
            // Check if we have valid lines
            if (!lines || !Array.isArray(lines) || lines.length === 0) {
                console.warn("⚠️ No valid lines extracted from OCR text for driver's license");
                return;
            }

            console.log("Processing driver's license with lines:", lines);

            let idNumber = "", lastName = "", firstName = "", middleName = "", suffix = "", birthdate = "", sex = "", city = "", barangay = "";

            // ─────────────────────────────────────────────────────────────
            // STRATEGY 1: License Number Extraction
            // Patterns: X00-00-000000, X00 00 000000, X0000000000
            // ─────────────────────────────────────────────────────────────
            const licensePatterns = [
                /\b[A-Z]\d{2}[\s\-]\d{2}[\s\-]\d{6}\b/,    // Standard: A00-00-000000
                /\b[A-Z0-9]{1,3}[\s\-]\d{2}[\s\-]\d{6}\b/, // Variations with letters
                /\b[A-Z]\d{10}\b/                           // Continuous: A0000000000
            ];

            for (const line of lines) {
                for (const pattern of licensePatterns) {
                    const match = line.match(pattern);
                    if (match) {
                        idNumber = match[0].replace(/\s+/g, '-').replace(/([A-Z0-9]{1,3})(\d{2})(\d{6})/, '$1-$2-$3');
                        console.log("License number found:", idNumber);
                        break;
                    }
                }
                if (idNumber) break;
            }

            // ─────────────────────────────────────────────────────────────
            // STRATEGY 2: Name Extraction (Multiple Approaches)
            // Format: LASTNAME, FIRSTNAME MIDDLENAME SUFFIX
            // ─────────────────────────────────────────────────────────────

            // Approach 2A: Look for comma-separated name pattern
            const namePattern = /^[A-ZñÑ\s\-'.]{3,},\s*[A-ZñÑ\s\-'.]{2,}$/i;
            for (const line of lines) {
                if (line.includes(',') && namePattern.test(line)) {
                    console.log("Name line found:", line);

                    const parts = line.split(',').map(s => s.trim());
                    if (parts.length >= 2) {
                        // Extract last name (may include suffix)
                        const lastNamePart = parts[0];
                        const suffixExtraction = extractSuffixFromName(lastNamePart);
                        lastName = cleanName(suffixExtraction.cleanedName);
                        if (suffixExtraction.suffix) suffix = suffixExtraction.suffix;

                        // Extract first and middle names
                        const firstMiddlePart = parts[1];
                        const suffixExtraction2 = extractSuffixFromName(firstMiddlePart);
                        if (suffixExtraction2.suffix && !suffix) suffix = suffixExtraction2.suffix;

                        const names = suffixExtraction2.cleanedName.split(/\s+/).filter(n => n.length > 1);
                        if (names.length > 0) firstName = cleanName(names[0]);
                        if (names.length > 1) middleName = cleanName(names.slice(1).join(' '));

                        console.log("Extracted - Last:", lastName, "First:", firstName, "Middle:", middleName, "Suffix:", suffix);
                        break;
                    }
                }
            }

            // Approach 2B: If no name found, look for labeled fields
            if (!lastName || !firstName) {
                const linesUpper = lines.map(l => l.toUpperCase());
                for (let i = 0; i < linesUpper.length; i++) {
                    const lineUpper = linesUpper[i];

                    if (lineUpper.includes('LAST NAME') || lineUpper.includes('SURNAME')) {
                        if (i + 1 < lines.length) lastName = cleanName(lines[i + 1]);
                    }
                    if (lineUpper.includes('FIRST NAME') || lineUpper.includes('GIVEN NAME')) {
                        if (i + 1 < lines.length) firstName = cleanName(lines[i + 1]);
                    }
                    if (lineUpper.includes('MIDDLE NAME')) {
                        if (i + 1 < lines.length) middleName = cleanName(lines[i + 1]);
                    }
                }
            }

            // ─────────────────────────────────────────────────────────────
            // STRATEGY 3: Birthdate Extraction (Multiple Formats)
            // ─────────────────────────────────────────────────────────────
            for (const line of lines) {
                const extracted = extractBirthdateFromText(line);
                if (extracted) {
                    birthdate = extracted;
                    console.log("Birthdate found:", birthdate);
                    break;
                }
            }

            // ─────────────────────────────────────────────────────────────
            // STRATEGY 4: Gender Extraction
            // ─────────────────────────────────────────────────────────────
            for (const line of lines) {
                const genderResult = cleanGender(line);
                if (genderResult) {
                    sex = genderResult;
                    console.log("Gender found:", sex);
                    break;
                }
            }

            // ─────────────────────────────────────────────────────────────
            // STRATEGY 5: Location Extraction (City & Barangay)
            // ─────────────────────────────────────────────────────────────
            city = extractCity(lines);
            barangay = extractBarangayFromText(lines);
            console.log("Location - City:", city, "Barangay:", barangay);

            // After extracting names, perform verification
            const extractedData = {
                first: firstName,
                middle: middleName,
                last: lastName,
                suffix: suffix
            };

            if (firstName && lastName) {
                const verificationResult = validateNameMatch(firstName, lastName, middleName);
                showNameVerificationResult(verificationResult, extractedData);
                
                // Update form fields
                updateFormFieldsFromDriverLicense(idNumber, firstName, middleName, lastName, birthdate, sex);
                
                if (!verificationResult.isValid) {
                    console.warn('Name mismatch detected in driver license');
                }
            }

            // Update suffix field if found
            if (suffix) {
                const suffixSelect = document.querySelector('select[name="Suffix"]');
                if (suffixSelect) {
                    const matchedOption = Array.from(suffixSelect.options).find(opt =>
                        opt.value.toLowerCase() === suffix.toLowerCase() ||
                        opt.text.toLowerCase() === suffix.toLowerCase()
                    );
                    if (matchedOption) suffixSelect.value = matchedOption.value;
                }
            }

            // Davao City verification and barangay auto-fill
            const isDavaoCity = checkIfDavaoCity(city);
            if (barangay && isDavaoCity) {
                const barangaySelect = document.querySelector('select[name="Barangay"]');
                if (barangaySelect) {
                    const normalizedBarangay = barangay.toLowerCase();
                    const matchedOption = Array.from(barangaySelect.options).find(opt =>
                        opt.value.toLowerCase() === normalizedBarangay ||
                        opt.value.toLowerCase().includes(normalizedBarangay) ||
                        normalizedBarangay.includes(opt.value.toLowerCase())
                    );
                    if (matchedOption) barangaySelect.value = matchedOption.value;
                }
            }

            showDavaoVerificationResult(isDavaoCity, city, barangay, 'driver-license');
            console.log("=== DRIVER'S LICENSE PARSING END ===");

        } catch (error) {
            console.error('❌ Error in parseDriverLicenseFront:', error);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 🎯 UPDATED FORM FIELD POPULATION
    // ═══════════════════════════════════════════════════════════════

    function updateFormFields(data) {
        if (!data) return;

        const map = {
            first: "#firstName",
            middle: "#middleName", 
            last: "#lastName",
            suffix: "#suffix",
            sex: "#gender",
            birthDate: "#birthDate",
            address: "#address",
            city: "#city",
            barangay: "#barangay"
        };

        for (const key in map) {
            const el = document.querySelector(map[key]);
            if (el && data[key]) {
                el.value = data[key];
            }
        }

        console.log("🎯 Form fields updated:", data);
    }

    function updateFormFieldsFromDriverLicense(idNumber, firstName, middleName, lastName, birthdate, sex) {
        console.log('=== UPDATING FORM FIELDS FROM DRIVER LICENSE ===');
        
        // Apply cleaning functions
        const cleanedFirstName = firstName ? cleanName(firstName) : '';
        const cleanedMiddleName = middleName ? cleanName(middleName) : '';
        const cleanedLastName = lastName ? cleanName(lastName) : '';
        const cleanedGender = sex ? cleanGender(sex) : '';

        const idnumberField = document.getElementById('idnumber');
        const firstnameField = document.getElementById('firstname');
        const middlenameField = document.getElementById('middlename');
        const lastnameField = document.getElementById('lastname');
        const birthdateField = document.getElementById('birthdate');
        const sexField = document.getElementById('sex');

        if (idNumber && idnumberField) idnumberField.value = idNumber;
        if (cleanedFirstName && firstnameField) firstnameField.value = cleanedFirstName;
        if (cleanedMiddleName && middlenameField) middlenameField.value = cleanedMiddleName;
        if (cleanedLastName && lastnameField) lastnameField.value = cleanedLastName;
        if (birthdate && birthdateField) birthdateField.value = birthdate;
        if (cleanedGender && sexField) sexField.value = cleanedGender;
    }

    // ═══════════════════════════════════════════════════════════════
    // 📋 FORM SUBMISSION VALIDATION WITH NAME VERIFICATION
    // ═══════════════════════════════════════════════════════════════

    function validateFormSubmission() {
        console.log('=== FINAL FORM VALIDATION ===');

        // Check terms and conditions
        if (!terms || !terms.checked) {
            alert('You must agree to the terms and conditions.');
            return false;
        }

        // Get current form values for final validation
        const firstnameField = document.getElementById('firstname');
        const lastnameField = document.getElementById('lastname');
        const middlenameField = document.getElementById('middlename');

        if (!firstnameField || !lastnameField) {
            console.error('Name fields not found');
            return true; // Allow submission if fields don't exist
        }

        const idFirstName = firstnameField.value.trim();
        const idLastName = lastnameField.value.trim();
        const idMiddleName = middlenameField ? middlenameField.value.trim() : '';

        // Final name verification
        const verificationResult = validateNameMatch(idFirstName, idLastName, idMiddleName);
        
        if (!verificationResult.isValid) {
            // Show final warning before submission
            const proceed = confirm(
                `Name mismatch detected!\n\n` +
                `ID Name: ${idFirstName} ${idMiddleName} ${idLastName}\n` +
                `Registered: ${registeredFirstName} ${registeredMiddleName} ${registeredLastName}\n\n` +
                `Are you sure you want to proceed? This may cause verification issues.`
            );
            
            if (!proceed) {
                console.log('❌ FORM SUBMISSION CANCELLED - User declined to proceed with name mismatch');
                return false;
            }
        }

        console.log('✅ FORM VALIDATION PASSED - Proceeding with submission');
        return true;
    }

    // Attach form validation
    const registrationForm = document.getElementById('registrationForm');
    if (registrationForm) {
        registrationForm.addEventListener('submit', function(e) {
            const isValid = validateFormSubmission();
            if (!isValid) {
                e.preventDefault();
                return false;
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // 🖼️ IMAGE PROCESSING AND OCR FUNCTIONS
    // ═══════════════════════════════════════════════════════════════

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
            if (message) {
                message.textContent = `You selected ${expectedType} but uploaded a ${detectedType}. Please upload the correct document type.`;
                wrongIdTypeWarning.classList.remove('hidden');
            }
        }

        // Clear uploaded files and reset
        clearUploadedFiles();
        if (resultBox) resultBox.classList.add('hidden');
        if (davaoVerification) davaoVerification.classList.add('hidden');
        if (status) {
            status.innerHTML = '<i class="fas fa-exclamation-triangle mr-2"></i>Wrong document type detected. Please upload the correct document.';
        }
    }

    // Handle document type selection
    documentType.addEventListener('change', function() {
        const selectedValue = this.value;

        // Hide any existing wrong ID warning when changing document type
        hideWrongIdTypeWarning();

        if (selectedValue === 'phil-id' || selectedValue === 'driver-license') {
            ocrEnabled = true;
            uploadFront.classList.remove('disabled');
            uploadBack.classList.remove('disabled');
            if (documentWarning) documentWarning.classList.add('hidden');
            if (status) {
                status.innerHTML = '<i class="fas fa-info-circle mr-2"></i>You can now upload ID images';
            }

            // Show Davao verification for both driver's license and national ID
            if (davaoVerification) davaoVerification.classList.add('hidden');
            enableFormFields();
        } else {
            ocrEnabled = false;
            uploadFront.classList.add('disabled');
            uploadBack.classList.add('disabled');
            if (selectedValue) {
                if (documentWarning) documentWarning.classList.remove('hidden');
                if (status) {
                    status.innerHTML = '<i class="fas fa-info-circle mr-2"></i>OCR not available for selected document type';
                }
            } else {
                if (documentWarning) documentWarning.classList.add('hidden');
                if (status) {
                    status.innerHTML = '<i class="fas fa-info-circle mr-2"></i>Select a document to begin';
                }
            }
            if (resultBox) resultBox.classList.add('hidden');
            if (davaoVerification) davaoVerification.classList.add('hidden');
            enableFormFields();
        }
    });

    // Setup uploaders for both front and back
    setupUploader(uploadFront, fileFront, imagePreview, false);
    setupUploader(uploadBack, fileBack, imagePreviewBack, true);

    function setupUploader(uploadArea, fileInput, preview, isBack = false) {
        if (!uploadArea || !fileInput) return;

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
    async function handleFileUpload(file, preview, section, isBack) {
        if (!validateFile(file)) return;

        const reader = new FileReader();
        reader.onload = async function(e) {
            // Show image preview
            if (preview) {
                preview.src = e.target.result;
                preview.classList.remove('hidden');
            }
            if (section) {
                section.classList.add('active');
            }

            // Process with OCR if enabled
            if (ocrEnabled) {
                // Wait for image to load
                preview.onload = async function() {
                    // Step 1: Analyze image quality
                    status.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Analyzing image quality...';
                    const qualityAnalysis = await analyzeImageQuality(preview);

                    console.log('Quality Analysis:', qualityAnalysis);

                    // Step 2: Check if image is too blurry
                    if (!qualityAnalysis.isAcceptable) {
                        status.innerHTML = '<i class="fas fa-exclamation-triangle mr-2 text-red-500"></i>Image too blurry';
                        showIntelligentErrorModal('too-blurry', { blurScore: qualityAnalysis.blurScore });
                        return;
                    }

                    // Step 3: Enhance image if slightly blurry
                    let imageToProcess = preview;
                    if (qualityAnalysis.needsEnhancement) {
                        status.innerHTML = '<i class="fas fa-magic fa-spin mr-2"></i>Enhancing image quality...';
                        imageToProcess = await enhanceImageQuality(preview);
                        console.log('Image enhanced for better OCR');
                    }

                    // Step 4: Proceed with OCR processing
                    processImageWithOCR(imageToProcess, isBack, qualityAnalysis);
                };
            }
        };
        reader.readAsDataURL(file);
    }

    // ═══════════════════════════════════════════════════════════════
    // 🔄 FIXED OCR PROCESSING WITH BETTER ERROR HANDLING
    // ═══════════════════════════════════════════════════════════════

    // Process image with OCR (with intelligent validation)
    function processImageWithOCR(preview, isBack, qualityAnalysis = null) {
        if (!status || !progressBar || !progress) return;

        status.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Reading ID information...';
        progressBar.classList.remove('hidden');
        progress.style.width = '0%';

        // Hide any previous wrong ID warning
        hideWrongIdTypeWarning();

        // Check if Tesseract is available
        if (typeof Tesseract === 'undefined') {
            console.error('Tesseract.js not loaded');
            status.innerHTML = '<i class="fas fa-exclamation-triangle mr-2"></i>OCR engine not available';
            progressBar.classList.add('hidden');
            return;
        }

        Tesseract.recognize(preview.src, 'eng', {
            logger: m => {
                if (m.status === 'recognizing text') {
                    progress.style.width = `${Math.round(m.progress * 100)}%`;
                }
            }
        }).then(({ data: { text, confidence } }) => {
            status.innerHTML = '<i class="fas fa-check-circle mr-2"></i>OCR completed!';
            progressBar.classList.add('hidden');

            console.log('OCR Confidence:', confidence);
            console.log('Extracted Text:', text);
            console.log('Extracted Text Length:', text?.length || 0);

            // Validate OCR result
            if (!text || typeof text !== 'string' || text.trim().length < 20) {
                console.error('Insufficient or invalid text extracted from image');
                showIntelligentErrorModal('no-data-extracted', { textLength: text?.length || 0 });
                return;
            }

            // Check 2: Validate OCR confidence
            if (confidence < 0.60) {
                console.warn('Low OCR confidence detected');
                if (qualityAnalysis && qualityAnalysis.quality === 'acceptable') {
                    showIntelligentErrorModal('low-confidence', { confidence: confidence });
                }
            }

            // Check 3: Detect ID type mismatch
            const detectedIdType = detectIdType(text);
            const selectedIdType = documentType.value;

            if (detectedIdType && detectedIdType !== selectedIdType) {
                const expectedName = selectedIdType === 'driver-license' ? 'Driver\'s License' : 'National ID';
                const detectedName = detectedIdType === 'driver-license' ? 'Driver\'s License' : 'National ID';

                console.error('ID type mismatch - Expected:', expectedName, 'Detected:', detectedName);
                showIntelligentErrorModal('wrong-id-type', {
                    expected: expectedName,
                    detected: detectedName
                });
                return;
            }

            // ═══════════════════════════════════════════════════════════════
            // PROCEED WITH DATA EXTRACTION
            // ═══════════════════════════════════════════════════════════════

            // Parse ID based on type
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
            status.innerHTML = '<i class="fas fa-exclamation-triangle mr-2"></i>Error processing ID';
            showIntelligentErrorModal('no-data-extracted', { error: err.message });
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

    // ═══════════════════════════════════════════════════════════════
    // 🔍 IMAGE QUALITY ANALYSIS AND ENHANCEMENT
    // ═══════════════════════════════════════════════════════════════

    async function analyzeImageQuality(imageElement) {
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d', { willReadFrequently: true });

        // Downsample large images for faster processing
        const scale = 0.4;
        const width = Math.floor((imageElement.naturalWidth || imageElement.width) * scale);
        const height = Math.floor((imageElement.naturalHeight || imageElement.height) * scale);
        canvas.width = width;
        canvas.height = height;
        ctx.drawImage(imageElement, 0, 0, width, height);

        const { data } = ctx.getImageData(0, 0, width, height);
        const gray = new Float32Array(width * height);

        // Convert to grayscale (weighted average)
        for (let i = 0, j = 0; i < data.length; i += 4, j++) {
            gray[j] = 0.299 * data[i] + 0.587 * data[i + 1] + 0.114 * data[i + 2];
        }

        // Compute Laplacian variance
        let sum = 0, sumSq = 0, count = 0;
        for (let y = 1; y < height - 1; y++) {
            for (let x = 1; x < width - 1; x++) {
                const idx = y * width + x;
                const lap =
                    -4 * gray[idx] +
                    gray[idx - 1] +
                    gray[idx + 1] +
                    gray[idx - width] +
                    gray[idx + width];
                sum += lap;
                sumSq += lap * lap;
                count++;
            }
        }

        const mean = sum / count;
        const variance = (sumSq / count) - (mean * mean);
        const blurScore = Math.max(0, variance);

        console.log('🔎 Image Blur Score:', blurScore.toFixed(2));

        return {
            blurScore,
            isAcceptable: blurScore >= 15,
            needsEnhancement: blurScore < 25,
            quality:
                blurScore >= 25 ? 'good' :
                    blurScore >= 15 ? 'acceptable' : 'poor'
        };
    }

    async function enhanceImageQuality(imageElement) {
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d', { willReadFrequently: true });
        canvas.width = imageElement.naturalWidth || imageElement.width;
        canvas.height = imageElement.naturalHeight || imageElement.height;
        ctx.drawImage(imageElement, 0, 0, canvas.width, canvas.height);

        let imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        let data = imageData.data;

        // Apply mild sharpening
        data = applySharpening(data, canvas.width, canvas.height);

        // Measure brightness and enhance contrast adaptively
        const avgBrightness = getAverageBrightness(data);
        const factor = avgBrightness < 100 ? 1.6 : 1.3;
        data = enhanceContrast(data, factor);

        // Put the enhanced data back
        ctx.putImageData(new ImageData(data, canvas.width, canvas.height), 0, 0);

        // Return as Image object
        const enhanced = new Image();
        enhanced.src = canvas.toDataURL();
        return new Promise(res => (enhanced.onload = () => {
            console.log(`✨ Image Enhanced (Contrast ×${factor.toFixed(1)}, Avg Brightness: ${avgBrightness.toFixed(1)})`);
            res(enhanced);
        }));
    }

    function getAverageBrightness(data) {
        let total = 0, count = 0;
        for (let i = 0; i < data.length; i += 4) {
            total += 0.299 * data[i] + 0.587 * data[i + 1] + 0.114 * data[i + 2];
            count++;
        }
        return total / count;
    }

    function applySharpening(data, width, height) {
        const result = new Uint8ClampedArray(data.length);
        const kernel = [0, -1, 0, -1, 5, -1, 0, -1, 0];
        const getIdx = (x, y, c) => ((y * width + x) << 2) + c;

        for (let y = 1; y < height - 1; y++) {
            for (let x = 1; x < width - 1; x++) {
                const base = (y * width + x) << 2;
                for (let c = 0; c < 3; c++) {
                    let sum = 0, k = 0;
                    for (let ky = -1; ky <= 1; ky++) {
                        const yIdx = y + ky;
                        for (let kx = -1; kx <= 1; kx++, k++) {
                            sum += data[getIdx(x + kx, yIdx, c)] * kernel[k];
                        }
                    }
                    result[base + c] = Math.min(255, Math.max(0, sum));
                }
                result[base + 3] = data[base + 3];
            }
        }
        return result;
    }

    function enhanceContrast(data, factor = 1.3) {
        const result = new Uint8ClampedArray(data.length);
        const mid = 128;
        for (let i = 0; i < data.length; i += 4) {
            result[i] = Math.min(255, Math.max(0, (data[i] - mid) * factor + mid));
            result[i + 1] = Math.min(255, Math.max(0, (data[i + 1] - mid) * factor + mid));
            result[i + 2] = Math.min(255, Math.max(0, (data[i + 2] - mid) * factor + mid));
            result[i + 3] = data[i + 3];
        }
        return result;
    }

    // ═══════════════════════════════════════════════════════════════
    // 🧹 TEXT CLEANING AND PROCESSING FUNCTIONS
    // ═══════════════════════════════════════════════════════════════

    // Enhanced name cleaning with multi-pass filtering
    function cleanName(text) {
        if (!text) return "";

        // Pass 1: Aggressive sanitization
        let cleaned = sanitizeOCRText(text);

        // Pass 2: Remove numbers and special chars (keep only letters, spaces, hyphens, apostrophes, ñ)
        cleaned = cleaned.replace(/[^a-zA-ZñÑ\s\-'.]/g, " ");

        // Pass 3: Remove OCR artifacts
        cleaned = cleaned
            // Remove standalone single letters (except initials pattern like "A. ")
            .replace(/\b[a-z](?!\.\s)/gi, "")
            // Remove multiple consecutive special chars
            .replace(/[-'.]{2,}/g, "")
            // Normalize spaces
            .replace(/\s{2,}/g, " ")
            // Trim edges
            .replace(/^[\s\-'.]+|[\s\-'.]+$/g, "");

        // Pass 4: Handle compound Filipino surnames (de la, del, san, etc.)
        cleaned = cleaned
            .replace(/\bDe\s+La\s+/gi, "Dela ")
            .replace(/\bDe\s+Los\s+/gi, "Delos ")
            .replace(/\bDel\s+(?=\w)/gi, "Del")
            .replace(/\bSan\s+(?=\w)/gi, "San")
            .replace(/\bSanta\s+(?=\w)/gi, "Santa");

        // Validation: Skip if too short or contains too many special chars
        if (cleaned.length < 2) return "";
        if ((cleaned.match(/[-']/g) || []).length > 2) return ""; // Too many hyphens/apostrophes

        // Pass 5: Proper case formatting
        return cleaned
            .toLowerCase()
            .trim()
            .split(/\s+/)
            .filter(word => word.length > 0)
            .map(word => {
                // Handle special cases
                if (word === 'ii' || word === 'iii' || word === 'iv') {
                    return word.toUpperCase();
                }
                if (word === 'jr' || word === 'sr') {
                    return word.charAt(0).toUpperCase() + word.slice(1);
                }
                // Capitalize first letter
                return word.charAt(0).toUpperCase() + word.slice(1);
            })
            .join(' ');
    }

    // Extract suffix from name string (Jr, Sr, II, III, IV)
    function extractSuffixFromName(nameText) {
        const suffixPatterns = /\b(Jr\.?|Sr\.?|II|III|IV|2nd|3rd|4th)\b/gi;
        const match = nameText.match(suffixPatterns);
        if (match) {
            const suffix = match[0].replace(/\./g, '');
            return {
                suffix: suffix.toUpperCase() === 'JR' || suffix === 'Jr' ? 'Jr.' :
                        suffix.toUpperCase() === 'SR' || suffix === 'Sr' ? 'Sr.' :
                        suffix.toUpperCase(),
                cleanedName: nameText.replace(suffixPatterns, '').trim()
            };
        }
        return { suffix: '', cleanedName: nameText };
    }

    // Clean gender field with optimized matching
    function cleanGender(text) {
        if (!text) return "";

        // Sanitize and normalize
        const cleaned = sanitizeOCRText(text)
            .replace(/[^a-zA-Z]/g, "")
            .toUpperCase();

        // Skip if too short
        if (cleaned.length === 0) return "";

        // Optimized pattern matching using regex for better performance
        if (/^(M|MALE|LALAKI|MAN)$/i.test(cleaned) || cleaned.includes("MALE")) {
            return "Male";
        }
        if (/^(F|FEMALE|BABAE|WOMAN)$/i.test(cleaned) || cleaned.includes("FEMALE")) {
            return "Female";
        }

        return "";
    }

    // Clean barangay field with improved precision
    function cleanBarangay(text) {
        if (!text) return "";

        // Sanitize first
        let cleaned = sanitizeOCRText(text);

        // Keep only valid barangay characters
        cleaned = cleaned.replace(/[^a-zA-Z0-9\s\-().ñÑ]/g, "");

        // Remove OCR artifacts
        cleaned = cleaned
            .replace(/\b[a-z0-9]\b/gi, "")     // Remove standalone single chars
            .replace(/\s{2,}/g, " ")           // Normalize spaces
            .replace(/^[\s\-().]+|[\s\-().]+$/g, ""); // Trim special chars

        // Skip if too short
        if (cleaned.length < 3) return "";

        // Proper case formatting
        return cleaned
            .toLowerCase()
            .split(/\s+/)
            .filter(word => word.length > 0)
            .map(word => word.charAt(0).toUpperCase() + word.slice(1))
            .join(' ');
    }

    // Clean civil status field with optimized matching
    function cleanCivilStatus(text) {
        if (!text) return "";

        // Sanitize and normalize
        const cleaned = sanitizeOCRText(text)
            .replace(/[^a-zA-Z]/g, "")
            .toLowerCase();

        // Skip if too short
        if (cleaned.length < 4) return "";

        // Optimized pattern matching with regex
        if (/^(single|solo|unmarried)$/.test(cleaned) || cleaned.includes("single")) {
            return "Single";
        }
        if (/^(married|kasal)$/.test(cleaned) || cleaned.includes("married")) {
            return "Married";
        }
        if (/^(widow|widowed|balo)$/.test(cleaned) || cleaned.includes("widow")) {
            return "Widowed";
        }
        if (/^(separated|hiwalay)$/.test(cleaned) || cleaned.includes("separated")) {
            return "Separated";
        }
        if (/^(divorced|diborsyado)$/.test(cleaned) || cleaned.includes("divorc")) {
            return "Divorced";
        }

        return "";
    }

    // General text cleaner for addresses and misc fields
    function cleanText(text) {
        if (!text) return "";

        return sanitizeOCRText(text)
            .replace(/[^a-zA-Z0-9\s\-,.]/g, '')
            .replace(/\s{2,}/g, ' ')
            .trim();
    }

    // Alias for compatibility
    function cleanSex(text) {
        return cleanGender(text);
    }

    // ═══════════════════════════════════════════════════════════════
    // 📅 DATE AND LOCATION EXTRACTION FUNCTIONS
    // ═══════════════════════════════════════════════════════════════

    // Optimized birthdate extraction with cached month mapping
    const MONTH_MAP = {
        'january': '01', 'jan': '01', 'february': '02', 'feb': '02', 'march': '03', 'mar': '03',
        'april': '04', 'apr': '04', 'may': '05', 'june': '06', 'jun': '06',
        'july': '07', 'jul': '07', 'august': '08', 'aug': '08', 'september': '09', 'sep': '09', 'sept': '09',
        'october': '10', 'oct': '10', 'november': '11', 'nov': '11', 'december': '12', 'dec': '12'
    };

    function extractBirthdateFromText(text) {
        if (!text || text.length < 6) return "";

        // Optimized date patterns (most common first for faster matching)
        const patterns = [
            { regex: /\b(\d{4})[\/-](\d{1,2})[\/-](\d{1,2})\b/, handler: (m) => `${m[1]}-${m[2].padStart(2, '0')}-${m[3].padStart(2, '0')}` },
            { regex: /\b(\d{1,2})[\/-](\d{1,2})[\/-](\d{4})\b/, handler: (m) => `${m[3]}-${m[2].padStart(2, '0')}-${m[1].padStart(2, '0')}` },
            { regex: /\b(\d{1,2})\s+(Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:t(?:ember)?)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\s+(\d{4})\b/i,
              handler: (m) => {
                  const monthNum = MONTH_MAP[m[2].toLowerCase()];
                  return monthNum ? `${m[3]}-${monthNum}-${m[1].padStart(2, '0')}` : "";
              }
            },
            { regex: /\b(Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:t(?:ember)?)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\s+(\d{1,2}),?\s+(\d{4})\b/i,
              handler: (m) => {
                  const monthNum = MONTH_MAP[m[1].toLowerCase()];
                  return monthNum ? `${m[3]}-${monthNum}-${m[2].padStart(2, '0')}` : "";
              }
            }
        ];

        for (const { regex, handler } of patterns) {
            const match = text.match(regex);
            if (match) {
                const result = handler(match);
                if (result) return result;
            }
        }

        return "";
    }

    // Optimized search for birthdate in all lines
    function findBirthdateInText(lines) {
        for (const line of lines) {
            const birthdate = extractBirthdateFromText(line);
            if (birthdate) return birthdate;
        }
        return "";
    }

    // Optimized date pattern checker
    function containsDate(text) {
        if (!text || text.length < 6) return false;

        // Simplified combined pattern for better performance
        return /\b(\d{4}[\/-]\d{1,2}[\/-]\d{1,2}|\d{1,2}[\/-]\d{1,2}[\/-]\d{4}|\d{1,2}\s+[A-Za-z]+\s+\d{4}|[A-Za-z]+\s+\d{1,2},?\s+\d{4})\b/.test(text);
    }

    // Advanced city extraction with pattern matching
    function extractCity(lines) {
        console.log("Extracting city from lines...");

        // Priority 1: Look for explicit city patterns
        const cityPatterns = [
            /DAVAO\s+CITY/i,
            /CITY\s+OF\s+DAVAO/i,
            /DAVAO\s+(?:ORIENTAL|DEL\s+SUR)?/i,
            /CEBU\s+CITY/i,
            /MANILA/i,
            /QUEZON\s+CITY/i,
            /MAKATI\s+CITY/i,
            /TAGUM\s+CITY/i,
            /DIGOS\s+CITY/i,
            /PANABO\s+CITY/i,
            /CAGAYAN\s+DE\s+ORO/i
        ];

        for (const line of lines) {
            for (const pattern of cityPatterns) {
                if (pattern.test(line)) {
                    const match = line.match(pattern);
                    if (match) {
                        console.log("City pattern matched:", match[0]);
                        // Normalize to proper format
                        if (/DAVAO/i.test(match[0])) return "Davao City";
                        if (/CEBU/i.test(match[0])) return "Cebu City";
                        if (/MANILA/i.test(match[0])) return "Manila";
                        if (/QUEZON/i.test(match[0])) return "Quezon City";
                        if (/MAKATI/i.test(match[0])) return "Makati City";
                        if (/TAGUM/i.test(match[0])) return "Tagum City";
                        if (/DIGOS/i.test(match[0])) return "Digos City";
                        if (/PANABO/i.test(match[0])) return "Panabo City";
                        if (/CAGAYAN/i.test(match[0])) return "Cagayan de Oro";
                        return cleanText(match[0]);
                    }
                }
            }
        }

        // Priority 2: Look for lines containing common city names
        const cityKeywords = ['DAVAO', 'CEBU', 'MANILA', 'QUEZON', 'MAKATI', 'TAGUM', 'DIGOS', 'PANABO'];
        for (const line of lines) {
            const upper = line.toUpperCase();
            for (const keyword of cityKeywords) {
                if (upper.includes(keyword)) {
                    console.log("City keyword found in line:", line);
                    if (keyword === 'DAVAO') return "Davao City";
                    return keyword.charAt(0) + keyword.slice(1).toLowerCase() + " City";
                }
            }
        }

        return "Not detected";
    }

    // Extract Barangay from OCR text with improved matching
    function extractBarangayFromText(lines) {
        console.log('=== EXTRACTING BARANGAY ===');
        console.log('OCR Lines:', lines);

        // Common barangay indicators
        const barangayKeywords = ['BRGY', 'BARANGAY', 'BRGAY', 'BGY', 'BRGY.', 'BARANGGAY'];

        // COMPREHENSIVE DAVAO CITY BARANGAYS LIST
        const davaoBarangays = [
            'Acacia', 'Agdao', 'Alambre', 'Alejandro Navarro', 'Alfonso Angliongto Sr.',
            'Angalan', 'Baguio Proper', 'Baliok', 'Bangkas Heights', 'Baracatan',
            'Bato', 'Bayabas', 'Biao Escuela', 'Biao Guianga', 'Binugao',
            'Bucana', 'Buhangin Proper', 'Cabantian', 'Cadalian', 'Calinan Proper',
            'Callawa', 'Camansi', 'Carmen', 'Catalunan Grande', 'Catalunan Pequeño',
            'Catigan', 'Cawayan', 'Centro (San Juan)', 'Colosas', 'Communal',
            'Crossing Bayabas', 'Dacudao', 'Dalagdag', 'Daliao', 'Dalican',
            'Datu Salumay', 'Dominga', 'Eden', 'Fatima (Benowang)', 'Gatungan',
            'Gov. Paciano Bangoy', 'Gov. Vicente Duterte', 'Gumalang', 'Gumitan',
            'Indangan', 'Kap. Tomas Monteverde Sr.', 'Kilate', 'Lamanan',
            'Lampianao', 'Langub', 'Lapu-lapu', 'Leon Garcia Sr.', 'Los Amigos',
            'Lubogan', 'Lumiad', 'Ma-a', 'Mabuhay', 'Madapo', 'Magtuod',
            'Mahayag', 'Malabog', 'Malagos', 'Malamba', 'Malandog', 'Mampising',
            'Manambulan', 'Mandug', 'Manuel Guianga', 'Mapula', 'Marapangi',
            'Marilog Proper', 'Matina Aplaya', 'Matina Crossing', 'Matina Pangi',
            'Mintal', 'Mudiang', 'Mulig', 'New Carmen', 'New Valencia', 'Pampanga',
            'Panacan', 'Pandaitan', 'Panorama', 'Paquibato Proper', 'Paradise Embak',
            'Rafael Castillo', 'Salapawan', 'Salaysay', 'Saloy', 'San Antonio',
            'San Isidro', 'Sasa', 'Sirib', 'Suawan', 'Tacunan', 'Tagakpan',
            'Tagluno', 'Tagurano', 'Talomo Proper', 'Talomo River', 'Tamurayan',
            'Tibungco', 'Tigatto', 'Tungkalan', 'Ubalde', 'Ugac', 'Ula',
            'Vicente Hizon Sr.', 'Waan', 'Wangan', 'Wilfredo Aquino', 'Wines'
        ];

        // Create normalized versions for better matching
        const normalizedBarangays = davaoBarangays.map(b => ({
            original: b,
            normalized: b.toUpperCase().replace(/[^A-Z0-9]/g, ''),
            upperCase: b.toUpperCase(),
            keywords: b.toUpperCase().split(/\s+/).filter(w => w.length >= 4) // significant words only
        }));

        // STEP 1: Look for barangay keywords and extract adjacent text
        for (let i = 0; i < lines.length; i++) {
            const lineUpper = lines[i].toUpperCase();

            for (const keyword of barangayKeywords) {
                if (lineUpper.includes(keyword)) {
                    // Extract text after the keyword
                    let potentialBarangay = cleanBarangay(lineUpper.replace(keyword, '').replace(/\./g, '').trim());
                    let normalizedPotential = potentialBarangay.toUpperCase().replace(/[^A-Z0-9]/g, '');

                    // Try exact and fuzzy matching
                    for (const item of normalizedBarangays) {
                        // Exact normalized match
                        if (normalizedPotential === item.normalized) {
                            console.log('✅ Barangay found (exact match):', item.original);
                            return item.original;
                        }

                        // Contains match (must be significant length)
                        if (normalizedPotential.length >= 5 && item.normalized.length >= 5) {
                            if (normalizedPotential.includes(item.normalized) || item.normalized.includes(normalizedPotential)) {
                                return item.original;
                            }
                        }

                        // Keyword matching for compound names
                        for (const keyword of item.keywords) {
                            if (potentialBarangay.toUpperCase().includes(keyword)) {
                                return item.original;
                            }
                        }
                    }

                    // Check next line if current line doesn't match
                    if (i + 1 < lines.length) {
                        const nextLine = cleanBarangay(lines[i + 1]).replace(/\./g, '');
                        const normalizedNext = nextLine.toUpperCase().replace(/[^A-Z0-9]/g, '');

                        for (const item of normalizedBarangays) {
                            if (normalizedNext === item.normalized) {
                                return item.original;
                            }
                            if (normalizedNext.length >= 5 && (normalizedNext.includes(item.normalized) || item.normalized.includes(normalizedNext))) {
                                return item.original;
                            }
                        }
                    }
                }
            }
        }

        // STEP 2: Scan all lines for direct barangay name matches
        for (const line of lines) {
            const cleanedLine = cleanBarangay(line).replace(/\./g, '');
            const normalizedLine = cleanedLine.toUpperCase().replace(/[^A-Z0-9]/g, '');

            if (normalizedLine.length < 3) continue; // Skip very short lines

            for (const item of normalizedBarangays) {
                // Exact match
                if (normalizedLine === item.normalized) {
                    return item.original;
                }

                // Contains match with length check to avoid false positives
                if (item.normalized.length >= 6 && normalizedLine.includes(item.normalized)) {
                    return item.original;
                }

                // Keyword-based matching for compound names
                const lineWords = cleanedLine.toUpperCase().split(/\s+/);
                for (const keyword of item.keywords) {
                    if (lineWords.includes(keyword)) {
                        return item.original;
                    }
                }
            }
        }

        // STEP 3: Common barangay partial matching (high confidence only)
        const commonBarangays = [
            { search: 'TIGATTO', minLength: 6 },
            { search: 'MATINA', minLength: 6 },
            { search: 'AGDAO', minLength: 5 },
            { search: 'BUHANGIN', minLength: 7 },
            { search: 'CATALUNAN', minLength: 8 },
            { search: 'TALOMO', minLength: 6 },
            { search: 'PANACAN', minLength: 6 },
            { search: 'MANDUG', minLength: 6 },
            { search: 'MINTAL', minLength: 6 },
            { search: 'SASA', minLength: 4 },
            { search: 'CABANTIAN', minLength: 8 },
            { search: 'INDANGAN', minLength: 7 }
        ];

        for (const line of lines) {
            const normalizedLine = line.toUpperCase().replace(/[^A-Z]/g, '');

            if (normalizedLine.length < 4) continue;

            for (const common of commonBarangays) {
                if (normalizedLine.includes(common.search) && normalizedLine.length >= common.minLength) {
                    // Find the matching barangay
                    for (const item of normalizedBarangays) {
                        if (item.normalized.includes(common.search)) {
                            return item.original;
                        }
                    }
                }
            }
        }

        return "";
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
    function showDavaoVerificationResult(isDavaoCity, detectedCity, detectedBarangay, idType) {
        if (!davaoVerification || !davaoResultIcon || !davaoResultTitle || !davaoResultMessage || !davaoStatusBadge) return;

        // Show for both driver's license and national ID
        davaoVerification.classList.remove('hidden');

        if (isDavaoCity) {
            // Valid Davao City ID
            davaoVerification.className = 'p-6 rounded-xl border-l-4 bg-green-50 border-green-200 slide-down';
            davaoResultIcon.className = 'fas fa-check-circle text-2xl mt-1 text-green-500';
            davaoResultTitle.textContent = idType === 'driver-license' ? 'Valid Davao City License' : 'Valid Davao City National ID';

            let message = idType === 'driver-license'
                ? 'Your driver\'s license has been verified as issued in Davao City.'
                : 'Your National ID has been verified as registered in Davao City.';

            if (detectedBarangay) {
                message += ` Barangay detected: ${detectedBarangay}`;
            }

            davaoResultMessage.textContent = message;
            davaoStatusBadge.className = 'px-3 py-1 rounded-full text-sm font-semibold bg-green-500 text-white';
            davaoStatusBadge.textContent = 'Valid';

            // Enable form fields for valid ID
            enableFormFields();
        } else {
            // Invalid (not Davao City) ID
            davaoVerification.className = 'p-6 rounded-xl border-l-4 bg-red-50 border-red-200 slide-down';
            davaoResultIcon.className = 'fas fa-times-circle text-2xl mt-1 text-red-500';
            davaoResultTitle.textContent = 'Not a Davao City Resident';
            davaoResultMessage.textContent = 'Lingap Online is only for Davao City Residents. The city detected from your ID is: ' +
                (detectedCity || 'Unknown') + '. Please ensure you are uploading a valid Davao City ID.';
            davaoStatusBadge.className = 'px-3 py-1 rounded-full text-sm font-semibold bg-red-500 text-white';
            davaoStatusBadge.textContent = 'Invalid';

            // Disable form fields and clear uploaded files for invalid ID
            disableFormFields();
            clearUploadedFiles();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // ⚠️ ERROR HANDLING AND UTILITY FUNCTIONS
    // ═══════════════════════════════════════════════════════════════

    function showIntelligentErrorModal(errorType, details = {}) {
        const modal = document.createElement('div');
        modal.className = 'fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50';
        modal.id = 'intelligent-error-modal';

        const errorTemplates = {
            'too-blurry': {
                icon: '<i class="fas fa-eye-slash text-5xl text-red-500"></i>',
                title: 'ID Image Too Blurry',
                message: `
                The uploaded ID image is too blurry.<br><br>
                <strong>Quality Score:</strong> ${details.blurScore?.toFixed(1) || 'Low'}<br>
                <strong>Tips:</strong><br>
                • Ensure good lighting<br>
                • Keep camera steady<br>
                • Clean the ID surface
            `,
                actionText: 'Try Again with Clearer Photo'
            },
            'no-data-extracted': {
                icon: '<i class="fas fa-exclamation-triangle text-5xl text-yellow-500"></i>',
                title: 'Unable to Read ID',
                message: `
                We couldn't extract readable information.<br><br>
                • Image too dark or overexposed<br>
                • ID partially covered<br>
                • Resolution too low
            `,
                actionText: 'Upload Better Quality Photo'
            },
            'low-confidence': {
                icon: '<i class="fas fa-question-circle text-5xl text-blue-500"></i>',
                title: 'Low OCR Confidence',
                message: `
                The text extraction confidence is low.<br>
                <strong>Confidence:</strong> ${(details.confidence * 100 || 0).toFixed(0)}%
            `,
                actionText: 'Continue Anyway'
            },
            'wrong-id-type': {
                icon: '<i class="fas fa-id-card text-5xl text-orange-500"></i>',
                title: 'Wrong Document Type',
                message: `
                You selected ${details.expected} but uploaded a ${details.detected}.<br><br>
                Please upload the correct document type for verification.
            `,
                actionText: 'Upload Correct Document'
            }
        };

        const { icon, title, message, actionText } =
            errorTemplates[errorType] || {
                icon: '<i class="fas fa-times-circle text-5xl text-red-500"></i>',
                title: 'Verification Error',
                message: 'An unexpected error occurred.',
                actionText: 'Try Again'
            };

        modal.innerHTML = `
        <div class="bg-white rounded-2xl shadow-2xl max-w-md w-full mx-4 overflow-hidden scale-in">
            <div class="p-8 text-center">
                <div class="mb-4">${icon}</div>
                <h3 class="text-2xl font-bold text-gray-800 mb-4">${title}</h3>
                <p class="text-gray-600 mb-6 leading-relaxed">${message}</p>
                <div class="flex gap-3">
                    <button onclick="closeIntelligentErrorModal()"
                        class="flex-1 px-6 py-3 bg-gray-200 text-gray-700 rounded-xl font-semibold hover:bg-gray-300 transition-all">
                        Cancel
                    </button>
                    <button onclick="closeIntelligentErrorModal(); retryUpload()"
                        class="flex-1 px-6 py-3 bg-crimson-500 text-white rounded-xl font-semibold hover:bg-crimson-600 transition-all">
                        ${actionText}
                    </button>
                </div>
            </div>
        </div>
    `;
        document.body.appendChild(modal);
    }

    function closeIntelligentErrorModal() {
        const modal = document.getElementById('intelligent-error-modal');
        if (modal) modal.remove();
    }

    function retryUpload() {
        if (fileFront) fileFront.value = '';
        if (fileBack) fileBack.value = '';
        if (imagePreview) imagePreview.classList.add('hidden');
        if (imagePreviewBack) imagePreviewBack.classList.add('hidden');
        if (uploadFront) uploadFront.classList.remove('active');
        if (uploadBack) uploadBack.classList.remove('active');
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
        if (fileFront) fileFront.value = '';
        if (fileBack) fileBack.value = '';

        // Hide preview images
        if (imagePreview) imagePreview.classList.add('hidden');
        if (imagePreviewBack) imagePreviewBack.classList.add('hidden');

        // Remove active class from upload areas
        if (uploadFront) uploadFront.classList.remove('active');
        if (uploadBack) uploadBack.classList.remove('active');

        // Clear any extracted data from form fields
        const idnumberField = document.getElementById('idnumber');
        const firstnameField = document.getElementById('firstname');
        const middlenameField = document.getElementById('middlename');
        const lastnameField = document.getElementById('lastname');
        const birthdateField = document.getElementById('birthdate');
        const sexField = document.getElementById('sex');

        if (idnumberField) idnumberField.value = '';
        if (firstnameField) firstnameField.value = '';
        if (middlenameField) middlenameField.value = '';
        if (lastnameField) lastnameField.value = '';
        if (birthdateField) birthdateField.value = '';
        if (sexField) sexField.value = '';
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

    // ═══════════════════════════════════════════════════════════════
    // 🇵🇭 FIXED PHILSYS BACK PARSING
    // ═══════════════════════════════════════════════════════════════

    function parsePhilSysBack(text) {
        console.log("=== PHILSYS BACK PARSING START ===");

        // Validate input
        if (!text || typeof text !== 'string') {
            console.warn("⚠️ Invalid text provided to parsePhilSysBack:", text);
            return;
        }

        try {
            const lines = preprocessOCRLines(text);
            
            // Check if we have valid lines
            if (!lines || !Array.isArray(lines) || lines.length === 0) {
                console.warn("⚠️ No valid lines extracted from OCR text for PhilSys back");
                return;
            }

            const linesUpper = lines.map(line => line.toUpperCase());

            let sex = "";
            let civilStatus = "";

            // ─────────────────────────────────────────────────────────────
            // STRATEGY 1: Gender Extraction with Filipino/English Labels
            // ─────────────────────────────────────────────────────────────
            const genderPatterns = /\b(KASARIAN|SEX|GENDER|KASARI)\b/i;

            for (let i = 0; i < linesUpper.length; i++) {
                const lineUpper = linesUpper[i];
                const currentLine = lines[i];
                const nextLine = lines[i + 1] || "";

                if (genderPatterns.test(lineUpper)) {
                    // Try same line first (label: value)
                    const sameLine = currentLine.replace(genderPatterns, '').trim();
                    sex = cleanGender(sameLine || nextLine);
                    if (sex) {
                        console.log("Gender found:", sex);
                        break;
                    }
                }
            }

            // Fallback: scan all lines for gender keywords
            if (!sex) {
                for (const line of lines) {
                    sex = cleanGender(line);
                    if (sex) {
                        console.log("Gender found (fallback):", sex);
                        break;
                    }
                }
            }

            // ─────────────────────────────────────────────────────────────
            // STRATEGY 2: Civil Status Extraction
            // ─────────────────────────────────────────────────────────────
            const civilStatusPatterns = /\b(CIVIL\s*STATUS|KATAYUAN|MARITAL\s*STATUS|KASAL)\b/i;

            for (let i = 0; i < linesUpper.length; i++) {
                const lineUpper = linesUpper[i];
                const currentLine = lines[i];
                const nextLine = lines[i + 1] || "";

                if (civilStatusPatterns.test(lineUpper)) {
                    // Try same line first
                    const sameLine = currentLine.replace(civilStatusPatterns, '').trim();
                    civilStatus = cleanCivilStatus(sameLine || nextLine);
                    if (civilStatus) {
                        console.log("Civil status found:", civilStatus);
                        break;
                    }
                }
            }

            // Fallback: scan all lines for civil status keywords
            if (!civilStatus) {
                for (const line of lines) {
                    civilStatus = cleanCivilStatus(line);
                    if (civilStatus) {
                        console.log("Civil status found (fallback):", civilStatus);
                        break;
                    }
                }
            }

            // Update form fields
            let extractedFields = [];

            if (sex) {
                const sexField = document.getElementById('sex');
                if (sexField) {
                    sexField.value = sex;
                    extractedFields.push("Gender");
                }
            }

            if (civilStatus) {
                const civilStatusSelect = document.querySelector('select[name="CivilStatus"]');
                if (civilStatusSelect) {
                    civilStatusSelect.value = civilStatus;
                    extractedFields.push("Civil Status");
                }
            }

            if (extractedFields.length > 0 && resultBox) {
                resultBox.classList.remove('hidden');
                resultBox.textContent = extractedFields.join(" and ") + " extracted from ID back. Please verify.";
            }

            console.log("=== PHILSYS BACK PARSING END ===");

        } catch (error) {
            console.error('❌ Error in parsePhilSysBack:', error);
        }
    }

    // Make functions globally available for modal buttons
    window.closeIntelligentErrorModal = closeIntelligentErrorModal;
    window.retryUpload = retryUpload;

    console.log('=== VERIFICATION OCR SYSTEM INITIALIZED ===');
});