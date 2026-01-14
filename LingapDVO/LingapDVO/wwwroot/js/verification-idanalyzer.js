// ACCOUNT VERIFICATION - ID ANALYZER API v2 INTEGRATION
// ============================================================================
// This script performs ID verification using ID Analyzer API v2
// Base URL: https://api2.idanalyzer.com
//
// Main Endpoint:
// - POST /api/IdAnalyzer/scan - Standard ID Scan
//   * Performs document data extraction using AI-powered recognition
//   * Performs face matching (if 'face' parameter included)
//   * Returns document data + face verification results in single response
//
// NEW VERIFICATION FLOW (COLLECT ALL IMAGES FIRST, THEN SUBMIT):
// ════════════════════════════════════════════════════════════════════════════
// STEP 1: COLLECT ALL REQUIRED IMAGES (NO API CALLS YET)
//    - User uploads Front ID image → Stored locally (base64)
//    - User uploads Back ID image → Stored locally (base64)
//    - User captures Selfie/Face image → Stored locally (base64)
//    - "Verify & Submit" button becomes enabled when all images are ready
//
// STEP 2: SUBMIT ALL IMAGES TO API (SINGLE API CALL)
//    - User clicks "Verify & Submit" button
//    - System calls POST /api/IdAnalyzer/scan with ALL images:
//        * document: Front ID (base64)
//        * documentBack: Back ID (base64)
//        * face: Selfie image (base64)
//        * profile: KYC Profile ID
//    - API performs:
//        1. Document data extraction (OCR)
//        2. Face matching (selfie vs ID photo)
//    - Returns combined results:
//        * Extracted document data (name, DOB, ID number, etc.)
//        * Face verification result (match/no match, similarity score)
//
// STEP 3: PROCESS RESULTS
//    - Populate form fields with extracted data
//    - Show face verification result
//    - Enable/disable form submission based on verification status
//
// ════════════════════════════════════════════════════════════════════════════
// Reference: https://developer.idanalyzer.com/reference/post-scan
// Reference: https://developer.idanalyzer.com/reference/post-face-2
// ============================================================================

// ID Analyzer API Configuration
const ID_ANALYZER_CONFIG = {
    API_URL: '/api/IdAnalyzer',
    SCAN_ENDPOINT: '/api/IdAnalyzer/scan',
    FACE_ENDPOINT: '/api/IdAnalyzer/face',
    ACCOUNT_ENDPOINT: '/api/IdAnalyzer/account',
    TRANSACTION_ENDPOINT: '/api/IdAnalyzer/transaction'  // GET /api/IdAnalyzer/transaction/{transactionId}
};

// ════════════════════════════════════════════════════════════════════════════
// INTERACTIVE PROGRESS TRACKING SYSTEM
// ════════════════════════════════════════════════════════════════════════════
const ProgressTracker = {
    currentStep: 0,
    steps: [
        { name: 'Preparing your files', percent: 5, duration: 300 },
        { name: 'Uploading front of ID', percent: 15, duration: 800 },
        { name: 'Uploading back of ID', percent: 30, duration: 800 },
        { name: 'Uploading your photo', percent: 45, duration: 600 },
        { name: 'Checking your ID', percent: 60, duration: 1200 },
        { name: 'Reading your information', percent: 75, duration: 1000 },
        { name: 'Matching your photo', percent: 85, duration: 800 },
        { name: 'Almost done', percent: 95, duration: 500 },
        { name: 'Complete', percent: 100, duration: 200 }
    ],

    reset() {
        this.currentStep = 0;
    },

    updateProgress(stepIndex, customMessage = null) {
        const progressBar = document.getElementById('upload-progress');
        const statusDiv = document.getElementById('upload-status');

        if (stepIndex >= this.steps.length) stepIndex = this.steps.length - 1;

        const step = this.steps[stepIndex];
        this.currentStep = stepIndex;

        if (progressBar) {
            // Smooth transition
            progressBar.style.transition = `width ${step.duration}ms ease-out`;
            progressBar.style.width = step.percent + '%';
        }

        if (statusDiv) {
            const message = customMessage || step.name;
            statusDiv.innerHTML = `<i class="fas fa-spinner fa-spin mr-2 text-blue-500"></i>${message}`;
        }

        console.log(`📊 Progress: ${step.percent}% - ${step.name}`);
    },

    async advanceStep(customMessage = null) {
        return new Promise((resolve) => {
            const step = this.steps[this.currentStep];
            this.updateProgress(this.currentStep, customMessage);

            setTimeout(() => {
                this.currentStep++;
                resolve();
            }, step.duration);
        });
    },

    async jumpToStep(stepIndex, customMessage = null) {
        if (stepIndex >= this.steps.length) stepIndex = this.steps.length - 1;
        this.updateProgress(stepIndex, customMessage);
        const step = this.steps[stepIndex];

        return new Promise((resolve) => {
            setTimeout(resolve, step.duration);
        });
    },

    setComplete(message = 'All done!') {
        this.updateProgress(this.steps.length - 1, `<i class="fas fa-check-circle mr-2 text-green-600"></i>${message}`);
    },

    setError(message = 'Something went wrong') {
        const statusDiv = document.getElementById('upload-status');
        if (statusDiv) {
            statusDiv.innerHTML = `<i class="fas fa-exclamation-circle mr-2 text-red-600"></i>${message}`;
        }
    }
};

// Make ProgressTracker globally accessible
window.ProgressTracker = ProgressTracker;

// ════════════════════════════════════════════════════════════════════════════
// FILE TO BASE64 CONVERTER
// ════════════════════════════════════════════════════════════════════════════
// Converts a File object to Base64 string using FileReader API
// Returns a Promise that resolves with Base64 string (includes data URI prefix)
// ════════════════════════════════════════════════════════════════════════════
async function convertFileToBase64(file) {
    return new Promise((resolve, reject) => {
        if (!file || !(file instanceof File)) {
            reject(new Error('Invalid file object'));
            return;
        }

        const reader = new FileReader();

        reader.onload = () => {
            // reader.result contains data URI: "data:image/jpeg;base64,..."
            resolve(reader.result);
        };

        reader.onerror = () => {
            reject(new Error('Failed to read file'));
        };

        // Read file as Data URL (Base64)
        reader.readAsDataURL(file);
    });
}

// ════════════════════════════════════════════════════════════════════════════
// VERIFICATION STATE MANAGEMENT
// ════════════════════════════════════════════════════════════════════════════
// Stores all collected images before sending to API
// ════════════════════════════════════════════════════════════════════════════
const VerificationState = {
    // Store Base64 encoded images - sent directly to API
    // Files are converted to Base64 immediately upon upload/capture
    // Only saved to disk AFTER API returns "accept" decision
    frontIdImage: null,       // Front ID as Base64 string
    backIdImage: null,        // Back ID as Base64 string
    selfieImage: null,        // Selfie as Base64 string (captured from camera)

    // Status flags
    frontIdUploaded: false,
    backIdUploaded: false,
    selfieUploaded: false,
    verificationInProgress: false,
    verificationComplete: false,

    // Results
    scanResult: null,
    faceMatchResult: null,
    extractedData: null,

    // Store front ID image as Base64
    setFrontId(base64Image) {
        this.frontIdImage = base64Image;
        this.frontIdUploaded = !!base64Image;
        this.updateVerifyButtonState();
    },

    // Store back ID image as Base64
    setBackId(base64Image) {
        this.backIdImage = base64Image;
        this.backIdUploaded = !!base64Image;
        this.updateVerifyButtonState();
    },

    // Store selfie image as Base64
    setSelfie(base64Image) {
        this.selfieImage = base64Image;
        this.selfieUploaded = !!base64Image;
        this.updateVerifyButtonState();
    },
    
    // Check if all required images are uploaded
    isReadyForVerification() {
        // Front ID, Back ID, and Selfie are ALL REQUIRED
        return this.frontIdUploaded && this.backIdUploaded && this.selfieUploaded;
    },
    
    // Check if all images including back are uploaded
    hasAllImages() {
        return this.frontIdUploaded && this.backIdUploaded && this.selfieUploaded;
    },
    
    // Update the verify button state based on collected images
    updateVerifyButtonState() {
        const verifyButton = document.getElementById('verify-submit-btn');
        const statusContainer = document.getElementById('upload-status-container');
        
        if (verifyButton) {
            if (this.isReadyForVerification()) {
                verifyButton.disabled = false;
                verifyButton.classList.remove('opacity-50', 'cursor-not-allowed', 'bg-gray-400');
                verifyButton.classList.add('bg-red-600', 'hover:bg-red-700', 'cursor-pointer');
            } else {
                verifyButton.disabled = true;
                verifyButton.classList.add('opacity-50', 'cursor-not-allowed', 'bg-gray-400');
                verifyButton.classList.remove('bg-red-600', 'hover:bg-red-700', 'cursor-pointer');
            }
        }
        
        // Update status display
        this.updateStatusDisplay();
    },
    
    // Update the status display showing which images are uploaded
    updateStatusDisplay() {
        const statusContainer = document.getElementById('upload-status-container');
        if (!statusContainer) return;
        
        const frontStatus = this.frontIdUploaded ? '✅' : '⏳';
        const backStatus = this.backIdUploaded ? '✅' : '⏳';
        const selfieStatus = this.selfieUploaded ? '✅' : '⏳';
        
        statusContainer.innerHTML = `
            <div class="flex flex-wrap gap-4 justify-center text-sm">
                <span class="px-3 py-1 rounded-full ${this.frontIdUploaded ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}">
                    ${frontStatus} Front ID
                </span>
                <span class="px-3 py-1 rounded-full ${this.backIdUploaded ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}">
                    ${backStatus} Back ID
                </span>
                <span class="px-3 py-1 rounded-full ${this.selfieUploaded ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}">
                    ${selfieStatus} Selfie
                </span>
            </div>
        `;
    },
    
    // Reset all state
    reset() {
        this.frontIdImage = null;
        this.backIdImage = null;
        this.selfieImage = null;
        this.frontIdUploaded = false;
        this.backIdUploaded = false;
        this.selfieUploaded = false;
        this.verificationInProgress = false;
        this.verificationComplete = false;
        this.scanResult = null;
        this.faceMatchResult = null;
        this.extractedData = null;
        this.updateVerifyButtonState();
    },

    // Get summary for logging
    getSummary() {
        return {
            frontId: this.frontIdUploaded,
            frontIdLength: this.frontIdImage?.length || 0,
            backId: this.backIdUploaded,
            backIdLength: this.backIdImage?.length || 0,
            selfie: this.selfieUploaded,
            selfieLength: this.selfieImage?.length || 0,
            ready: this.isReadyForVerification(),
            complete: this.hasAllImages()
        };
    }
};

// Make VerificationState globally accessible
window.VerificationState = VerificationState;

// ═══════════════════════════════════════════════════════════════════════════════
// DAVAO CITY BARANGAYS LIST - For Address Validation
// ═══════════════════════════════════════════════════════════════════════════════
const DAVAO_CITY_BARANGAYS = [
    "Acacia", "Agdao", "Alambre", "Alejandro Navarro", "Alfonso Angliongto Sr.",
    "Angalan", "Baguio Proper", "Baliok", "Bangkas Heights", "Baracatan",
    "Bato", "Bayabas", "Biao Escuela", "Biao Guianga", "Binugao",
    "Bucana", "Buhangin", "Buhangin (Pob.)", "Buhangin Proper", "Cabantian",
    "Cadalian", "Calinan Proper", "Callawa", "Camansi", "Carmen",
    "Catalunan Grande", "Catalunan Pequeño", "Catigan", "Cawayan", "Centro (San Juan)",
    "Colosas", "Communal", "Crossing Bayabas", "Dacudao", "Dalagdag",
    "Daliao", "Dalican", "Datu Salumay", "Dominga", "Eden",
    "Fatima (Benowang)", "Gatungan", "Gov. Paciano Bangoy", "Gov. Vicente Duterte", "Gumalang",
    "Gumitan", "Indangan", "Kap. Tomas Monteverde Sr.", "Kilate", "Lamanan",
    "Lampianao", "Langub", "Lapu-lapu", "Leon Garcia Sr.", "Los Amigos",
    "Lubogan", "Lumiad", "Ma-a", "Mabuhay", "Madapo",
    "Magtuod", "Mahayag", "Malabog", "Malagos", "Malamba",
    "Malandog", "Mampising", "Manambulan", "Mandug", "Manuel Guianga",
    "Mapula", "Marapangi", "Marilog Proper", "Matina Aplaya", "Matina Crossing",
    "Matina Pangi", "Mintal", "Mudiang", "Mulig", "New Carmen",
    "New Valencia", "Pampanga", "Panacan", "Pandaitan", "Panorama",
    "Paquibato Proper", "Paradise Embak", "Rafael Castillo", "Salapawan", "Salaysay",
    "Saloy", "San Antonio", "San Isidro", "Sasa", "Sirib",
    "Suawan", "Tacunan", "Tagakpan", "Tagluno", "Tagurano",
    "Talomo Proper", "Talomo River", "Tamurayan", "Tibungco", "Tigatto",
    "Tungkalan", "Ubalde", "Ugac", "Ula", "Vicente Hizon Sr.",
    "Waan", "Wangan", "Wilfredo Aquino", "Wines"
];

// ACCEPTED ID TYPES - Only these ID types are allowed
const ACCEPTED_ID_TYPES = {
    'National ID': ['NATIONAL ID', 'PHIL-SYS', 'PHILSYS', 'PHILIPPINE IDENTIFICATION', 'PHILID'],
    'Driver\'s License': ['DRIVER LICENSE', 'DRIVER\'S LICENSE', 'DRIVERS LICENSE', 'DL'],
    'UMID': ['UMID', 'UNIFIED MULTI-PURPOSE ID', 'SSS UMID']
};

// ═══════════════════════════════════════════════════════════════════════════════
// VALIDATION FUNCTIONS
// ═══════════════════════════════════════════════════════════════════════════════

// Validate if address is from Davao City and barangay is valid
// NOTE: This ONLY validates the API address data (Address1 ONLY), NOT the user's input from Tab 1
// IMPORTANT: Only uses Address1, ignores Address2 and Place of Birth
function validateDavaoCityAddress(address1, address2) {
    // ONLY USE ADDRESS1 - Ignore address2, placeOfBirth, and other fields
    const fullAddress = (address1 || '').toUpperCase().trim();

    console.log('🏠 Address Validation - API Data (Address1 ONLY):');
    console.log('   Address 1:', address1 || 'N/A');
    console.log('   Address 2 (IGNORED):', address2 || 'N/A');
    console.log('   Using Address:', fullAddress || 'EMPTY');

    // If no address1 data from API, skip validation (API might not always return address)
    if (!fullAddress || fullAddress === '') {
        console.warn('⚠️ No Address1 data from API - Skipping address validation');
        return {
            valid: true,
            message: 'Address validation skipped (no Address1 data from API)',
            warning: true
        };
    }

    // Check if address contains "DAVAO CITY" or "DAVAO"
    const isDavaoCity = fullAddress.includes('DAVAO CITY') ||
                        fullAddress.includes('DAVAO,') ||
                        fullAddress.includes('DAVAO ') ||
                        fullAddress.includes(' DAVAO');

    console.log('   Contains "Davao":', isDavaoCity);

    if (!isDavaoCity) {
        console.log('❌ Address validation failed: Not from Davao City');
        return {
            valid: false,
            message: 'Your ID shows an address outside Davao City. Only Davao City residents can register.'
        };
    }

    // Flexible barangay validation - normalize and compare
    // Helper function to normalize barangay names for comparison
    function normalizeBarangay(barangay) {
        if (!barangay) return '';

        // Convert to uppercase, remove special characters, extra spaces
        let normalized = barangay.toUpperCase()
            .replace(/\(POB\.?\)/gi, '')
            .replace(/PROPER/gi, '')
            .replace(/[().,]/g, '')
            .replace(/-/g, ' ')
            .trim();

        // Remove extra spaces
        while (normalized.includes('  ')) {
            normalized = normalized.replace('  ', ' ');
        }

        return normalized;
    }

    // Normalize the full address
    const normalizedAddress = normalizeBarangay(fullAddress);

    // Check if barangay is in the Davao City barangays list (flexible matching)
    let barangayFound = false;
    let matchedBarangay = '';

    for (const barangay of DAVAO_CITY_BARANGAYS) {
        const normalizedBarangay = normalizeBarangay(barangay);

        // Check for exact match or partial match (contains)
        if (normalizedAddress.includes(normalizedBarangay) ||
            normalizedBarangay.includes(normalizedAddress.split(/\s+/)[0])) {
            barangayFound = true;
            matchedBarangay = barangay;
            break;
        }
    }

    console.log('   Barangay found:', barangayFound);
    if (barangayFound) {
        console.log('   Matched Barangay:', matchedBarangay);
    }

    // If barangay not found in the list, still allow but with warning
    // (Some IDs might have abbreviated or alternate barangay names)
    if (!barangayFound) {
        console.warn('⚠️ Barangay not in standard list, but Davao City confirmed - Allowing');
        return {
            valid: true,
            message: 'Address validated: Davao City',
            warning: true
        };
    }

    console.log('✅ Address validation passed');
    return {
        valid: true,
        message: 'Address validated: Davao City',
        barangay: matchedBarangay
    };
}

// Validate if ID type is accepted
function validateIdType(documentName) {
    if (!documentName) {
        return {
            valid: false,
            message: 'ID type not detected. Please use a clearer photo.'
        };
    }

    const docNameUpper = documentName.toUpperCase();

    for (const [acceptedType, variants] of Object.entries(ACCEPTED_ID_TYPES)) {
        for (const variant of variants) {
            if (docNameUpper.includes(variant)) {
                return {
                    valid: true,
                    message: `ID type accepted: ${acceptedType}`,
                    idType: acceptedType
                };
            }
        }
    }

    return {
        valid: false,
        message: `ID type "${documentName}" is not accepted. Only National ID, Driver's License, and UMID are accepted.`,
        documentName: documentName
    };
}

// Validate if names match between ID and registration
function validateNameMatch(apiData, registeredData) {
    console.log('👤 Name Validation - Comparing ID with Registration:');
    console.log('   API Data:', apiData);
    console.log('   Registered Data:', registeredData);

    const errors = [];

    // Normalize function - remove extra spaces, convert to uppercase, remove special chars
    const normalize = (str) => (str || '').trim().toUpperCase().replace(/\s+/g, ' ').replace(/[.,]/g, '');

    const apiFirst = normalize(apiData.firstName);
    const apiMiddle = normalize(apiData.middleName);
    const apiLast = normalize(apiData.lastName);

    const regFirst = normalize(registeredData.firstName);
    const regMiddle = normalize(registeredData.middleName);
    const regLast = normalize(registeredData.lastName);

    console.log('   Normalized API:', { first: apiFirst, middle: apiMiddle, last: apiLast });
    console.log('   Normalized Reg:', { first: regFirst, middle: regMiddle, last: regLast });

    // Check last name (more lenient - check if one contains the other)
    if (apiLast && regLast) {
        const lastNameMatch = apiLast === regLast ||
                             apiLast.includes(regLast) ||
                             regLast.includes(apiLast);
        if (!lastNameMatch) {
            console.log('❌ Last name mismatch');
            errors.push(`Last name mismatch: ID shows "${apiData.lastName}" but you entered "${registeredData.lastName}"`);
        } else {
            console.log('✅ Last name matches');
        }
    }

    // Check first name (more lenient - check if one contains the other)
    if (apiFirst && regFirst) {
        const firstNameMatch = apiFirst === regFirst ||
                              apiFirst.includes(regFirst) ||
                              regFirst.includes(apiFirst);
        if (!firstNameMatch) {
            console.log('❌ First name mismatch');
            errors.push(`First name mismatch: ID shows "${apiData.firstName}" but you entered "${registeredData.firstName}"`);
        } else {
            console.log('✅ First name matches');
        }
    }

    // Check middle name (optional - only if both have values, and be more lenient)
    if (apiMiddle && regMiddle) {
        const middleNameMatch = apiMiddle === regMiddle ||
                               apiMiddle.includes(regMiddle) ||
                               regMiddle.includes(apiMiddle) ||
                               apiMiddle.charAt(0) === regMiddle.charAt(0); // Match first initial
        if (!middleNameMatch) {
            console.log('⚠️ Middle name mismatch (allowing due to variations)');
            // Don't add to errors - middle names often have variations
        } else {
            console.log('✅ Middle name matches');
        }
    }

    if (errors.length > 0) {
        console.log('❌ Name validation failed');
        return {
            valid: false,
            errors: errors,
            message: 'Name mismatch detected. Please ensure your personal information matches your ID.'
        };
    }

    console.log('✅ Name validation passed');
    return {
        valid: true,
        message: 'Names match successfully'
    };
}

// ═══════════════════════════════════════════════════════════════════════════════
// GENDER VALIDATION - Compare user-selected gender with API-extracted gender
// ═══════════════════════════════════════════════════════════════════════════════
function validateGenderMatch(userGender, apiGender) {
    console.log('⚧️ Gender Validation:');
    console.log('   User selected:', userGender);
    console.log('   ID shows:', apiGender);

    // If API didn't return gender, skip validation
    if (!apiGender || apiGender.trim() === '') {
        console.log('⚠️ Gender not extracted from ID - skipping validation');
        return { valid: true, message: 'Gender validation skipped (not extracted from ID)', skipped: true };
    }

    // If user didn't select gender, require it
    if (!userGender || userGender.trim() === '') {
        return { valid: false, message: 'Please select your gender in Personal Info tab' };
    }

    // Normalize gender values for comparison
    const normalizeGender = (gender) => {
        const g = (gender || '').toUpperCase().trim();
        // Map common variations to standard values
        if (['M', 'MALE', 'LALAKI', 'MAN', 'BOY'].includes(g)) return 'MALE';
        if (['F', 'FEMALE', 'BABAE', 'WOMAN', 'GIRL'].includes(g)) return 'FEMALE';
        return g;
    };

    const normalizedUser = normalizeGender(userGender);
    const normalizedApi = normalizeGender(apiGender);

    console.log('   Normalized user:', normalizedUser);
    console.log('   Normalized API:', normalizedApi);

    if (normalizedUser !== normalizedApi) {
        console.log('❌ Gender mismatch detected');
        return {
            valid: false,
            message: `Gender mismatch: You selected "${userGender}" but your ID shows "${apiGender}". Please correct your gender selection.`
        };
    }

    console.log('✅ Gender matches');
    return { valid: true, message: 'Gender matches ID document' };
}

// ═══════════════════════════════════════════════════════════════════════════════
// BIRTHDATE VALIDATION - Compare user-entered birthdate with API-extracted birthdate
// ═══════════════════════════════════════════════════════════════════════════════
function validateBirthdateMatch(userBirthdate, apiBirthdate) {
    console.log('📅 Birthdate Validation:');
    console.log('   User entered:', userBirthdate);
    console.log('   ID shows:', apiBirthdate);

    // If API didn't return birthdate, skip validation
    if (!apiBirthdate || apiBirthdate.trim() === '') {
        console.log('⚠️ Birthdate not extracted from ID - skipping validation');
        return { valid: true, message: 'Birthdate validation skipped (not extracted from ID)', skipped: true };
    }

    // If user didn't enter birthdate, require it
    if (!userBirthdate || userBirthdate.trim() === '') {
        return { valid: false, message: 'Please enter your date of birth in Personal Info tab' };
    }

    // Parse dates for comparison
    // User format: YYYY-MM-DD (from date input)
    // API format: Various (YYYY-MM-DD, MM/DD/YYYY, DD/MM/YYYY, etc.)
    const parseDate = (dateStr) => {
        if (!dateStr) return null;
        
        // Try YYYY-MM-DD format first
        let match = dateStr.match(/^(\d{4})-(\d{2})-(\d{2})$/);
        if (match) {
            return { year: parseInt(match[1]), month: parseInt(match[2]), day: parseInt(match[3]) };
        }
        
        // Try MM/DD/YYYY format
        match = dateStr.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
        if (match) {
            return { year: parseInt(match[3]), month: parseInt(match[1]), day: parseInt(match[2]) };
        }
        
        // Try DD/MM/YYYY format (common in PH)
        match = dateStr.match(/^(\d{1,2})-(\d{1,2})-(\d{4})$/);
        if (match) {
            return { year: parseInt(match[3]), month: parseInt(match[2]), day: parseInt(match[1]) };
        }
        
        // Try parsing as Date object
        const date = new Date(dateStr);
        if (!isNaN(date.getTime())) {
            return { year: date.getFullYear(), month: date.getMonth() + 1, day: date.getDate() };
        }
        
        return null;
    };

    const userDate = parseDate(userBirthdate);
    const apiDate = parseDate(apiBirthdate);

    console.log('   Parsed user date:', userDate);
    console.log('   Parsed API date:', apiDate);

    if (!userDate) {
        return { valid: false, message: 'Invalid date format entered' };
    }

    if (!apiDate) {
        console.log('⚠️ Could not parse API birthdate - skipping validation');
        return { valid: true, message: 'Birthdate validation skipped (could not parse ID date)', skipped: true };
    }

    // Compare year, month, and day
    if (userDate.year !== apiDate.year || userDate.month !== apiDate.month || userDate.day !== apiDate.day) {
        console.log('❌ Birthdate mismatch detected');
        const userFormatted = `${userDate.month}/${userDate.day}/${userDate.year}`;
        const apiFormatted = `${apiDate.month}/${apiDate.day}/${apiDate.year}`;
        return {
            valid: false,
            message: `Birthdate mismatch: You entered "${userFormatted}" but your ID shows "${apiFormatted}". Please correct your date of birth.`
        };
    }

    console.log('✅ Birthdate matches');
    return { valid: true, message: 'Birthdate matches ID document' };
}

// ═══════════════════════════════════════════════════════════════════════════════
// SUFFIX VALIDATION - Compare user-selected suffix with API-extracted suffix
// ═══════════════════════════════════════════════════════════════════════════════
function validateSuffixMatch(userSuffix, apiSuffix) {
    console.log('📛 Suffix Validation:');
    console.log('   User selected:', userSuffix);
    console.log('   ID shows:', apiSuffix);

    // Normalize suffix values
    const normalizeSuffix = (suffix) => {
        const s = (suffix || '').toUpperCase().trim().replace('.', '');
        if (s === 'NONE' || s === '' || s === 'N/A') return '';
        if (s === 'JUNIOR') return 'JR';
        if (s === 'SENIOR') return 'SR';
        return s;
    };

    const normalizedUser = normalizeSuffix(userSuffix);
    const normalizedApi = normalizeSuffix(apiSuffix);

    console.log('   Normalized user:', normalizedUser || '(none)');
    console.log('   Normalized API:', normalizedApi || '(none)');

    // If API didn't extract suffix, skip validation (optional field)
    if (normalizedApi === '') {
        console.log('⚠️ Suffix not extracted from ID - skipping validation');
        return { valid: true, message: 'Suffix validation skipped (not extracted from ID)', skipped: true };
    }

    // If API shows suffix but user didn't select one
    if (normalizedApi !== '' && normalizedUser === '') {
        console.log('⚠️ ID shows suffix but user selected none - warning only');
        return { 
            valid: true, 
            message: `Your ID shows suffix "${apiSuffix}" but you selected "None". Please verify this is correct.`,
            warning: true 
        };
    }

    // If user selected suffix but API doesn't show one
    if (normalizedUser !== '' && normalizedApi === '') {
        console.log('⚠️ User selected suffix but ID shows none - warning only');
        return { 
            valid: true, 
            message: `You selected suffix "${userSuffix}" but your ID doesn't show a suffix. Please verify this is correct.`,
            warning: true 
        };
    }

    // Both have suffix - must match
    if (normalizedUser !== normalizedApi) {
        console.log('❌ Suffix mismatch detected');
        return {
            valid: false,
            message: `Suffix mismatch: You selected "${userSuffix}" but your ID shows "${apiSuffix}". Please correct your suffix selection.`
        };
    }

    console.log('✅ Suffix matches');
    return { valid: true, message: 'Suffix matches ID document' };
}

// ═══════════════════════════════════════════════════════════════════════════════
// BARANGAY VALIDATION FUNCTION
// ═══════════════════════════════════════════════════════════════════════════════
// Compares user-selected barangay with barangay found in ID address
// Uses flexible matching with normalization for common variations
function validateBarangayMatch(address1, userSelectedBarangay) {
    console.log('🏘️ Barangay Validation Input:', {
        address1: address1,
        userSelectedBarangay: userSelectedBarangay,
        address1Length: address1?.length || 0
    });

    // If no address data from API, skip validation
    if (!address1 || address1.trim() === '') {
        console.warn('⚠️ Barangay validation skipped - no address');
        return {
            valid: true,
            message: 'Barangay validation skipped (no address data from API)',
            warning: true
        };
    }

    // If user didn't select a barangay, fail validation
    if (!userSelectedBarangay || userSelectedBarangay.trim() === '') {
        return {
            valid: false,
            message: 'Please select your barangay in Tab 1'
        };
    }

    // Normalize function for barangay names - handles common variations
    const normalizeBarangayName = (name) => {
        if (!name) return '';
        return name.toUpperCase()
            .replace(/^BRGY\.?\s*/i, '')           // Remove "Brgy." prefix
            .replace(/^BARANGAY\s*/i, '')          // Remove "Barangay" prefix
            .replace(/\(POB\.?\)/gi, '')           // Remove "(Pob.)" suffix
            .replace(/PROPER$/i, '')               // Remove "Proper" suffix
            .replace(/[().,\-]/g, ' ')             // Replace punctuation with spaces
            .replace(/\s+/g, ' ')                  // Normalize multiple spaces
            .trim();
    };

    const fullAddress = address1.toUpperCase().trim();
    const normalizedAddress = normalizeBarangayName(fullAddress);
    const normalizedUserBarangay = normalizeBarangayName(userSelectedBarangay);

    console.log('🏘️ Normalized values:', {
        normalizedAddress: normalizedAddress,
        normalizedUserBarangay: normalizedUserBarangay
    });

    // Strategy 1: Direct contains check (most common case)
    if (normalizedAddress.includes(normalizedUserBarangay)) {
        console.log('✅ Barangay found via direct contains match');
        return {
            valid: true,
            message: `Barangay confirmed: ${userSelectedBarangay}`,
            matchedBarangay: userSelectedBarangay
        };
    }

    // Strategy 2: Word-by-word matching for compound barangay names
    // Split the user's barangay into words and check if ALL words appear in address
    const barangayWords = normalizedUserBarangay.split(/\s+/).filter(w => w.length >= 2);
    if (barangayWords.length > 0) {
        const allWordsFound = barangayWords.every(word => normalizedAddress.includes(word));
        if (allWordsFound) {
            console.log('✅ Barangay found via word-by-word match:', barangayWords);
            return {
                valid: true,
                message: `Barangay confirmed: ${userSelectedBarangay}`,
                matchedBarangay: userSelectedBarangay
            };
        }
    }

    // Strategy 3: Check if the first significant word of barangay appears in address
    // This handles cases like "Matina Crossing" where only "MATINA" appears in ID
    const firstWord = barangayWords[0];
    if (firstWord && firstWord.length >= 4 && normalizedAddress.includes(firstWord)) {
        console.log('✅ Barangay found via primary word match:', firstWord);
        return {
            valid: true,
            message: `Barangay confirmed: ${userSelectedBarangay}`,
            matchedBarangay: userSelectedBarangay
        };
    }

    // Strategy 4: Fuzzy matching - check if barangay is a substring or vice versa
    // This handles abbreviated names on IDs
    const addressWords = normalizedAddress.split(/\s+/).filter(w => w.length >= 3);
    for (const addressWord of addressWords) {
        // Check if user barangay contains this address word (or vice versa)
        if (normalizedUserBarangay.includes(addressWord) || addressWord.includes(normalizedUserBarangay)) {
            console.log('✅ Barangay found via fuzzy match:', addressWord);
            return {
                valid: true,
                message: `Barangay confirmed: ${userSelectedBarangay}`,
                matchedBarangay: userSelectedBarangay
            };
        }
        // Check individual barangay words
        for (const barangayWord of barangayWords) {
            if (addressWord.includes(barangayWord) || barangayWord.includes(addressWord)) {
                if (barangayWord.length >= 4 && addressWord.length >= 4) {
                    console.log('✅ Barangay found via word fuzzy match:', { addressWord, barangayWord });
                    return {
                        valid: true,
                        message: `Barangay confirmed: ${userSelectedBarangay}`,
                        matchedBarangay: userSelectedBarangay
                    };
                }
            }
        }
    }

    // Strategy 5: Check against DAVAO_CITY_BARANGAYS list for common variations
    // Find all possible barangay names in the address
    const detectedBarangays = [];
    for (const brgy of DAVAO_CITY_BARANGAYS) {
        const normalizedBrgy = normalizeBarangayName(brgy);
        if (normalizedAddress.includes(normalizedBrgy) || normalizedBrgy.includes(normalizedAddress.split(/\s+/)[0])) {
            detectedBarangays.push(brgy);
        }
    }

    // If the user's selected barangay is in the detected list, it's valid
    if (detectedBarangays.length > 0) {
        console.log('🏘️ Detected barangays in address:', detectedBarangays);
        const normalizedDetected = detectedBarangays.map(b => normalizeBarangayName(b));
        if (normalizedDetected.includes(normalizedUserBarangay)) {
            console.log('✅ Barangay found via list comparison');
            return {
                valid: true,
                message: `Barangay confirmed: ${userSelectedBarangay}`,
                matchedBarangay: userSelectedBarangay
            };
        }
    }

    console.log('❌ Barangay validation failed - no match found');
    console.log('   User selected:', userSelectedBarangay);
    console.log('   ID Address:', address1);
    console.log('   Detected barangays:', detectedBarangays);

    return {
        valid: false,
        message: `Your ID address does not match the selected barangay "${userSelectedBarangay}". ID shows: "${address1}"`
    };
}

// ═══════════════════════════════════════════════════════════════════════════════
// TAB 3 REVIEW PAGE VALIDATION ORCHESTRATOR
// ═══════════════════════════════════════════════════════════════════════════════
// This function runs all three validation checks when user enters Tab 3
// Returns an object with individual validation results and overall pass/fail status
function runTab3Validations() {
    console.log('🔍 Tab 3 Validations - VerificationState:', {
        hasExtractedData: !!VerificationState.extractedData,
        hasScanResult: !!VerificationState.scanResult,
        extractedData: VerificationState.extractedData,
        scanResultData: VerificationState.scanResult?.data
    });

    // Check if API data exists
    if (!VerificationState.extractedData) {
        console.warn('⚠️ No extractedData available');
        return {
            hasData: false,
            overallPass: false,
            validations: {
                nameValidation: { valid: false, message: 'No verification data available' },
                barangayValidation: { valid: false, message: 'No verification data available' },
                davaoCityValidation: { valid: false, message: 'No verification data available' },
                genderValidation: { valid: false, message: 'No verification data available' },
                birthdateValidation: { valid: false, message: 'No verification data available' },
                suffixValidation: { valid: false, message: 'No verification data available' }
            }
        };
    }

    const apiData = VerificationState.extractedData;

    // Get address from scanResult if not in extractedData
    const address1 = apiData.address1 || VerificationState.scanResult?.data?.address1 || '';

    console.log('📍 Address data for validation:', {
        fromExtractedData: apiData.address1,
        fromScanResult: VerificationState.scanResult?.data?.address1,
        finalAddress: address1
    });

    // VALIDATION 1: Name Matching
    const userInputData = {
        firstName: (document.getElementById('firstname')?.value || '').trim(),
        middleName: (document.getElementById('middlename')?.value || '').trim(),
        lastName: (document.getElementById('lastname')?.value || '').trim(),
        suffix: (document.getElementById('suffix')?.value || '').trim()
    };

    const nameValidation = validateNameMatch(apiData, userInputData);

    // VALIDATION 2: Barangay Matching (moved before Davao City)
    const barangayValidation = validateBarangayMatch(
        address1,
        document.getElementById('barangay')?.value || ''
    );

    // VALIDATION 3: Davao City Address (moved after Barangay)
    const davaoCityValidation = validateDavaoCityAddress(
        address1,
        null
    );

    // VALIDATION 4: Gender Matching
    const userGender = (document.getElementById('gender')?.value || '').trim();
    const apiGender = apiData.data?.sex || apiData.sex || VerificationState.scanResult?.data?.sex || '';
    const genderValidation = validateGenderMatch(userGender, apiGender);

    // VALIDATION 5: Birthdate Matching
    const userBirthdate = (document.getElementById('birthdate')?.value || '').trim();
    const apiBirthdate = apiData.data?.dateOfBirth || apiData.dateOfBirth || VerificationState.scanResult?.data?.dateOfBirth || '';
    const birthdateValidation = validateBirthdateMatch(userBirthdate, apiBirthdate);

    // VALIDATION 6: Suffix Matching
    const userSuffix = (document.getElementById('suffix')?.value || '').trim();
    const apiSuffix = apiData.data?.suffix || apiData.suffix || VerificationState.scanResult?.data?.suffix || '';
    const suffixValidation = validateSuffixMatch(userSuffix, apiSuffix);

    // Determine overall pass/fail (suffix is warning only, not blocking)
    const overallPass = nameValidation.valid &&
                       barangayValidation.valid &&
                       davaoCityValidation.valid &&
                       genderValidation.valid &&
                       birthdateValidation.valid &&
                       suffixValidation.valid;

    return {
        hasData: true,
        overallPass: overallPass,
        validations: {
            nameValidation,
            barangayValidation,
            davaoCityValidation,
            genderValidation,
            birthdateValidation,
            suffixValidation
        }
    };
}

// Show processing modal
function showProcessingModal() {
    const modalElement = document.getElementById('apiProcessingModal');
    const modalContent = document.getElementById('api-modal-content');

    if (!modalElement || !modalContent) return;

    modalContent.innerHTML = `
        <div class="flex flex-col items-center text-center">
            <div class="mb-4">
                <div class="w-16 h-16 bg-blue-500 rounded-full flex items-center justify-center animate-spin">
                    <i class="fas fa-spinner text-white text-2xl"></i>
                </div>
            </div>
            <h3 class="text-lg sm:text-xl font-bold text-blue-800 mb-2">Processing Your Documents...</h3>
            <p class="text-blue-700 text-sm">Please wait while we verify your identity</p>
            <div class="mt-4 flex items-center gap-2 text-gray-600 text-xs">
                <i class="fas fa-lock text-green-600"></i>
                <span>Your data is encrypted and secure</span>
            </div>
        </div>
    `;

    const modal = new bootstrap.Modal(modalElement);
    modal.show();
}

// Show success animation in modal
function showSuccessAnimation(message = 'Verification Complete!') {
    const modalElement = document.getElementById('apiProcessingModal');
    const modalContent = document.getElementById('api-modal-content');

    if (!modalElement || !modalContent) return;

    modalContent.innerHTML = `
        <div class="flex flex-col items-center text-center">
            <div class="mb-4 relative">
                <div class="w-20 h-20 bg-green-500 rounded-full flex items-center justify-center animate-bounce">
                    <i class="fas fa-check text-white text-4xl"></i>
                </div>
                <div class="absolute -top-1 -right-1 w-6 h-6 bg-emerald-400 rounded-full animate-ping"></div>
            </div>
            <h3 class="text-xl sm:text-2xl font-bold text-green-800 mb-2">${message}</h3>
            <p class="text-green-700 text-sm mb-4">Your ID has been successfully verified and data extracted.</p>
            <div class="flex items-center gap-2 text-green-600 text-sm mb-4">
                <i class="fas fa-shield-check"></i>
                <span class="font-medium">Secure & Encrypted</span>
            </div>
            <button onclick="closeProcessingModal()" class="px-6 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg font-semibold transition-all">
                <i class="fas fa-check mr-2"></i>Continue
            </button>
        </div>
    `;
}

// Show failure animation in modal
function showFailureAnimation(message = 'Verification Failed', details = '') {
    const modalElement = document.getElementById('apiProcessingModal');
    const modalContent = document.getElementById('api-modal-content');

    if (!modalElement || !modalContent) return;

    modalContent.innerHTML = `
        <div class="flex flex-col items-center text-center">
            <div class="mb-4 relative">
                <div class="w-20 h-20 bg-red-500 rounded-full flex items-center justify-center animate-pulse">
                    <i class="fas fa-times text-white text-4xl"></i>
                </div>
            </div>
            <h3 class="text-xl sm:text-2xl font-bold text-red-800 mb-2">${message}</h3>
            <p class="text-red-700 text-sm mb-4">${details || 'Please check your documents and try again.'}</p>
            <button onclick="closeProcessingModal()" class="px-6 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg font-semibold transition-all">
                <i class="fas fa-redo mr-2"></i>Try Again
            </button>
        </div>
    `;

    // Show retry button on error
    showRetryButton();
}

// Helper function to show retry button
function showRetryButton() {
    const retryBtn = document.getElementById('retry-verification-btn');
    const startBtn = document.getElementById('start-verification-btn');

    if (retryBtn) {
        retryBtn.classList.remove('hidden');
        retryBtn.classList.add('flex');
    }
    if (startBtn) {
        startBtn.classList.add('hidden');
        startBtn.style.display = 'none';
    }
}

// Close processing modal
function closeProcessingModal() {
    const modalElement = document.getElementById('apiProcessingModal');
    if (modalElement) {
        const modal = bootstrap.Modal.getInstance(modalElement);
        if (modal) {
            modal.hide();
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// GLOBAL SUBMIT VERIFICATION FUNCTION (WITH COMPREHENSIVE VALIDATION)
// ═══════════════════════════════════════════════════════════════════════════════
// Defined at global scope to be immediately available when script loads.
// Sends Front ID, Back ID, and Selfie to the API as Base64.
// Validates: Address (Davao City + Barangay), ID Type, Document Number, Name Matching
// ═══════════════════════════════════════════════════════════════════════════════
window.submitVerification = async function() {
    console.log('═══════════════════════════════════════════════════════════════');
    console.log('🚀 window.submitVerification() CALLED');
    console.log('═══════════════════════════════════════════════════════════════');

    // Check if all required images are ready
    if (!VerificationState.isReadyForVerification()) {
        console.error('❌ Not ready for verification:', VerificationState.getSummary());
        alert('Please upload all required photos:\n• Front of ID\n• Back of ID\n• Your photo (selfie)');
        return;
    }

    // Update UI to show processing state
    VerificationState.verificationInProgress = true;
    const verifyButton = document.getElementById('verify-submit-btn');
    if (verifyButton) {
        verifyButton.disabled = true;
        verifyButton.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Verifying...';
    }

    // Show processing modal
    showProcessingModal();

    try {
        console.log('🔍 VERIFICATION STATE:');
        console.log('   frontIdImage:', VerificationState.frontIdImage ? `✅ ${VerificationState.frontIdImage.length} chars` : '❌ NULL');
        console.log('   backIdImage:', VerificationState.backIdImage ? `✅ ${VerificationState.backIdImage.length} chars` : '❌ NULL');
        console.log('   selfieImage:', VerificationState.selfieImage ? `✅ ${VerificationState.selfieImage.length} chars` : '❌ NULL');

        // Verify required Base64 images exist
        if (!VerificationState.frontIdImage) {
            throw new Error('Front ID image is missing. Please upload your ID again.');
        }
        if (!VerificationState.backIdImage) {
            throw new Error('Back ID image is missing. Please upload your ID again.');
        }
        if (!VerificationState.selfieImage) {
            throw new Error('Selfie image is missing. Please capture your photo again.');
        }

        // Build JSON payload with Base64 images
        // IMPORTANT: Property names must match C# model (PascalCase)
        const payload = {
            DocumentImage: VerificationState.frontIdImage,
            BackImage: VerificationState.backIdImage,
            FaceImage: VerificationState.selfieImage
        };

        console.log('📤 Sending to:', ID_ANALYZER_CONFIG.SCAN_ENDPOINT);
        console.log('📦 Payload sizes:');
        console.log('   DocumentImage:', payload.DocumentImage.length, 'chars');
        console.log('   BackImage:', payload.BackImage.length, 'chars');
        console.log('   FaceImage:', payload.FaceImage.length, 'chars');

        // Send to API with JSON payload
        const response = await fetch(ID_ANALYZER_CONFIG.SCAN_ENDPOINT, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        const responseText = await response.text();
        console.log('📥 Response status:', response.status);

        let result;
        try {
            result = JSON.parse(responseText);
        } catch (e) {
            throw new Error('Invalid API response');
        }

        if (!result.success) {
            throw new Error(result.error || 'Verification failed');
        }

        // Store result
        VerificationState.scanResult = result;
        VerificationState.extractedData = result;

        const decision = (result.decision || '').toLowerCase();
        console.log('📋 Decision:', decision);

        // Handle REJECT decision
        if (decision === 'reject') {
            VerificationState.verificationInProgress = false;
            showFailureAnimation('Verification Rejected', 'Your ID verification was rejected. Please ensure your photos are clear and try again.');
            if (verifyButton) {
                verifyButton.disabled = false;
                verifyButton.innerHTML = '<i class="fas fa-sync-alt mr-2"></i>Retry';
            }
            return;
        }

        // Handle REVIEW decision
        if (decision === 'review') {
            showFailureAnimation('Manual Review Required', 'Your ID is under review. We will notify you when the review is complete.');
            if (verifyButton) {
                verifyButton.disabled = false;
                verifyButton.innerHTML = '<i class="fas fa-clock mr-2"></i>Pending Review';
            }
            return;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ACCEPT DECISION - PERFORM COMPREHENSIVE VALIDATION
        // ═══════════════════════════════════════════════════════════════════════

        if (decision === 'accept') {
            console.log('✅ API Decision: ACCEPT - Performing validation checks...');

            const validationErrors = [];

            // VALIDATION 1: Check ID Type - PRIORITIZE MANUAL SELECTION
            // Check if user has manually selected ID type from dropdown
            const manuallySelectedIdType = document.getElementById('idtype-display')?.value;
            let idTypeValidation;

            if (manuallySelectedIdType && manuallySelectedIdType !== '') {
                // User has manually selected ID type - use that instead of API detection
                console.log('✅ ID Type manually selected by user:', manuallySelectedIdType);
                idTypeValidation = {
                    valid: true,
                    idType: manuallySelectedIdType,
                    message: `ID Type manually selected: ${manuallySelectedIdType}`
                };
            } else {
                // No manual selection - validate using API's documentName
                console.log('📋 No manual ID Type selection - Using API detection');
                idTypeValidation = validateIdType(result.data?.documentName);
            }

            if (!idTypeValidation.valid) {
                validationErrors.push(idTypeValidation.message);
            } else {
                console.log('✅ ID Type Validation:', idTypeValidation.message);
            }

            // VALIDATION 2: Check Davao City Address (ONLY Address1, NOT Address2)
            const addressValidation = validateDavaoCityAddress(
                result.data?.address1,
                null  // Explicitly pass null for address2 to ensure it's not used
            );
            if (!addressValidation.valid) {
                validationErrors.push(addressValidation.message);
            } else {
                console.log('✅ Address Validation:', addressValidation.message);
            }

            // VALIDATION 3: Check Name Matching (firstName, middleName, lastName)
            const registeredNames = {
                firstName: document.getElementById('firstname')?.value || '',
                middleName: document.getElementById('middlename')?.value || '',
                lastName: document.getElementById('lastname')?.value || ''
            };

            const nameValidation = validateNameMatch(result.data, registeredNames);
            if (!nameValidation.valid) {
                validationErrors.push(...nameValidation.errors);
            } else {
                console.log('✅ Name Validation:', nameValidation.message);
            }

            // VALIDATION 4: Check Gender Matching
            const userGender = document.getElementById('gender')?.value || '';
            const apiGender = result.data?.sex || '';
            const genderValidation = validateGenderMatch(userGender, apiGender);
            if (!genderValidation.valid) {
                validationErrors.push(genderValidation.message);
            } else if (!genderValidation.skipped) {
                console.log('✅ Gender Validation:', genderValidation.message);
            }

            // VALIDATION 5: Check Birthdate Matching
            const userBirthdate = document.getElementById('birthdate')?.value || '';
            const apiBirthdate = result.data?.dateOfBirth || '';
            const birthdateValidation = validateBirthdateMatch(userBirthdate, apiBirthdate);
            if (!birthdateValidation.valid) {
                validationErrors.push(birthdateValidation.message);
            } else if (!birthdateValidation.skipped) {
                console.log('✅ Birthdate Validation:', birthdateValidation.message);
            }

            // VALIDATION 6: Check Suffix Matching (warning only, not blocking)
            const userSuffix = document.getElementById('suffix')?.value || '';
            const apiSuffix = result.data?.suffix || '';
            const suffixValidation = validateSuffixMatch(userSuffix, apiSuffix);
            if (!suffixValidation.valid) {
                validationErrors.push(suffixValidation.message);
            } else if (suffixValidation.warning) {
                console.log('⚠️ Suffix Warning:', suffixValidation.message);
                // Show warning but don't block
            } else if (!suffixValidation.skipped) {
                console.log('✅ Suffix Validation:', suffixValidation.message);
            }

            // If any validation failed, show error and stop
            if (validationErrors.length > 0) {
                const errorMessage = validationErrors.join('<br>');
                showFailureAnimation('Validation Failed', errorMessage);
                VerificationState.verificationInProgress = false;
                if (verifyButton) {
                    verifyButton.disabled = false;
                    verifyButton.innerHTML = '<i class="fas fa-sync-alt mr-2"></i>Retry';
                }
                return;
            }

            // ALL VALIDATIONS PASSED - Proceed with success
            VerificationState.verificationComplete = true;
            console.log('✅ All validations passed! Verification ACCEPTED');

            // ID Type - User must select manually (auto-detection disabled)
            const idTypeField = document.getElementById('idtype-display');
            const idTypeHidden = document.getElementById('document-type'); // Hidden form field for submission
            const idTypeVerifiedBadge = document.getElementById('idtype-verified-badge');
            const idTypeHelpText = document.getElementById('idtype-help-text');

            // Keep the badge hidden - no auto-detection
            if (idTypeVerifiedBadge) {
                idTypeVerifiedBadge.classList.add('hidden');
            }
            
            // Update help text to remind user to select ID type
            if (idTypeHelpText) {
                idTypeHelpText.textContent = 'Please select your ID type from the dropdown';
                idTypeHelpText.parentElement.classList.remove('text-gray-500', 'text-green-600');
                idTypeHelpText.parentElement.classList.add('text-blue-600');
            }

            // Remove any auto-detected styling
            if (idTypeField) {
                idTypeField.classList.remove('border-green-300', 'bg-green-50');
                idTypeField.classList.add('border-gray-300', 'bg-white');
            }

            // Add change event listener to sync dropdown with hidden field
            if (idTypeField && idTypeHidden) {
                idTypeField.addEventListener('change', function() {
                    idTypeHidden.value = this.value;
                    console.log('ID Type manually selected:', this.value);
                });
            }

            // Auto-populate ID Number if API returns "accept"
            const idNumberField = document.getElementById('idnumber');
            const idNumberContainer = document.getElementById('idnumber-container');
            if (idNumberField && result.data?.documentNumber) {
                idNumberField.value = result.data.documentNumber;
                idNumberField.readOnly = true; // Make it readonly after auto-population
                console.log('✅ ID Number auto-populated:', result.data.documentNumber);

                // Show ID Number field after successful verification
                if (idNumberContainer) {
                    idNumberContainer.classList.remove('hidden');
                }
            }

            // Show ID Type field after successful verification
            const idTypeContainer = document.getElementById('idtype-container');
            if (idTypeContainer) {
                idTypeContainer.classList.remove('hidden');
            }

            // NOTE: Block/Lot/Street is NOT auto-populated - user must enter it in Tab 1
            // Address validation only checks if it matches Davao City from API data

            // Store decision and transaction ID
            const decisionField = document.getElementById('verification-decision');
            const transactionField = document.getElementById('transaction-id');
            if (decisionField) decisionField.value = result.decision || '';
            if (transactionField) transactionField.value = result.transactionId || '';

            // Show success animation
            showSuccessAnimation('Verification Successful!');

            // Update button to show success
            if (verifyButton) {
                verifyButton.disabled = false;
                verifyButton.innerHTML = '<i class="fas fa-check mr-2"></i>Verified';
                verifyButton.classList.remove('bg-blue-600', 'hover:bg-blue-700');
                verifyButton.classList.add('bg-green-600', 'hover:bg-green-700');
            }

            // Enable "Next" button to proceed to Review tab
            const nextButton = document.getElementById('next-to-review-btn');
            if (nextButton) {
                nextButton.disabled = false;
                nextButton.classList.remove('bg-gray-300', 'text-gray-500', 'cursor-not-allowed');
                nextButton.classList.add('bg-blue-600', 'hover:bg-blue-700', 'text-white');
                const nextBtnText = nextButton.querySelector('#next-btn-text');
                if (nextBtnText) {
                    nextBtnText.textContent = 'Next';
                }
                const nextBtnIcon = nextButton.querySelector('i');
                if (nextBtnIcon) {
                    nextBtnIcon.classList.remove('fa-lock');
                    nextBtnIcon.classList.add('fa-arrow-right');
                }
            }

            console.log('✅ VERIFICATION COMPLETE');
        }

    } catch (error) {
        console.error('❌ Verification error:', error);
        VerificationState.verificationInProgress = false;
        showFailureAnimation('Error Occurred', error.message);
        if (verifyButton) {
            verifyButton.disabled = false;
            verifyButton.innerHTML = '<i class="fas fa-sync-alt mr-2></i>Retry';
        }
    }
};

console.log('✅ window.submitVerification defined at global scope');

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

    // Get DOM elements with error checking
    const documentType = document.getElementById('document-type');
    const documentWarning = document.getElementById('document-warning');
    const uploadFront = document.getElementById('upload-area-front');
    const uploadBack = document.getElementById('upload-area-back');
    const fileFront = document.getElementById('file-input-front');
    const fileBack = document.getElementById('file-input-back');
    const imagePreview = document.getElementById('image-preview');
    const imagePreviewBack = document.getElementById('image-preview-back');
    const status = document.getElementById('ocr-status'); // ID Analyzer status display
    const resultBox = document.getElementById('ocr-result'); // ID Analyzer result display
    const progressBar = document.getElementById('progress-bar');
    const progress = document.getElementById('progress');
    const terms = document.getElementById('terms');

    // Davao City Verification Elements
    const davaoVerification = document.getElementById('davao-verification');
    const davaoResultIcon = document.getElementById('davao-result-icon');
    const davaoResultTitle = document.getElementById('davao-result-title');
    const davaoResultMessage = document.getElementById('davao-result-message');
    const davaoStatusBadge = document.getElementById('davao-status-badge');

    // Check if critical elements exist
    if (!documentType || !uploadFront || !uploadBack || !fileFront || !fileBack) {
        return;
    }

    // Form fields to disable when license is invalid
    const formFields = [
        'idnumber', 'phonenumber', 'lastname', 'firstname', 'middlename',
        'sex', 'birthdate', 'BlkLotStreet', 'SubVill', 'Barangay', 'District',
        'SecurityQuestions', 'Securityanswer'
    ];

    // Track if ID Analyzer scanning is enabled
    let scanningEnabled = true;

    // Real-time verification status polling
    let verificationPollingInterval = null;
    const POLLING_INTERVAL_MS = 5000; // Poll every 5 seconds
    let currentVerificationDecision = null;

    // Debug mode for testing and calibration
    const DEBUG_MODE = false;

    // Store last scan result for debugging
    let lastScanText = "";
    let lastExtractedData = {};

    // Rate limiting for API calls
    let lastAPICallTime = 0;
    const MIN_TIME_BETWEEN_CALLS = 1000; // 1 second minimum between calls

    // Get user's name from Tab 1 form fields (Personal Info) dynamically
    // These are updated when needed by calling getUserEnteredName()
    function getRegisteredUserName() {
        if (typeof window.getUserEnteredName === 'function') {
            return window.getUserEnteredName();
        }
        // Fallback to reading form fields directly
        return {
            firstName: (document.getElementById('firstname')?.value || '').trim(),
            middleName: (document.getElementById('middlename')?.value || '').trim(),
            lastName: (document.getElementById('lastname')?.value || '').trim(),
            suffix: (document.getElementById('suffix')?.value || '').trim()
        };
    }

    // Variables to hold current user name (updated dynamically)
    let registeredFirstName = "";
    let registeredMiddleName = "";
    let registeredLastName = "";
    let registeredSuffix = "";

    // Function to refresh registered user name from form fields
    function refreshRegisteredUserName() {
        const userName = getRegisteredUserName();
        registeredFirstName = userName.firstName;
        registeredMiddleName = userName.middleName;
        registeredLastName = userName.lastName;
        registeredSuffix = userName.suffix;
    }

    // ═══════════════════════════════════════════════════════════════
    // BACK ID TOGGLE - Disable/Enable based on ID type
    // ═══════════════════════════════════════════════════════════════
    // UMID and Driver's License don't require back ID upload
    // Only National ID (PhilSys) requires back ID
    function toggleBackIdUpload(idType) {
        const backIdRequired = idType === 'phil-id'; // Only National ID requires back
        
        if (uploadBack) {
            if (backIdRequired) {
                // Enable back ID upload
                uploadBack.style.display = 'block';
                uploadBack.style.opacity = '1';
                uploadBack.style.pointerEvents = 'auto';
                uploadBack.classList.remove('disabled-upload');
            } else {
                // Disable back ID upload for UMID and Driver's License
                uploadBack.style.display = 'block'; // Keep visible but disabled
                uploadBack.style.opacity = '0.5';
                uploadBack.style.pointerEvents = 'none';
                uploadBack.classList.add('disabled-upload');
                
                // Clear any uploaded back image
                if (fileBack) {
                    fileBack.value = '';
                }
                if (imagePreviewBack) {
                    imagePreviewBack.classList.add('hidden');
                    imagePreviewBack.src = '';
                }
                
                // Update the back upload area content to show it's not required
                const backContent = uploadBack.querySelector('.id-upload-content');
                if (backContent) {
                    const backTitle = backContent.querySelector('.font-semibold');
                    if (backTitle) {
                        const idTypeName = idType === 'driver-license' ? "Driver's License" : 
                                          idType === 'umid' ? 'UMID' : 'this ID type';
                        backTitle.innerHTML = `Back of ID <span class="text-green-600 text-sm">(Not Required for ${idTypeName})</span>`;
                    }
                }
            }
        }
        
    }

    // ═══════════════════════════════════════════════════════════════
    // ✅ FIX: Ensure submit button is enabled by default on page load
    // ═══════════════════════════════════════════════════════════════
    // The submit button should be enabled by default, allowing users
    // to submit the form manually even if OCR validation hasn't run.
    // It will only be disabled if there's an actual validation failure.
    const submitButton = document.querySelector('button[type="submit"]');
    if (submitButton) {
        submitButton.disabled = false;
        submitButton.classList.remove('opacity-50', 'cursor-not-allowed');
        submitButton.title = '';
    }

    // ═══════════════════════════════════════════════════════════════
    // PHONE NUMBER AUTO-FORMATTING (09123456789)
    // ═══════════════════════════════════════════════════════════════
    const phoneNumberField = document.getElementById('phonenumber');
    if (phoneNumberField) {
        phoneNumberField.addEventListener('input', function (e) {
            let value = e.target.value;

            // Remove all non-numeric characters
            value = value.replace(/\D/g, '');

            // Limit to 11 digits
            if (value.length > 11) {
                value = value.substring(0, 11);
            }

            // Set the value as-is (no dashes)
            e.target.value = value;
        });

        // Also format on paste
        phoneNumberField.addEventListener('paste', function (e) {
            setTimeout(() => {
                let value = e.target.value.replace(/\D/g, '');
                if (value.length > 11) {
                    value = value.substring(0, 11);
                }
                e.target.value = value;
            }, 10);
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // VALID SUFFIXES LIST - Only these suffixes will be recognized
    // ═══════════════════════════════════════════════════════════════
    // Any suffix not in this list will be ignored and the suffix field
    // will be set to "None" (which equals empty string in database)
    const VALID_SUFFIXES = [
        'JR', 'JR.', 'JUNIOR',
        'SR', 'SR.', 'SENIOR',
        'II', 'III', 'IV', 'V', 'VI', 'VII', 'VIII', 'IX', 'X',
        'I', '1ST', '2ND', '3RD', '4TH', '5TH',
        'FIRST', 'SECOND', 'THIRD', 'FOURTH', 'FIFTH'
    ];

    // Normalized suffix mapping (e.g., JUNIOR -> JR, SENIOR -> SR)
    const SUFFIX_NORMALIZATION = {
        'JUNIOR': 'JR',
        'JR.': 'JR',
        'SENIOR': 'SR',
        'SR.': 'SR',
        '1ST': 'I',
        '2ND': 'II',
        '3RD': 'III',
        '4TH': 'IV',
        '5TH': 'V',
        'FIRST': 'I',
        'SECOND': 'II',
        'THIRD': 'III',
        'FOURTH': 'IV',
        'FIFTH': 'V'
    };

    // Enhanced: List of known Filipino compound/double surnames and name particles
    const COMPOUND_NAMES = [
        // De/Dela/Del compound surnames
        'DE LA CRUZ', 'DELA CRUZ', 'DE LOS REYES', 'DELOS REYES',
        'DE LA ROSA', 'DELA ROSA', 'DE LA TORRE', 'DELA TORRE',
        'DE LOS SANTOS', 'DELOS SANTOS', 'DE LOS ANGELES', 'DELOS ANGELES',
        'DE LEON', 'DELEON', 'DE GUZMAN', 'DEGUZMAN',
        'DE CASTRO', 'DECASTRO', 'DE JESUS', 'DEJESUS',
        'DEL ROSARIO', 'DELROSARIO', 'DEL MUNDO', 'DELMUNDO',
        'DEL CARMEN', 'DELCARMEN', 'DEL PILAR', 'DELPILAR',
        'DE VERA', 'DEVERA', 'DE OCAMPO', 'DEOCAMPO',
        'DE BELEN', 'DEBELEN', 'DE SILVA', 'DESILVA',
        'DE MESA', 'DEMESA', 'DE DIOS', 'DEDIOS',

        // Santa/San compound surnames
        'SANTA MARIA', 'SANTAMARIA', 'SANTA CRUZ', 'SANTACRUZ',
        'SANTA ROSA', 'SANTAROSA', 'SANTA ANA', 'SANTAANA',
        'SAN JOSE', 'SANJOSE', 'SAN JUAN', 'SANJUAN',
        'SAN PEDRO', 'SANPEDRO', 'SAN DIEGO', 'SANDIEGO',
        'SAN AGUSTIN', 'SANAGUSTIN', 'SAN MIGUEL', 'SANMIGUEL',
        'SAN ANDRES', 'SANANDRES', 'SAN ANTONIO', 'SANANTONIO',

        // Villa compound surnames
        'VILLA NUEVA', 'VILLANUEVA', 'VILLA REAL', 'VILLAREAL',
        'VILLA MAYOR', 'VILLAMAYOR', 'VILLA VERDE', 'VILLAVERDE',
        'VILLA CRUZ', 'VILLACRUZ', 'VILLA FRANCA', 'VILLAFRANCA',
        'VILLA FLOR', 'VILLAFLOR', 'VILLA GRACIA', 'VILLAGRACIA',

        // Monte compound surnames
        'MONTE MAYOR', 'MONTEMAYOR', 'MONTE REAL', 'MONTEREAL',
        'MONTE VERDE', 'MONTEVERDE', 'MONTE NEGRO', 'MONTENEGRO',
        'MONTE DE OCA', 'MONTEDEOCA',

        // Buen compound surnames
        'BUEN CAMINO', 'BUENCAMINO', 'BUEN DIA', 'BUENDIA',
        'BUEN ROSTRO', 'BUENROSTRO', 'BUEN ABAD', 'BUENABAD',
        'BUEN VENTURA', 'BUENVENTURA',

        // Other common compound surnames
        'LA PAZ', 'LAPAZ', 'LA TORRE', 'LATORRE',
        'LOS BAÑOS', 'LOSBAÑOS', 'LOS SANTOS', 'LOSSANTOS',
        'CASTILLO VERDE', 'CASTILLOVERDE',
        'FERNANDEZ DE LEON', 'FERNANDEZDELEON',

        // Particles (used for detection)
        'DE', 'DEL', 'DELA', 'DELOS', 'DE LOS', 'DE LA',
        'SAN', 'SANTA', 'VILLA', 'MONTE', 'BUEN', 'LA', 'LOS'
    ];

    // Enhanced: Check if word sequence forms a known Filipino compound surname
    function isCompoundName(words) {
        if (!words || words.length === 0) return false;

        const normalized = words.map(w => w.toUpperCase().trim()).join(' ');

        // Check for exact match in compound names list
        if (COMPOUND_NAMES.includes(normalized)) {
            return true;
        }

        // Check if starts with a compound name
        for (const compound of COMPOUND_NAMES) {
            if (normalized === compound || normalized.startsWith(compound + ' ')) {
                return true;
            }
        }

        // Check for particle-based compound names
        const particles = ['DE', 'DEL', 'DELA', 'DELOS', 'SAN', 'SANTA', 'VILLA', 'MONTE', 'BUEN', 'LA', 'LOS'];
        if (words.length >= 2) {
            const firstWord = words[0].toUpperCase().trim();
            if (particles.includes(firstWord)) {
                const secondWord = words[1].toUpperCase().trim();
                const prepositions = ['A', 'AN', 'THE', 'AT', 'IN', 'ON', 'OF', 'TO'];
                if (!prepositions.includes(secondWord) && secondWord.length >= 3) {
                    return true;
                }
            }
        }

        // Handle "DE LOS" or "DE LA" (2-word particles)
        if (words.length >= 3) {
            const firstTwo = words.slice(0, 2).map(w => w.toUpperCase().trim()).join(' ');
            if (['DE LOS', 'DE LA'].includes(firstTwo)) {
                return true;
            }
        }

        return false;
    }

    // Driver's License Compound Middle Name Validation
    function validateDriverLicenseCompoundMiddleName(firstName, extractedMiddleName) {

        // If no middle name or single word, return as-is
        if (!extractedMiddleName || extractedMiddleName.trim() === '') {
            return { firstName, middleName: extractedMiddleName };
        }

        const middleWords = extractedMiddleName.split(/\s+/).filter(w => w.length > 0);

        // Single word middle name - no validation needed
        if (middleWords.length === 1) {
            return { firstName, middleName: extractedMiddleName };
        }


        // Check if the entire middle name is in COMPOUND_NAMES
        const fullMiddleNameUpper = extractedMiddleName.toUpperCase().trim();
        const isInCompoundList = COMPOUND_NAMES.includes(fullMiddleNameUpper);

        if (isInCompoundList) {
            return { firstName, middleName: extractedMiddleName };
        }


        // Check for partial compound matches
        let validCompoundPart = '';
        let remainingWords = [];

        // Try 3-word compound first
        if (middleWords.length >= 3) {
            const first3Words = middleWords.slice(0, 3);
            const first3Compound = first3Words.join(' ').toUpperCase();

            if (COMPOUND_NAMES.includes(first3Compound)) {
                validCompoundPart = first3Words.join(' ');
                remainingWords = middleWords.slice(3);
            }
        }

        // Try 2-word compound if no 3-word match
        if (!validCompoundPart && middleWords.length >= 2) {
            const first2Words = middleWords.slice(0, 2);
            const first2Compound = first2Words.join(' ').toUpperCase();

            if (COMPOUND_NAMES.includes(first2Compound)) {
                validCompoundPart = first2Words.join(' ');
                remainingWords = middleWords.slice(2);
            }
        }

        if (validCompoundPart) {
            const correctedMiddleName = validCompoundPart;
            let correctedFirstName = firstName;

            if (remainingWords.length > 0) {
                const wordsToMove = remainingWords.join(' ');
                correctedFirstName = correctedFirstName + ' ' + wordsToMove;
            }


            return {
                firstName: correctedFirstName.trim(),
                middleName: correctedMiddleName
            };
        }

        // No valid compound found - extract last word as middle name, move rest to first name

        const actualMiddleName = middleWords[middleWords.length - 1];
        const wordsToFirstname = middleWords.slice(0, -1).join(' ');

        let correctedFirstName = firstName;
        if (wordsToFirstname) {
            correctedFirstName = correctedFirstName + ' ' + wordsToFirstname;
        }


        return {
            firstName: correctedFirstName.trim(),
            middleName: actualMiddleName
        };
    }

    // Field Label Mappings - Core of Key-Value Pairing System
    const FIELD_LABELS = {
        lastName: [
            'LAST NAME', 'LASTNAME', 'SURNAME', 'FAMILY NAME', 'LAST',
            'APELYIDO', 'APELYEDO', 'HULING PANGALAN', 'HULING NGALAN',
            'PANGALAN (APELYIDO)', 'APELYIDO/SURNAME'
        ],
        firstName: [
            'FIRST NAME', 'FIRSTNAME', 'GIVEN NAME', 'GIVEN NAMES', 'GIVENNAME', 'FIRST',
            'PANGALAN', 'UNANG PANGALAN', 'MGA PANGALAN', 'PANGALAN (UNA)',
            'PANGALAN/GIVEN NAME', 'PANGALAN/NAME', 'GIVEN', 'UNANG NGALAN',
            'BINANSAGANG PANGALAN'
        ],
        middleName: [
            'MIDDLE NAME', 'MIDDLENAME', 'MIDDLE INITIAL', 'M.I.', 'MID NAME', 'M NAME',
            'MIDDLE', 'GITNA',
            'GITNANG PANGALAN', 'GITNANG APELYIDO', 'GITNANG NGALAN', 'GITNANG',
            'PANGALAN (GITNA)', 'PANGALAN/MIDDLE NAME',
            'MIDDLE/GITNANG', 'GITNANG/MIDDLE', 'MIDDLE NAME/GITNANG PANGALAN'
        ],
        fullName: [
            'FULL NAME', 'COMPLETE NAME', 'NAME', 'HOLDER NAME',
            'BUONG PANGALAN', 'KUMPLETONG PANGALAN', 'PANGALAN NG MAY-ARI',
            'PANGALAN/NAME', 'PANGALAN NG CARD HOLDER'
        ],
        birthdate: [
            'DATE OF BIRTH', 'BIRTH DATE', 'BIRTHDATE', 'DOB', 'BIRTHDAY', 'BIRTH',
            'DATE OF BIRTH/PETSA', 'BIRTH (MM/DD/YYYY)',
            'PETSA NG KAPANGANAKAN', 'KAPANGANAKAN', 'PETSA NG PAGSILANG',
            'ARAW NG KAPANGANAKAN', 'IPINANGANAK',
            'KAPANGANAKAN/DATE OF BIRTH', 'PETSA/DATE'
        ],
        gender: [
            'SEX', 'GENDER', 'SEX/KASARIAN',
            'KASARIAN', 'KASARIAN/SEX',
            'KASARIAN (SEX)', 'SEX (M/F)', 'KASARIAN/GENDER'
        ],
        address: [
            'ADDRESS', 'RESIDENCE', 'RESIDENTIAL ADDRESS', 'HOME ADDRESS',
            'PRESENT ADDRESS', 'PERMANENT ADDRESS', 'STREET ADDRESS',
            'TIRAHAN', 'LUGAR', 'LUGAR NG TIRAHAN', 'TIRAHAN/ADDRESS',
            'TAHANAN', 'ADDRESS/TIRAHAN',
            'KASALUKUYANG TIRAHAN', 'PERMANENTENG TIRAHAN'
        ],
        city: [
            'CITY', 'CITY/MUNICIPALITY', 'MUNICIPALITY', 'CITY/MUNIC',
            'LUNGSOD', 'BAYAN', 'MUNISIPALIDAD', 'LUNGSOD/BAYAN',
            'LUNGSOD/CITY', 'CITY OR MUNICIPALITY', 'LUNSOD/MUNISIPALIDAD'
        ],
        idNumber: [
            'ID NUMBER', 'ID NO', 'ID #', 'NUMBER', 'NO.', 'ID',
            'LICENSE NO', 'LICENSE NUMBER', 'DL NO', 'LICENSE #',
            'LISENSYA BLGD', 'DRIVER LICENSE NO',
            'PHILSYS NUMBER', 'PSN', 'PHIL-SYS NO', 'PHILSYS NO',
            'PCN', 'NATIONAL ID NUMBER', 'NATIONAL ID NO',
            'UMID NUMBER', 'UMID NO', 'CRN', 'CARD REFERENCE NUMBER',
            'UMID CRN'
        ],
        nationality: [
            'NATIONALITY', 'CITIZEN', 'CITIZENSHIP',
            'NASYONALIDAD', 'PAGKAMAMAMAYAN', 'BANSA',
            'NASYONALIDAD/NATIONALITY', 'CITIZENSHIP/NASYONALIDAD'
        ],
        civilStatus: [
            'CIVIL STATUS', 'MARITAL STATUS', 'STATUS', 'MAR STATUS',
            'KATAYUANG SIBIL', 'LAGAY NG PAG-AASAWA', 'KATAYUAN',
            'ESTADO SIBIL',
            'KATAYUANG SIBIL/CIVIL STATUS', 'CIVIL STATUS/KATAYUAN'
        ],
        suffix: [
            'SUFFIX', 'NAME SUFFIX', 'EXT', 'EXTENSION',
            'HULAPI', 'KARUGTONG', 'SUFFIX/HULAPI'
        ],
        bloodType: [
            'BLOOD TYPE', 'BLOOD', 'BLOOD GROUP', 'BT',
            'URI NG DUGO', 'DUGO', 'BLOOD TYPE/URI NG DUGO'
        ],
        height: [
            'HEIGHT', 'HT', 'HGT',
            'TAAS', 'TANGKAD', 'HEIGHT/TAAS'
        ],
        weight: [
            'WEIGHT', 'WT', 'WGT',
            'TIMBANG', 'BIGAT', 'WEIGHT/TIMBANG'
        ],
        placeOfBirth: [
            'PLACE OF BIRTH', 'BIRTH PLACE', 'POB', 'BIRTHPLACE',
            'LUGAR NG KAPANGANAKAN', 'PINANGANAK SA', 'LUGAR NG PAGSILANG',
            'LUGAR NG KAPANGANAKAN/PLACE OF BIRTH', 'PLACE OF BIRTH/LUGAR'
        ],
        barangay: [
            'BARANGAY', 'BRGY', 'BRGY.', 'BARANGGAY',
            'BARANGGAY', 'BRGY/BARANGAY'
        ],
        street: [
            'STREET', 'ST', 'ST.', 'STREET ADDRESS', 'HOUSE NO',
            'BLOCK', 'LOT', 'BLOCK/LOT', 'BLK', 'BLK/LOT',
            'KALYE', 'KALSADA', 'NUMERO NG BAHAY'
        ],
        subdivision: [
            'SUBDIVISION', 'SUBDV', 'SUBD', 'VILLAGE', 'VILL',
            'SUBDIBISYON', 'PAMAYANAN'
        ],
        district: [
            'DISTRICT', 'DIST', 'DIST.', 'ZONE',
            'DISTRITO', 'DISTRITO/DISTRICT'
        ],
        province: [
            'PROVINCE', 'PROV', 'PROV.', 'PROVINCIAL',
            'PROBINSYA', 'LALAWIGAN', 'PROBINSYA/PROVINCE'
        ]
    };

    // Helper function to check if text contains "unusual words" (actual data, not labels)
    function containsUnusualWords(text) {
        if (!text || text.length < 2) return false;

        const upperText = text.toUpperCase().trim();

        // List of common COMPLETE label words that should NOT appear in actual data
        const commonLabelWords = [
            'NAME', 'PANGALAN', 'APELYIDO', 'SURNAME', 'FIRST', 'LAST', 'MIDDLE', 'GIVEN',
            'KASARIAN', 'SEX', 'GENDER', 'BIRTHDATE', 'KAPANGANAKAN', 'ADDRESS', 'TIRAHAN',
            'PETSA', 'DATE', 'BIRTH', 'NGALAN', 'UNANG', 'HULING', 'GITNANG',
            'NUMBER', 'NUMERO', 'IDENTIFICATION', 'NATIONALITY', 'NACIONALIDAD'
        ];

        const words = upperText.split(/\s+/).filter(w => w.length > 1);

        // If no words, reject
        if (words.length === 0) return false;

        // IMPROVED MATCHING: Use exact word matching, not substring matching
        const labelWordsCount = words.filter(word => {
            // Exact match check
            if (commonLabelWords.includes(word)) {
                return true;
            }

            // For longer words (6+ chars), check if they're mostly a label word
            if (word.length >= 6) {
                for (const label of commonLabelWords) {
                    if (label.length >= 4 && word.includes(label) && label.length >= word.length * 0.6) {
                        return true;
                    }
                }
            }

            return false;
        }).length;

        // SPECIAL CASE: Single word that's not an exact label match is likely actual data
        if (words.length === 1 && labelWordsCount === 0) {
            return true;
        }

        // If less than 30% of words are label-related, it's likely actual data
        const unusualWordPercentage = ((words.length - labelWordsCount) / words.length) * 100;

        if (unusualWordPercentage >= 70) {
            return true;
        }

        return false;
    }

    // Filter non-name words (Common OCR artifacts, noise, and filler words)
    function isNonNameWord(word) {
        if (!word || word.trim() === '') return true;

        const cleaned = word.trim().toUpperCase();

        // List of common noise words that appear in OCR but are NOT names
        const noiseWords = [
            // Common English articles, prepositions, and conjunctions
            'THE', 'A', 'AN', 'AND', 'OR', 'BUT', 'IN', 'ON', 'AT', 'TO', 'FOR',
            'OF', 'WITH', 'BY', 'FROM', 'AS', 'IS', 'WAS', 'ARE', 'BE', 'BEEN',

            // Filipino common words (not names)
            'NG', 'SA', 'ANG', 'MGA', 'NA', 'AT', 'O', 'KUNG', 'PARA',

            // Government/Organizational/Institutional words (NOT personal names)
            'DEPARTMENT', 'OFFICE', 'BUREAU', 'AGENCY', 'MINISTRY', 'COMMISSION',
            'TRANSPORTATION', 'TRANSPORT', 'MUNICIPAL', 'CITY', 'PROVINCIAL', 'NATIONAL',
            'REPUBLIC', 'GOVERNMENT', 'PUBLIC', 'SERVICES', 'SERVICE', 'ADMINISTRATION',
            'AFFAIRS', 'AUTHORITY', 'CORPORATION', 'COMPANY', 'INCORPORATED', 'LIMITED',
            'DIVISION', 'SECTION', 'UNIT', 'BRANCH', 'REGIONAL', 'LOCAL',
            'KAGAWARAN', 'TANGGAPAN', 'PAMAHALAAN', 'PAMBANSA', 'LUNGSOD',

            // License/ID related words
            'LICENSE', 'CARD', 'IDENTIFICATION', 'DOCUMENT', 'PERMIT', 'CERTIFICATE',
            'DRIVER', 'DRIVERS', 'PROFESSIONAL', 'NON-PROFESSIONAL', 'RESTRICTION',
            'LISENSYA', 'KARD', 'PAGKAKAKILANLAN',

            // Common OCR artifacts
            'I', 'II', 'III', 'IV', 'V', 'VI', 'VII', 'VIII', 'IX', 'X',
            'L', 'C', 'D', 'M', // Roman numerals that might appear alone

            // Common ID field indicators that leak into names
            'NO', 'NUMBER', 'ID', 'DATE', 'EXPIRY', 'ISSUED', 'VALID', 'EXPIRES',
            'UNTIL', 'CONDITION', 'RESTRICTIONS', 'CODE', 'TYPE', 'CLASS',

            // Punctuation-only or special chars (sometimes extracted)
            '.', ',', '-', '/', '\\', '|', ':', ';', '!', '?', '@', '#', '$', '%', '^', '&', '*',

            // Numbers as words (unlikely in names but can appear as OCR errors)
            'ONE', 'TWO', 'THREE', 'FOUR', 'FIVE', 'SIX', 'SEVEN', 'EIGHT', 'NINE', 'TEN'
        ];

        // Check if word is in noise list
        if (noiseWords.includes(cleaned)) {
            return true;
        }

        // Check if word is a single character (likely OCR error)
        if (cleaned.length === 1) {
            return true;
        }

        // Check if word contains only punctuation or special characters
        if (/^[A-Za-z]+$/.test(cleaned)) {
            return false; // Valid alphabetic word
        }

        // Check if word contains numbers (names shouldn't have digits)
        if (/\d/.test(cleaned)) {
            return true;
        }

        return true; // If not purely alphabetic, filter it out
    }

    // Clean and filter name words, removing noise
    function cleanNameWords(words) {
        const cleaned = words
            .map(w => w.trim())
            .filter(w => w.length > 0)
            .filter(w => !isNonNameWord(w));

        if (cleaned.length !== words.length) {
            const filtered = words.filter(w => isNonNameWord(w));
        }

        return cleaned;
    }

    // Helper function to check if extracted text is actually a label (should be rejected)
    function isExtractedTextALabel(text) {
        if (!text || text.length < 2) return true;

        const allLabels = Object.values(FIELD_LABELS).flat();
        const upperText = text.toUpperCase().trim();

        // Check if the entire text is a label (exact match)
        if (allLabels.some(label => upperText === label)) {
            return true;
        }

        // Check if text is mostly labels (more than 60% is label words)
        const words = upperText.split(/\s+/).filter(w => w.length > 1);
        const labelWords = words.filter(word => {
            // Check if this word exactly matches any label or is contained in a label as a complete word
            return allLabels.some(label => {
                const labelWords = label.split(/\s+/);
                return labelWords.includes(word) || label === word;
            });
        });

        // More lenient threshold (60% instead of 50%) to reduce false positives
        if (labelWords.length > words.length * 0.6) {
            return true;
        }

        // Comprehensive list of label-only terms (English + Filipino)
        const labelOnlyTerms = [
            // English name labels
            'NAME', 'SURNAME', 'FIRST', 'LAST', 'MIDDLE', 'GIVEN', 'FULL', 'COMPLETE',
            'FIRSTNAME', 'LASTNAME', 'MIDDLENAME', 'FULLNAME', 'GIVENNAME', 'FAMILYNAME',
            // Filipino name labels
            'PANGALAN', 'APELYIDO', 'APELYEDO', 'NGALAN', 'UNANG', 'HULING', 'GITNANG',
            'BUONG', 'KUMPLETONG', 'BINANSAGANG',
            // Combined formats
            'UNANG PANGALAN', 'HULING PANGALAN', 'GITNANG PANGALAN',
            'FIRST NAME', 'LAST NAME', 'MIDDLE NAME', 'GIVEN NAME', 'FAMILY NAME',
            // Gender labels
            'KASARIAN', 'SEX', 'GENDER',
            // Other field labels
            'KAPANGANAKAN', 'BIRTHDATE', 'BIRTH', 'TIRAHAN', 'ADDRESS',
            'PETSA', 'DATE', 'LUNGSOD', 'CITY', 'BAYAN'
        ];

        // Check if text is a label-only term
        if (labelOnlyTerms.includes(upperText)) {
            return true;
        }

        // Reject if text contains ONLY label patterns (no actual name content)
        const labelOnlyPatterns = [
            /^(APELYIDO|PANGALAN|SURNAME|NAME|FIRST|LAST|MIDDLE|GIVEN)[\s\/\-:]*$/i,
            /^(KASARIAN|SEX|GENDER)[\s\/\-:]*$/i,
            /^(KAPANGANAKAN|BIRTHDATE|BIRTH|PETSA|DATE)[\s\/\-:]*$/i,
            /^(TIRAHAN|ADDRESS|LUGAR)[\s\/\-:]*$/i,
            /^(NGALAN|APELYEDO|UNANG|HULING|GITNANG|BUONG)[\s\/\-:]*$/i
        ];

        if (labelOnlyPatterns.some(pattern => pattern.test(upperText))) {
            return true;
        }

        // Additional check: if ALL words in the text are exact label-only terms
        const allWordsAreLabels = words.every(word => labelOnlyTerms.includes(word));

        if (allWordsAreLabels && words.length > 0) {
            return true;
        }

        // Passed all checks - this is likely actual data, not a label
        return false;
    }

    // Validate extracted name data to ensure no labels are present
    function validateExtractedNames(extractedData) {
        const issues = [];


        // Check firstName
        if (!extractedData.firstName || extractedData.firstName.trim() === '') {
            issues.push('First Name is empty');
        } else if (isExtractedTextALabel(extractedData.firstName)) {
            issues.push(`First Name contains label: "${extractedData.firstName}"`);
        } else if (extractedData.firstName.length < 2) {
            issues.push(`First Name too short: "${extractedData.firstName}"`);
        } else {
        }

        // Check lastName
        if (!extractedData.lastName || extractedData.lastName.trim() === '') {
            issues.push('Last Name is empty');
        } else if (isExtractedTextALabel(extractedData.lastName)) {
            issues.push(`Last Name contains label: "${extractedData.lastName}"`);
        } else if (extractedData.lastName.length < 2) {
            issues.push(`Last Name too short: "${extractedData.lastName}"`);
        } else {
        }

        // Check middleName (REQUIRED FIELD - must be present and valid)
        if (!extractedData.middleName || extractedData.middleName.trim() === '') {
            issues.push('Middle Name is REQUIRED but was not found - please ensure ID shows middle name clearly');
        } else if (isExtractedTextALabel(extractedData.middleName)) {
            issues.push(`Middle Name contains label: "${extractedData.middleName}"`);
        } else if (extractedData.middleName.length < 2) {
            issues.push(`Middle Name too short: "${extractedData.middleName}" (minimum 2 characters required)`);
        } else {
        }

        // Check suffix (optional, so only validate if present)
        if (extractedData.suffix && extractedData.suffix.trim() !== '') {
            if (isExtractedTextALabel(extractedData.suffix)) {
                issues.push(`Suffix contains label: "${extractedData.suffix}"`);
            } else {
            }
        } else {
        }

        const valid = issues.length === 0;

        if (valid) {
        } else {
            // Validation failed
        }


        return { valid, issues };
    }

    // Smart label-value extraction function - Core of Key-Value Pairing System
    function extractFieldValue(lines, fieldType) {
        const labels = FIELD_LABELS[fieldType];
        if (!labels) {
            return null;
        }


        for (let i = 0; i < lines.length; i++) {
            const line = lines[i].trim();
            const upperLine = line.toUpperCase();

            // Check if line contains any of the field labels
            for (const label of labels) {
                if (upperLine.includes(label)) {

                    // PRIORITY METHOD 1: Value on NEXT line (Philippine ID standard format)
                    const singleLabelWords = ['FIRST', 'LAST', 'MIDDLE', 'GIVEN', 'SURNAME',
                        'NAME', 'APELYIDO', 'PANGALAN', 'GITNANG',
                        'UNANG', 'HULING', 'NGALAN', 'APELYEDO',
                        'FIRSTNAME', 'LASTNAME', 'MIDDLENAME',
                        'GIVENNAME', 'FAMILYNAME', 'FULLNAME'];

                    // Check up to 5 lines after the label to find the actual value
                    for (let offset = 1; offset <= 5 && (i + offset) < lines.length; offset++) {
                        const candidateLine = lines[i + offset].trim();

                        // Skip empty lines
                        if (!candidateLine || candidateLine.length === 0) {
                            continue;
                        }

                        // CRITICAL CHECK 1: Reject exact label word matches
                        const candidateUpper = candidateLine.toUpperCase().trim();
                        const isSingleLabelWord = singleLabelWords.includes(candidateUpper);

                        if (isSingleLabelWord) {
                            continue;
                        }

                        // CRITICAL CHECK 2: Reject if line contains ONLY label words
                        const candidateWords = candidateLine.split(/\s+/);
                        const allWordsAreLabels = candidateWords.every(word => {
                            const upperWord = word.toUpperCase().trim();
                            return singleLabelWords.includes(upperWord);
                        });

                        if (allWordsAreLabels) {
                            continue;
                        }

                        // CRITICAL CHECK 3: Validate not a label using main function
                        const isLineLabel = isExtractedTextALabel(candidateLine);

                        if (isLineLabel) {
                            break;
                        }

                        // CRITICAL CHECK 4: Minimum length requirement
                        if (candidateLine.length === 1 && !/^[A-Z]$/i.test(candidateLine)) {
                            continue;
                        }

                        // PASSED ALL CHECKS - This is the actual value
                        return { value: candidateLine, method: 'next-line', label: label };
                    }


                    // METHOD 2: Value on FOLLOWING line (extended search)
                    for (let j = i + 6; j < Math.min(i + 11, lines.length); j++) {
                        const followingLine = lines[j].trim();

                        if (!followingLine || followingLine.length === 0) {
                            continue;
                        }

                        // CRITICAL CHECK 1: Reject exact label words
                        const followingLineUpper = followingLine.toUpperCase().trim();
                        const isSingleLabelWord = singleLabelWords.includes(followingLineUpper);

                        if (isSingleLabelWord) {
                            continue;
                        }

                        // CRITICAL CHECK 2: Reject if contains only label words
                        const followingWords = followingLine.split(/\s+/);
                        const allWordsAreLabels = followingWords.every(word => {
                            return singleLabelWords.includes(word.toUpperCase().trim());
                        });

                        if (allWordsAreLabels) {
                            continue;
                        }

                        // CRITICAL CHECK 3: Not a label
                        const isLabel = isExtractedTextALabel(followingLine);

                        if (isLabel) {
                            continue;
                        }

                        // PASSED CHECKS
                        return { value: followingLine, method: 'following-line', label: label };
                    }

                    // METHOD 3: Value on SAME line (LAST RESORT - uncommon for Philippine IDs)
                    let valueOnSameLine = line
                        .replace(new RegExp(label, 'i'), '')  // Remove the label
                        .replace(/^[\s:\/\-\|]+/, '')          // Remove leading separators
                        .replace(/[\s:\/\-\|]+$/, '')          // Remove trailing separators
                        .trim();

                    // Remove other field-specific labels
                    for (const otherLabel of labels) {
                        if (otherLabel !== label) {
                            valueOnSameLine = valueOnSameLine
                                .replace(new RegExp(otherLabel, 'i'), '')
                                .replace(/^[\s:\/\-\|]+/, '')
                                .trim();
                        }
                    }

                    // Remove ALL known labels
                    const allLabels = Object.values(FIELD_LABELS).flat();
                    for (const anyLabel of allLabels) {
                        const labelRegex = new RegExp(`\\b${anyLabel}\\b`, 'i');
                        valueOnSameLine = valueOnSameLine.replace(labelRegex, '').trim();
                    }

                    // Final cleanup
                    valueOnSameLine = valueOnSameLine
                        .replace(/^[\s:\/\-\|]+/, '')
                        .replace(/[\s:\/\-\|]+$/, '')
                        .trim();

                    // Strict validation for same-line extraction
                    const isStillALabel = isExtractedTextALabel(valueOnSameLine);
                    const hasUnusualWords = containsUnusualWords(valueOnSameLine);

                    if (valueOnSameLine &&
                        valueOnSameLine.length > 2 &&
                        !isStillALabel &&
                        hasUnusualWords) {
                        return { value: valueOnSameLine, method: 'same-line', label: label };
                    } else {
                    }

                    break; // Try next label variant
                }
            }
        }

        return null;
    }

    // Extract First Name (with Middle Name and Suffix parsing)
    function extractFirstName(lines) {

        // Try using extractFieldValue first
        const result = extractFieldValue(lines, 'firstName');
        if (result && result.value) {
            const rawValue = cleanName(result.value);

            // Additional validation: reject if still contains labels
            if (isExtractedTextALabel(rawValue)) {
                return { firstName: "", middleName: "", suffix: "" };
            }

            // Parse to separate first name from middle name and suffix
            const words = rawValue.split(/\s+/).filter(w => w.length > 0);

            if (words.length > 0) {
                const parsed = parseFirstMiddleSuffix(words);

                // Final validation: ensure we have actual name data
                if (!parsed.firstName || parsed.firstName.length < 2) {
                    return { firstName: "", middleName: "", suffix: "" };
                }

                // Return all three since they're parsed together
                return {
                    firstName: parsed.firstName,
                    middleName: parsed.middleName,
                    suffix: parsed.suffix
                };
            }
        }

        return { firstName: "", middleName: "", suffix: "" };
    }

    // Independent extraction function for Middle Name
    function extractMiddleName(lines, alreadyParsedFromFirstName = "") {

        // If already extracted from first name field, use that
        if (alreadyParsedFromFirstName) {
            return alreadyParsedFromFirstName;
        }

        // Try using extractFieldValue for separate middle name field
        const result = extractFieldValue(lines, 'middleName');
        if (result && result.value) {
            let value = cleanName(result.value);

            // Additional validation: reject if still contains labels
            if (isExtractedTextALabel(value)) {
                return "";
            }

            // Validate length
            if (value.length < 2) {
                return "";
            }

            return value;
        }

        return "";
    }

    // Independent extraction function for Suffix
    // CASE-INSENSITIVE: Handles all letter casings (jr, Jr, JR, jR all → "JR")
    function extractSuffix(lines, alreadyParsedFromFirstName = "") {

        // If already extracted from first name field, use that
        if (alreadyParsedFromFirstName) {
            return alreadyParsedFromFirstName;
        }

        // Try using extractFieldValue for labeled suffix field FIRST
        const result = extractFieldValue(lines, 'suffix');
        if (result && result.value) {
            let value = cleanName(result.value);
            const upperValue = value.toUpperCase().trim(); // Convert to uppercase for matching

            // Validate it's actually a suffix from VALID_SUFFIXES list (not a label or other text)
            if (VALID_SUFFIXES.includes(upperValue) || VALID_SUFFIXES.includes(upperValue + '.')) {
                // Normalize using mapping or keep as-is (always returns uppercase)
                return SUFFIX_NORMALIZATION[upperValue] || upperValue;
            } else {
                // Not a valid suffix, return empty string
            }
        }

        // Fallback: Try finding suffix patterns in lines (create regex from VALID_SUFFIXES)
        // Regex flag 'i' makes it case-insensitive (matches jr, Jr, JR, jR, etc.)
        const suffixPattern = new RegExp('\\b(' + VALID_SUFFIXES.filter(s => !s.includes('.')).join('|') + ')\\b', 'i');

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const match = line.match(suffixPattern);

            if (match) {
                let suffix = match[1].toUpperCase(); // Convert to uppercase for normalization

                // STRICT VALIDATION: Check context
                const upperLine = line.toUpperCase();

                // REJECTION 1: Reject if it's part of a label or instruction
                const rejectPatterns = [
                    /SUFFIX/i,
                    /HULAPI/i,
                    /EXTENSION/i,
                    /IF APPLICABLE/i,
                    /OPTIONAL/i,
                    /EXAMPLE/i,
                    /SAMPLE/i,
                    /N\/A/i,
                    /NA/i
                ];

                const isInLabel = rejectPatterns.some(pattern => upperLine.match(pattern));

                if (isInLabel) {
                    continue;
                }

                // REJECTION 2: Reject if it's a Driver's License restriction code
                const isRestrictionCode = /RESTRICTION|RESTRICTIONS|COND|CONDITIONS|CODE/i.test(upperLine);
                if (isRestrictionCode) {
                    continue;
                }

                // REJECTION 3: Reject if line contains numbers (likely ID numbers, dates, or codes)
                const hasNumbers = /\d/.test(line);
                if (hasNumbers && suffix.match(/^(II|III|IV|V)$/)) {
                    continue;
                }

                // REJECTION 4: Check if line contains actual name-like words
                const nameWords = line.match(/\b[A-Z][a-z]{2,}\b/g) || [];
                const hasMultipleNameWords = nameWords.length >= 2;

                if (!hasMultipleNameWords) {
                    continue;
                }

                // REJECTION 5: For "III" specifically, require very strong name context
                if (suffix === 'III') {
                    // Must have at least 3 name-like words
                    if (nameWords.length < 3) {
                        continue;
                    }

                    // Must be at the end of the line or followed only by spaces
                    const suffixIndex = line.toUpperCase().lastIndexOf('III');
                    const afterSuffix = line.substring(suffixIndex + 3).trim();
                    if (afterSuffix.length > 0) {
                        continue;
                    }
                }

                // VALIDATION PASSED: This appears to be a legitimate suffix
                // Normalize using mapping or keep as-is
                suffix = SUFFIX_NORMALIZATION[suffix] || suffix;

                return suffix;
            }
        }

        return "";
    }

    // Independent extraction function for Civil Status
    function extractCivilStatus(lines) {

        // Try using extractFieldValue first for labeled fields
        const result = extractFieldValue(lines, 'civilStatus');
        if (result && result.value) {
            let value = result.value.toUpperCase().trim();

            // Remove any remaining Filipino/English label fragments
            value = value
                .replace(/KATAYUANG SIBIL/g, '')
                .replace(/CIVIL STATUS/g, '')
                .replace(/MARITAL STATUS/g, '')
                .replace(/STATUS/g, '')
                .replace(/KATAYUAN/g, '')
                .replace(/[\/:]/g, '')
                .trim();


            // Normalize the value to standard civil status values
            // Single
            if (value.includes('SINGLE') || value.includes('SOLO') || value.includes('WALANG ASAWA')) {
                return 'Single';
            }
            // Married
            if (value.includes('MARRIED') || value.includes('KASAL') || value.includes('MAY ASAWA')) {
                return 'Married';
            }
            // Widowed
            if (value.includes('WIDOW') || value.includes('BIYUDO') || value.includes('BIYUDA')) {
                return 'Widowed';
            }
            // Separated
            if (value.includes('SEPARATED') || value.includes('HIWALAY')) {
                return 'Separated';
            }
            // Divorced (rare in Philippines but possible)
            if (value.includes('DIVORCED') || value.includes('DIBORSYADO')) {
                return 'Divorced';
            }

            // Return the cleaned value even if not normalized
            return value;
        }

        // Fallback: Search all lines for civil status patterns
        for (const line of lines) {
            const upper = line.toUpperCase().trim();

            // Check for specific civil status values
            if (/\b(SINGLE|SOLO|WALANG ASAWA)\b/.test(upper)) {
                return 'Single';
            }
            if (/\b(MARRIED|KASAL|MAY ASAWA)\b/.test(upper)) {
                return 'Married';
            }
            if (/\b(WIDOW|WIDOWED|BIYUDO|BIYUDA)\b/.test(upper)) {
                return 'Widowed';
            }
            if (/\b(SEPARATED|HIWALAY)\b/.test(upper)) {
                return 'Separated';
            }
        }

        return "";
    }

    // Independent extraction function for Gender/Sex
    function extractGender(lines) {

        // Try using extractFieldValue first for labeled fields
        const result = extractFieldValue(lines, 'gender');
        if (result && result.value) {
            let value = result.value.toUpperCase().trim();

            // Remove any remaining Filipino/English label fragments
            value = value
                .replace(/KASARIAN/g, '')
                .replace(/GENDER/g, '')
                .replace(/SEX/g, '')
                .replace(/[\/:]/g, '')
                .trim();


            // Normalize the value to Male/Female
            // Check for Male indicators (M, MALE, LALAKI)
            if (/\bM\b/.test(value) || value.includes('MALE') || value.includes('LALAKI')) {
                return 'Male';
            }
            // Check for Female indicators (F, FEMALE, BABAE)
            if (/\bF\b/.test(value) || value.includes('FEMALE') || value.includes('BABAE')) {
                return 'Female';
            }

        }


        // Fallback: Search all lines for gender patterns
        for (const line of lines) {
            const upper = line.toUpperCase().trim();

            // Check for explicit gender indicators
            if (/\bSEX\b.*\bM\b/.test(upper) ||
                /\bGENDER\b.*\bM\b/.test(upper) ||
                /\bKASARIAN\b.*\bM\b/.test(upper) ||
                /\bSEX\b.*\bMALE\b/.test(upper) ||
                /\bGENDER\b.*\bMALE\b/.test(upper) ||
                /\bSEX\b.*\bLALAKI\b/.test(upper) ||
                upper === 'M' ||
                upper === 'MALE' ||
                upper === 'LALAKI') {
                return 'Male';
            }

            if (/\bSEX\b.*\bF\b/.test(upper) ||
                /\bGENDER\b.*\bF\b/.test(upper) ||
                /\bKASARIAN\b.*\bF\b/.test(upper) ||
                /\bSEX\b.*\bFEMALE\b/.test(upper) ||
                /\bGENDER\b.*\bFEMALE\b/.test(upper) ||
                /\bSEX\b.*\bBABAE\b/.test(upper) ||
                upper === 'F' ||
                upper === 'FEMALE' ||
                upper === 'BABAE') {
                return 'Female';
            }
        }

        return "";
    }

    // Enhanced: Parse First Name + Middle Name + Suffix from combined field
    function parseFirstMiddleSuffix(words, preferAllAsFirstName = false) {
        let firstName = '';
        let middleName = '';
        let suffix = '';

        // Validate input
        if (!words || words.length === 0) {
            return { firstName, middleName, suffix };
        }


        // === STEP 1: Check for suffix at the end (ONLY from VALID_SUFFIXES list) ===
        // CASE-INSENSITIVE: All suffix matching is case-insensitive
        // Examples: "jr", "Jr", "JR", "jR" all match and normalize to "JR"
        //           "iii", "III", "Iii" all match and normalize to "III"
        let workingWords = [...words];  // Create copy to avoid modifying original

        const lastWord = workingWords[workingWords.length - 1].toUpperCase().replace(/\./g, '').trim();

        // Check if the last word is a valid suffix from VALID_SUFFIXES (case-insensitive)
        if (VALID_SUFFIXES.includes(lastWord) || VALID_SUFFIXES.includes(lastWord + '.')) {
            // Normalize suffix using mapping or keep as-is
            // Result will always be uppercase (JR, SR, I, II, III, etc.) to match dropdown values
            suffix = SUFFIX_NORMALIZATION[lastWord] || lastWord;
            workingWords = workingWords.slice(0, -1);  // Remove suffix from working words
        }
        // If not in valid suffixes list, leave suffix empty (will become "None" in the form)

        // === STEP 2: Handle edge case - no words left after suffix removal ===
        if (workingWords.length === 0) {
            return { firstName: '', middleName: '', suffix };
        }

        // === STEP 3: Handle preferAllAsFirstName flag ===
        if (preferAllAsFirstName) {
            firstName = workingWords.join(' ');
            return { firstName, middleName: '', suffix };
        }

        // === STEP 4: NEW LOGIC - Check for COMPOUND middle names ONLY from COMPOUND_NAMES list ===

        // Check for 3-word compound middle name (must be in COMPOUND_NAMES)
        if (workingWords.length >= 4) {  // Need at least 4: 1 first + 3 middle
            const last3Words = workingWords.slice(-3);
            const potentialCompound = last3Words.join(' ').toUpperCase();


            if (isCompoundName(last3Words)) {
                middleName = last3Words.join(' ');
                firstName = workingWords.slice(0, -3).join(' ');
                return { firstName, middleName, suffix };
            } else {
            }
        }

        // Check for 2-word compound middle name (must be in COMPOUND_NAMES)
        if (workingWords.length >= 3) {  // Need at least 3: 1 first + 2 middle
            const last2Words = workingWords.slice(-2);
            const potentialCompound = last2Words.join(' ').toUpperCase();


            if (isCompoundName(last2Words)) {
                middleName = last2Words.join(' ');
                firstName = workingWords.slice(0, -2).join(' ');
                return { firstName, middleName, suffix };
            } else {
            }
        }

        // === STEP 5: Check for middle initial (single letter or letter with period) ===
        if (workingWords.length >= 2) {
            const lastWord = workingWords[workingWords.length - 1].replace(/\./g, '');
            if (lastWord.length === 1 && /^[A-Z]$/i.test(lastWord)) {
                middleName = lastWord.toUpperCase();
                firstName = workingWords.slice(0, -1).join(' ');
                return { firstName, middleName, suffix };
            }
        }

        // === STEP 6: STANDARD PARSING - Last word is SINGLE middle name ===

        if (workingWords.length >= 2) {
            // Last word is SINGLE middle name (not compound)
            middleName = workingWords[workingWords.length - 1];
            // All other words are first name
            firstName = workingWords.slice(0, -1).join(' ');

        } else {
            // Only one word - it's the first name, no middle name detected
            firstName = workingWords[0];
        }

        return { firstName, middleName, suffix };
    }

    // Complete list of Davao City barangays (matches dropdown in Accountverification.cshtml)
    // Includes 116 named barangays + 40 numbered poblacion barangays = 156 total
    const DAVAO_CITY_BARANGAYS = [
        // Numbered poblacion barangays (for OCR extraction from IDs)
        "1-A", "2-A", "3-A", "4-A", "5-A", "6-A", "7-A", "8-A", "9-A", "10-A",
        "11-B", "12-B", "13-B", "14-B", "15-B", "16-B", "17-B", "18-B", "19-B", "20-B",
        "21-C", "22-C", "23-C", "24-C", "25-C", "26-C", "27-C", "28-C", "29-C", "30-C",
        "31-D", "32-D", "33-D", "34-D", "35-D", "36-D", "37-D", "38-D", "39-D", "40-D",
        // Named barangays (official list from dropdown)
        "Acacia", "Agdao", "Alambre", "Alejandro Navarro", "Alfonso Angliongto Sr.",
        "Angalan", "Baguio Proper", "Baliok", "Bangkas Heights", "Baracatan",
        "Bato", "Bayabas", "Biao Escuela", "Biao Guianga", "Binugao",
        "Bucana", "Buhangin", "Buhangin (Pob.)", "Buhangin Proper", "Cabantian",
        "Cadalian", "Calinan Proper", "Callawa", "Camansi", "Carmen",
        "Catalunan Grande", "Catalunan Pequeño", "Catigan", "Cawayan", "Centro (San Juan)",
        "Colosas", "Communal", "Crossing Bayabas", "Dacudao", "Dalagdag",
        "Daliao", "Dalican", "Datu Salumay", "Dominga", "Eden",
        "Fatima (Benowang)", "Gatungan", "Gov. Paciano Bangoy", "Gov. Vicente Duterte", "Gumalang",
        "Gumitan", "Indangan", "Kap. Tomas Monteverde Sr.", "Kilate", "Lamanan",
        "Lampianao", "Langub", "Lapu-lapu", "Leon Garcia Sr.", "Los Amigos",
        "Lubogan", "Lumiad", "Ma-a", "Mabuhay", "Madapo",
        "Magtuod", "Mahayag", "Malabog", "Malagos", "Malamba",
        "Malandog", "Mampising", "Manambulan", "Mandug", "Manuel Guianga",
        "Mapula", "Marapangi", "Marilog Proper", "Matina Aplaya", "Matina Crossing",
        "Matina Pangi", "Mintal", "Mudiang", "Mulig", "New Carmen",
        "New Valencia", "Pampanga", "Panacan", "Pandaitan", "Panorama",
        "Paquibato Proper", "Paradise Embak", "Rafael Castillo", "Salapawan", "Salaysay",
        "Saloy", "San Antonio", "San Isidro", "Sasa", "Sirib",
        "Suawan", "Tacunan", "Tagakpan", "Tagluno", "Tagurano",
        "Talomo Proper", "Talomo River", "Tamurayan", "Tibungco", "Tigatto",
        "Tungkalan", "Ubalde", "Ugac", "Ula", "Vicente Hizon Sr.",
        "Waan", "Wangan", "Wilfredo Aquino", "Wines"
    ];

    // Allowed ID types - STRICT VALIDATION
    const ALLOWED_ID_TYPES = ['phil-id', 'driver-license', 'umid'];

    // ID type display element
    const idTypeDisplay = document.getElementById('id-type-display');

    // Go Back button functionality
    const goBackBtn = document.getElementById('goBackBtn');
    if (goBackBtn) {
        goBackBtn.addEventListener('click', function () {
            window.location.href = '/Homepage';
        });
    }

    // Debug logger
    function debugLog(category, data) {
        if (DEBUG_MODE) {
        }
    }

    // Format extracted data with labels for easy parsing and validation
    function formatExtractedDataWithLabels(extractedData, idType = '') {
        const formatted = [];
        const labels = {
            idNumber: 'ID Number',
            lastName: 'Last Name',
            firstName: 'First Name',
            middleName: 'Middle Name',
            suffix: 'Suffix',
            birthdate: 'Date of Birth',
            sex: 'Gender',
            civilStatus: 'Civil Status',
            city: 'City',
            address: 'Address'
        };

        // Add ID type header if provided
        if (idType) {
            const idTypeNames = {
                'driver-license': 'Driver\'s License',
                'phil-id': 'Philippine National ID',
                'umid': 'UMID'
            };
            formatted.push(`ID Type: ${idTypeNames[idType] || idType}`);
        }

        // Format each field with its label
        Object.keys(labels).forEach(key => {
            if (extractedData[key] && extractedData[key] !== "") {
                formatted.push(`${labels[key]}: ${extractedData[key]}`);
            } else if (key === 'suffix') {
                // Show "No value" for suffix if empty (suffix is often not present)
                formatted.push(`${labels[key]}: No value`);
            }
        });

        return formatted;
    }

    // Log extracted data in labeled format
    function logExtractedData(extractedData, idType, overallConfidence) {
        const formattedData = formatExtractedDataWithLabels(extractedData, idType);
        // Logging disabled for security
    }

    function getExtractedDataLabeled(extractedData) {
        return {
            'ID Number': extractedData.idNumber || '',
            'Last Name': extractedData.lastName || '',
            'First Name': extractedData.firstName || '',
            'Middle Name': extractedData.middleName || '',
            'Suffix': extractedData.suffix || '',
            'Date of Birth': extractedData.birthdate || '',
            'Gender': extractedData.sex || '',
            'Civil Status': extractedData.civilStatus || '',
            'City': extractedData.city || '',
            'Address': extractedData.address || ''
        };
    }

    // Update ID type display (confidence logged to console only for debugging)
    function updateIDTypeDisplay(idType, confidence) {
        if (!idTypeDisplay) return;

        const idTypeNames = {
            'driver-license': 'Driver\'s License',
            'phil-id': 'Philippine National ID',
            'umid': 'UMID (Unified Multi-Purpose ID)'
        };

        const name = idTypeNames[idType] || 'Unknown ID';

        // Log confidence to console for debugging

        // Display ID type without showing confidence level in UI
        if (confidence >= 90) {
            idTypeDisplay.className = 'w-full px-4 py-3 border-2 border-green-500 bg-green-50 rounded-xl text-gray-800 font-medium';
            idTypeDisplay.innerHTML = `<i class="fas fa-check-circle mr-2 text-green-500"></i>${name}`;
        } else if (confidence >= 70) {
            idTypeDisplay.className = 'w-full px-4 py-3 border-2 border-yellow-500 bg-yellow-50 rounded-xl text-gray-800 font-medium';
            idTypeDisplay.innerHTML = `<i class="fas fa-exclamation-circle mr-2 text-yellow-500"></i>${name}`;
        } else {
            idTypeDisplay.className = 'w-full px-4 py-3 border-2 border-red-500 bg-red-50 rounded-xl text-gray-800 font-medium';
            idTypeDisplay.innerHTML = `<i class="fas fa-times-circle mr-2 text-red-500"></i>${name}`;
        }
    }

    // Handle document type selection
    documentType.addEventListener('change', function () {
        const selectedValue = this.value;

        // Enable ID scanning for all supported ID types OR when empty (for auto-detection)
        if (!selectedValue || selectedValue === 'phil-id' || selectedValue === 'driver-license' || selectedValue === 'umid') {
            scanningEnabled = true;
            if (uploadFront) uploadFront.classList.remove('disabled');
            if (uploadBack) uploadBack.classList.remove('disabled');
            if (documentWarning) documentWarning.classList.add('hidden');

            if (!selectedValue) {
                if (status) status.innerHTML = '<i class="fas fa-info-circle mr-2"></i>Upload your ID - we\'ll detect the type automatically';
            } else {
                if (status) status.innerHTML = '<i class="fas fa-info-circle mr-2"></i>You can now upload ID images';
            }

            // Show Davao verification for all ID types
            if (davaoVerification) davaoVerification.classList.add('hidden');
            enableFormFields();
        } else {
            scanningEnabled = false;
            if (uploadFront) uploadFront.classList.add('disabled');
            if (uploadBack) uploadBack.classList.add('disabled');
            if (documentWarning) documentWarning.classList.remove('hidden');
            if (status) status.innerHTML = '<i class="fas fa-info-circle mr-2"></i>OCR not available for selected document type';
            if (resultBox) resultBox.classList.add('hidden');
            if (davaoVerification) davaoVerification.classList.add('hidden');
            enableFormFields();
        }
    });

    // Setup uploaders for both front and back
    setupUploader(uploadFront, fileFront, imagePreview, false);
    setupUploader(uploadBack, fileBack, imagePreviewBack, true);

    function setupUploader(uploadArea, fileInput, preview, isBack = false) {
        if (!uploadArea || !fileInput || !preview) {
            return;
        }

        try {
            // Click handler
            uploadArea.addEventListener('click', () => {
                if (!scanningEnabled) return;
                fileInput.click();
            });

            // File input change handler
            fileInput.addEventListener('change', (event) => {
                if (!scanningEnabled) return;
                if (event.target.files.length) {
                    handleFileUpload(event.target.files[0], preview, uploadArea, isBack);
                }
            });

            // Drag and drop handlers
            uploadArea.addEventListener('dragover', (e) => {
                if (!scanningEnabled) return;
                e.preventDefault();
                uploadArea.classList.add('dragover');
            });

            uploadArea.addEventListener('dragleave', () => {
                if (!scanningEnabled) return;
                uploadArea.classList.remove('dragover');
            });

            uploadArea.addEventListener('drop', (e) => {
                if (!scanningEnabled) return;
                e.preventDefault();
                uploadArea.classList.remove('dragover');

                if (e.dataTransfer.files.length) {
                    fileInput.files = e.dataTransfer.files;
                    handleFileUpload(e.dataTransfer.files[0], preview, uploadArea, isBack);
                }
            });
        } catch (e) {
            // Silent error handling
        }
    }

    // Handle file upload and processing
    // CRITICAL FIX: Store the actual File object (ZERO conversion)
    // This is the ONLY way to preserve 100% of original file data including EXIF
    function handleFileUpload(file, preview, section, isBack) {
        console.log('═══════════════════════════════════════════════════════════════');
        console.log(`🔄 handleFileUpload CALLED`);
        console.log(`   file: ${file ? `File(${file.name}, ${file.size} bytes)` : 'NULL'}`);
        console.log(`   isBack: ${isBack}`);
        console.log(`   scanningEnabled: ${scanningEnabled}`);
        console.log('═══════════════════════════════════════════════════════════════');

        if (!validateFile(file)) return;

        // ═══════════════════════════════════════════════════════════════════════════
        // NEW APPROACH: Store RAW FILE OBJECT + Preview separately
        // 1. Store actual File object for API submission (ZERO conversion, ZERO modification)
        // 2. Read as DataURL ONLY for preview display (not for API)
        // ═══════════════════════════════════════════════════════════════════════════

        // Reader: For preview display ONLY (not used for API)
        const previewReader = new FileReader();
        previewReader.onload = function (e) {
            // Show image preview immediately and replace upload box content
            if (preview) {
                preview.src = e.target.result;
                preview.classList.remove('hidden');
            }

            // Hide upload instructions and show preview prominently
            if (section) {
                const uploadContent = section.querySelector('.id-upload-content');
                if (uploadContent) {
                    uploadContent.classList.add('preview-mode');
                }
                section.classList.add('active', 'has-image');
            }
        };
        previewReader.readAsDataURL(file);

        // CRITICAL: Store the RAW FILE OBJECT without any conversion
        // This File object will be sent to backend using FormData (multipart/form-data)
        // The backend will read the raw bytes and handle conversion if needed
        if (scanningEnabled) {
            console.log(`🔄 Calling storeOriginalFile with file: ${file ? file.name : 'NULL'}`);
            storeOriginalFile(file, preview, isBack);
        } else {
            console.warn('⚠️ scanningEnabled is FALSE - not storing file!');
        }
    }

    /**
     * Convert file to Base64 and store for API submission
     * Converts File object to Base64 string immediately upon upload
     * Files are sent to backend as JSON payload for direct API submission
     * Only saved to disk AFTER API returns "accept" decision
     * @param {File} file - The raw File object from input
     * @param {HTMLElement} preview - Preview image element
     * @param {boolean} isBack - Whether this is back ID or front ID
     */
    async function storeOriginalFile(file, preview, isBack) {
        try {
            if (!file || !(file instanceof File)) {
                throw new Error('Invalid file object');
            }

            // Log detailed information for verification
            console.log('═══════════════════════════════════════════════════════════════');
            console.log(`📁 CONVERTING ${isBack ? 'BACK' : 'FRONT'} ID TO BASE64`);
            console.log('═══════════════════════════════════════════════════════════════');
            console.log(`📄 Filename: ${file.name}`);
            console.log(`🖼️  MIME Type: ${file.type}`);
            console.log(`💾 File Size: ${Math.round(file.size / 1024)} KB`);
            console.log(`📅 Last Modified: ${new Date(file.lastModified).toLocaleString()}`);
            console.log(`🔐 Converting to: Base64 string`);
            console.log(`📤 Will be sent as: JSON payload to API`);
            console.log(`💾 Saved to disk: Only if API returns "accept"`);
            console.log('═══════════════════════════════════════════════════════════════');

            // Convert File to Base64 string
            const base64Image = await convertFileToBase64(file);
            console.log(`✅ Base64 conversion complete: ${base64Image.length} chars`);

            // Store Base64 string in VerificationState
            if (isBack) {
                VerificationState.setBackId(base64Image);
                if (status) {
                    status.innerHTML = '<i class="fas fa-check-circle mr-2 text-green-500"></i>Back ID converted to Base64. Ready for verification.';
                }
            } else {
                VerificationState.setFrontId(base64Image);
                if (status) {
                    status.innerHTML = '<i class="fas fa-check-circle mr-2 text-green-500"></i>Front ID converted to Base64. Please upload back ID and selfie.';
                }
            }

        } catch (error) {
            console.error('❌ Error converting file to Base64:', error);
            if (status) {
                status.innerHTML = '<i class="fas fa-exclamation-triangle mr-2 text-red-500"></i>Error processing file. Please try again.';
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // IMAGE QUALITY VALIDATION FUNCTIONS
    // ═══════════════════════════════════════════════════════════════

    /**
     * DEPRECATED: Canvas-based blur detection
     * REPLACED BY: ID Analyzer API's built-in quality checks
     * DATE DEPRECATED: 2025-12-18
     */
    function checkImageBlur(canvas, ctx) {
        const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const data = imageData.data;
        const width = canvas.width;
        const height = canvas.height;

        // Convert to grayscale and calculate Laplacian variance
        const gray = [];
        for (let i = 0; i < data.length; i += 4) {
            gray.push(0.299 * data[i] + 0.587 * data[i + 1] + 0.114 * data[i + 2]);
        }

        // Calculate Laplacian variance (blur metric)
        let laplacian = 0;
        let count = 0;

        for (let y = 1; y < height - 1; y++) {
            for (let x = 1; x < width - 1; x++) {
                const idx = y * width + x;
                const lap = Math.abs(
                    -gray[idx - width - 1] - gray[idx - width] - gray[idx - width + 1] +
                    -gray[idx - 1] + 8 * gray[idx] - gray[idx + 1] +
                    -gray[idx + width - 1] - gray[idx + width] - gray[idx + width + 1]
                );
                laplacian += lap;
                count++;
            }
        }

        const variance = laplacian / count;

        // Thresholds (tuned for ID cards)
        // Higher variance = sharper image
        const CRITICAL_BLUR_THRESHOLD = 8;   // Too blurry - reject
        const WARNING_BLUR_THRESHOLD = 15;   // Slightly blurry - try to process with enhancement
        const GOOD_BLUR_THRESHOLD = 25;      // Good quality

        let quality = 'good';
        let isBlurry = false;

        if (variance < CRITICAL_BLUR_THRESHOLD) {
            quality = 'critical';  // Too blurry
            isBlurry = true;
        } else if (variance < WARNING_BLUR_THRESHOLD) {
            quality = 'warning';   // Slightly blurry
            isBlurry = false;      // Allow but will enhance
        } else if (variance < GOOD_BLUR_THRESHOLD) {
            quality = 'acceptable';
        }

        return { isBlurry, variance, quality };
    }

    /**
     * DEPRECATED: Canvas-based brightness/contrast detection
     * REPLACED BY: ID Analyzer API's built-in quality checks
     * DATE DEPRECATED: 2025-12-18
     */
    function checkImageBrightnessContrast(canvas, ctx) {
        const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const data = imageData.data;

        let sum = 0;
        let min = 255;
        let max = 0;

        // Calculate average brightness and range
        for (let i = 0; i < data.length; i += 4) {
            const brightness = 0.299 * data[i] + 0.587 * data[i + 1] + 0.114 * data[i + 2];
            sum += brightness;
            min = Math.min(min, brightness);
            max = Math.max(max, brightness);
        }

        const avgBrightness = sum / (data.length / 4);
        const contrast = max - min;

        // Thresholds
        const CRITICAL_DARK_THRESHOLD = 40;    // Too dark - reject
        const WARNING_DARK_THRESHOLD = 70;     // Slightly dark - enhance
        const CRITICAL_BRIGHT_THRESHOLD = 240; // Too bright - reject
        const LOW_CONTRAST_THRESHOLD = 50;     // Low contrast - enhance
        const CRITICAL_LOW_CONTRAST = 30;      // Critical low contrast - reject

        const isDark = avgBrightness < WARNING_DARK_THRESHOLD;
        const isTooDark = avgBrightness < CRITICAL_DARK_THRESHOLD;
        const isTooBright = avgBrightness > CRITICAL_BRIGHT_THRESHOLD;
        const isLowContrast = contrast < LOW_CONTRAST_THRESHOLD;
        const isCriticalLowContrast = contrast < CRITICAL_LOW_CONTRAST;

        return {
            brightness: avgBrightness,
            contrast: contrast,
            isDark: isDark && !isTooDark,
            isTooDark,
            isTooBright,
            isLowContrast: isLowContrast && !isCriticalLowContrast,
            isCriticalLowContrast
        };
    }

    /**
     * DEPRECATED: Image enhancement via pixel manipulation
     * ISSUE: Creates patterns identical to image editing software
     * DATE DEPRECATED: 2025-12-18
     */
    function enhanceImageQuality(canvas, ctx, qualityIssues) {
        const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const data = imageData.data;

        // Calculate enhancement factors based on issues
        let brightnessFactor = 1.0;
        let contrastFactor = 1.0;

        if (qualityIssues.isDark) {
            brightnessFactor = 1.3;  // Increase brightness by 30%
        }

        if (qualityIssues.isLowContrast) {
            contrastFactor = 1.4;    // Increase contrast by 40%
        }

        // Apply brightness and contrast adjustments
        for (let i = 0; i < data.length; i += 4) {
            // Brightness adjustment
            data[i] *= brightnessFactor;
            data[i + 1] *= brightnessFactor;
            data[i + 2] *= brightnessFactor;

            // Contrast adjustment
            data[i] = ((data[i] - 128) * contrastFactor) + 128;
            data[i + 1] = ((data[i + 1] - 128) * contrastFactor) + 128;
            data[i + 2] = ((data[i + 2] - 128) * contrastFactor) + 128;

            // Clamp values to 0-255
            data[i] = Math.max(0, Math.min(255, data[i]));
            data[i + 1] = Math.max(0, Math.min(255, data[i + 1]));
            data[i + 2] = Math.max(0, Math.min(255, data[i + 2]));
        }

        ctx.putImageData(imageData, 0, 0);
    }

    /**
     * Show image quality error modal with specific guidance
     */
    function showImageQualityErrorModal(issues) {
        let title = 'Image Quality Issue';
        let message = 'The ID image you uploaded has quality issues that prevent accurate reading.\n\n';
        let tips = [];

        if (issues.isTooDark) {
            message += '❌ Image is too dark\n';
            tips.push('Take the photo in a well-lit area');
            tips.push('Avoid shadows on the ID');
            tips.push('Use natural light or good indoor lighting');
        }

        if (issues.isTooBright) {
            message += '❌ Image is too bright/overexposed\n';
            tips.push('Avoid direct flash on the ID');
            tips.push('Reduce lighting or move away from direct light');
            tips.push('Avoid glare and reflections');
        }

        if (issues.isBlurry) {
            message += '❌ Image is too blurry\n';
            tips.push('Hold your phone steady when taking the photo');
            tips.push('Make sure the ID is in focus');
            tips.push('Clean your camera lens');
            tips.push('Get closer to the ID for a clearer shot');
        }

        if (issues.isCriticalLowContrast) {
            message += '❌ Image has very low contrast (text is hard to read)\n';
            tips.push('Ensure good lighting conditions');
            tips.push('Avoid photographing the ID on similar colored backgrounds');
            tips.push('Make sure the ID surface is clean');
        }

        message += '\n📸 Tips for better ID photos:\n';
        tips.forEach(tip => {
            message += `  • ${tip}\n`;
        });

        message += '\n💡 For best results:\n';
        message += '  • Place ID on a dark, flat surface\n';
        message += '  • Use good lighting (natural light works best)\n';
        message += '  • Hold camera parallel to the ID\n';
        message += '  • Ensure all text is clearly visible\n';

        showVerificationErrorModal(message);
    }

    /**
     * DEPRECATED: This function was used to preprocess images with canvas manipulation
     * ISSUE: Canvas processing strips EXIF metadata and creates compression artifacts
     *        that trigger ID Analyzer's tamper detection as "edited in Microsoft Paint"
     * REPLACED BY: storeOriginalImage() - preserves original image integrity
     * DATE DEPRECATED: 2025-12-18
     */
    async function preprocessAndStoreImage(imageSrc, preview, isBack) {
        try {

            // Load image asynchronously
            const img = await loadImage(imageSrc);

            // Optional: Light compression if image is too large
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');

            // Keep original dimensions or scale down if too large
            let width = img.width;
            let height = img.height;
            const maxDimension = 2048;

            if (width > maxDimension || height > maxDimension) {
                if (width > height) {
                    height = (height / width) * maxDimension;
                    width = maxDimension;
                } else {
                    width = (width / height) * maxDimension;
                    height = maxDimension;
                }
            }

            canvas.width = width;
            canvas.height = height;

            ctx.imageSmoothingEnabled = true;
            ctx.imageSmoothingQuality = 'high';
            ctx.drawImage(img, 0, 0, width, height);

            // Check image quality
            const blurCheck = checkImageBlur(canvas, ctx);
            const brightnessCheck = checkImageBrightnessContrast(canvas, ctx);

            // Check for critical issues
            const criticalIssues = {
                isBlurry: blurCheck.isBlurry,
                isTooDark: brightnessCheck.isTooDark,
                isTooBright: brightnessCheck.isTooBright,
                isCriticalLowContrast: brightnessCheck.isCriticalLowContrast
            };

            const hasCriticalIssue = Object.values(criticalIssues).some(issue => issue);

            if (hasCriticalIssue) {
                showImageQualityErrorModal(criticalIssues);
                if (status) {
                    status.innerHTML = '<i class="fas fa-exclamation-triangle mr-2 text-red-500"></i>Image quality too poor. Please retake.';
                }
                return;
            }

            // Enhance if needed
            const needsEnhancement = brightnessCheck.isDark || brightnessCheck.isLowContrast;
            if (needsEnhancement) {
                enhanceImageQuality(canvas, ctx, {
                    isDark: brightnessCheck.isDark,
                    isLowContrast: brightnessCheck.isLowContrast
                });
            }

            // Get processed image as data URL
            const processedImage = canvas.toDataURL('image/jpeg', 0.92);

            // ═══════════════════════════════════════════════════════════════════════════
            // STORE IN VerificationState (NO API CALL YET)
            // ═══════════════════════════════════════════════════════════════════════════
            if (isBack) {
                VerificationState.setBackId(processedImage);
                if (status) {
                    status.innerHTML = '<i class="fas fa-check-circle mr-2 text-green-500"></i>Back ID uploaded. Ready for verification.';
                }
            } else {
                VerificationState.setFrontId(processedImage);
                if (status) {
                    status.innerHTML = '<i class="fas fa-check-circle mr-2 text-green-500"></i>Front ID uploaded. Please upload back ID and selfie.';
                }
            }

            // Log current state

        } catch (error) {
            if (status) {
                status.innerHTML = '<i class="fas fa-exclamation-triangle mr-2 text-red-500"></i>Error processing image. Please try again.';
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SUBMIT ALL IMAGES TO API (Called when user clicks Verify button)
    // ═══════════════════════════════════════════════════════════════════════════
    // This function sends all collected images (front ID, back ID, selfie) to the
    // ID Analyzer API in a single request for both OCR and face verification.
    // ═══════════════════════════════════════════════════════════════════════════
    async function submitVerification() {

        // Check if all required images are ready
        if (!VerificationState.isReadyForVerification()) {
            showVerificationErrorModal('Please upload all required photos:\n\n• Front of ID\n• Back of ID\n• Your photo (selfie)\n\nAll photos are needed to continue.');
            return;
        }

        // Update UI to show processing state
        VerificationState.verificationInProgress = true;
        const verifyButton = document.getElementById('verify-submit-btn');
        if (verifyButton) {
            verifyButton.disabled = true;
            verifyButton.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Verifying...';
        }

        if (progressBar) {
            progressBar.classList.remove('hidden');
        }

        // Reset and start progress tracking
        ProgressTracker.reset();
        await ProgressTracker.advanceStep(); // Step 0: Preparing your files (5%)

        try {
            // ═══════════════════════════════════════════════════════════════════════════
            // NEW FLOW: Send files DIRECTLY to API, then save only if decision = "accept"
            // 1. Send Front ID, Back ID, Selfie to API (with AES256 encryption)
            // 2. API returns decision
            // 3. ONLY if decision = "accept" → Backend saves files to storage
            // ═══════════════════════════════════════════════════════════════════════════

            console.log('═══════════════════════════════════════════════════════════════');
            console.log('🔍 VERIFICATION STATE CHECK:');
            console.log('═══════════════════════════════════════════════════════════════');
            console.log(`   frontIdFile: ${VerificationState.frontIdFile ? 'EXISTS' : 'NULL'}`);
            console.log(`   backIdFile: ${VerificationState.backIdFile ? 'EXISTS' : 'NULL'}`);
            console.log(`   selfieFileName: ${VerificationState.selfieFileName || 'NULL'}`);

            if (VerificationState.frontIdFile) {
                console.log(`   frontIdFile is File: ${VerificationState.frontIdFile instanceof File}`);
                console.log(`   frontIdFile name: ${VerificationState.frontIdFile.name}`);
                console.log(`   frontIdFile size: ${VerificationState.frontIdFile.size} bytes`);
            }
            if (VerificationState.backIdFile) {
                console.log(`   backIdFile is File: ${VerificationState.backIdFile instanceof File}`);
                console.log(`   backIdFile name: ${VerificationState.backIdFile.name}`);
                console.log(`   backIdFile size: ${VerificationState.backIdFile.size} bytes`);
            }
            console.log('═══════════════════════════════════════════════════════════════');

            // Verify we have required files
            if (!VerificationState.frontIdFile || !(VerificationState.frontIdFile instanceof File)) {
                console.error('❌ ERROR: Front ID file is missing!');
                throw new Error('Front ID file is missing. Please upload your ID again.');
            }

            if (!VerificationState.backIdFile || !(VerificationState.backIdFile instanceof File)) {
                console.error('❌ ERROR: Back ID file is missing!');
                throw new Error('Back ID file is missing. Please upload your ID again.');
            }

            // Build FormData to send files directly to API
            const formData = new FormData();

            // Add Front ID (RAW FILE OBJECT - preserves 100% of data)
            formData.append('documentFile', VerificationState.frontIdFile);
            console.log('✅ Added documentFile to FormData (RAW FILE)');

            // Add Back ID (REQUIRED, RAW FILE OBJECT)
            formData.append('backFile', VerificationState.backIdFile);
            console.log('✅ Added backFile to FormData (RAW FILE)');

            // Add Selfie filename (selfie already saved in UsersImg via saveSelfie endpoint)
            if (VerificationState.selfieFileName) {
                formData.append('selfieFileName', VerificationState.selfieFileName);
                console.log(`✅ Added selfieFileName: ${VerificationState.selfieFileName}`);
            }

            // Add User ID for database storage
            if (window.currentUserId) {
                formData.append('userId', window.currentUserId);
                console.log(`✅ Added userId: ${window.currentUserId}`);
            } else {
                console.warn('⚠️ User ID not found - verification data will not be saved to database');
            }

            // Log request details
            console.log('═══════════════════════════════════════════════════════════════');
            console.log('🚀 SENDING FILES DIRECTLY TO SCAN ENDPOINT');
            console.log('═══════════════════════════════════════════════════════════════');
            console.log('📤 PAYLOAD TYPE: multipart/form-data (RAW FILES)');
            console.log(`   Front ID: ${VerificationState.frontIdFile ? '✅ RAW FILE' : '❌ Missing'}`);
            if (VerificationState.frontIdFile) {
                console.log(`      Filename: ${VerificationState.frontIdFile.name}`);
                console.log(`      Size: ${Math.round(VerificationState.frontIdFile.size / 1024)} KB`);
                console.log(`      Type: ${VerificationState.frontIdFile.type}`);
            }
            console.log(`   Back ID: ${VerificationState.backIdFile ? '✅ RAW FILE' : '❌ Missing (REQUIRED)'}`);
            if (VerificationState.backIdFile) {
                console.log(`      Filename: ${VerificationState.backIdFile.name}`);
                console.log(`      Size: ${Math.round(VerificationState.backIdFile.size / 1024)} KB`);
                console.log(`      Type: ${VerificationState.backIdFile.type}`);
            }
            console.log(`   Selfie: ${VerificationState.selfieFileName ? '✅ Filename' : '❌ Missing'}`);
            if (VerificationState.selfieFileName) {
                console.log(`      Filename: ${VerificationState.selfieFileName}`);
            }
            console.log('');
            console.log('🔐 BACKEND PROCESS:');
            console.log('   1️⃣ Receive RAW files (100% original data preserved)');
            console.log('   2️⃣ Convert to base64 for API transmission');
            console.log('   3️⃣ Send UNENCRYPTED data to ID Analyzer API');
            console.log('   4️⃣ ONLY if decision = "accept" → Save files to storage');
            console.log('');
            console.log('✅ EXIF METADATA: 100% PRESERVED (RAW FILES)');
            console.log('✅ PAINT DETECTION: SAFE - Untouched file data');
            console.log('⚠️ ENCRYPTION: NOT applied (API needs actual images)');
            console.log('📡 Content-Type: multipart/form-data');
            console.log('═══════════════════════════════════════════════════════════════');

            await ProgressTracker.jumpToStep(1); // Step 1: Uploading front of ID (15%)
            await ProgressTracker.advanceStep(); // Step 2: Uploading back of ID (30%)
            await ProgressTracker.advanceStep(); // Step 3: Uploading your photo (45%)

            // Send to scan endpoint with raw files
            console.log(`📡 Sending POST request to: ${ID_ANALYZER_CONFIG.SCAN_ENDPOINT}`);
            const response = await fetch(ID_ANALYZER_CONFIG.SCAN_ENDPOINT, {
                method: 'POST',
                body: formData  // FormData automatically sets Content-Type: multipart/form-data
            });

            console.log(`📥 Response status: ${response.status} ${response.statusText}`);

            await ProgressTracker.advanceStep(); // Step 4: Checking your ID (60%)

            const result = await response.json();

            // Log response

            await ProgressTracker.advanceStep(); // Step 5: Reading your information (75%)

            if (!result.success) {
                throw new Error(result.error || 'Verification failed');
            }

            // ═══════════════════════════════════════════════════════════════════════════
            // CRITICAL: CHECK API DECISION BEFORE PROCEEDING
            // ═══════════════════════════════════════════════════════════════════════════
            // ID Analyzer v2 API returns one of three decision values:
            // - "accept": Verification passed - proceed to form population
            // - "review": Manual review required - BLOCK and show warning
            // - "reject": Verification failed - BLOCK and show error
            // ═══════════════════════════════════════════════════════════════════════════

            const apiDecision = (result.decision || '').toLowerCase();

            // REJECT Decision - BLOCK verification
            if (apiDecision === 'reject') {
                ProgressTracker.setError('Photo not clear enough');

                const title = 'Photo Not Clear Enough';
                const message = 'We could not read your ID clearly. Please take a new photo.';

                const suggestions = [
                    'Make sure your ID photo is clear and well-lit',
                    'All text should be easy to read (not blurry)',
                    'Show the entire ID in the photo',
                    'Avoid reflections or shadows on your ID'
                ];

                // Add API warnings if available
                if (result.warnings && result.warnings.length > 0) {
                    suggestions.unshift(`Issues detected: ${result.warnings.join(', ')}`);
                }

                showErrorModal(title, message, suggestions);

                // Reset verification state
                VerificationState.verificationInProgress = false;
                if (verifyButton) {
                    verifyButton.disabled = false;
                    verifyButton.innerHTML = '<i class="fas fa-sync-alt mr-2"></i>Retry Verification';
                    // Enable retry flow on button click
                    verifyButton.onclick = function() {
                        if (typeof showRetryVerification === 'function') {
                            showRetryVerification('Your ID verification was rejected. Please re-upload clear photos of your ID and take a new selfie.');
                        }
                    };
                }
                
                // Update start-verification-btn to show FAILED state (RED)
                const startVerificationBtn = document.getElementById('start-verification-btn');
                if (startVerificationBtn) {
                    startVerificationBtn.innerHTML = '<i class="fas fa-times-circle"></i><span>Verification Failed</span>';
                    startVerificationBtn.classList.remove('from-blue-500', 'to-blue-600', 'hover:from-blue-600', 'hover:to-blue-700');
                    startVerificationBtn.classList.remove('from-green-500', 'to-green-600', 'hover:from-green-600', 'hover:to-green-700');
                    startVerificationBtn.classList.add('from-red-500', 'to-red-600', 'hover:from-red-600', 'hover:to-red-700');
                    // Re-enable click to retry
                    startVerificationBtn.onclick = function() {
                        if (typeof showRetryVerification === 'function') {
                            showRetryVerification('Your ID verification was rejected. Please re-upload clear photos of your ID and take a new selfie.');
                        } else {
                            IDUploadModal.startFlow();
                        }
                    };
                }
                
                return; // STOP execution - user cannot proceed
            }

            // REVIEW Decision - BLOCK verification
            if (apiDecision === 'review') {
                const title = 'We Need to Check Your ID';
                const message = 'Someone from our team will review your ID. We will let you know once we are done checking. Please wait for our message.';

                const suggestions = [
                    'Our team is checking your ID',
                    'We will send you a message when we are done',
                    'This page will update automatically when ready',
                    'You can also upload clearer photos if you want'
                ];

                // Add API warnings if available
                if (result.warnings && result.warnings.length > 0) {
                    suggestions.unshift(`Issues found: ${result.warnings.join(', ')}`);
                }

                showWarningModal(title, message, suggestions);

                // Reset verification state
                VerificationState.verificationInProgress = false;
                if (verifyButton) {
                    verifyButton.disabled = false;
                    verifyButton.innerHTML = '<i class="fas fa-clock mr-2"></i>Waiting for Review...';
                    verifyButton.classList.add('bg-yellow-500', 'hover:bg-yellow-600');
                }

                // HIDE start-verification-btn during processing/review
                const startVerificationBtn = document.getElementById('start-verification-btn');
                if (startVerificationBtn) {
                    startVerificationBtn.style.display = 'none';
                    // Also hide the helper text below the button
                    const helperText = startVerificationBtn.parentElement?.querySelector('p');
                    if (helperText) {
                        helperText.style.display = 'none';
                    }
                }

                // Start polling for decision updates
                // Get user ID from window.currentUserId (must be set by server-side code)
                const userId = window.currentUserId || null;
                if (userId) {
                    console.log('📊 Verification is under review - starting real-time status polling');
                    startVerificationPolling(userId, 'review');
                } else {
                    console.warn('⚠️ User ID not found - cannot start verification polling');
                }

                return; // STOP execution - user cannot proceed until review is complete
            }

            // UNKNOWN Decision - BLOCK verification
            if (apiDecision !== 'accept') {
                const title = 'Something Went Wrong';
                const message = `We encountered a problem checking your ID. Please try again.`;
                const suggestions = [
                    'Try uploading your ID photos again',
                    'Contact us if this keeps happening'
                ];

                showErrorModal(title, message, suggestions);

                // Reset verification state
                VerificationState.verificationInProgress = false;
                if (verifyButton) {
                    verifyButton.disabled = false;
                    verifyButton.innerHTML = '<i class="fas fa-sync-alt mr-2"></i>Retry Verification';
                    // Enable retry flow on button click
                    verifyButton.onclick = function() {
                        if (typeof showRetryVerification === 'function') {
                            showRetryVerification('An unexpected error occurred during verification. Please try again with new images.');
                        }
                    };
                }
                
                // Update start-verification-btn to show FAILED state (RED)
                const startVerificationBtn = document.getElementById('start-verification-btn');
                if (startVerificationBtn) {
                    startVerificationBtn.innerHTML = '<i class="fas fa-times-circle"></i><span>Verification Failed</span>';
                    startVerificationBtn.classList.remove('from-blue-500', 'to-blue-600', 'hover:from-blue-600', 'hover:to-blue-700');
                    startVerificationBtn.classList.remove('from-green-500', 'to-green-600', 'hover:from-green-600', 'hover:to-green-700');
                    startVerificationBtn.classList.add('from-red-500', 'to-red-600', 'hover:from-red-600', 'hover:to-red-700');
                    // Re-enable click to retry
                    startVerificationBtn.onclick = function() {
                        if (typeof showRetryVerification === 'function') {
                            showRetryVerification('An unexpected error occurred during verification. Please try again with new images.');
                        } else {
                            IDUploadModal.startFlow();
                        }
                    };
                }
                
                return; // STOP execution - user cannot proceed
            }

            // ═══════════════════════════════════════════════════════════════════════════
            // DECISION = "ACCEPT" - Proceed with verification
            // ═══════════════════════════════════════════════════════════════════════════

            // Store results
            VerificationState.scanResult = result;
            VerificationState.extractedData = result.data;
            VerificationState.faceMatchResult = {
                isMatch: result.data?.faceMatch,
                similarity: result.data?.faceSimilarity,
                confidence: result.data?.faceConfidence
            };

            await ProgressTracker.advanceStep(); // Step 6: Matching your photo (85%)

            // Process results - populate form fields
            if (result.data) {
                await processVerificationResults(result.data);
            }

            await ProgressTracker.advanceStep(); // Step 7: Almost done (95%)

            if (progress) progress.style.width = '100%';

            // Update UI for success
            VerificationState.verificationInProgress = false;
            VerificationState.verificationComplete = true;

            if (verifyButton) {
                verifyButton.innerHTML = '<i class="fas fa-check-circle mr-2"></i>Verification Complete';
                verifyButton.classList.remove('bg-red-600', 'hover:bg-red-700');
                verifyButton.classList.add('bg-green-600');
            }

            // HIDE start-verification-btn when verification is successful (accept decision)
            const startVerificationBtn = document.getElementById('start-verification-btn');
            if (startVerificationBtn) {
                startVerificationBtn.style.display = 'none';
                // Also hide the helper text below the button
                const helperText = startVerificationBtn.parentElement?.querySelector('p');
                if (helperText) {
                    helperText.style.display = 'none';
                }
            }

            // Step 8: Complete (100%) - Show success message
            ProgressTracker.setComplete('Your information is ready!');

            if (status) {
                status.innerHTML = '<i class="fas fa-check-circle mr-2 text-green-500"></i>Verification successful! Form fields populated.';
            }

            // Face verification results are no longer displayed separately
            // The system follows the API's decision field only

        } catch (error) {

            VerificationState.verificationInProgress = false;

            // Update progress bar to show error
            ProgressTracker.setError('Something went wrong');

            if (verifyButton) {
                verifyButton.disabled = false;
                verifyButton.innerHTML = '<i class="fas fa-sync-alt mr-2"></i>Retry Verification';
            }

            if (status) {
                status.innerHTML = `<i class="fas fa-exclamation-triangle mr-2 text-red-500"></i>Verification failed: ${error.message}`;
            }
            
            // Update start-verification-btn to show FAILED state (RED)
            const startVerificationBtn = document.getElementById('start-verification-btn');
            if (startVerificationBtn) {
                startVerificationBtn.innerHTML = '<i class="fas fa-times-circle"></i><span>Verification Failed</span>';
                startVerificationBtn.classList.remove('from-blue-500', 'to-blue-600', 'hover:from-blue-600', 'hover:to-blue-700');
                startVerificationBtn.classList.remove('from-green-500', 'to-green-600', 'hover:from-green-600', 'hover:to-green-700');
                startVerificationBtn.classList.remove('from-yellow-500', 'to-yellow-600', 'hover:from-yellow-600', 'hover:to-yellow-700');
                startVerificationBtn.classList.add('from-red-500', 'to-red-600', 'hover:from-red-600', 'hover:to-red-700');
                startVerificationBtn.disabled = false;
                startVerificationBtn.style.cursor = 'pointer';
                // Re-enable click to retry
                startVerificationBtn.onclick = function() {
                    if (typeof showRetryVerification === 'function') {
                        showRetryVerification(`Verification failed: ${error.message}. Please try again with new images.`);
                    } else {
                        IDUploadModal.startFlow();
                    }
                };
            }

            showVerificationErrorModal(`Verification failed:\n\n${error.message}\n\nPlease check your images and try again.`);
        }
    }

    // Make submitVerification globally accessible
    window.submitVerification = submitVerification;

    // Process verification results and populate form fields
    async function processVerificationResults(data) {
        if (data.faceMatch !== undefined) {
        }

        // Populate form fields using existing function
        populateFormFromApiData(data, null, false);

        // CRITICAL: Store verification decision and transaction ID for form submission
        // These will be checked server-side before marking account as verified
        const decisionField = document.getElementById('verification-decision');
        const transactionField = document.getElementById('transaction-id');

        if (decisionField && VerificationState.scanResult) {
            decisionField.value = VerificationState.scanResult.decision || '';
            console.log('📝 Stored verification decision:', decisionField.value);
        }

        if (transactionField && VerificationState.scanResult) {
            transactionField.value = VerificationState.scanResult.transactionId || '';
            console.log('📝 Stored transaction ID:', transactionField.value);
        }
    }

    // REMOVED: showFaceVerificationResult() function
    // The system now only follows the API's "decision" field (accept/review/reject)
    // Face verification results are handled internally by the ID Analyzer API
    // and reflected in the decision field, not displayed separately

    // Enhanced preprocessing with quality validation and enhancement (Async)
    // LEGACY: This function is kept for backward compatibility
    // Preprocess and send image to ID Analyzer API for scanning
    async function preprocessAndScan(imageSrc, preview, isBack) {
        try {

            // Load image asynchronously
            const img = await loadImage(imageSrc);

            // Optional: Light compression if image is too large (>1MB for free tier)
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');

            // Keep original dimensions or scale down if too large
            let width = img.width;
            let height = img.height;
            const maxDimension = 2048; // Reasonable limit

            if (width > maxDimension || height > maxDimension) {
                if (width > height) {
                    height = (height / width) * maxDimension;
                    width = maxDimension;
                } else {
                    width = (width / height) * maxDimension;
                    height = maxDimension;
                }
            }

            canvas.width = width;
            canvas.height = height;

            // Use high-quality image smoothing
            ctx.imageSmoothingEnabled = true;
            ctx.imageSmoothingQuality = 'high';
            ctx.drawImage(img, 0, 0, width, height);

            // ═══════════════════════════════════════════════════════════════
            // STEP 1: VALIDATE IMAGE QUALITY
            // ═══════════════════════════════════════════════════════════════

            // Check for blur
            const blurCheck = checkImageBlur(canvas, ctx);
            debugLog('BLUR-CHECK', blurCheck);

            // Check brightness and contrast
            const brightnessCheck = checkImageBrightnessContrast(canvas, ctx);
            debugLog('BRIGHTNESS-CHECK', brightnessCheck);

            // ═══════════════════════════════════════════════════════════════
            // STEP 2: HANDLE CRITICAL QUALITY ISSUES (REJECT)
            // ═══════════════════════════════════════════════════════════════

            const criticalIssues = {
                isBlurry: blurCheck.isBlurry,
                isTooDark: brightnessCheck.isTooDark,
                isTooBright: brightnessCheck.isTooBright,
                isCriticalLowContrast: brightnessCheck.isCriticalLowContrast
            };

            const hasCriticalIssue = Object.values(criticalIssues).some(issue => issue);

            if (hasCriticalIssue) {
                // Show user-friendly error modal with specific guidance
                showImageQualityErrorModal(criticalIssues);

                // Update status
                status.innerHTML = '<i class="fas fa-exclamation-triangle mr-2 text-red-500"></i>Image quality too poor for processing.';
                status.className = 'text-sm text-red-600 mt-2';

                return; // Stop processing
            }

            // ═══════════════════════════════════════════════════════════════
            // STEP 3: ENHANCE SLIGHTLY PROBLEMATIC IMAGES
            // ═══════════════════════════════════════════════════════════════

            const needsEnhancement = brightnessCheck.isDark || brightnessCheck.isLowContrast;

            if (needsEnhancement) {
                debugLog('IMAGE-ENHANCEMENT', 'Applying enhancement for slight quality issues');
                status.innerHTML = '<i class="fas fa-magic fa-spin mr-2"></i>Enhancing image quality...';
                status.className = 'text-sm text-blue-600 mt-2';

                // Apply enhancement
                enhanceImageQuality(canvas, ctx, {
                    isDark: brightnessCheck.isDark,
                    isLowContrast: brightnessCheck.isLowContrast
                });

                // Small delay to show enhancement message
                await new Promise(resolve => setTimeout(resolve, 500));
            }

            // ═══════════════════════════════════════════════════════════════
            // STEP 4: FINAL PROCESSING
            // ═══════════════════════════════════════════════════════════════

            // Get image as data URL (JPEG for smaller size, 92% quality)
            const processedImage = canvas.toDataURL('image/jpeg', 0.92);

            // Send to ID Analyzer API v2 for processing
            await processImageWithOCR(processedImage, isBack);
        } catch (error) {
            status.innerHTML = '<i class="fas fa-exclamation-triangle mr-2 text-red-500"></i>Cannot process image. Please try uploading a different photo.';
            status.className = 'text-sm text-red-600 mt-2';

            // Show user-friendly error modal
            showVerificationErrorModal(
                'We could not process your image.\n\n' +
                'Please try:\n\n' +
                '• Taking a new photo of your ID\n' +
                '• Making sure the photo is clear and well-lit\n' +
                '• Using a different device or camera\n\n' +
                'If the problem continues, please contact support.'
            );

            // Log technical details for developers only
            debugLog('IMAGE-PROCESSING-ERROR', error);
        }
    }

    // Helper function to load image asynchronously
    function loadImage(src) {
        return new Promise((resolve, reject) => {
            const img = new Image();
            img.onload = () => resolve(img);
            img.onerror = reject;
            img.src = src;
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // REAL-TIME VERIFICATION STATUS POLLING
    // ═══════════════════════════════════════════════════════════════

    /**
     * Start polling for verification decision updates
     * Used when decision is "review" to check if admin has updated it to "accept"/"reject"
     */
    function startVerificationPolling(userId, initialDecision) {
        // Stop any existing polling
        stopVerificationPolling();

        currentVerificationDecision = initialDecision;

        console.log(`🔄 Starting verification status polling for user ${userId} (current decision: ${initialDecision})`);

        // Poll immediately, then every POLLING_INTERVAL_MS
        checkVerificationStatus(userId);

        verificationPollingInterval = setInterval(() => {
            checkVerificationStatus(userId);
        }, POLLING_INTERVAL_MS);
    }

    /**
     * Stop polling for verification decision updates
     */
    function stopVerificationPolling() {
        if (verificationPollingInterval) {
            clearInterval(verificationPollingInterval);
            verificationPollingInterval = null;
            console.log('🛑 Stopped verification status polling');
        }
    }

    /**
     * Check verification status from the server
     */
    async function checkVerificationStatus(userId) {
        try {
            const response = await fetch(`/api/IdAnalyzer/verificationStatus?userId=${userId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                console.error('Failed to check verification status:', response.statusText);
                return;
            }

            const result = await response.json();

            if (result.success && result.decision) {
                const newDecision = result.decision;

                // Check if decision has changed
                if (newDecision !== currentVerificationDecision && currentVerificationDecision === 'review') {
                    console.log(`✅ Verification decision updated: ${currentVerificationDecision} → ${newDecision}`);
                    currentVerificationDecision = newDecision;

                    // Stop polling
                    stopVerificationPolling();

                    // Show notification to user
                    handleDecisionUpdate(newDecision);
                }
            }
        } catch (error) {
            console.error('Error checking verification status:', error);
        }
    }

    /**
     * Handle verification decision update from "review" to "accept"/"reject"
     */
    function handleDecisionUpdate(newDecision) {
        const startVerificationBtn = document.getElementById('start-verification-btn');
        
        if (newDecision === 'accept') {
            // Decision changed from review to accept - user can proceed
            const title = 'Verification Approved! ✅';
            const message = 'Your identity verification has been approved by an administrator. You can now proceed with your application.';

            showSuccessModal(title, message);

            // Update button to allow submission
            const verifyButton = document.getElementById('verify-and-submit-btn');
            if (verifyButton) {
                verifyButton.disabled = false;
                verifyButton.innerHTML = '<i class="fas fa-check-circle mr-2"></i>Submit Application';
                verifyButton.classList.remove('bg-gray-400');
                verifyButton.classList.add('bg-green-600', 'hover:bg-green-700');
            }
            
            // HIDE start-verification-btn when verification is successful (accept decision)
            if (startVerificationBtn) {
                startVerificationBtn.style.display = 'none';
                // Also hide the helper text below the button
                const helperText = startVerificationBtn.parentElement?.querySelector('p');
                if (helperText) {
                    helperText.style.display = 'none';
                }
            }

            // Allow user to proceed
            setTimeout(() => {
                // Trigger form submission or next step
                proceedWithVerification();
            }, 2000);

        } else if (newDecision === 'reject') {
            // Decision changed from review to reject
            const title = 'ID Not Accepted ❌';
            const message = 'We could not verify your ID. Please upload clearer photos and try again.';

            showErrorModal(title, message);

            // Update button to show retry
            const verifyButton = document.getElementById('verify-and-submit-btn');
            if (verifyButton) {
                verifyButton.disabled = false;
                verifyButton.innerHTML = '<i class="fas fa-sync-alt mr-2"></i>Retry Verification';
                verifyButton.onclick = function() {
                    if (typeof showRetryVerification === 'function') {
                        showRetryVerification('Your verification was rejected. Please re-upload clearer photos and take a new selfie.');
                    }
                };
            }
            
            // Update start-verification-btn to show FAILED state (RED)
            if (startVerificationBtn) {
                startVerificationBtn.innerHTML = '<i class="fas fa-times-circle"></i><span>Verification Failed</span>';
                startVerificationBtn.classList.remove('from-blue-500', 'to-blue-600', 'hover:from-blue-600', 'hover:to-blue-700');
                startVerificationBtn.classList.remove('from-green-500', 'to-green-600', 'hover:from-green-600', 'hover:to-green-700');
                startVerificationBtn.classList.remove('from-yellow-500', 'to-yellow-600', 'hover:from-yellow-600', 'hover:to-yellow-700');
                startVerificationBtn.classList.add('from-red-500', 'to-red-600', 'hover:from-red-600', 'hover:to-red-700');
                startVerificationBtn.disabled = false;
                startVerificationBtn.style.cursor = 'pointer';
                startVerificationBtn.onclick = function() {
                    if (typeof showRetryVerification === 'function') {
                        showRetryVerification('Your verification was rejected. Please re-upload clearer photos and take a new selfie.');
                    } else {
                        IDUploadModal.startFlow();
                    }
                };
            }
        }
    }

    /**
     * Show success modal for verification approval
     */
    function showSuccessModal(title, message) {
        // Use existing modal or create alert
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'success',
                title: title,
                text: message,
                confirmButtonText: 'Continue',
                timer: 5000
            });
        } else {
            alert(`${title}\n\n${message}`);
        }
    }

    /**
     * Show error modal for verification rejection
     */
    function showErrorModal(title, message) {
        // Use existing modal or create alert
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'error',
                title: title,
                text: message,
                confirmButtonText: 'Retry',
                allowOutsideClick: false
            });
        } else {
            alert(`${title}\n\n${message}`);
        }
    }

    /**
     * Proceed with verification after approval
     */
    function proceedWithVerification() {
        // This will be called after admin approves the verification
        // The form submission logic will be triggered
        console.log('✅ Verification approved - proceeding with application');

        // Enable form submission
        VerificationState.verificationPassed = true;

        // You may want to trigger automatic form submission here
        // or just enable the submit button and let user click it
    }

    // ═══════════════════════════════════════════════════════════════
    // END POLLING FUNCTIONS
    // ═══════════════════════════════════════════════════════════════

    // Advanced text cleaning to remove noise and garbage
    // Clean extracted text from ID Analyzer API
    function cleanExtractedText(text) {
        if (!text) return "";

        debugLog('RAW-EXTRACTED-TEXT', text);

        // Step 1: Fix common OCR character misreads
        text = text.replace(/[|]/g, 'I'); // Replace pipe with I
        text = text.replace(/[l1]/g, (match, offset, string) => {
            // Context-aware: if surrounded by letters, it's likely 'I'
            const before = string[offset - 1];
            const after = string[offset + 1];
            if (before && after && /[A-Z]/i.test(before) && /[A-Z]/i.test(after)) {
                return 'I';
            }
            return match;
        });

        // Step 2: Fix zero/O confusion in specific contexts
        text = text.replace(/O/g, (match, offset, string) => {
            // If surrounded by numbers, it's likely zero
            const before = string[offset - 1];
            const after = string[offset + 1];
            if (before && after && /[0-9]/.test(before) && /[0-9]/.test(after)) {
                return '0';
            }
            return match;
        });

        // Step 3: Remove noise characters
        text = text.replace(/[~`@#$%^&*_+=<>{}[\]\\]/g, '');

        // Step 4: Fix spacing issues
        text = text.replace(/\s{2,}/g, ' '); // Collapse multiple spaces

        // Step 5: Handle accented characters
        text = text.replace(/[^\x00-\x7F]/g, (char) => {
            const charMap = {
                'ñ': 'n', 'Ñ': 'N',
                'á': 'a', 'Á': 'A', 'à': 'a', 'À': 'A',
                'é': 'e', 'É': 'E', 'è': 'e', 'È': 'E',
                'í': 'i', 'Í': 'I', 'ì': 'i', 'Ì': 'I',
                'ó': 'o', 'Ó': 'O', 'ò': 'o', 'Ò': 'O',
                'ú': 'u', 'Ú': 'U', 'ù': 'u', 'Ù': 'U'
            };
            return charMap[char] || char;
        });

        // Step 6: Remove isolated noise characters
        text = text.split('\n').map(line => {
            line = line.trim();
            // Remove lines with only single non-alphanumeric characters
            if (line.length === 1 && !/[A-Z0-9]/i.test(line)) return '';
            // Remove lines with excessive special characters (likely noise)
            const specialCharCount = (line.match(/[^a-zA-Z0-9\s\-,.:/]/g) || []).length;
            if (specialCharCount > line.length / 2) return '';
            return line;
        }).filter(line => line).join('\n');

        debugLog('CLEANED-OCR', text);
        return text.trim();
    }

    // Validate extracted ID number format
    function validateIDNumber(idNumber, idType) {
        if (!idNumber) return { valid: false, confidence: 0 };

        let pattern, expectedLength;

        switch (idType) {
            case 'driver-license':
                // Format: L00-00-000000 or similar
                pattern = /^[A-Z]\d{2}-\d{2}-\d{6}$/;
                expectedLength = [11]; // with dashes
                break;
            case 'phil-id':
                // Format: 1234-5678-9012-3456
                pattern = /^\d{4}-\d{4}-\d{4}-\d{4}$/;
                expectedLength = [19]; // with dashes
                break;
            case 'umid':
                // Format: 0000-0000000-0 (CRN - Common Reference Number)
                pattern = /^\d{4}-\d{7}-\d{1}$/;
                expectedLength = [14]; // with dashes
                break;
            default:
                return { valid: false, confidence: 0 };
        }

        const valid = pattern.test(idNumber);
        const lengthMatch = expectedLength.includes(idNumber.length);
        const confidence = valid && lengthMatch ? 100 : (valid ? 70 : 0);

        debugLog('ID-VALIDATION', { idNumber, idType, valid, lengthMatch, confidence });

        return { valid: valid && lengthMatch, confidence };
    }

    // Validate extracted name
    function validateName(name, fieldType) {
        if (!name) return { valid: false, confidence: 0 };

        // Remove extra spaces and validate
        name = name.trim();

        // Name should be at least 2 characters
        if (name.length < 2) return { valid: false, confidence: 0 };

        // Name should only contain letters and spaces
        if (!/^[A-Za-z\s]+$/.test(name)) return { valid: false, confidence: 0 };

        // Name shouldn't be all uppercase noise words
        const noiseWords = ['NAME', 'SURNAME', 'FIRST', 'LAST', 'MIDDLE', 'GIVEN',
            'APELYIDO', 'PANGALAN', 'GITNANG', 'UNANG'];
        if (noiseWords.includes(name.toUpperCase())) return { valid: false, confidence: 0 };

        // Calculate confidence based on characteristics
        let confidence = 80;

        // Boost confidence for proper capitalization
        if (/^[A-Z][a-z]+(\s[A-Z][a-z]+)*$/.test(name)) confidence = 100;

        // Reduce confidence for very short names (might be initials or noise)
        if (name.length < 3) confidence -= 20;

        // Reduce confidence for very long names (might include extra text)
        if (name.length > 30) confidence -= 10;

        debugLog('NAME-VALIDATION', { name, fieldType, confidence });

        return { valid: true, confidence };
    }

    // Validate birthdate
    function validateBirthdate(birthdate) {
        if (!birthdate) return { valid: false, confidence: 0 };

        // Expected format: YYYY-MM-DD
        const datePattern = /^\d{4}-\d{2}-\d{2}$/;
        if (!datePattern.test(birthdate)) return { valid: false, confidence: 0 };

        const [year, month, day] = birthdate.split('-').map(Number);
        const currentYear = new Date().getFullYear();

        // Validate ranges
        if (year < 1900 || year > currentYear) return { valid: false, confidence: 0 };
        if (month < 1 || month > 12) return { valid: false, confidence: 0 };
        if (day < 1 || day > 31) return { valid: false, confidence: 0 };

        // Check if person is at least 18 years old (typical requirement)
        const age = currentYear - year;
        const confidence = age >= 18 && age <= 100 ? 100 : 70;

        debugLog('BIRTHDATE-VALIDATION', { birthdate, age, confidence });

        return { valid: true, confidence };
    }

    // Calculate string similarity using Levenshtein distance
    function calculateSimilarity(str1, str2) {
        if (!str1 || !str2) return 0;

        str1 = str1.toUpperCase();
        str2 = str2.toUpperCase();

        // Exact match
        if (str1 === str2) return 100;

        // One contains the other
        if (str1.includes(str2) || str2.includes(str1)) return 90;

        // Calculate Levenshtein distance
        const len1 = str1.length;
        const len2 = str2.length;
        const matrix = [];

        for (let i = 0; i <= len1; i++) {
            matrix[i] = [i];
        }

        for (let j = 0; j <= len2; j++) {
            matrix[0][j] = j;
        }

        for (let i = 1; i <= len1; i++) {
            for (let j = 1; j <= len2; j++) {
                const cost = str1[i - 1] === str2[j - 1] ? 0 : 1;
                matrix[i][j] = Math.min(
                    matrix[i - 1][j] + 1,
                    matrix[i][j - 1] + 1,
                    matrix[i - 1][j - 1] + cost
                );
            }
        }

        const distance = matrix[len1][len2];
        const maxLen = Math.max(len1, len2);
        const similarity = ((maxLen - distance) / maxLen) * 100;

        return Math.round(similarity);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // STRICT LETTER-BY-LETTER NAME MATCHING LAYER
    // Final validation layer that performs character-by-character comparison
    // ═══════════════════════════════════════════════════════════════════════════
    function performStrictNameMatching(extractedFirstName, extractedMiddleName, extractedLastName, extractedSuffix,
        registeredFirstName, registeredMiddleName, registeredLastName, registeredSuffix) {


        // Normalize function - strict: only uppercase letters, remove all special chars and spaces
        const strictNormalize = (name) => {
            if (!name) return "";
            return name.trim().toUpperCase().replace(/[^A-Z]/g, '');
        };

        // Normalize all name components
        const extFirst = strictNormalize(extractedFirstName);
        const extMiddle = strictNormalize(extractedMiddleName);
        const extLast = strictNormalize(extractedLastName);
        const extSuffix = strictNormalize(extractedSuffix);

        const regFirst = strictNormalize(registeredFirstName);
        const regMiddle = strictNormalize(registeredMiddleName);
        const regLast = strictNormalize(registeredLastName);
        const regSuffix = strictNormalize(registeredSuffix || "");


        // Letter-by-letter comparison function
        const letterByLetterMatch = (str1, str2, componentName) => {
            // Empty strings handling
            if (!str1 && !str2) {
                return { matches: true, reason: 'Both empty', details: '' };
            }

            if (!str1 || !str2) {
                const missing = !str1 ? 'extracted' : 'registered';
                return {
                    matches: false,
                    reason: `${componentName} missing in ${missing}`,
                    details: `Expected: "${str2 || 'N/A'}", Got: "${str1 || 'N/A'}"`
                };
            }

            // Exact match check
            if (str1 === str2) {
                return { matches: true, reason: 'Exact match', details: '' };
            }

            // Letter-by-letter comparison
            const minLen = Math.min(str1.length, str2.length);
            const maxLen = Math.max(str1.length, str2.length);
            let matchingChars = 0;
            let firstMismatchPos = -1;
            let mismatchDetails = [];

            for (let i = 0; i < maxLen; i++) {
                const char1 = str1[i] || '';
                const char2 = str2[i] || '';

                if (char1 === char2 && char1 !== '') {
                    matchingChars++;
                } else if (firstMismatchPos === -1) {
                    firstMismatchPos = i;
                    mismatchDetails.push(`Position ${i + 1}: got '${char1 || 'END'}' expected '${char2 || 'END'}'`);
                } else if (mismatchDetails.length < 3) {
                    mismatchDetails.push(`Position ${i + 1}: got '${char1 || 'END'}' expected '${char2 || 'END'}'`);
                }
            }

            const matchPercentage = (matchingChars / maxLen) * 100;

            // Display comparison
            const displayStr1 = str1.length > 17 ? str1.substring(0, 14) + '...' : str1;
            const displayStr2 = str2.length > 17 ? str2.substring(0, 14) + '...' : str2;

            return {
                matches: false,
                reason: `Letter mismatch at position ${firstMismatchPos + 1}`,
                details: mismatchDetails.join('; '),
                matchPercentage: Math.round(matchPercentage),
                expectedLength: str2.length,
                actualLength: str1.length
            };
        };

        // Perform strict matching for each component
        const lastNameResult = letterByLetterMatch(extLast, regLast, 'Last Name');
        const firstNameResult = letterByLetterMatch(extFirst, regFirst, 'First Name');
        const middleNameResult = letterByLetterMatch(extMiddle, regMiddle, 'Middle Name');

        // Suffix is optional - only check if both exist
        let suffixResult = { matches: true, reason: 'Suffix optional', details: '' };
        if (extSuffix && regSuffix) {
            suffixResult = letterByLetterMatch(extSuffix, regSuffix, 'Suffix');
        } else if (extSuffix || regSuffix) {
            suffixResult = { matches: true, reason: 'Suffix optional (one empty)', details: '' };
        }


        // Check if all required components match
        const allMatch = lastNameResult.matches && firstNameResult.matches && middleNameResult.matches;

        if (allMatch) {
            if (extSuffix || regSuffix) {
            }

            return {
                passed: true,
                confidence: 100,
                message: 'All name components match exactly (letter-by-letter validation)'
            };
        }

        // Collect all mismatches
        const mismatches = [];
        const mismatchDetails = [];

        if (!lastNameResult.matches) {
            mismatches.push('Last Name');
            mismatchDetails.push(`Last Name: ${lastNameResult.details}`);
        }

        if (!firstNameResult.matches) {
            mismatches.push('First Name');
            mismatchDetails.push(`First Name: ${firstNameResult.details}`);
        }

        if (!middleNameResult.matches) {
            mismatches.push('Middle Name');
            mismatchDetails.push(`Middle Name: ${middleNameResult.details}`);
        }


        return {
            passed: false,
            confidence: 0,
            message: `Letter-by-letter mismatch in: ${mismatches.join(', ')}`,
            failedComponents: mismatches,
            details: mismatchDetails,
            lastNameResult,
            firstNameResult,
            middleNameResult,
            suffixResult
        };
    }

    // Validate Name Match - TWO-STEP VALIDATION
    function validateNameMatch(extractedFirstName, extractedMiddleName, extractedLastName, extractedSuffix = "", idType = "") {
        // Normalize names for comparison
        const normalizeForComparison = (name) => {
            if (!name) return "";
            return name.trim().toUpperCase().replace(/[^A-Z\s]/g, '');
        };

        const extractedFirst = normalizeForComparison(extractedFirstName);
        const extractedMiddle = normalizeForComparison(extractedMiddleName);
        const extractedLast = normalizeForComparison(extractedLastName);
        const extractedSuff = normalizeForComparison(extractedSuffix);

        const regFirst = normalizeForComparison(registeredFirstName);
        const regMiddle = normalizeForComparison(registeredMiddleName);
        const regLast = normalizeForComparison(registeredLastName);
        const regSuff = normalizeForComparison(registeredSuffix || "");


        // CRITICAL: If registered name is empty, this is a configuration error
        if (!regFirst && !regMiddle && !regLast) {
            return {
                matches: false,
                reason: 'No registered name found in database. Please contact support.',
                confidence: 0,
                details: {
                    firstMatches: false,
                    middleMatches: false,
                    lastMatches: false,
                    extractedName: `${extractedLastName}, ${extractedFirstName} ${extractedMiddleName} ${extractedSuffix}`.trim(),
                    registeredName: 'Not found in database'
                }
            };
        }

        // STEP 1: Check Individual Components First

        // Calculate similarity for each component
        const lastSimilarity = calculateSimilarity(extractedLast, regLast);
        const firstSimilarity = calculateSimilarity(extractedFirst, regFirst);
        let middleSimilarity = 100; // Default to 100 if no middle name

        // Middle name: handle initials vs full names
        if (regMiddle && extractedMiddle) {
            middleSimilarity = calculateSimilarity(extractedMiddle, regMiddle);

            // If similarity is low, check if one is initial of the other
            if (middleSimilarity < 80) {
                const regInitial = regMiddle.charAt(0);
                const extInitial = extractedMiddle.charAt(0);

                if (regInitial === extInitial) {
                    middleSimilarity = 95; // High similarity if initials match
                } else if (extractedMiddle.startsWith(regInitial) || regMiddle.startsWith(extInitial)) {
                    middleSimilarity = 90;
                }
            }
        } else if (!regMiddle && !extractedMiddle) {
            middleSimilarity = 100; // Both empty
        } else if (!regMiddle || !extractedMiddle) {
            middleSimilarity = 85; // One missing is acceptable
        }

        // Suffix matching (OPTIONAL - no suffix is always acceptable)
        let suffixMatches = true; // Default to true (suffix is optional)
        if (extractedSuff && regSuff) {
            // Only check if both have suffix
            suffixMatches = extractedSuff === regSuff;
        }
        // If one has suffix and other doesn't, still acceptable (suffixMatches = true)

        const SIMILARITY_THRESHOLD = 80; // 80% similarity required

        const lastMatches = lastSimilarity >= SIMILARITY_THRESHOLD;
        const firstMatches = firstSimilarity >= SIMILARITY_THRESHOLD;
        const middleMatches = middleSimilarity >= SIMILARITY_THRESHOLD;


        // Check if all REQUIRED components match (suffix is optional, not checked)
        const allComponentsMatch = lastMatches && firstMatches && middleMatches;

        if (allComponentsMatch) {
            const overallSimilarity = Math.round((lastSimilarity * 0.4 + firstSimilarity * 0.4 + middleSimilarity * 0.2));

            // ═══════════════════════════════════════════════════════════════════════════
            // FINAL STRICT VALIDATION LAYER - Letter-by-letter matching
            // Even if similarity is high, perform exact character comparison
            // ═══════════════════════════════════════════════════════════════════════════

            const strictResult = performStrictNameMatching(
                extractedFirstName, extractedMiddleName, extractedLastName, extractedSuffix,
                registeredFirstName, registeredMiddleName, registeredLastName, registeredSuffix
            );

            if (!strictResult.passed) {
                // Strict validation failed - reject the match

                return {
                    matches: false,
                    reason: `Strict validation failed: ${strictResult.message}`,
                    confidence: 0,
                    strictValidation: strictResult,
                    details: {
                        firstMatches: false,
                        middleMatches: false,
                        lastMatches: false,
                        extractedName: `${extractedLastName}, ${extractedFirstName} ${extractedMiddleName} ${extractedSuffix}`.trim(),
                        registeredName: `${registeredLastName}, ${registeredFirstName} ${registeredMiddleName} ${registeredSuffix || ''}`.trim(),
                        strictValidationDetails: strictResult.details
                    }
                };
            }

            // Both similarity check AND strict validation passed

            return {
                matches: true,
                reason: `Names match - Component validation (${overallSimilarity}% similarity) + Strict letter-by-letter validation`,
                confidence: overallSimilarity,
                strictValidation: strictResult
            };
        }


        // STEP 2: Full Name Comparison (Driver's License Priority)
        const usesFullNameComparison = idType === 'driver-license';

        if (usesFullNameComparison) {

            // Concatenate full names (accept different formats)
            // Format 1: "First Middle Last Suffix"
            const extractedFullName1 = `${extractedFirst} ${extractedMiddle} ${extractedLast} ${extractedSuff}`.replace(/\s+/g, ' ').trim();
            const registeredFullName1 = `${regFirst} ${regMiddle} ${regLast} ${regSuff}`.replace(/\s+/g, ' ').trim();

            // Format 2: "Last First Middle Suffix"
            const extractedFullName2 = `${extractedLast} ${extractedFirst} ${extractedMiddle} ${extractedSuff}`.replace(/\s+/g, ' ').trim();
            const registeredFullName2 = `${regLast} ${regFirst} ${regMiddle} ${regSuff}`.replace(/\s+/g, ' ').trim();

            // Format 3: "First Last" (no middle)
            const extractedFullName3 = `${extractedFirst} ${extractedLast}`.replace(/\s+/g, ' ').trim();
            const registeredFullName3 = `${regFirst} ${regLast}`.replace(/\s+/g, ' ').trim();


            // Check all format combinations
            const fullNameSimilarity1 = calculateSimilarity(extractedFullName1, registeredFullName1);
            const fullNameSimilarity2 = calculateSimilarity(extractedFullName2, registeredFullName2);
            const fullNameSimilarity3 = calculateSimilarity(extractedFullName1, registeredFullName2);
            const fullNameSimilarity4 = calculateSimilarity(extractedFullName2, registeredFullName1);
            const fullNameSimilarity5 = calculateSimilarity(extractedFullName3, registeredFullName3);

            const maxFullNameSimilarity = Math.max(
                fullNameSimilarity1,
                fullNameSimilarity2,
                fullNameSimilarity3,
                fullNameSimilarity4,
                fullNameSimilarity5
            );


            if (maxFullNameSimilarity >= SIMILARITY_THRESHOLD) {

                // ═══════════════════════════════════════════════════════════════════════════
                // FINAL STRICT VALIDATION LAYER - Letter-by-letter matching
                // Even if full name similarity is high, perform exact character comparison
                // ═══════════════════════════════════════════════════════════════════════════

                const strictResult = performStrictNameMatching(
                    extractedFirstName, extractedMiddleName, extractedLastName, extractedSuffix,
                    registeredFirstName, registeredMiddleName, registeredLastName, registeredSuffix
                );

                if (!strictResult.passed) {
                    // Strict validation failed - reject the match

                    return {
                        matches: false,
                        reason: `Strict validation failed: ${strictResult.message}`,
                        confidence: 0,
                        strictValidation: strictResult,
                        details: {
                            firstMatches: false,
                            middleMatches: false,
                            lastMatches: false,
                            extractedName: `${extractedLastName}, ${extractedFirstName} ${extractedMiddleName} ${extractedSuffix}`.trim(),
                            registeredName: `${registeredLastName}, ${registeredFirstName} ${registeredMiddleName} ${registeredSuffix || ''}`.trim(),
                            strictValidationDetails: strictResult.details
                        }
                    };
                }

                // Both full name similarity AND strict validation passed

                return {
                    matches: true,
                    reason: `Names match - Full name comparison (${maxFullNameSimilarity}% similarity) + Strict letter-by-letter validation`,
                    confidence: maxFullNameSimilarity,
                    strictValidation: strictResult
                };
            }

        } else {
        }

        // FINAL RESULT: Name mismatch
        const overallSimilarity = Math.round((lastSimilarity * 0.4 + firstSimilarity * 0.4 + middleSimilarity * 0.2));


        let reason = 'Name mismatch: ';
        const issues = [];

        if (!lastMatches) issues.push(`Last name (${lastSimilarity}%)`);
        if (!firstMatches) issues.push(`First name (${firstSimilarity}%)`);
        if (!middleMatches) issues.push(`Middle name (${middleSimilarity}%)`);
        // Suffix is optional, don't report as issue

        reason += issues.join(', ');

        return {
            matches: false,
            reason: reason.trim(),
            confidence: overallSimilarity,
            details: {
                firstMatches,
                middleMatches,
                lastMatches,
                suffixMatches,
                firstSimilarity,
                middleSimilarity,
                lastSimilarity,
                overallSimilarity,
                extractedName: `${extractedLastName}, ${extractedFirstName} ${extractedMiddleName} ${extractedSuffix}`.trim(),
                registeredName: `${registeredLastName}, ${registeredFirstName} ${registeredMiddleName} ${registeredSuffix || ''}`.trim()
            }
        };
    }

    // Extract barangay from position before "Davao City" in address
    function extractBarangayFromAddressPosition(address) {
        if (!address) {
            return null;
        }

        const cleanAddress = address.trim();
        const upperAddress = cleanAddress.toUpperCase();


        // List of Davao City variations to look for
        const davaoCityVariations = [
            'DAVAO CITY',
            'DAVAO',
            'DAVAO CITY, PHILIPPINES',
            'DAVAO CITY PHILIPPINES',
            'DAVAO, PHILIPPINES'
        ];

        // Find which variation exists in the address
        let davaoCityPattern = null;
        let davaoCityIndex = -1;

        for (const variation of davaoCityVariations) {
            const index = upperAddress.indexOf(variation);
            if (index !== -1) {
                davaoCityPattern = variation;
                davaoCityIndex = index;
                break;
            }
        }

        if (davaoCityIndex === -1) {
            return null;
        }

        // Extract text before "Davao City"
        const textBeforeDavao = cleanAddress.substring(0, davaoCityIndex).trim();

        if (!textBeforeDavao) {
            return null;
        }

        // Split by common separators (comma, dash, etc.)
        const parts = textBeforeDavao.split(/[,\-|\/]/);

        // Get the last part (closest to Davao City = most likely the barangay)
        const lastPart = parts[parts.length - 1].trim();

        if (!lastPart) {
            return null;
        }

        // Match this against our barangay list
        const upperLastPart = lastPart.toUpperCase();

        // Search for exact or close match in barangay list
        const sortedBarangays = [...DAVAO_CITY_BARANGAYS].sort((a, b) => b.length - a.length);

        for (const barangay of sortedBarangays) {
            const upperBarangay = barangay.toUpperCase();

            // Remove common prefixes for matching
            let cleanedLastPart = upperLastPart
                .replace(/^BRGY\.?\s*/i, '')
                .replace(/^BARANGAY\s*/i, '')
                .trim();

            // Exact match
            if (cleanedLastPart === upperBarangay) {
                return barangay;
            }

            // Contains match (the candidate contains the barangay name)
            if (cleanedLastPart.includes(upperBarangay)) {
                return barangay;
            }

            // Reverse: barangay contains the candidate (for abbreviated names)
            if (upperBarangay.includes(cleanedLastPart) && cleanedLastPart.length >= 4) {
                return barangay;
            }
        }


        return null;
    }

    // Extract barangay from address text (full text search)
    function extractBarangayFromAddress(text) {
        if (!text) {
            return null;
        }

        // Clean and normalize the text
        const cleanText = text.trim();
        const upperText = cleanText.toUpperCase();


        // Try to find a match in our barangay list
        // Search for longer names first to avoid partial matches
        const sortedBarangays = [...DAVAO_CITY_BARANGAYS].sort((a, b) => b.length - a.length);

        let matchesFound = [];

        for (const barangay of sortedBarangays) {
            const upperBarangay = barangay.toUpperCase();

            // Check for exact word match (with word boundaries)
            const escapedBarangay = upperBarangay.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            const wordBoundaryPattern = new RegExp(`\\b${escapedBarangay}\\b`);

            if (wordBoundaryPattern.test(upperText)) {
                return barangay;
            }

            // Check for match with common abbreviations
            const withBrgy = `BRGY ${upperBarangay}`;
            const withBrgyDot = `BRGY. ${upperBarangay}`;
            const withBarangay = `BARANGAY ${upperBarangay}`;

            if (upperText.includes(withBrgy) || upperText.includes(withBrgyDot) || upperText.includes(withBarangay)) {
                return barangay;
            }

            // Check for substring match (record for debugging)
            if (upperText.includes(upperBarangay)) {
                matchesFound.push(barangay);
            }
        }

        // If we have substring matches, return the first one (longest due to sorting)
        if (matchesFound.length > 0) {
            return matchesFound[0];
        }


        debugLog('BARANGAY-EXTRACTION', {
            searchText: text,
            found: null
        });

        return null;
    }

    /**
     * Get Transaction Data from ID Analyzer API v2
     * Endpoint: GET /api/IdAnalyzer/transaction/{transactionId}
     * Reference: https://developer.idanalyzer.com/reference/get-transaction-transactionid
     * 
     * Retrieves all extracted data from a previously processed ID scan including:
     * - Personal info (firstName, middleName, lastName, dob, sex)
     * - Document info (documentNumber, documentType, documentName)
     * - Address info (address - concatenated address1 + address2)
     * 
     * @param {string} transactionId - The transaction ID returned from a previous scan
     * @returns {Promise<Object>} Transaction data or null if error
     */
    async function getTransactionData(transactionId) {
        if (!transactionId) {
            return null;
        }

        try {
            debugLog('GET-TRANSACTION', { transactionId });

            const response = await fetch(`${ID_ANALYZER_CONFIG.TRANSACTION_ENDPOINT}/${transactionId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();
            debugLog('TRANSACTION-RESULT', result);

            if (!result.success) {
                return null;
            }

            // Return the transaction data with all extracted fields
            return {
                transactionId: result.transactionId,
                decision: result.decision,
                verificationPassed: result.verificationPassed,
                warnings: result.warnings,
                
                // Extracted data fields mapped for account verification
                data: {
                    // Personal Information
                    firstName: result.data?.firstName || '',
                    middleName: result.data?.middleName || '',
                    lastName: result.data?.lastName || '',
                    suffix: result.data?.suffix || '',
                    fullName: result.data?.fullName || '',
                    dateOfBirth: result.data?.dateOfBirth || '',
                    sex: result.data?.sex || '',
                    nationality: result.data?.nationality || '',
                    
                    // Document Information
                    documentNumber: result.data?.documentNumber || '',
                    documentType: result.data?.documentType || '',
                    documentName: result.data?.documentName || '',
                    issueDate: result.data?.issueDate || '',
                    expiryDate: result.data?.expiryDate || '',
                    
                    // Address Information (concatenated)
                    address: result.data?.address || '',
                    address1: result.data?.address1 || '',
                    address2: result.data?.address2 || '',
                    city: result.data?.city || '',
                    state: result.data?.state || '',
                    postalCode: result.data?.postalCode || '',
                    country: result.data?.country || ''
                },
                
                // Face matching results
                face: {
                    isMatch: result.face?.isMatch || false,
                    similarity: result.face?.similarity || 0,
                    confidence: result.face?.confidence || 0
                }
            };
        } catch (error) {
            return null;
        }
    }

    // Process image with ID Analyzer API v2 (replaces OCR.space)
    async function processImageWithOCR(imageSrc, isBack) {
        // Rate limiting check
        const now = Date.now();
        const timeSinceLastCall = now - lastAPICallTime;

        if (timeSinceLastCall < MIN_TIME_BETWEEN_CALLS) {
            const waitTime = MIN_TIME_BETWEEN_CALLS - timeSinceLastCall;
            status.innerHTML = `<i class="fas fa-clock mr-2"></i>Please wait ${Math.ceil(waitTime / 1000)}s before processing another image...`;
            status.className = 'text-sm text-blue-600 mt-2';

            // Wait asynchronously
            await new Promise(resolve => setTimeout(resolve, waitTime));
            // Retry after waiting
            return processImageWithOCR(imageSrc, isBack);
        }

        lastAPICallTime = now;

        // Progress messages for ID Analyzer
        const ocrProgressMessages = [
            { percent: 10, message: '<i class="fas fa-upload fa-spin mr-2"></i>Uploading your ID photo...' },
            { percent: 25, message: '<i class="fas fa-shield-alt fa-spin mr-2"></i>Checking your ID...' },
            { percent: 45, message: '<i class="fas fa-search fa-spin mr-2"></i>Reading your information...' }
        ];

        let currentMessageIndex = 0;
        status.innerHTML = ocrProgressMessages[0].message;
        progressBar.classList.remove('hidden');
        progress.style.width = ocrProgressMessages[0].percent + '%';

        // Show progress with realistic timing
        const progressInterval = setInterval(() => {
            if (currentMessageIndex < ocrProgressMessages.length - 1) {
                currentMessageIndex++;
                progress.style.width = ocrProgressMessages[currentMessageIndex].percent + '%';
                status.innerHTML = ocrProgressMessages[currentMessageIndex].message;
            }
        }, 800);

        try {
            // ═══════════════════════════════════════════════════════════════════════════
            // UPDATED FLOW: ID SCAN FIRST, THEN FACE VERIFICATION
            // ═══════════════════════════════════════════════════════════════════════════
            // Step 1: Scan ID document ONLY (no face parameter)
            // Step 2: After ID scan succeeds, perform face verification separately
            // This ensures ID scanning completes before face matching begins
            // ═══════════════════════════════════════════════════════════════════════════


            // Build request payload for ID Analyzer Standard ID Scan API
            // NO SELFIE - ID scan only
            const requestPayload = {
                documentImage: imageSrc,
                selfieFileName: null,    // NULL: Face verification will be done separately after ID scan
                backImage: isBack ? imageSrc : null
            };

            // ═══════════════════════════════════════════════════════════════════════════
            // CONSOLE LOG: JSON DATA BEING SENT TO ID ANALYZER API
            // ═══════════════════════════════════════════════════════════════════════════

            // Call ID Analyzer Standard ID Scan API
            // Endpoint: POST /api/IdAnalyzer/scan
            // If 'face' parameter is included, API will perform face matching automatically
            const response = await fetch(ID_ANALYZER_CONFIG.SCAN_ENDPOINT, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(requestPayload)
            });

            const result = await response.json();
            clearInterval(progressInterval);

            // ═══════════════════════════════════════════════════════════════════════════
            // CONSOLE LOG: JSON RESPONSE FROM ID ANALYZER API
            // ═══════════════════════════════════════════════════════════════════════════
            if (result.data) {
                if (result.data.faceMatch !== undefined) {
                }
            }

            // Update progress
            if (progress) progress.style.width = '60%';
            if (status) {
                status.innerHTML = '<i class="fas fa-file-alt fa-pulse mr-2"></i>Processing extracted data...';
            }

            if (!result.success) {
                debugLog('ID-ANALYZER-ERROR', {
                    errorMessage: result.error,
                    errorCode: result.errorCode
                });

                // Show specific error message from API
                let errorMsg = result.error || 'Unknown API error';
                if (result.errorCode) {
                    errorMsg += ` (Code: ${result.errorCode})`;
                }

                showVerificationErrorModal(`ID Analyzer API Error:\n\n${errorMsg}\n\nPlease try again with a clearer image.`);
                return;
            }

            // Check if data was returned
            const data = result.data;
            if (!data) {
                const title = 'No Data Extracted';
                const message = 'Unable to extract any information from the uploaded image.';
                const suggestions = [
                    'Verify the image contains a valid Philippine ID',
                    'Ensure the image is clear and properly oriented',
                    'Try uploading a better quality photo'
                ];
                showErrorModal(title, message, suggestions);
                return;
            }

            // ═══════════════════════════════════════════════════════════════════════════
            // STEP 1: CHECK API DECISION
            // ═══════════════════════════════════════════════════════════════════════════
            // ID Analyzer v2 API returns one of three decision values:
            // - "accept": No thresholds met, verification passed
            // - "review": Review threshold met, requires manual review
            // - "reject": Reject threshold met, verification failed
            // Reference: https://developer.idanalyzer.com/docs/decision-flow
            // ═══════════════════════════════════════════════════════════════════════════

            const apiDecision = (result.decision || '').toLowerCase();

            // Only "accept" decision is allowed to proceed
            // "review" and "reject" decisions must be handled
            if (apiDecision === 'reject') {
                const title = 'Verification Rejected';
                const message = 'The ID verification failed quality checks and cannot be accepted.';
                const suggestions = [
                    'Use a clear, well-lit photo of your ID',
                    'Ensure all text is readable and not blurry',
                    'Make sure the entire ID is visible',
                    'Avoid glare, shadows, or damage on the ID'
                ];
                if (result.warnings && result.warnings.length > 0) {
                    suggestions.unshift(`Issues detected: ${result.warnings.join(', ')}`);
                }
                showErrorModal(title, message, suggestions);
                return;
            }

            if (apiDecision === 'review') {
                const title = 'Manual Review Required';
                const message = 'The ID image quality needs improvement before automatic verification can proceed.';
                const suggestions = [
                    'Upload a clearer, higher quality image',
                    'Ensure proper lighting without glare or shadows',
                    'Make sure all text is sharp and readable',
                    'Verify the entire ID is visible in the frame'
                ];
                if (result.warnings && result.warnings.length > 0) {
                    suggestions.unshift(`Issues found: ${result.warnings.join(', ')}`);
                }
                showWarningModal(title, message, suggestions);
                return;
            }

            if (apiDecision !== 'accept') {
                const title = 'Verification Error';
                const message = `Unexpected verification response received. Decision: ${result.decision || 'Unknown'}`;
                const suggestions = [
                    'Try uploading your ID again',
                    'Contact support if this issue persists'
                ];
                showErrorModal(title, message, suggestions);
                return;
            }


            // ═══════════════════════════════════════════════════════════════════════════
            // STEP 2: DAVAO CITY RESIDENCE VALIDATION
            // ═══════════════════════════════════════════════════════════════════════════

            const address1 = data.address1 || data.address || '';
            const address2 = data.address2 || '';
            const city = data.city || '';
            // Use only address1 for the Address field
            const fullAddress = address1;


            // Validate Davao City residence
            const isDavaoCityResident = validateDavaoCityAddress(fullAddress, city, address1, address2);


            if (!isDavaoCityResident) {
                const title = 'Not a Davao City Resident';
                const message = `This system is only available for Davao City residents. Your ID address does not indicate Davao City residence.\n\nDetected Address: ${fullAddress || 'Not detected'}`;
                const suggestions = [
                    'Verify your ID shows a Davao City address',
                    'If you are a Davao City resident, contact the administrator for assistance'
                ];

                showErrorModal(title, message, suggestions);
                return;
            }


            // ═══════════════════════════════════════════════════════════════════════════
            // STEP 3: NAME MATCHING VALIDATION
            // ═══════════════════════════════════════════════════════════════════════════

            // Get registered user name from ViewBag (passed from server)
            const registeredName = window.registeredUserName || {};


            // Perform name matching validation
            const nameMatchResult = validateNameMatch(
                data.firstName || '',
                data.middleName || '',
                data.lastName || '',
                data.suffix || '',
                registeredName.firstName || '',
                registeredName.middleName || '',
                registeredName.lastName || '',
                registeredName.suffix || '',
                null // idType - will be detected later
            );


            if (!nameMatchResult.matches) {
                const idName = `${data.lastName || ''}, ${data.firstName || ''} ${data.middleName || ''} ${data.suffix || ''}`.replace(/\s+/g, ' ').trim();
                const regName = `${registeredName.lastName || ''}, ${registeredName.firstName || ''} ${registeredName.middleName || ''} ${registeredName.suffix || ''}`.replace(/\s+/g, ' ').trim();

                const title = 'Name Mismatch';
                const message = `The name on your ID does not match your registered account name.\n\nRegistered: ${regName}\nID Name: ${idName}`;
                const suggestions = [
                    'Verify you are using the correct account',
                    'Ensure the ID belongs to you',
                    'Check that the ID image is clear and readable'
                ];

                showErrorModal(title, message, suggestions);
                return;
            }


            // ═══════════════════════════════════════════════════════════════════════════
            // ALL VALIDATIONS PASSED - PROCEED TO FORM POPULATION
            // ═══════════════════════════════════════════════════════════════════════════

            // If API explicitly says verification failed, show error with details
            if (data.verificationPassed === false) {
                const missing = [];
                if (!data.firstName && !data.lastName && !data.fullName) missing.push('Name');
                if (!data.documentNumber) missing.push('Document number');

                const title = 'Incomplete Data Extraction';
                const message = `Unable to extract complete information from your ID. Missing: ${missing.join(', ')}`;
                const suggestions = [
                    'Use a clear, well-lit photo',
                    'Ensure all text is readable and sharp',
                    'Make sure the entire ID is visible'
                ];

                showErrorModal(title, message, suggestions);
                return;
            }

            // Extract text representation from API response for ID type detection
            let extractedText = buildTextFromApiResponse(data);
            
            // Debug: Log raw API data

            // Check face verification results from Standard ID Scan
            // If selfie was included, API returns face matching results
            if (selfieImage && data.faceMatch !== undefined) {

                // Store face verification results for later use
                if (typeof FaceVerificationFlow !== 'undefined') {
                    FaceVerificationFlow.lastFaceResult = {
                        isMatch: data.faceMatch,
                        similarity: data.faceSimilarity,
                        confidence: data.faceConfidence,
                        similarityPercentage: data.faceSimilarityPercentage
                    };
                    FaceVerificationFlow.faceVerified = data.faceMatch;
                }
            }

            debugLog('ID-ANALYZER-DATA', {
                documentType: data.documentType,
                documentName: data.documentName,
                firstName: data.firstName,
                lastName: data.lastName,
                documentNumber: data.documentNumber,
                extractedText: extractedText,
                faceMatch: data.faceMatch,
                faceSimilarity: data.faceSimilarity
            });
            
            // Clean text for parsing
            const cleanedText = cleanExtractedText(extractedText);
            lastScanText = cleanedText;

            // === STEP 1: ID TYPE DETECTION ===
            if (status) {
                status.innerHTML = '<i class="fas fa-id-card fa-pulse mr-2"></i>Detecting ID type...';
            }
            if (progress) progress.style.width = '70%';

            // Detect ID type from document type returned by API or text analysis
            let detectedIdType = mapApiDocumentType(data.documentType, data.documentName);

            if (!detectedIdType) {
                detectedIdType = detectIdType(cleanedText);
            }

            const selectedIdType = documentType.value;

            debugLog('ID-DETECTION', {
                apiDocType: data.documentType,
                apiDocName: data.documentName,
                detected: detectedIdType,
                selected: selectedIdType
            });

            // VALIDATION: Check if detected ID type is in allowed list
            if (detectedIdType && !ALLOWED_ID_TYPES.includes(detectedIdType)) {
                showInvalidIdTypeModal(getIdTypeName(detectedIdType));
                return;
            }

            // ALWAYS auto-set ID type when detected
            if (detectedIdType) {
                documentType.value = detectedIdType;
                const detectionConfidence = 95; // Higher confidence with ID Analyzer
                updateIDTypeDisplay(detectedIdType, detectionConfidence);
                if (status) {
                    status.innerHTML = `<i class="fas fa-check-circle mr-2 text-green-600"></i>ID Type: ${getIdTypeName(detectedIdType)}`;
                }
                if (progress) progress.style.width = '75%';
                
                // Toggle back ID upload based on detected ID type
                toggleBackIdUpload(detectedIdType);
            }

            const idTypeToProcess = detectedIdType || selectedIdType;

            // Final check: If still no valid ID type, show error
            if (!idTypeToProcess || !ALLOWED_ID_TYPES.includes(idTypeToProcess)) {
                let errorMessage = 'Unable to detect ID type from the uploaded image.\n\n';
                errorMessage += 'Please ensure:\n';
                errorMessage += '1. The image is clear and well-lit\n';
                errorMessage += '2. All text on the ID is readable\n';
                errorMessage += '3. The entire ID is visible in the image\n';
                errorMessage += '4. You are using one of these ACCEPTED IDs ONLY:\n';
                errorMessage += '   • Philippine National ID (PhilSys)\n';
                errorMessage += '   • Driver\'s License (LTO)\n';
                errorMessage += '   • UMID (Unified Multi-Purpose ID)\n';
                errorMessage += '   ❌ NOT ACCEPTED: PhilHealth, Senior Citizen, PWD, Postal ID, etc.\n\n';

                showVerificationErrorModal(errorMessage);
                return;
            }

            // === STEP 1.5: VALIDATE ID SIDE (FRONT vs BACK) ===
            const detectedSide = detectIDSide(cleanedText, idTypeToProcess);

            if (detectedSide !== 'unknown') {
                if (!isBack && detectedSide === 'back') {
                    showIDSideMismatchModal('front', 'back', idTypeToProcess);
                    clearUploadArea(isBack);
                    return;
                } else if (isBack && detectedSide === 'front') {
                    showIDSideMismatchModal('back', 'front', idTypeToProcess);
                    clearUploadArea(isBack);
                    return;
                }
            }

            // === STEP 2 & 3: Use API data directly when available ===
            if (status) {
                status.innerHTML = '<i class="fas fa-user-check fa-pulse mr-2"></i>Extracting personal information...';
            }
            if (progress) progress.style.width = '85%';

            // ===== ALWAYS USE API DATA WHEN AVAILABLE =====
            // The API has already extracted the data successfully (verificationPassed check passed above)
            // We should ALWAYS use the API data, even if some fields are missing
            // Individual field validation will happen later in the form submission


            // ALWAYS populate from API data - even if some fields are empty
            // The form validation will handle missing required fields
            populateFormFromApiData(data, idTypeToProcess, isBack);

        } catch (err) {
            clearInterval(progressInterval);
            progressBar.classList.add('hidden');


            let errorMessage = 'Unable to process the ID image. ';

            if (err.message.includes('network') || err.message.includes('fetch')) {
                errorMessage = 'Cannot connect to the verification service.\n\n' +
                    'Please check your internet connection and try again.';
                status.innerHTML = '<i class="fas fa-wifi mr-2"></i>Connection issue';
            } else if (err.message.includes('rate') || err.message.includes('limit')) {
                errorMessage = 'Too many verification attempts.\n\n' +
                    'Please wait a moment before trying again.';
                status.innerHTML = '<i class="fas fa-stopwatch mr-2"></i>Please wait';
            } else {
                status.innerHTML = '<i class="fas fa-exclamation-triangle mr-2"></i>Processing error';
                errorMessage = 'An error occurred while processing your ID.\n\n' +
                    'Technical details: ' + err.message + '\n\n' +
                    'Please try again or contact support if the issue persists.';
            }

            showVerificationErrorModal(errorMessage);

            debugLog('ID-ANALYZER-ERROR', {
                message: err.message,
                stack: err.stack,
                timestamp: new Date().toISOString()
            });
        }
    }

    // Build text string from API response for ID type detection
    function buildTextFromApiResponse(data) {
        let parts = [];
        if (data.fullName) parts.push(data.fullName);
        if (data.firstName) parts.push(data.firstName);
        if (data.lastName) parts.push(data.lastName);
        if (data.middleName) parts.push(data.middleName);
        if (data.documentNumber) parts.push(data.documentNumber);
        if (data.documentType) parts.push(data.documentType);
        if (data.documentName) parts.push(data.documentName);
        if (data.address) parts.push(data.address);
        if (data.city) parts.push(data.city);
        if (data.dateOfBirth) parts.push(data.dateOfBirth);
        if (data.sex) parts.push(data.sex);
        if (data.nationality) parts.push(data.nationality);
        if (data.country) parts.push(data.country);
        return parts.join(' ');
    }

    // Map API document type to our internal ID types
    // ID Analyzer documentType codes:
    // - D = Driver's License
    // - I = Identity Card/National ID  
    // - P = Passport
    // - V = Visa
    // - O = Other
    // documentName contains the human-readable name like "Driver License", "National ID", etc.
    //
    // Accepted ID Types for Account Verification:
    // 1. National ID (PhilSys/Philippine Identification)
    // 2. Driver's License (LTO)
    // 3. Unified Multi-Purpose ID (UMID)
    function mapApiDocumentType(apiDocType, apiDocName) {
        const docType = (apiDocType || '').toUpperCase().trim();
        const docName = (apiDocName || '').toUpperCase();


        debugLog('MAP-DOC-TYPE', { docType, docName });

        // Check by documentType code first (most reliable)
        if (docType === 'D') {
            // Driver's License
            return 'driver-license';
        }

        if (docType === 'I') {
            // Identity card - check documentName for specific type
            // Unified Multi-Purpose ID (UMID)
            if (docName.includes('UMID') || docName.includes('UNIFIED') ||
                docName.includes('MULTI-PURPOSE') || docName.includes('MULTIPURPOSE') ||
                docName.includes('SSS')) {
                return 'umid';
            }
            // Default to National ID for identity cards
            return 'phil-id';
        }

        // Fallback to documentName parsing

        // National ID (PhilSys/Philippine Identification)
        if (docName.includes('PHILSYS') || docName.includes('PHILIPPINE IDENTIFICATION') ||
            docName.includes('NATIONAL ID') || docName.includes('NATIONAL IDENTIFICATION') ||
            docName.includes('PSN') || docName.includes('PCN') ||
            docName.includes('PAMBANSANG')) {
            return 'phil-id';
        }

        // Driver's License
        if (docName.includes('DRIVER') || docName.includes('LICENSE') ||
            docName.includes('LTO') || docName.includes('LISENSYA')) {
            return 'driver-license';
        }

        // Unified Multi-Purpose ID (UMID)
        if (docName.includes('UMID') || docName.includes('UNIFIED') ||
            docName.includes('MULTI-PURPOSE') || docName.includes('MULTIPURPOSE') ||
            docName.includes('SSS')) {
            return 'umid';
        }

        // If documentType is set but not matched, log for debugging
        if (docType) {
        } else {
        }

        return null;
    }

    // Populate form fields from API structured data
    // Field mapping per ID Analyzer API v2:
    // - firstName -> "First Name"
    // - middleName -> "Middle Name"
    // - lastName -> "Last Name"
    // - suffix -> "Suffix" (optional)
    // - dateOfBirth -> "Date of Birth"
    // - documentNumber -> "Document Number" (ID Number)
    // - documentName -> "Document Name" (ID Type)
    // - address -> Concatenated "Address 1" + "Address 2"
    function populateFormFromApiData(data, idType, isBack) {

        debugLog('POPULATE-FROM-API', { data, idType, isBack });

        // Only populate from front side data (not back)
        if (isBack) {
            // For back side, just validate and show success
            if (progress) progress.style.width = '100%';
            if (status) {
                status.innerHTML = '<i class="fas fa-check-circle mr-2 text-green-600"></i>Back ID processed successfully';
            }
            return;
        }

        // Extract names from API response (with fallbacks)
        let firstName = data.firstName || '';
        let lastName = data.lastName || '';
        let middleName = data.middleName || '';
        let suffix = data.suffix || '';


        // If individual names not available, parse from fullName
        if (!firstName && !lastName && data.fullName) {
            const nameParts = parseFullName(data.fullName);
            firstName = nameParts.firstName;
            lastName = nameParts.lastName;
            middleName = nameParts.middleName;
        }

        // ===== EXTRACT ADDRESS COMPONENTS =====
        // Use only address1 for the Address field
        const address1 = data.address1 || '';
        const address2 = data.address2 || '';
        const city = data.city || '';
        const fullAddress = address1; // Only use address1
        const address = data.address || fullAddress;


        // Extract Barangay from address
        // Barangay is usually in address1 or address2
        let barangay = '';
        const combinedAddress = `${address1} ${address2}`.toLowerCase();

        // Common barangay patterns in Philippine IDs
        const barangayPatterns = [
            /b(?:a)?r(?:a)?ng(?:a)?y\.?\s+([^,\n]+)/i,  // Matches "Brgy", "Barangay", "Bgy"
            /brgy\.?\s+([^,\n]+)/i,
            /barangay\s+([^,\n]+)/i
        ];

        for (const pattern of barangayPatterns) {
            const match = combinedAddress.match(pattern);
            if (match && match[1]) {
                barangay = match[1].trim();
                // Clean up common suffixes
                barangay = barangay.replace(/,.*$/, '').trim();
                break;
            }
        }


        // Get other fields
        const dateOfBirth = data.dateOfBirth || '';
        const sex = data.sex || '';
        const civilStatus = data.civilStatus || '';  // From back ID (National ID only)
        const documentNumber = data.documentNumber || '';
        const documentName = data.documentName || '';

        // Update ID type from documentName if available
        if (documentName && documentType) {
            // Map document name to internal ID type
            const mappedIdType = mapDocumentNameToIdType(documentName);
            if (mappedIdType) {
                documentType.value = mappedIdType;
                updateIDTypeDisplay(mappedIdType, 95);
            }
        }

        // Call existing form population function with mapped values
        // Parameters: idNumber, firstName, middleName, lastName, birthdate, sex, civilStatus, suffix, barangay, fullOcrText, idType, confidence
        updateFormFieldsAdvanced(
            documentNumber,     // ID Number (from "Document Number")
            firstName,          // First Name
            middleName,         // Middle Name
            lastName,           // Last Name
            dateOfBirth,        // Birthdate (from "Date of Birth")
            sex,                // Sex/Gender (from ID)
            civilStatus,        // Civil Status (from back ID - National ID only)
            suffix,             // Suffix
            barangay,           // Barangay (extracted from address)
            fullAddress,        // Full address
            idType,             // ID Type
            95                  // Confidence
        );

        // Populate address field if it exists
        const addressField = document.getElementById('address') || document.getElementById('Address');
        if (addressField && fullAddress) {
            addressField.value = fullAddress;
        }

        // ===== DAVAO CITY VALIDATION (IMPROVED) =====
        // Validate address for Davao City residency with multiple patterns
        const isDavaoCityResult = validateDavaoCityAddress(fullAddress, city, address1, address2);
        showDavaoVerificationResult(isDavaoCityResult, fullAddress || city || 'Not detected', idType);

        // Complete ID scan progress
        if (progress) progress.style.width = '100%';
        if (status) {
            status.innerHTML = '<i class="fas fa-check-circle mr-2 text-green-600"></i>✅ Step 1 Complete: ID Scanned Successfully';
        }


        // NOTE: Face verification is now done together with ID scan in the same API call
        // The selfie is sent along with the ID document, so no separate face verification step needed

    }

    /**
     * Validate if address is from Davao City
     * Uses address and city fields from ID Analyzer API
     */
    function validateDavaoCityAddress(address, city, address1, address2) {
        // Combine all address fields for comprehensive checking
        const fullAddressText = `${address || ''} ${city || ''} ${address1 || ''} ${address2 || ''}`.toUpperCase();


        // Comprehensive Davao City patterns
        const davaoPatterns = [
            'DAVAO CITY',
            'CITY OF DAVAO',
            'DAVAO, CITY',
            'DAVAO CITY,',
            'DIGOS CITY',         // Digos City (also part of Davao region)
            'CITY OF DIGOS',
            'TAGUM CITY',         // Tagum City (also part of Davao region)
            'CITY OF TAGUM',
            'DAVAO DEL SUR',      // Province indicator
            'DAVAO REGION',
            'REGION XI',          // Davao Region
            ', DAVAO',            // ", Davao" at end of address
            'DAVAO$',             // "Davao" at end
            'DVO'                 // Abbreviation
        ];

        // Check each pattern
        for (const pattern of davaoPatterns) {
            if (fullAddressText.includes(pattern)) {
                return true;
            }
        }

        // Check for known Davao City barangays
        if (DAVAO_CITY_BARANGAYS && DAVAO_CITY_BARANGAYS.length > 0) {
            for (const barangay of DAVAO_CITY_BARANGAYS) {
                if (fullAddressText.includes(barangay.toUpperCase())) {
                    return true;
                }
            }
        }

        return false;
    }

    // Map document name from API to internal ID type
    function mapDocumentNameToIdType(documentName) {
        const name = (documentName || '').toUpperCase();
        
        if (name.includes('PHILSYS') || name.includes('NATIONAL ID') || name.includes('PSN')) {
            return 'phil-id';
        }
        if (name.includes('DRIVER') || name.includes('LICENSE') || name.includes('LTO')) {
            return 'driver-license';
        }
        if (name.includes('UMID') || name.includes('UNIFIED') || name.includes('SSS')) {
            return 'umid';
        }
        
        return null;
    }

    // Parse full name into components
    function parseFullName(fullName) {
        if (!fullName) return { firstName: '', lastName: '', middleName: '' };
        
        const parts = fullName.trim().split(/\s+/);
        if (parts.length === 1) {
            return { firstName: parts[0], lastName: '', middleName: '' };
        } else if (parts.length === 2) {
            return { firstName: parts[0], lastName: parts[1], middleName: '' };
        } else {
            // Assume: FirstName MiddleName(s) LastName
            return {
                firstName: parts[0],
                middleName: parts.slice(1, -1).join(' '),
                lastName: parts[parts.length - 1]
            };
        }
    }

    // Get friendly ID type name
    // Accepted ID Types:
    // 1. National ID
    // 2. Driver's License
    // 3. Unified Multi-Purpose ID
    function getIdTypeName(idType) {
        const idTypeNames = {
            'driver-license': "Driver's License",
            'phil-id': 'National ID',
            'umid': 'Unified Multi-Purpose ID'
        };
        return idTypeNames[idType] || idType;
    }

    // DETECT ID SIDE (FRONT vs BACK) - INTELLIGENT DETECTION
    function detectIDSide(text, idType) {
        if (!text || !idType) return 'unknown';

        const upperText = text.toUpperCase();

        let frontScore = 0;
        let backScore = 0;
        const detectedIndicators = { front: [], back: [] };

        switch (idType) {
            case 'driver-license':
                // Driver's License Front Indicators (weighted scoring)
                const dlFrontIndicators = {
                    'RESTRICTION CODE': 10,
                    'RESTRICTION': 8,
                    'LICENSE NO': 10,
                    'LICENSE NUMBER': 10,
                    'LISENSYA': 9,
                    'GIVEN NAME': 7,
                    'LAST NAME': 7,
                    'APELYIDO': 7,
                    'FIRST NAME': 7,
                    'MIDDLE NAME': 6,
                    'DATE OF BIRTH': 8,
                    'NATIONALITY': 6,
                    'WEIGHT': 5,
                    'HEIGHT': 5,
                    'EYES': 4,
                    'AGENCY CODE': 7,
                    'EXPIRATION DATE': 6,
                    'NON-PROFESSIONAL': 8,
                    'PROFESSIONAL': 8
                };

                // Driver's License Back Indicators
                const dlBackIndicators = {
                    'CONDITIONS': 10,
                    'REMARKS': 8,
                    'IN CASE OF EMERGENCY': 9,
                    'NOTIFY': 7,
                    'EMERGENCY CONTACT': 9,
                    'BLOOD TYPE': 6
                };

                // Score front indicators
                for (const [keyword, weight] of Object.entries(dlFrontIndicators)) {
                    if (upperText.includes(keyword)) {
                        frontScore += weight;
                        detectedIndicators.front.push(keyword);
                    }
                }

                // Score back indicators
                for (const [keyword, weight] of Object.entries(dlBackIndicators)) {
                    if (upperText.includes(keyword)) {
                        backScore += weight;
                        detectedIndicators.back.push(keyword);
                    }
                }

                break;

            case 'phil-id':
                // National ID Front Indicators (has names, PhilSys number, birthdate)
                const philFrontIndicators = {
                    'PCN': 10,
                    'PHILSYS NUMBER': 10,
                    'PHILSYS NO': 10,
                    'PAMBANSANG': 9,
                    'GIVEN NAME': 9,
                    'PANGALAN/GIVEN NAME': 9,
                    'PANGALAN': 8,
                    'FIRST NAME': 9,
                    'LAST NAME': 9,
                    'SURNAME': 9,
                    'APELYIDO': 8,
                    'MIDDLE NAME': 8,
                    'DATE OF BIRTH': 10,
                    'KAPANGANAKAN': 10,
                    'REPUBLIC OF THE PHILIPPINES': 6,
                    'PHILIPPINE IDENTIFICATION': 7
                };

                // National ID Back Indicators (ONLY gender/sex and civil/marital status)
                const philBackIndicators = {
                    'SEX': 10,
                    'KASARIAN': 10,
                    'GENDER': 10,
                    'MARITAL STATUS': 10,
                    'CIVIL STATUS': 10,
                    'KALAGAYAN': 10,
                    'MALE': 8,
                    'FEMALE': 8,
                    'LALAKI': 8,
                    'BABAE': 8,
                    'SINGLE': 7,
                    'MARRIED': 7,
                    'WIDOW': 7,
                    'WIDOWED': 7,
                    'SEPARATED': 7,
                    'KASAL': 7,
                    'BALO': 7,
                    'WALANG ASAWA': 7
                };

                // Score front indicators
                for (const [keyword, weight] of Object.entries(philFrontIndicators)) {
                    if (upperText.includes(keyword)) {
                        frontScore += weight;
                        detectedIndicators.front.push(keyword);
                    }
                }

                // Score back indicators
                for (const [keyword, weight] of Object.entries(philBackIndicators)) {
                    if (upperText.includes(keyword)) {
                        backScore += weight;
                        detectedIndicators.back.push(keyword);
                    }
                }

                break;

            case 'umid':
                // UMID Front Indicators
                const umidFrontIndicators = {
                    'CRN': 10,
                    'CARD REFERENCE NUMBER': 10,
                    'UMID NUMBER': 10,
                    'UMID NO': 9,
                    'FULL NAME': 7,
                    'GIVEN NAME': 7,
                    'SURNAME': 7,
                    'DATE OF BIRTH': 8,
                    'SEX': 6
                };

                // UMID Back Indicators
                const umidBackIndicators = {
                    'PERMANENT ADDRESS': 10,
                    'SIGNATURE': 9,
                    'PLACE OF BIRTH': 8,
                    'CONTACT NUMBER': 8,
                    'EMERGENCY CONTACT': 8,
                    'TIN': 7,
                    'SSS': 7,
                    'GSIS': 7
                };

                // Score front indicators
                for (const [keyword, weight] of Object.entries(umidFrontIndicators)) {
                    if (upperText.includes(keyword)) {
                        frontScore += weight;
                        detectedIndicators.front.push(keyword);
                    }
                }

                // Score back indicators
                for (const [keyword, weight] of Object.entries(umidBackIndicators)) {
                    if (upperText.includes(keyword)) {
                        backScore += weight;
                        detectedIndicators.back.push(keyword);
                    }
                }

                break;

            default:
                return 'unknown';
        }

        // Determine side based on scores

        let detectedSide = 'unknown';
        const minScoreThreshold = 5; // Minimum score to make a determination

        if (frontScore > backScore && frontScore >= minScoreThreshold) {
            detectedSide = 'front';
        } else if (backScore > frontScore && backScore >= minScoreThreshold) {
            detectedSide = 'back';
        } else if (frontScore === backScore && frontScore >= minScoreThreshold) {
            // If scores are equal, prefer the side with more indicators
            if (detectedIndicators.front.length > detectedIndicators.back.length) {
                detectedSide = 'front';
            } else if (detectedIndicators.back.length > detectedIndicators.front.length) {
                detectedSide = 'back';
            } else {
            }
        } else {
        }

        return detectedSide;
    }

    // SHOW ID SIDE MISMATCH MODAL
    function showIDSideMismatchModal(expectedSide, detectedSide, idType) {

        const modal = document.getElementById('wrongIdModal');
        if (!modal) {
            alert(`Wrong ID Side\n\nYou uploaded the ${detectedSide} side of your ${getIdTypeName(idType)}, but we need the ${expectedSide} side.\n\nPlease upload the ${expectedSide} side of your ID.`);
            return;
        }

        const titleElement = document.getElementById('wrong-id-title');
        const messageElement = document.getElementById('wrong-id-message');
        const selectedTypeElement = document.getElementById('selected-type');
        const detectedTypeElement = document.getElementById('detected-type');

        if (titleElement) {
            titleElement.textContent = 'Wrong ID Side Uploaded';
        }

        if (messageElement) {
            const idTypeName = getIdTypeName(idType);
            messageElement.innerHTML = `
                <p class="mb-3">
                    You uploaded the <strong class="text-red-600">${detectedSide.toUpperCase()}</strong> side of your ${idTypeName},
                    but this upload area requires the <strong class="text-green-600">${expectedSide.toUpperCase()}</strong> side.
                </p>
                <p class="text-sm text-gray-700">
                    <i class="fas fa-info-circle text-blue-500 mr-1"></i>
                    Please flip your ID and upload the <strong>${expectedSide}</strong> side to continue with verification.
                </p>
            `;
        }

        if (selectedTypeElement) {
            selectedTypeElement.textContent = `${expectedSide.charAt(0).toUpperCase() + expectedSide.slice(1)} ID (Required)`;
        }

        if (detectedTypeElement) {
            detectedTypeElement.textContent = `${detectedSide.charAt(0).toUpperCase() + detectedSide.slice(1)} ID (Uploaded)`;
        }

        // Show modal using Bootstrap 5
        const bootstrapModal = new bootstrap.Modal(modal);
        bootstrapModal.show();
    }

    // CLEAR UPLOAD AREA
    function clearUploadArea(isBack) {
        const fileInput = isBack ? fileBack : fileFront;
        const preview = isBack ? imagePreviewBack : imagePreview;
        const uploadArea = isBack ? uploadBack : uploadFront;

        if (fileInput) {
            fileInput.value = '';
        }

        if (preview) {
            preview.src = '';
            preview.classList.add('hidden');
        }

        if (uploadArea) {
            const uploadContent = uploadArea.querySelector('.id-upload-content');
            if (uploadContent) {
                uploadContent.classList.remove('preview-mode');
            }
            uploadArea.classList.remove('active', 'has-image');
        }

        // Hide progress bar and reset status
        if (progressBar) {
            progressBar.classList.add('hidden');
        }

        if (status) {
            status.innerHTML = '<i class="fas fa-info-circle mr-2"></i>Please upload the correct ID side';
            status.className = 'text-sm text-gray-600 mt-2';
        }
    }

    // Enhanced ID Type Detection with Multiple Detection Methods
    function detectIdType(text) {
        if (!text) return null;

        const upperText = text.toUpperCase();

        // === STEP 0: REJECT NON-APPROVED IDs ===
        // Only National ID/PhilSys, Driver's License, and UMID are accepted
        // All other IDs must be explicitly rejected with clear error messages

        // SSS ID detection - NOT ACCEPTED
        if (upperText.includes('SOCIAL SECURITY SYSTEM') ||
            upperText.includes('SSS MEMBER') ||
            upperText.includes('SSS ID') ||
            upperText.includes('S.S.S') ||
            upperText.includes('SISTEMA NG SEGURIDAD SOSYAL') ||
            upperText.includes('SEGURIDAD SOSYAL') ||
            (upperText.includes('SSS') && !upperText.includes('GSIS') && !upperText.includes('UMID') &&
                (upperText.includes('NUMBER') || upperText.includes('CARD') || upperText.includes('MEMBER') || upperText.includes('NUMERO')))) {
            showRejectedIdModal('SSS (Social Security System) ID');
            return 'REJECTED';
        }

        // PhilHealth ID detection
        if (upperText.includes('PHILHEALTH') ||
            upperText.includes('PHILIPPINE HEALTH') ||
            upperText.includes('PHIC') ||
            (upperText.includes('HEALTH') && upperText.includes('INSURANCE'))) {
            showRejectedIdModal('PhilHealth ID');
            return 'REJECTED';
        }

        // Senior Citizen ID detection
        if (upperText.includes('SENIOR CITIZEN') ||
            upperText.includes('SENIOR CARD') ||
            upperText.includes('OSCA') ||
            (upperText.includes('SENIOR') && upperText.includes('OFFICE'))) {
            showRejectedIdModal('Senior Citizen ID');
            return 'REJECTED';
        }

        // PWD ID detection
        if (upperText.includes('PWD') ||
            upperText.includes('PERSON WITH DISABILITY') ||
            upperText.includes('PERSONS WITH DISABILITIES') ||
            upperText.includes('DISABILITY AFFAIRS')) {
            showRejectedIdModal('PWD (Person with Disability) ID');
            return 'REJECTED';
        }

        // Postal ID detection
        if (upperText.includes('POSTAL') ||
            upperText.includes('PHILPOST') ||
            upperText.includes('POST OFFICE')) {
            showRejectedIdModal('Postal ID');
            return 'REJECTED';
        }

        // Voter's ID detection
        if (upperText.includes('VOTER') ||
            upperText.includes('COMELEC') ||
            upperText.includes('COMMISSION ON ELECTIONS')) {
            showRejectedIdModal('Voter\'s ID');
            return 'REJECTED';
        }

        // PRC ID detection
        if (upperText.includes('PROFESSIONAL REGULATION COMMISSION') ||
            (upperText.includes('PRC') && upperText.includes('LICENSE'))) {
            showRejectedIdModal('PRC Professional ID');
            return 'REJECTED';
        }

        // TIN ID detection
        if (upperText.includes('TAX IDENTIFICATION') ||
            upperText.includes('TIN CARD') ||
            (upperText.includes('TIN') && upperText.includes('BIR'))) {
            showRejectedIdModal('TIN (Tax Identification Number) Card');
            return 'REJECTED';
        }

        // Barangay ID detection
        if (upperText.includes('BARANGAY ID') ||
            upperText.includes('BARANGAY IDENTIFICATION') ||
            (upperText.includes('BARANGAY') && upperText.includes('RESIDENT'))) {
            showRejectedIdModal('Barangay ID');
            return 'REJECTED';
        }

        // === METHOD 1: STRONG KEYWORD DETECTION (High confidence) ===
        // National ID / PhilSys - ACCEPTED (English + Filipino variations)
        if (upperText.includes('PHILSYS') ||
            upperText.includes('PHILIPPINE IDENTIFICATION SYSTEM') ||
            upperText.includes('PHILIPPINE IDENTIFICATION CARD') ||
            upperText.includes('PHILIPPINE ID') ||
            upperText.includes('NATIONAL ID') ||
            upperText.includes('PAMBANSANG PAGKAKAKILANLAN') || // Filipino: "National Identity"
            upperText.includes('PAMBANSANG ID') || // Filipino: "National ID"
            upperText.includes('PAMBANSANG PAGKAKILANLAN') || // Alternate spelling
            upperText.includes('PCN') || // PhilSys Card Number
            (upperText.includes('PHILIPPINE') && upperText.includes('IDENTIFICATION')) ||
            (upperText.includes('PAMBANSANG') && upperText.includes('PAGKAKAKILANLAN'))) {
            return 'phil-id';
        }

        // UMID - ACCEPTED (English + Filipino variations)
        if (upperText.includes('UMID') ||
            upperText.includes('UNIFIED MULTI-PURPOSE ID') ||
            upperText.includes('UNIFIED MULTIPURPOSE ID') ||
            upperText.includes('UNIFIED MULTI PURPOSE ID') ||
            upperText.includes('UNIFIED ID') ||
            upperText.includes('UMID CARD') ||
            upperText.includes('UNIFIED MULTIPURPOSE IDENTIFICATION') ||
            upperText.includes('PINAG-ISANG MULTI-LAYUNING ID') || // Filipino
            upperText.includes('PINAG ISANG MULTI LAYUNING ID') || // Filipino (no dash)
            (upperText.includes('UNIFIED') && upperText.includes('MULTIPURPOSE'))) {
            return 'umid';
        }

        // Driver's License - ACCEPTED (English + Filipino variations)
        if (upperText.includes('LAND TRANSPORTATION OFFICE') ||
            upperText.includes('DRIVER\'S LICENSE') ||
            upperText.includes('DRIVERS LICENSE') ||
            upperText.includes('LICENSE NO') ||
            upperText.includes('RESTRICTION CODE') ||
            upperText.includes('LISENSYA SA PAGMAMANEHO') || // Filipino: "Driver's License"
            upperText.includes('LISENSIYA SA PAGMAMANEHO') || // Alternate spelling
            upperText.includes('PROFESSIONAL DRIVER') ||
            upperText.includes('NON-PROFESSIONAL DRIVER') ||
            upperText.includes('PROPESYONAL NA DRIVER') || // Filipino
            upperText.includes('DI-PROPESYONAL NA DRIVER') || // Filipino
            (upperText.includes('LTO') && (upperText.includes('LICENSE') || upperText.includes('RESTRICTION') || upperText.includes('LISENSYA')))) {
            return 'driver-license';
        }

        // === METHOD 2: ID NUMBER PATTERN DETECTION ===
        // Each ID has a unique number format

        // UMID CRN: 0000-0000000-0 (4 digits, 7 digits, 1 digit)
        if (/\d{4}-\d{7}-\d{1}/.test(text)) {
            return 'umid';
        }

        // National ID: 0000-0000-0000-0000 (16 digits in 4 groups)
        if (/\d{4}-\d{4}-\d{4}-\d{4}/.test(text)) {
            return 'phil-id';
        }

        // Driver's License: L00-00-000000 (Letter followed by numbers)
        if (/[A-Z]\d{2}-\d{2}-\d{6}/.test(text)) {
            return 'driver-license';
        }

        // === METHOD 3: COMPREHENSIVE INDICATOR SCORING ===

        // Driver's License indicators (weighted)
        const driverLicenseIndicators = {
            // High weight (5 points)
            'DRIVER\'S LICENSE': 5,
            'DRIVERS LICENSE': 5,
            'DRIVER LICENSE': 5,
            'LICENSE TO DRIVE': 5,
            // Medium weight (3 points)
            'NON-PROFESSIONAL': 3,
            'PROFESSIONAL DRIVER': 3,
            'RESTRICTION': 3,
            'AGENCY CODE': 3,
            'BLOOD TYPE': 3,
            'NATIONALITY': 3,
            // Low weight (1 point)
            'LTO': 1,
            'EXPIRATION': 1,
            'CONDITIONS': 1
        };

        // National ID indicators (weighted)
        const nationalIdIndicators = {
            // High weight (5 points)
            'PHILIPPINE IDENTIFICATION': 5,
            'NATIONAL ID': 5,
            'PHILSYS NUMBER': 5,
            'PAMBANSANG PAGKAKAKILANLAN': 5, // Filipino
            'PAMBANSANG PAGKAKILANLAN': 5, // Filipino (alternate)
            'PAMBANSANG ID': 5, // Filipino
            // Medium weight (3 points)
            'PSA': 3,
            'STATISTICS AUTHORITY': 3,
            'FULL NAME': 3,
            'PHILSYS': 3,
            // Low weight (1 point)
            'REPUBLIC': 1,
            'PHILIPPINES': 1,
            'PILIPINAS': 1 // Filipino
        };

        // UMID indicators (weighted)
        const umidIndicators = {
            // High weight (5 points) - already checked above, but keep for completeness
            'UMID CARD': 5,
            'UNIFIED MULTI': 5,
            // Medium weight (3 points)
            'GSIS': 3
        };

        let driverLicenseScore = 0;
        let nationalIdScore = 0;
        let umidScore = 0;

        // Calculate weighted scores
        Object.entries(driverLicenseIndicators).forEach(([indicator, weight]) => {
            if (upperText.includes(indicator)) {
                driverLicenseScore += weight;
            }
        });

        Object.entries(nationalIdIndicators).forEach(([indicator, weight]) => {
            if (upperText.includes(indicator)) {
                nationalIdScore += weight;
            }
        });

        Object.entries(umidIndicators).forEach(([indicator, weight]) => {
            if (upperText.includes(indicator)) {
                umidScore += weight;
            }
        });

        const scores = {
            'driver-license': driverLicenseScore,
            'phil-id': nationalIdScore,
            'umid': umidScore
        };


        // Find highest score (minimum score of 1 required)
        let maxScore = 0;
        let detectedType = null;

        for (const [type, score] of Object.entries(scores)) {
            if (score > maxScore && score >= 1) { // Lowered threshold to 1
                maxScore = score;
                detectedType = type;
            }
        }

        if (detectedType) {
            return detectedType;
        }

        // === METHOD 4: FALLBACK SINGLE KEYWORD DETECTION ===

        if (upperText.includes('LICENSE')) {
            return 'driver-license';
        }

        if (upperText.includes('NATIONAL') || upperText.includes('IDENTIFICATION')) {
            return 'phil-id';
        }

        if (upperText.includes('UNIFIED') || upperText.includes('UMID')) {
            return 'umid';
        }

        return null;
    }

    // Parse Driver's License Front with precision extraction
    function parseDriverLicenseFront(text) {
        const lines = text.split('\n').map(line => line.trim()).filter(line => line);

        lines.forEach((line, index) => {
        });

        debugLog('DL-PARSING', { totalLines: lines.length, lines });

        let extractedData = {
            idNumber: "",
            lastName: "",
            firstName: "",
            middleName: "",
            suffix: "",
            birthdate: "",
            sex: "",
            civilStatus: "",
            address: "",
            barangay: "",
            city: "",
            confidence: {}
        };

        // Extract License Number
        extractedData.idNumber = extractDriverLicenseNumber(lines);
        if (extractedData.idNumber) {
            const validation = validateIDNumber(extractedData.idNumber, 'driver-license');
            extractedData.confidence.idNumber = validation.confidence;
        }

        // Extract names
        const nameResult = extractDriverLicenseName(lines, text);
        extractedData.lastName = nameResult.lastName;
        extractedData.firstName = nameResult.firstName;
        extractedData.middleName = nameResult.middleName;
        extractedData.suffix = nameResult.suffix || "";

        // Validate names
        if (extractedData.lastName) {
            const validation = validateName(extractedData.lastName, 'lastName');
            extractedData.confidence.lastName = validation.confidence;
        }
        if (extractedData.firstName) {
            const validation = validateName(extractedData.firstName, 'firstName');
            extractedData.confidence.firstName = validation.confidence;
        }
        if (extractedData.middleName) {
            const validation = validateName(extractedData.middleName, 'middleName');
            extractedData.confidence.middleName = validation.confidence;
        }

        // Extract birthdate - CRITICAL FOR DRIVER'S LICENSE
        extractedData.birthdate = extractDriverLicenseBirthdate(lines);

        if (extractedData.birthdate) {
            const validation = validateBirthdate(extractedData.birthdate);
            extractedData.confidence.birthdate = validation.confidence;
        } else {
        }

        // Extract gender using unified function with label matching
        extractedData.sex = extractGender(lines);
        extractedData.confidence.sex = extractedData.sex ? 90 : 0;
        if (extractedData.sex) {
        } else {
        }

        // Extract address - MULTI-LINE SUPPORT
        extractedData.address = extractMultiLineAddress(lines);
        if (extractedData.address) {
        } else {
        }

        // Extract barangay (PRIORITY for address components)
        // Driver's licenses typically don't have "BARANGAY" label, so search through address text

        let detectedBarangay = null;

        // PRIORITY 1: Extract from position before "Davao City" (MOST RELIABLE for driver's license)
        if (extractedData.address) {
            detectedBarangay = extractBarangayFromAddressPosition(extractedData.address);

            if (detectedBarangay) {
            } else {
            }
        }

        // PRIORITY 2: Full text search in extracted address (fallback)
        if (!detectedBarangay && extractedData.address) {
            detectedBarangay = extractBarangayFromAddress(extractedData.address);

            if (detectedBarangay) {
            } else {
            }
        }

        // PRIORITY 3: Try label-based extraction (rare but possible)
        if (!detectedBarangay) {
            const barangayResult = extractFieldValue(lines, 'barangay');
            if (barangayResult && barangayResult.value) {
                detectedBarangay = barangayResult.value.trim();
            } else {
            }
        }

        // PRIORITY 4: Search through ALL OCR text as last resort
        if (!detectedBarangay) {
            const fullText = text || lines.join(' ');

            // Try positional extraction on full text first
            detectedBarangay = extractBarangayFromAddressPosition(fullText);

            // If still not found, try full text search
            if (!detectedBarangay) {
                detectedBarangay = extractBarangayFromAddress(fullText);
            }

            if (detectedBarangay) {
            } else {
            }
        }

        // Set the extracted barangay
        if (detectedBarangay) {
            extractedData.barangay = detectedBarangay;
        } else {
            extractedData.barangay = "";
        }

        // Extract city for Davao verification (analyzes complete address)
        extractedData.city = extractDriverLicenseCity(lines);

        // Store extracted data for debugging
        lastExtractedData = extractedData;
        debugLog('DL-EXTRACTED', extractedData);

        // Calculate overall confidence
        const confidenceValues = Object.values(extractedData.confidence).filter(v => v > 0);
        const overallConfidence = confidenceValues.length > 0
            ? Math.round(confidenceValues.reduce((a, b) => a + b, 0) / confidenceValues.length)
            : 0;

        // Log extracted data in labeled format
        logExtractedData(extractedData, 'driver-license', overallConfidence);

        // === STEP 1.5: VALIDATE EXTRACTED DATA (NO LABELS AS VALUES) ===
        const dataValidation = validateExtractedNames(extractedData);
        if (!dataValidation.valid) {
            // Hide progress bar on validation error
            if (progressBar) {
                progressBar.classList.add('hidden');
            }
            if (status) {
                status.innerHTML = '<i class="fas fa-exclamation-circle mr-2 text-red-600"></i>Extraction failed - invalid data detected';
                status.className = 'text-sm text-red-600 mt-2 font-semibold';
            }

            if (resultBox) {
                resultBox.classList.remove('hidden');
                resultBox.className = "p-4 bg-red-50 border border-red-200 rounded-xl text-sm text-red-700";
                resultBox.innerHTML = `
                    <i class="fas fa-exclamation-triangle mr-2"></i>
                    <strong>Extraction Error:</strong> Cannot read ID data properly.
                    <br><small>The following issues were detected:</small>
                    <ul class="mt-2 ml-4 list-disc">
                        ${dataValidation.issues.map(issue => `<li>${issue}</li>`).join('')}
                    </ul>
                    <small class="block mt-2">Please ensure the ID image is clear and well-lit, then try again.</small>
                `;
            }
            return; // STOP - Do not proceed with invalid data
        }

        // === STEP 2: NAME MATCHING VALIDATION (CRITICAL) ===
        if (status) {
            status.innerHTML = '<i class="fas fa-user-check fa-pulse mr-2"></i>Validating name match...';
        }
        if (progress) progress.style.width = '90%';

        const nameValidation = validateNameMatch(
            extractedData.firstName,
            extractedData.middleName,
            extractedData.lastName,
            extractedData.suffix,
            'driver-license' // Enable two-step validation with full name fallback
        );

        if (!nameValidation.matches) {
            // Hide progress bar on name mismatch
            if (progressBar) {
                progressBar.classList.add('hidden');
            }
            if (status) {
                status.innerHTML = '<i class="fas fa-exclamation-circle mr-2 text-red-600"></i>Name validation failed';
                status.className = 'text-sm text-red-600 mt-2 font-semibold';
            }

            showNameMismatchModal(
                nameValidation.details ? nameValidation.details.extractedName : `${extractedData.lastName}, ${extractedData.firstName} ${extractedData.middleName}`,
                nameValidation.details ? nameValidation.details.registeredName : `${registeredLastName}, ${registeredFirstName} ${registeredMiddleName}`,
                nameValidation.details || {}
            );
            return; // BLOCK - Do not proceed with data extraction
        }


        // === STEP 3: DATA EXTRACTION & POPULATION ===
        if (status) {
            status.innerHTML = '<i class="fas fa-database fa-pulse mr-2"></i>Populating form fields...';
        }
        if (progress) progress.style.width = '95%';

        updateFormFieldsAdvanced(
            extractedData.idNumber,
            extractedData.firstName,
            extractedData.middleName,
            extractedData.lastName,
            extractedData.birthdate,
            extractedData.sex,
            extractedData.civilStatus,
            extractedData.suffix,
            extractedData.barangay,
            text,
            'driver-license',
            overallConfidence
        );

        // Check if it's Davao City using CONCATENATED ADDRESS (supports multi-line)
        // FALLBACK: Also check full OCR text if address extraction failed
        let isDavaoCityResult = isDavaoCity(extractedData.address);
        
        // Fallback: If address-based check failed, check the FULL OCR text
        if (!isDavaoCityResult) {
            isDavaoCityResult = isDavaoCity(text);
            if (isDavaoCityResult) {
            }
        }
        
        showDavaoVerificationResult(isDavaoCityResult, extractedData.address || text, 'driver-license');
    }

    // Extract Driver's License Number
    function extractDriverLicenseNumber(lines) {
        // Look for license number patterns
        for (const line of lines) {
            // Pattern: L00-00-000000 (Letter, 2 digits, dash, 2 digits, dash, 6 digits)
            const licensePattern = /[A-Z]\d{2}-\d{2}-\d{6}/;
            const match = line.match(licensePattern);
            if (match) {
                return match[0];
            }

            // Also check for variations without dashes
            const noDashPattern = /[A-Z]\d{10}/;
            const noDashMatch = line.match(noDashPattern);
            if (noDashMatch) {
                const num = noDashMatch[0];
                return `${num.substring(0, 3)}-${num.substring(3, 5)}-${num.substring(5)}`;
            }
        }
        return "";
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // Helper: Check if line contains name-like words (not labels)
    // ═══════════════════════════════════════════════════════════════════════════════
    function containsUnusualWords(line) {
        if (!line || line.trim().length === 0) return false;

        const upperLine = line.toUpperCase().trim();

        // Reject if contains label keywords
        const labelKeywords = [
            'SURNAME', 'GIVEN', 'MIDDLE', 'FIRST', 'LAST', 'NAME',
            'APELYIDO', 'PANGALAN', 'GITNANG', 'UNANG', 'HULING',
            'LICENSE', 'DRIVER', 'NUMBER', 'ADDRESS', 'BIRTHDATE',
            'SEX', 'GENDER', 'NATIONALITY', 'RESTRICTION', 'WEIGHT',
            'HEIGHT', 'BLOOD', 'AGENCY', 'EXPIRATION', 'REPUBLIC'
        ];

        // If line contains any label keyword, reject it
        if (labelKeywords.some(keyword => upperLine.includes(keyword))) {
            return false;
        }

        // Reject if contains numbers (likely ID numbers, dates, etc.)
        if (/\d/.test(line)) {
            return false;
        }

        // Must have at least 2 words of 2+ characters
        const words = line.split(/\s+/).filter(w => w.length >= 2);
        if (words.length < 2) {
            return false;
        }

        // Accept if it looks like a name
        return true;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // Parse Driver's License Full Name into Components
    // ═══════════════════════════════════════════════════════════════════════════════
    // DRIVER'S LICENSE NAME FORMAT: "LASTNAME, FIRSTNAME SUFFIX MIDDLENAME"
    //
    // Example: "ESPANOLA, ANTHONY JR AVENA"
    //   - ESPANOLA = Last Name
    //   - ANTHONY = First Name
    //   - JR = Suffix
    //   - AVENA = Middle Name
    //
    // Common formats found in Philippine Driver's License:
    // 1. "DELA CRUZ, JUAN JR MIGUEL" (with suffix between first and middle)
    // 2. "SANTOS, MARIA CLARA" (no suffix, just first and middle)
    // 3. "REYES, JOSE SR ANTONIO" (with SR suffix)
    //
    // Parsing strategy:
    // - Split by comma: Before comma = Last Name
    // - After comma: First word = First Name, check for suffix, rest = Middle Name
    //
    // Returns: { lastName, firstName, middleName, suffix }
    // ═══════════════════════════════════════════════════════════════════════════════
    function parseDriverLicenseFullName(fullName) {
        if (!fullName || fullName.trim().length === 0) {
            return null;
        }

        let lastName = "", firstName = "", middleName = "", suffix = "";

        // Clean the full name
        fullName = fullName.trim();

        // Valid suffixes pattern
        const suffixPattern = /^(JR|SR|II|III|IV|V|JUNIOR|SENIOR)$/i;

        // ═══════════════════════════════════════════════════════════════════════
        // FORMAT 1: Comma-separated (LASTNAME, FIRSTNAME SUFFIX MIDDLENAME)
        // This is the standard Philippine Driver's License format
        // ═══════════════════════════════════════════════════════════════════════
        if (fullName.includes(',')) {
            const parts = fullName.split(',').map(p => p.trim());
            if (parts.length >= 2) {
                lastName = parts[0].trim();

                // Parse first name, suffix, and middle from second part
                // Format: "FIRSTNAME SUFFIX MIDDLENAME" or "FIRSTNAME MIDDLENAME"
                const remainingWords = parts[1].trim().split(/\s+/).filter(w => w.length > 0);
                
                if (remainingWords.length >= 1) {
                    firstName = remainingWords[0];
                    
                    if (remainingWords.length >= 2) {
                        // Check if second word is a suffix
                        const secondWord = remainingWords[1].toUpperCase().replace(/\./g, '');
                        
                        if (suffixPattern.test(secondWord)) {
                            // Second word is a suffix: FIRSTNAME SUFFIX MIDDLENAME
                            suffix = secondWord;
                            if (suffix === 'JUNIOR') suffix = 'JR';
                            if (suffix === 'SENIOR') suffix = 'SR';
                            // Middle name is everything after the suffix
                            middleName = remainingWords.slice(2).join(' ');
                        } else {
                            // No suffix in second position, check if last word is suffix
                            const lastWord = remainingWords[remainingWords.length - 1].toUpperCase().replace(/\./g, '');
                            
                            if (suffixPattern.test(lastWord) && remainingWords.length >= 3) {
                                // Suffix at end (fallback): FIRSTNAME MIDDLENAME SUFFIX
                                suffix = lastWord;
                                if (suffix === 'JUNIOR') suffix = 'JR';
                                if (suffix === 'SENIOR') suffix = 'SR';
                                middleName = remainingWords.slice(1, -1).join(' ');
                            } else {
                                // No suffix: FIRSTNAME MIDDLENAME
                                middleName = remainingWords.slice(1).join(' ');
                            }
                        }
                    }
                }


                return {
                    lastName: cleanName(lastName),
                    firstName: cleanName(firstName),
                    middleName: cleanName(middleName),
                    suffix
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // FORMAT 2: Space-separated (no comma - less common)
        // Try to detect: LASTNAME FIRSTNAME SUFFIX MIDDLENAME
        // ═══════════════════════════════════════════════════════════════════════
        const nameWords = fullName.split(/\s+/).filter(w => w.length > 0);

        if (nameWords.length >= 3) {
            // Check if any word in positions 2+ is a suffix
            let suffixIndex = -1;
            for (let i = 2; i < nameWords.length; i++) {
                const word = nameWords[i].toUpperCase().replace(/\./g, '');
                if (suffixPattern.test(word)) {
                    suffixIndex = i;
                    suffix = word;
                    if (suffix === 'JUNIOR') suffix = 'JR';
                    if (suffix === 'SENIOR') suffix = 'SR';
                    break;
                }
            }

            // Check for compound last names
            const first2Words = nameWords.slice(0, 2);
            const first3Words = nameWords.slice(0, 3);

            if (isCompoundName(first3Words) && nameWords.length >= 5) {
                // 3-word compound last name
                lastName = first3Words.join(' ');
                firstName = nameWords[3];
                if (suffixIndex > 3) {
                    middleName = nameWords.slice(4, suffixIndex).join(' ') + ' ' + nameWords.slice(suffixIndex + 1).join(' ');
                } else if (suffixIndex === 4) {
                    middleName = nameWords.slice(5).join(' ');
                } else {
                    middleName = nameWords.slice(4).join(' ');
                }
            } else if (isCompoundName(first2Words) && nameWords.length >= 4) {
                // 2-word compound last name
                lastName = first2Words.join(' ');
                firstName = nameWords[2];
                if (suffixIndex > 2) {
                    // Suffix found after first name
                    if (suffixIndex === 3) {
                        middleName = nameWords.slice(4).join(' ');
                    } else {
                        middleName = nameWords.slice(3, suffixIndex).join(' ') + ' ' + nameWords.slice(suffixIndex + 1).join(' ');
                    }
                } else {
                    middleName = nameWords.slice(3).join(' ');
                }
            } else {
                // Simple format: LASTNAME FIRSTNAME SUFFIX MIDDLENAME
                lastName = nameWords[0];
                firstName = nameWords[1];
                if (suffixIndex === 2) {
                    middleName = nameWords.slice(3).join(' ');
                } else if (suffixIndex > 2) {
                    middleName = nameWords.slice(2, suffixIndex).join(' ') + ' ' + nameWords.slice(suffixIndex + 1).join(' ');
                } else {
                    middleName = nameWords.slice(2).join(' ');
                }
            }

            // Clean up middle name (remove extra spaces)
            middleName = middleName.trim().replace(/\s+/g, ' ');


            return {
                lastName: cleanName(lastName),
                firstName: cleanName(firstName),
                middleName: cleanName(middleName),
                suffix
            };
        } else if (nameWords.length === 2) {
            // Only 2 words: LAST FIRST (no middle, no suffix)
            lastName = nameWords[0];
            firstName = nameWords[1];
            middleName = "";

            return {
                lastName: cleanName(lastName),
                firstName: cleanName(firstName),
                middleName,
                suffix
            };
        }

        return null;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Extract Address From Lines (Pattern-Based - No Label Required)
    // ═══════════════════════════════════════════════════════════════════════════
    // For UMID cards that don't have an ADDRESS label, this scans ALL OCR lines
    // and finds address patterns based on keywords and concatenates them
    // ═══════════════════════════════════════════════════════════════════════════
    function extractAddressFromLines(lines) {

        // Address keywords that indicate a line is part of an address
        const addressKeywords = [
            'BARANGAY', 'BRGY', 'BARRIO',
            'DAVAO', 'CITY',
            'STREET', 'ST', 'AVENUE', 'AVE', 'ROAD', 'RD', 'BOULEVARD', 'BLVD',
            'BLOCK', 'BLK', 'LOT',
            'PUROK', 'SITIO', 'ZONE', 'DISTRICT',
            'SUBDIVISION', 'SUBD', 'VILLAGE', 'VILL',
            'DEL SUR', 'DEL NORTE', 'ORIENTAL', 'OCCIDENTAL',
            'PROVINCE', 'MUNICIPALITY'
        ];

        const addressLines = [];
        let consecutiveNonAddressLines = 0;

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i].trim();
            if (!line || line.length === 0) continue;

            const upperLine = line.toUpperCase();

            // Skip if line is clearly a label (contains only label text)
            const isLabel = /^(ADDRESS|TIRAHAN|NAME|PANGALAN|BIRTHDATE|KAPANGANAKAN|SEX|KASARIAN|GENDER)[\s:]*$/i.test(line);
            if (isLabel) {
                continue;
            }

            // Skip if line looks like a person name (and doesn't have address keywords)
            const hasAddressKeyword = addressKeywords.some(kw => upperLine.includes(kw));
            const looksLikeName = /^[A-Z\s\-',]+$/.test(line) &&
                line.split(/\s+/).length <= 4 &&
                line.length < 50 &&
                !hasAddressKeyword;

            if (looksLikeName) {
                consecutiveNonAddressLines++;
                if (consecutiveNonAddressLines >= 2 && addressLines.length > 0) {
                    // If we found some address lines and then hit 2+ non-address lines, stop
                    break;
                }
                continue;
            }

            // Check if this line contains address keywords
            if (hasAddressKeyword) {
                addressLines.push(line);
                consecutiveNonAddressLines = 0;
            } else if (addressLines.length > 0 && consecutiveNonAddressLines < 1) {
                // If we've already found address lines, include nearby lines (might be continuation)
                // But only if they have some address-like characteristics
                const hasNumbers = /\d/.test(line);
                const hasComma = line.includes(',');
                if (hasNumbers || hasComma || line.length > 10) {
                    addressLines.push(line);
                    consecutiveNonAddressLines = 0;
                } else {
                    consecutiveNonAddressLines++;
                }
            } else {
                consecutiveNonAddressLines++;
            }

            // Stop after collecting up to 4 address lines
            if (addressLines.length >= 4) {
                break;
            }
        }

        if (addressLines.length > 0) {
            const concatenated = addressLines.join(' ').replace(/\s+/g, ' ').trim();
            return concatenated;
        }

        return "";
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Extract Multi-Line Address (SAME-LINE + 1-4 Lines Support)
    // ═══════════════════════════════════════════════════════════════════════════
    // Philippine National IDs and UMID have address in various formats:
    // - UMID: Often on SAME LINE as label ("ADDRESS: MANDUG, DAVAO CITY")
    // - National ID: Often spanning 1-4 lines AFTER the ADDRESS label
    // This function extracts and concatenates ALL address lines for Davao City detection
    //
    // SUPPORTED FORMATS:
    //
    // SAME-LINE ADDRESS (UMID Common Format):
    //   ADDRESS: MANDUG, DAVAO CITY
    //   Result: "MANDUG, DAVAO CITY"
    //
    // 1-LINE ADDRESS:
    //   ADDRESS:
    //   DAVAO CITY
    //   Result: "DAVAO CITY"
    //
    // 2-LINE ADDRESS (Most Common for National ID):
    //   ADDRESS:
    //   BLK.5 LOT 25 DAGOHOY ST., DDF VILL., MANDUG, CITY OF
    //   DAVAO, DAVAO DEL SUR
    //   Result: "BLK.5 LOT 25 DAGOHOY ST., DDF VILL., MANDUG, CITY OF DAVAO, DAVAO DEL SUR"
    //
    // 3-LINE ADDRESS:
    //   ADDRESS:
    //   BLK 10 LOT 5 PUROK 3
    //   MATINA APLAYA BARANGAY
    //   DAVAO CITY DAVAO DEL SUR
    //   Result: "BLK 10 LOT 5 PUROK 3 MATINA APLAYA BARANGAY DAVAO CITY DAVAO DEL SUR"
    //
    // 4-LINE ADDRESS (UMID Support):
    //   ADDRESS:
    //   BLK 5 LOT 3
    //   PUROK 2
    //   BARANGAY MATINA
    //   DAVAO CITY DAVAO DEL SUR
    //   Result: "BLK 5 LOT 3 PUROK 2 BARANGAY MATINA DAVAO CITY DAVAO DEL SUR"
    //
    // ═══════════════════════════════════════════════════════════════════════════
    function extractMultiLineAddress(lines) {

        const addressLabels = FIELD_LABELS.address || [];
        if (addressLabels.length === 0) {
            return "";
        }

        // Find the address label
        for (let i = 0; i < lines.length; i++) {
            const line = lines[i].trim();
            const upperLine = line.toUpperCase();

            // Check if this line contains an address label
            for (const label of addressLabels) {
                if (upperLine.includes(label)) {

                    // Collect the next 1-4 lines as potential address lines
                    // NATIONAL ID / UMID: Address can span:
                    //   SAME LINE: "ADDRESS: MANDUG, DAVAO CITY" (UMID common format)
                    //   1 line: "DAVAO CITY"
                    //   2 lines: "Street/Barangay" + "City/Province"
                    //   3 lines: "Street" + "Barangay" + "City/Province"
                    //   4 lines: "House/Block/Lot" + "Street" + "Barangay" + "City/Province"
                    const addressLines = [];
                    const maxAddressLines = 4; // Support up to 4 address lines for UMID

                    // ═══════════════════════════════════════════════════════════════
                    // CRITICAL FIX: Check if address is on SAME LINE as label
                    // ═══════════════════════════════════════════════════════════════
                    // UMID format: "ADDRESS: MANDUG, DAVAO CITY"
                    // National ID format: "ADDRESS:" then next line has address
                    // ═══════════════════════════════════════════════════════════════
                    const labelIndex = upperLine.indexOf(label);
                    const afterLabel = line.substring(labelIndex + label.length).trim();

                    // Remove common separators (colon, dash, etc.)
                    const cleanAfterLabel = afterLabel.replace(/^[:\-\s]+/, '').trim();

                    if (cleanAfterLabel.length > 0) {
                        // Address value is on the SAME line as the label
                        addressLines.push(cleanAfterLabel);
                    } else {
                    }


                    for (let j = i + 1; j < Math.min(i + 1 + maxAddressLines, lines.length); j++) {
                        const nextLine = lines[j].trim();

                        // Skip completely empty lines
                        if (!nextLine || nextLine.length === 0) {
                            continue;
                        }

                        // Check if this line is likely part of the address
                        const upperNextLine = nextLine.toUpperCase();

                        // ===============================================================
                        // CRITICAL: Stop if we hit NAME-related labels
                        // ===============================================================
                        const isNameLabel =
                            upperNextLine.includes('LAST NAME') ||
                            upperNextLine.includes('FIRST NAME') ||
                            upperNextLine.includes('MIDDLE NAME') ||
                            upperNextLine.includes('FULL NAME') ||
                            upperNextLine.includes('SURNAME') ||
                            upperNextLine.includes('GIVEN NAME') ||
                            upperNextLine.includes('APELYIDO') ||
                            upperNextLine.includes('PANGALAN') ||
                            upperNextLine.includes('LASTNAME') ||
                            upperNextLine.includes('FIRSTNAME') ||
                            upperNextLine.includes('MIDDLENAME') ||
                            upperNextLine.includes('FULLNAME') ||
                            upperNextLine.includes('HOLDER NAME') ||
                            upperNextLine.includes('NAME OF');

                        if (isNameLabel) {
                            break;
                        }

                        // ===============================================================
                        // CRITICAL: Reject lines that look like person names
                        // ===============================================================
                        // Characteristics of names on IDs:
                        // - Usually 1-4 words
                        // - All uppercase letters
                        // - No numbers, special chars except hyphens/spaces
                        // - Not too long (names are typically < 50 chars)
                        const isLikelyName = (text) => {
                            const cleanText = text.trim();

                            // FIRST: Check if line contains address keywords
                            // If it has address keywords, it's NOT a name (even if it has comma)
                            const addressKeywords = ['STREET', 'ST', 'BLVD', 'BOULEVARD', 'AVENUE', 'AVE', 'ROAD', 'RD',
                                'BLOCK', 'LOT', 'BLK', 'BARANGAY', 'BRGY', 'CITY', 'DAVAO',
                                'SUBDIVISION', 'VILLAGE', 'VILL', 'PUROK', 'SITIO', 'ZONE', 'DISTRICT',
                                'PROVINCE', 'DEL SUR', 'DEL NORTE', 'ORIENTAL', 'OCCIDENTAL'];
                            const upperText = cleanText.toUpperCase();
                            const hasAddressKeyword = addressKeywords.some(kw => upperText.includes(kw));

                            if (hasAddressKeyword) {
                                // Contains address keywords - definitely NOT a name
                                return false;
                            }

                            // Name patterns: all caps, 1-4 words, no numbers
                            const wordCount = cleanText.split(/\s+/).length;
                            const hasNumbers = /\d/.test(cleanText);
                            const hasSpecialChars = /[^A-Z\s\-']/.test(cleanText.toUpperCase());
                            const isAllCaps = cleanText === cleanText.toUpperCase();
                            const hasComma = cleanText.includes(','); // Name format: "LAST, FIRST MIDDLE"

                            // Likely a name if:
                            // - 1-4 words, all caps, no numbers, has comma (e.g., "DELA CRUZ, JUAN")
                            // - OR 1-3 words, all caps, no numbers, no special chars
                            if (hasComma && wordCount >= 2 && wordCount <= 5 && !hasNumbers) {
                                return true;
                            }

                            if (wordCount >= 1 && wordCount <= 3 && isAllCaps && !hasNumbers && !hasSpecialChars && cleanText.length < 50) {
                                return true;
                            }

                            return false;
                        };

                        if (isLikelyName(nextLine)) {
                            continue; // Skip this line but continue looking for address lines
                        }

                        // Stop if we hit another field label (not address-related)
                        const isAnotherFieldLabel =
                            upperNextLine.includes('BIRTHDATE') ||
                            upperNextLine.includes('DATE OF BIRTH') ||
                            upperNextLine.includes('SEX') ||
                            upperNextLine.includes('GENDER') ||
                            upperNextLine.includes('CIVIL STATUS') ||
                            upperNextLine.includes('HEIGHT') ||
                            upperNextLine.includes('WEIGHT') ||
                            upperNextLine.includes('BLOOD TYPE') ||
                            upperNextLine.includes('LICENSE') ||
                            upperNextLine.includes('ID NUMBER') ||
                            upperNextLine.includes('NATIONALITY');

                        if (isAnotherFieldLabel) {
                            break;
                        }

                        // Check if line is a label itself (not a value)
                        if (isExtractedTextALabel(nextLine)) {
                            break;
                        }

                        // This line appears to be part of the address
                        addressLines.push(nextLine);
                    }

                    // ═══════════════════════════════════════════════════════════════
                    // CONCATENATE ADDRESS LINES (Supports same-line + 1-4 multi-lines)
                    // ═══════════════════════════════════════════════════════════════
                    // Join all address lines with space to create complete address
                    // This ensures "CITY OF" + "DAVAO" becomes "CITY OF DAVAO"
                    //
                    // Examples:
                    //   SAME LINE: "ADDRESS: MANDUG, DAVAO CITY"
                    //              = "MANDUG, DAVAO CITY"
                    //   1 line:  "DAVAO CITY"
                    //   2 lines: "MANDUG, CITY OF" + "DAVAO, DAVAO DEL SUR"
                    //            = "MANDUG, CITY OF DAVAO, DAVAO DEL SUR"
                    //   3 lines: "BLK 10" + "MATINA APLAYA" + "DAVAO CITY"
                    //            = "BLK 10 MATINA APLAYA DAVAO CITY"
                    //   4 lines: "BLK 5 LOT 3" + "PUROK 2" + "BARANGAY MATINA" + "DAVAO CITY"
                    //            = "BLK 5 LOT 3 PUROK 2 BARANGAY MATINA DAVAO CITY"
                    // ═══════════════════════════════════════════════════════════════
                    if (addressLines.length > 0) {
                        // Log each extracted line
                        addressLines.forEach((line, idx) => {
                        });

                        // Concatenate all lines with single space separator
                        let completeAddress = addressLines.join(' ');

                        // Clean up any extra spaces from concatenation
                        completeAddress = completeAddress.replace(/\s+/g, ' ').trim();


                        return completeAddress;
                    } else {
                        return "";
                    }
                }
            }
        }

        return "";
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // DAVAO CITY VALIDATION - Detects all variants in concatenated multi-line addresses
    // ═══════════════════════════════════════════════════════════════════════════════
    // This function validates Davao City from CONCATENATED ADDRESS (1-4 lines combined)
    // Works with addresses extracted from National ID and UMID where address may span multiple lines
    //
    // INPUT EXAMPLES (After Concatenation):
    //   1 line:  "DAVAO CITY"
    //   2 lines: "BLK.5 LOT 25 DAGOHOY ST., DDF VILL., MANDUG, CITY OF DAVAO, DAVAO DEL SUR"
    //   3 lines: "BLK 10 LOT 5 PUROK 3 MATINA APLAYA BARANGAY DAVAO CITY DAVAO DEL SUR"
    //   4 lines: "BLK 5 LOT 3 PUROK 2 BARANGAY MATINA DAVAO CITY DAVAO DEL SUR"
    //
    // DETECTION: Checks for Davao City variants anywhere in the concatenated address
    // ═══════════════════════════════════════════════════════════════════════════════
    function isDavaoCity(address) {
        if (!address || typeof address !== 'string') {
            return false;
        }

        // Normalize address: remove extra spaces, convert to uppercase
        const upperAddress = address.toUpperCase().replace(/\s+/g, ' ').trim();
        

        // ═══════════════════════════════════════════════════════════════════════
        // PRIORITY 1: Direct matches (MOST RELIABLE - case insensitive)
        // If ANY of these exact phrases appear, ACCEPT immediately
        // ═══════════════════════════════════════════════════════════════════════
        const DIRECT_MATCH_VARIANTS = [
            'DAVAO CITY',
            'CITY OF DAVAO',
            'DVO CITY',
            'DAVAOCITY',      // Without space (OCR sometimes removes spaces)
            'CITYOFDAVAO',    // Without spaces
            'DVOCITY'         // Without space
        ];

        for (const variant of DIRECT_MATCH_VARIANTS) {
            if (upperAddress.includes(variant)) {
                return true;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PRIORITY 2: Pattern-based detection (handles OCR variations)
        // ═══════════════════════════════════════════════════════════════════════
        
        // Pattern: "DAVAO" followed by "CITY" with possible characters between
        if (/DAVAO\s*,?\s*CITY/i.test(upperAddress)) {
            return true;
        }
        
        // Pattern: "CITY" followed by "OF" followed by "DAVAO"
        if (/CITY\s+OF\s+DAVAO/i.test(upperAddress)) {
            return true;
        }
        
        // Pattern: "DVO" followed by "CITY"
        if (/DVO\.?\s*CITY/i.test(upperAddress)) {
            return true;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PRIORITY 3: Additional variations (typos, abbreviations)
        // ═══════════════════════════════════════════════════════════════════════
        const ADDITIONAL_VARIANTS = [
            'DVAO CITY',      // Common typo
            'DABAW CITY',     // Bisaya spelling
            'DAVAO CTY',      // Abbreviation
            'DAVAO, PHILIPPINES',
            'DAVAO PHILIPPINES'
        ];

        for (const variant of ADDITIONAL_VARIANTS) {
            if (upperAddress.includes(variant)) {
                return true;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PRIORITY 4: Standalone "DAVAO" check (with rejection list)
        // Accept "DAVAO" alone ONLY if no other Davao province cities are present
        // ═══════════════════════════════════════════════════════════════════════
        if (upperAddress.includes('DAVAO')) {
            // Rejection list: Other cities in Davao region that are NOT Davao City
            const REJECTION_CITIES = [
                'DIGOS', 'TAGUM', 'PANABO', 'SAMAL', 'MATI', 'MACO', 'MALITA',
                'BANSALAN', 'SANTA CRUZ', 'KAPALONG', 'CARMEN', 'LUPON', 'CATEEL'
            ];
            
            const REJECTION_PROVINCES = [
                'DAVAO DEL NORTE', 'DAVAO DEL SUR', 'DAVAO DE ORO', 
                'DAVAO OCCIDENTAL', 'DAVAO ORIENTAL'
            ];

            // Check if any rejection city is mentioned (these are NOT Davao City)
            const hasRejectionCity = REJECTION_CITIES.some(city => upperAddress.includes(city));
            
            // Check if province name appears WITHOUT "DAVAO CITY"
            const hasProvinceOnly = REJECTION_PROVINCES.some(prov => {
                if (upperAddress.includes(prov)) {
                    // Province is mentioned, but check if "DAVAO CITY" is also there
                    // If "DAVAO CITY" or "CITY OF DAVAO" exists, still accept
                    const hasDavaoCity = upperAddress.includes('DAVAO CITY') || 
                                        upperAddress.includes('CITY OF DAVAO');
                    return !hasDavaoCity;
                }
                return false;
            });

            if (!hasRejectionCity && !hasProvinceOnly) {
                return true;
            }
        }

        return false;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // Extract Driver's License Name
    // ═══════════════════════════════════════════════════════════════════════════════
    // LOCATION: Below the label "LAST NAME, FIRST NAME, MIDDLE NAME" or similar indicator
    // FORMAT: "Last name, First name, Middle name, Suffix" (comma-separated)
    // EXAMPLE: "DELA CRUZ, JUAN MIGUEL JR"
    // ═══════════════════════════════════════════════════════════════════════════════
    // ═══════════════════════════════════════════════════════════════════════════════
    // Extract Driver's License Name with Compound Middle Name Validation
    // ═══════════════════════════════════════════════════════════════════════════════
    function extractDriverLicenseName(lines, text) {

        // Print all lines for debugging
        lines.forEach((line, idx) => {
        });

        let lastName = "", firstName = "", middleName = "", suffix = "";

        // PRIORITY: Look for name below the label indicator
        for (let i = 0; i < lines.length; i++) {
            const line = lines[i].trim().toUpperCase();

            // Check if this line contains the name label indicator
            if (line.includes('LAST NAME') || line.includes('FIRST NAME') ||
                line.includes('SURNAME') || line.includes('APELYIDO') ||
                (line.includes('LASTNAME') && line.includes('FIRSTNAME'))) {


                // Name should be in the next line(s)
                for (let j = i + 1; j < Math.min(i + 4, lines.length); j++) {
                    const nameLine = lines[j].trim();
                    const words = nameLine.split(/\s+/).filter(w => w.length > 1);

                    // Skip labels and lines that don't look like names
                    if (!isExtractedTextALabel(nameLine) && containsUnusualWords(nameLine) && words.length >= 2) {

                        const parsed = parseDriverLicenseFullName(nameLine);
                        if (parsed) {

                            // === APPLY COMPOUND MIDDLE NAME VALIDATION ===
                            const validatedNames = validateDriverLicenseCompoundMiddleName(parsed.firstName, parsed.middleName);

                            return {
                                lastName: parsed.lastName,
                                firstName: validatedNames.firstName,
                                middleName: validatedNames.middleName,
                                suffix: parsed.suffix
                            };
                        }
                    }
                }
            }
        }


        // FALLBACK: Check lines 6, 7, 8 for full name
        for (const idx of [5, 6, 7]) {
            if (idx < lines.length) {
                const line = lines[idx].trim();
                const words = line.split(/\s+/).filter(w => w.length > 1);

                if (words.length >= 2 && !isExtractedTextALabel(line) && containsUnusualWords(line)) {

                    const parsed = parseDriverLicenseFullName(line);
                    if (parsed) {

                        // === APPLY COMPOUND MIDDLE NAME VALIDATION ===
                        const validatedNames = validateDriverLicenseCompoundMiddleName(parsed.firstName, parsed.middleName);

                        return {
                            lastName: parsed.lastName,
                            firstName: validatedNames.firstName,
                            middleName: validatedNames.middleName,
                            suffix: parsed.suffix
                        };
                    }
                }
            }
        }


        // Extract Last Name using label
        const lastNameResult = extractFieldValue(lines, 'lastName');
        if (lastNameResult && lastNameResult.value) {
            let rawLastName = lastNameResult.value.trim();

            // Check if value contains other name labels (indicating multiple names on one line)
            const hasFirstNameLabel = FIELD_LABELS.firstName.some(label =>
                rawLastName.toUpperCase().includes(label)
            );
            const hasMiddleNameLabel = FIELD_LABELS.middleName.some(label =>
                rawLastName.toUpperCase().includes(label)
            );

            if (hasFirstNameLabel || hasMiddleNameLabel) {

                // Extract only the first word/value before any delimiter or next label
                const parts = rawLastName.split(/[\s,\/]+/);
                rawLastName = parts[0];
            }

            lastName = cleanName(rawLastName);

            // Validate: not a label, reasonable length, no duplication indicators
            if (!isExtractedTextALabel(lastName) && lastName.length >= 2) {
                // Additional check: ensure it doesn't contain multiple distinct name words
                const words = lastName.split(/\s+/);
                if (words.length <= 3) {  // Reasonable for compound surnames
                } else {
                    lastName = words[0];  // Use only first word
                }
            } else {
                lastName = "";
            }
        } else {
        }

        // Extract First Name using label
        const firstNameResult = extractFieldValue(lines, 'firstName');
        if (firstNameResult && firstNameResult.value) {
            let rawFirstName = firstNameResult.value.trim();

            // Check if value contains other name labels
            const hasLastNameLabel = FIELD_LABELS.lastName.some(label =>
                rawFirstName.toUpperCase().includes(label)
            );
            const hasMiddleNameLabel = FIELD_LABELS.middleName.some(label =>
                rawFirstName.toUpperCase().includes(label)
            );

            if (hasLastNameLabel || hasMiddleNameLabel) {

                // Extract only the first word/value
                const parts = rawFirstName.split(/[\s,\/]+/);
                rawFirstName = parts[0];
            }

            firstName = cleanName(rawFirstName);

            if (!isExtractedTextALabel(firstName) && firstName.length >= 2) {
                // Check for duplication with lastName
                const firstUpper = firstName.toUpperCase();
                const lastUpper = lastName.toUpperCase();

                if (lastUpper && firstUpper.includes(lastUpper)) {
                    // Remove lastName from firstName
                    const cleanedFirst = firstName.replace(new RegExp(lastName, 'gi'), '').trim();
                    if (cleanedFirst.length >= 2) {
                        firstName = cleanedFirst;
                    } else {
                    }
                }

                const words = firstName.split(/\s+/);
                if (words.length <= 3) {  // Reasonable for compound first names
                } else {
                    firstName = words[0];  // Use first word only
                }
            } else {
                firstName = "";
            }
        } else {
        }

        // Extract Middle Name using label - FORCED EXTRACTION

        const middleNameResult = extractFieldValue(lines, 'middleName');

        if (middleNameResult && middleNameResult.value) {

            let rawMiddleName = middleNameResult.value.trim();

            // CRITICAL: Check if first name contains this middle name value
            // If first name already has the middle name, we need to remove it from first name
            if (firstName && rawMiddleName) {
                const firstNameWords = firstName.split(/\s+/);
                const middleNameWords = rawMiddleName.split(/\s+/);

                // Check if any middle name word appears in first name
                const hasOverlap = middleNameWords.some(mWord =>
                    firstNameWords.some(fWord => fWord.toUpperCase() === mWord.toUpperCase())
                );

                if (hasOverlap) {

                    // Remove middle name words from first name
                    const cleanedFirstWords = firstNameWords.filter(fWord =>
                        !middleNameWords.some(mWord => mWord.toUpperCase() === fWord.toUpperCase())
                    );

                    if (cleanedFirstWords.length > 0) {
                        firstName = cleanedFirstWords.join(' ');
                    }
                }
            }

            // Check if value contains other name labels
            const hasLastNameLabel = FIELD_LABELS.lastName.some(label =>
                rawMiddleName.toUpperCase().includes(label)
            );
            const hasFirstNameLabel = FIELD_LABELS.firstName.some(label =>
                rawMiddleName.toUpperCase().includes(label)
            );

            if (hasLastNameLabel || hasFirstNameLabel) {

                // Extract only the first word/value
                const parts = rawMiddleName.split(/[\s,\/]+/);
                rawMiddleName = parts[0];
            }

            middleName = cleanName(rawMiddleName);

            // CRITICAL: Multi-layer validation to prevent label extraction
            // This is a safety net that MUST catch any label words that slip through
            const middleNameUpper = middleName.toUpperCase().trim();

            // Layer 1: Explicit label word list (expanded)
            const explicitLabelRejects = [
                // English labels
                'MIDDLE', 'MIDDLENAME', 'MIDDLE NAME', 'M.I.', 'MI', 'M.N.', 'MN',
                'FIRST', 'FIRSTNAME', 'FIRST NAME', 'F.N.', 'FN',
                'LAST', 'LASTNAME', 'LAST NAME', 'L.N.', 'LN',
                'GIVEN', 'GIVENNAME', 'GIVEN NAME',
                'SURNAME', 'FAMILY', 'FAMILYNAME', 'FAMILY NAME',
                'NAME', 'NAMES', 'FULLNAME', 'FULL NAME',
                // Filipino labels
                'GITNANG', 'GITNANG PANGALAN', 'GITNANG APELYIDO',
                'UNANG', 'UNANG PANGALAN', 'UNANG NGALAN',
                'HULING', 'HULING PANGALAN', 'HULING APELYIDO',
                'PANGALAN', 'APELYIDO', 'NGALAN', 'APELYEDO'
            ];

            // Layer 2: Partial match detection (if middleName contains ANY label word)
            const containsLabelWord = explicitLabelRejects.some(label =>
                middleNameUpper.includes(label) || label.includes(middleNameUpper)
            );

            // Layer 3: Single letter check (unless it's a valid initial like "A" or "B")
            const isSingleLetter = middleName.length === 1 && /[A-Z]/i.test(middleName);

            // Layer 4: Check if it's ONLY label words with no actual name data
            const words = middleName.split(/\s+/);
            const allWordsAreLabels = words.every(word => {
                const upperWord = word.toUpperCase().trim();
                return explicitLabelRejects.includes(upperWord) ||
                    upperWord === 'NAME' || upperWord === 'PANGALAN' ||
                    upperWord === 'MIDDLE' || upperWord === 'GITNA';
            });

            // Layer 5: CRITICAL - Explicitly reject standalone "MIDDLE" or "GITNA"
            const isStandaloneLabelWord = middleNameUpper === 'MIDDLE' || middleNameUpper === 'GITNA' ||
                middleNameUpper === 'GITNANG' || middleNameUpper === 'MID' ||
                middleNameUpper === 'INITIAL';

            if (explicitLabelRejects.includes(middleNameUpper) || containsLabelWord || allWordsAreLabels || isStandaloneLabelWord) {
                middleName = "";
            } else if (isSingleLetter && !isValidMiddleInitial(middleName)) {
            }

            if (middleName && middleName !== "" && !isExtractedTextALabel(middleName) && middleName.length >= 1) {
                // Check for duplication with other names
                const middleUpper = middleName.toUpperCase();
                const lastUpper = lastName.toUpperCase();
                const firstUpper = firstName.toUpperCase();

                if ((lastUpper && middleUpper.includes(lastUpper)) ||
                    (firstUpper && middleUpper.includes(firstUpper))) {
                    let cleanedMiddle = middleName;
                    if (lastName) cleanedMiddle = cleanedMiddle.replace(new RegExp(lastName, 'gi'), '').trim();
                    if (firstName) cleanedMiddle = cleanedMiddle.replace(new RegExp(firstName, 'gi'), '').trim();

                    if (cleanedMiddle.length >= 2) {
                        middleName = cleanedMiddle;
                    }
                }

                const words = middleName.split(/\s+/);
                if (words.length <= 2) {  // Middle name usually 1-2 words
                } else {
                    middleName = words[0];
                }
            } else if (middleName === "") {
                // middleName was explicitly rejected as a label (set to "" above)
            } else {
                middleName = "";
            }
        } else {
        }

        // === APPLY DRIVER'S LICENSE COMPOUND MIDDLE NAME VALIDATION ===
        if (middleName && middleName.trim() !== '') {
            const validatedNames = validateDriverLicenseCompoundMiddleName(firstName, middleName);
            firstName = validatedNames.firstName;
            middleName = validatedNames.middleName;

        } else {
        }

        // Extract Suffix using label
        const suffixResult = extractFieldValue(lines, 'suffix');
        if (suffixResult && suffixResult.value) {
            const rawSuffix = cleanName(suffixResult.value);
            // Validate suffix pattern
            if (/^(JR|SR|II|III|IV|V|JUNIOR|SENIOR)$/i.test(rawSuffix.trim())) {
                suffix = rawSuffix.toUpperCase();
                if (suffix === 'JUNIOR') suffix = 'JR';
                if (suffix === 'SENIOR') suffix = 'SR';
            } else {
            }
        } else {
        }

        // ===================================================================
        // CRITICAL: Final validation - ensure all 4 components are DISTINCT
        // Verify no overlap between lastName, firstName, middleName, suffix
        // ===================================================================

        // Check 1: Last name vs First name
        if (lastName && firstName) {
            const lastWords = lastName.toUpperCase().split(/\s+/);
            const firstWords = firstName.toUpperCase().split(/\s+/);
            const overlap = lastWords.filter(lw => firstWords.includes(lw));

            if (overlap.length > 0) {
                const cleanedFirstWords = firstWords.filter(fw => !lastWords.includes(fw));
                if (cleanedFirstWords.length > 0) {
                    firstName = cleanedFirstWords.join(' ');
                }
            }
        }

        // Check 2: First name vs Middle name (CRITICAL)
        if (firstName && middleName) {
            const firstWords = firstName.toUpperCase().split(/\s+/);
            const middleWords = middleName.toUpperCase().split(/\s+/);
            const overlap = firstWords.filter(fw => middleWords.includes(fw));

            if (overlap.length > 0) {

                const cleanedFirstWords = firstWords.filter(fw => !middleWords.includes(fw));
                if (cleanedFirstWords.length > 0) {
                    firstName = cleanedFirstWords.join(' ');
                } else {
                }
            }
        }

        // Check 3: Middle name vs Last name
        if (middleName && lastName) {
            const middleWords = middleName.toUpperCase().split(/\s+/);
            const lastWords = lastName.toUpperCase().split(/\s+/);
            const overlap = middleWords.filter(mw => lastWords.includes(mw));

            if (overlap.length > 0) {
                const cleanedMiddleWords = middleWords.filter(mw => !lastWords.includes(mw));
                if (cleanedMiddleWords.length > 0) {
                    middleName = cleanedMiddleWords.join(' ');
                }
            }
        }

        // Check 4: Suffix should not contain any other name parts
        if (suffix) {
            const suffixUpper = suffix.toUpperCase();
            const allNameWords = [
                ...(lastName ? lastName.toUpperCase().split(/\s+/) : []),
                ...(firstName ? firstName.toUpperCase().split(/\s+/) : []),
                ...(middleName ? middleName.toUpperCase().split(/\s+/) : [])
            ];

            const hasNameInSuffix = allNameWords.some(word => suffixUpper.includes(word));
            if (hasNameInSuffix) {
                suffix = "";
            }
        }

        // FINAL OUTPUT - COMPACT FORMAT

        return { lastName, firstName, middleName, suffix };
    }

    function extractBirthdate(lines, idType = 'generic') {

        // FORCED LABEL-BASED EXTRACTION
        // Aggressively search for birthdate labels and extract immediately

        // Print all lines with index for debugging
        lines.forEach((line, idx) => {
        });

        const labelResult = extractFieldValue(lines, 'birthdate');

        if (labelResult && labelResult.value) {
            let dateValue = labelResult.value.trim();


            // ===================================================================
            // CRITICAL VALIDATION: Ensure extracted value is actually a DATE
            // Reject values that are clearly NOT dates (like "Weight (kg)", "Height", etc.)
            // ===================================================================

            // Check 1: Reject if value contains common non-date keywords
            const nonDateKeywords = [
                'WEIGHT', 'HEIGHT', 'KG', 'CM', 'LBS', 'POUNDS', 'FEET', 'INCHES',
                'BLOOD', 'TYPE', 'EYES', 'HAIR', 'COLOR', 'RESTRICTION',
                'AGENCY', 'CODE', 'CLASS', 'CATEGORY', 'LICENSE', 'ID',
                'ADDRESS', 'STREET', 'CITY', 'PROVINCE', 'BARANGAY'
            ];

            const upperDateValue = dateValue.toUpperCase();
            for (const keyword of nonDateKeywords) {
                if (upperDateValue.includes(keyword)) {
                    return "";
                }
            }

            // Check 2: Ensure value contains at least one digit (dates must have numbers)
            if (!/\d/.test(dateValue)) {
                return "";
            }

            // Check 3: Reject values with too many letters (dates can have month names but not paragraphs)
            const letterCount = (dateValue.match(/[a-zA-Z]/g) || []).length;
            const digitCount = (dateValue.match(/\d/g) || []).length;
            if (letterCount > 20 && digitCount < 2) {
                return "";
            }


            // ===================================================================
            // AGE AND DATE RANGE VALIDATION HELPER
            // ===================================================================
            function validateBirthdateRange(normalizedDate) {
                try {
                    const dateParts = normalizedDate.split('-');
                    if (dateParts.length !== 3) {
                        return { valid: false, reason: 'Invalid format' };
                    }

                    const year = parseInt(dateParts[0]);
                    const month = parseInt(dateParts[1]);
                    const day = parseInt(dateParts[2]);

                    // Create date object
                    const birthDate = new Date(year, month - 1, day);
                    const today = new Date();

                    // Calculate age
                    let age = today.getFullYear() - birthDate.getFullYear();
                    const monthDiff = today.getMonth() - birthDate.getMonth();
                    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
                        age--;
                    }

                    // Validation checks

                    // Check 1: Not before year 1900 (reasonable historical limit)
                    if (year < 1900) {
                        return { valid: false, reason: 'Year before 1900' };
                    }

                    // Check 2: Not in the future
                    if (birthDate > today) {
                        return { valid: false, reason: 'Future date' };
                    }

                    // Check 3: Person must be at least 18 years old
                    if (age < 18) {
                        return { valid: false, reason: `Only ${age} years old (minimum 18 required)`, age: age };
                    }

                    // Check 4: Reasonable maximum age (150 years)
                    if (age > 150) {
                        return { valid: false, reason: 'Age exceeds 150 years' };
                    }

                    // Check 5: Valid month (1-12)
                    if (month < 1 || month > 12) {
                        return { valid: false, reason: 'Invalid month' };
                    }

                    // Check 6: Valid day for the month
                    const daysInMonth = new Date(year, month, 0).getDate();
                    if (day < 1 || day > daysInMonth) {
                        return { valid: false, reason: 'Invalid day for month' };
                    }

                    return { valid: true, age };
                } catch (error) {
                    return { valid: false, reason: 'Validation error' };
                }
            }

            // ===================================================================
            // ENHANCED BIRTHDATE FORMAT NORMALIZATION
            // Normalize all formats to yyyy-mm-dd (required by HTML5 date input)
            // Supports text-based dates: "December 25, 2001", "Dec 25, 2001", etc.
            // ===================================================================

            // SPECIAL FORMAT FOR DRIVER'S LICENSE: YYYY-DD-MM
            // Driver's License uses an unusual format: Year-Day-Month
            // Example: "2003-25-12" means December 25, 2003 (NOT 2003-25th month-12th day)
            if (idType === "Driver's License") {
                const dlMatch = dateValue.match(/\b(\d{4})-(\d{1,2})-(\d{1,2})\b/);
                if (dlMatch) {
                    const [_, year, day, month] = dlMatch;
                    const dayNum = parseInt(day);
                    const monthNum = parseInt(month);

                    // Validate that day and month are reasonable
                    if (monthNum >= 1 && monthNum <= 12 && dayNum >= 1 && dayNum <= 31) {
                        const normalized = `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;

                        // Validate age and date range
                        const validation = validateBirthdateRange(normalized);

                        if (validation.valid) {
                            return normalized;
                        } else {

                            // Show user-friendly error modal for age validation failures
                            if (validation.reason && validation.reason.includes('years old')) {
                                const errorMessage =
                                    `Age Requirement Not Met\n\n` +
                                    `The birthdate extracted from your ID shows you are ${validation.age || 'under 18'} years old.\n\n` +
                                    `To use this service, you must be at least 18 years of age.\n\n` +
                                    `Requirements:\n` +
                                    `• Minimum age: 18 years\n` +
                                    `• Current year: ${new Date().getFullYear()}\n` +
                                    `• Your birth year must be ${new Date().getFullYear() - 18} or earlier\n\n` +
                                    `If you believe this is an error, please ensure:\n` +
                                    `1. Your ID is clear and readable\n` +
                                    `2. The birthdate on your ID is clearly visible\n` +
                                    `3. You are using a valid government-issued ID`;
                                showVerificationErrorModal(errorMessage);
                            }

                            return "";
                        }
                    } else {
                    }
                }
            }

            // Format 1: MM/DD/YYYY (American format) → Convert to yyyy-mm-dd
            let match = dateValue.match(/\b(\d{1,2})\/(\d{1,2})\/(\d{4})\b/);
            if (match) {
                const [_, month, day, year] = match;
                const normalized = `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;

                // Validate age and date range
                const validation = validateBirthdateRange(normalized);
                if (validation.valid) {
                    return normalized;
                } else {
                    return "";
                }
            }

            // Format 2: YYYY-MM-DD (ISO format - already correct) → Normalize with padding
            match = dateValue.match(/\b(\d{4})-(\d{1,2})-(\d{1,2})\b/);
            if (match) {
                const [_, year, month, day] = match;
                const normalized = `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;

                // Validate age and date range
                const validation = validateBirthdateRange(normalized);
                if (validation.valid) {
                    return normalized;
                } else {
                    return "";
                }
            }

            // Format 3: DD-MM-YYYY (European with dashes) → Convert to yyyy-mm-dd
            match = dateValue.match(/\b(\d{1,2})-(\d{1,2})-(\d{4})\b/);
            if (match) {
                const [_, part1, part2, year] = match;
                let normalized;
                // If part1 > 12, it's DD-MM-YYYY; otherwise ambiguous (assume MM-DD-YYYY)
                if (parseInt(part1) > 12) {
                    normalized = `${year}-${part2.padStart(2, '0')}-${part1.padStart(2, '0')}`;
                } else {
                    normalized = `${year}-${part1.padStart(2, '0')}-${part2.padStart(2, '0')}`;
                }

                // Validate age and date range
                const validation = validateBirthdateRange(normalized);
                if (validation.valid) {
                    return normalized;
                } else {
                    return "";
                }
            }

            // Format 4 & 5: Month name formats (all variations)
            const monthNames = {
                'JANUARY': '01', 'JAN': '01',
                'FEBRUARY': '02', 'FEB': '02',
                'MARCH': '03', 'MAR': '03',
                'APRIL': '04', 'APR': '04',
                'MAY': '05',
                'JUNE': '06', 'JUN': '06',
                'JULY': '07', 'JUL': '07',
                'AUGUST': '08', 'AUG': '08',
                'SEPTEMBER': '09', 'SEP': '09', 'SEPT': '09',
                'OCTOBER': '10', 'OCT': '10',
                'NOVEMBER': '11', 'NOV': '11',
                'DECEMBER': '12', 'DEC': '12'
            };

            // Format 4a: MMMM DD,YYYY (no space after comma) - e.g., "JUNE 03,2003"
            // Format 4b: MMMM DD, YYYY (with space after comma) - e.g., "JUNE 03, 2003"
            // Format 4c: MMMM DD YYYY (no comma) - e.g., "JUNE 03 2003"
            // This pattern handles all variations with flexible spacing and optional comma
            const monthNameMatch = dateValue.match(/\b([A-Z]{3,9})\s+(\d{1,2})[,\s]+(\d{4})\b/i);
            if (monthNameMatch) {
                const [_, monthName, day, year] = monthNameMatch;
                const monthNum = monthNames[monthName.toUpperCase()];
                if (monthNum) {
                    const normalized = `${year}-${monthNum}-${day.padStart(2, '0')}`;

                    // Validate age and date range
                    const validation = validateBirthdateRange(normalized);
                    if (validation.valid) {
                        return normalized;
                    } else {
                        return "";
                    }
                }
            }

            // Format 5a: DD MMMM YYYY - e.g., "03 JUNE 2003"
            // Format 5b: DD-MMMM-YYYY - e.g., "03-JUNE-2003"
            // Format 5c: DD/MMMM/YYYY - e.g., "03/JUNE/2003"
            const dayMonthYearMatch = dateValue.match(/\b(\d{1,2})[\s\-\/]+([A-Z]{3,9})[\s\-\/]+(\d{4})\b/i);
            if (dayMonthYearMatch) {
                const [_, day, monthName, year] = dayMonthYearMatch;
                const monthNum = monthNames[monthName.toUpperCase()];
                if (monthNum) {
                    const normalized = `${year}-${monthNum}-${day.padStart(2, '0')}`;

                    // Validate age and date range
                    const validation = validateBirthdateRange(normalized);
                    if (validation.valid) {
                        return normalized;
                    } else {
                        return "";
                    }
                }
            }

            // Format 6: Filipino month names (all variations)
            const filipinoMonths = {
                'ENERO': '01', 'ENE': '01',
                'PEBRERO': '02', 'FEBRERO': '02', 'PEB': '02', 'FEB': '02',
                'MARSO': '03', 'MAR': '03',
                'ABRIL': '04', 'ABR': '04',
                'MAYO': '05', 'MAY': '05',
                'HUNYO': '06', 'HUN': '06',
                'HULYO': '07', 'HUL': '07',
                'AGOSTO': '08', 'AGO': '08',
                'SETYEMBRE': '09', 'SEPTYEMBRE': '09', 'SET': '09', 'SEP': '09',
                'OKTUBRE': '10', 'OCTUBRE': '10', 'OKT': '10', 'OCT': '10',
                'NOBYEMBRE': '11', 'NOVYEMBRE': '11', 'NOB': '11', 'NOV': '11',
                'DISYEMBRE': '12', 'DICSYEMBRE': '12', 'DIS': '12', 'DIC': '12'
            };

            // Check for Filipino month names in DD MONTH YYYY format
            for (const [filMonth, monthNum] of Object.entries(filipinoMonths)) {
                const regex = new RegExp(`\\b(\\d{1,2})[\\s\\-\\/]+${filMonth}[\\s\\-\\/]+(\\d{4})\\b`, 'i');
                const match = dateValue.match(regex);
                if (match) {
                    const [_, day, year] = match;
                    const normalized = `${year}-${monthNum}-${day.padStart(2, '0')}`;

                    // Validate age and date range
                    const validation = validateBirthdateRange(normalized);
                    if (validation.valid) {
                        return normalized;
                    } else {
                        return "";
                    }
                }
            }

            // Format 7: COMPREHENSIVE MONTH NAME FALLBACK
            // This catches any remaining month name format we might have missed
            // Searches for ANY month name (English or Filipino) in the text
            const allMonthMappings = { ...monthNames, ...filipinoMonths };
            for (const [monthText, monthNum] of Object.entries(allMonthMappings)) {
                const upperDateValue = dateValue.toUpperCase();
                if (upperDateValue.includes(monthText)) {
                    // Try to extract day and year from the text
                    // Pattern: any sequence with month name and 2 numbers (day and year)
                    const flexPattern = new RegExp(
                        `\\b${monthText}[\\s,\\-\\/]*(\\d{1,2})[\\s,\\-\\\/]*(\\d{4})\\b|` +
                        `\\b(\\d{1,2})[\\s,\\-\\/]*${monthText}[\\s,\\-\\/]*(\\d{4})\\b`,
                        'i'
                    );
                    const flexMatch = dateValue.match(flexPattern);
                    if (flexMatch) {
                        let day, year;
                        if (flexMatch[1]) {
                            // Month first: JUNE 03 2003
                            day = flexMatch[1];
                            year = flexMatch[2];
                        } else {
                            // Day first: 03 JUNE 2003
                            day = flexMatch[3];
                            year = flexMatch[4];
                        }
                        if (day && year) {
                            const normalized = `${year}-${monthNum}-${day.padStart(2, '0')}`;

                            // Validate age and date range
                            const validation = validateBirthdateRange(normalized);
                            if (validation.valid) {
                                return normalized;
                            } else {
                                return "";
                            }
                        }
                    }
                }
            }

            // Format 8: Simple YYYYMMDD (no separators) → Convert to yyyy-mm-dd
            match = dateValue.match(/\b(\d{4})(\d{2})(\d{2})\b/);
            if (match) {
                const [_, year, month, day] = match;
                const normalized = `${year}-${month}-${day}`;

                // Validate age and date range
                const validation = validateBirthdateRange(normalized);
                if (validation.valid) {
                    return normalized;
                } else {
                    return "";
                }
            }

            // Format 9: DDMMYYYY (no separators) → Convert to yyyy-mm-dd
            match = dateValue.match(/\b(\d{2})(\d{2})(\d{4})\b/);
            if (match) {
                const [_, part1, part2, year] = match;
                // Assume DD-MM-YYYY if part1 > 12, otherwise MM-DD-YYYY
                let normalized;
                if (parseInt(part1) > 12) {
                    normalized = `${year}-${part2}-${part1}`;
                } else if (parseInt(part2) > 12) {
                    normalized = `${year}-${part1}-${part2}`;
                } else {
                    // Ambiguous - assume MM-DD-YYYY (US format common in PH)
                    normalized = `${year}-${part1}-${part2}`;
                }

                // Validate age and date range
                const validation = validateBirthdateRange(normalized);
                if (validation.valid) {
                    return normalized;
                } else {
                    return "";
                }
            }

            // Format 10: DD.MM.YYYY (European format with dots) → Convert to yyyy-mm-dd
            match = dateValue.match(/\b(\d{1,2})\.(\d{1,2})\.(\d{4})\b/);
            if (match) {
                const [_, part1, part2, year] = match;
                let normalized;
                if (parseInt(part1) > 12) {
                    normalized = `${year}-${part2.padStart(2, '0')}-${part1.padStart(2, '0')}`;
                } else {
                    normalized = `${year}-${part2.padStart(2, '0')}-${part1.padStart(2, '0')}`;
                }

                // Validate age and date range
                const validation = validateBirthdateRange(normalized);
                if (validation.valid) {
                    return normalized;
                } else {
                    return "";
                }
            }

            // Format 11: YYYY.MM.DD (with dots) → Convert to yyyy-mm-dd
            match = dateValue.match(/\b(\d{4})\.(\d{1,2})\.(\d{1,2})\b/);
            if (match) {
                const [_, year, month, day] = match;
                const normalized = `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;

                // Validate age and date range
                const validation = validateBirthdateRange(normalized);
                if (validation.valid) {
                    return normalized;
                } else {
                    return "";
                }
            }

            // Format 12: YY-MM-DD or YY/MM/DD (2-digit year) → Convert to yyyy-mm-dd
            match = dateValue.match(/\b(\d{2})[\-\/](\d{1,2})[\-\/](\d{1,2})\b/);
            if (match) {
                const [_, year2, month, day] = match;
                // Convert 2-digit year to 4-digit (assume 1900s for 50-99, 2000s for 00-49)
                const yearNum = parseInt(year2);
                const fullYear = yearNum >= 50 ? `19${year2}` : `20${year2}`;
                const normalized = `${fullYear}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;

                // Validate age and date range
                const validation = validateBirthdateRange(normalized);
                if (validation.valid) {
                    return normalized;
                } else {
                    return "";
                }
            }

            // Format 13: YYYY/MM/DD (with slashes) → Convert to yyyy-mm-dd
            match = dateValue.match(/\b(\d{4})\/(\d{1,2})\/(\d{1,2})\b/);
            if (match) {
                const [_, year, month, day] = match;
                const normalized = `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;

                // Validate age and date range
                const validation = validateBirthdateRange(normalized);
                if (validation.valid) {
                    return normalized;
                } else {
                    return "";
                }
            }

            return dateValue;
        }

        // ===================================================================
        // STRICT REQUIREMENT: NO LABEL = NO EXTRACTION
        // If no birthdate label/indicator was found, we DO NOT extract any dates
        // This prevents accidentally extracting random dates (ID issue date, expiry date, etc.)
        // ===================================================================
        return "";
    }

    // Driver's License Birthdate - SPECIAL HANDLING
    // Driver's License has "Weight(kg)" or other fields after birthdate label
    // So we CANNOT use next-line method - must search for date patterns around label
    function extractDriverLicenseBirthdate(lines) {

        // Print all lines for debugging
        lines.forEach((line, idx) => {
        });

        // Search for birthdate label
        const birthdateLabels = FIELD_LABELS['birthdate'] || [
            'DATE OF BIRTH', 'BIRTH DATE', 'BIRTHDATE', 'DOB', 'BIRTHDAY', 'BIRTH'
        ];

        let labelLineIndex = -1;
        let detectedLabel = '';
        let extractedDate = "";

        // PRIORITY SEARCH: First check lines 14-16 (0-indexed: 13-15)
        // This is the typical location for "Date of Birth" on Philippine Driver's License

        // First, try to find date pattern directly in lines 14-16 (birthdate value is often here)
        for (let i = 13; i <= 15 && i < lines.length; i++) {
            const line = lines[i].trim();
            const upperLine = line.toUpperCase();


            // Check if this line contains a birthdate label
            for (const label of birthdateLabels) {
                if (upperLine.includes(label)) {
                    labelLineIndex = i;
                    detectedLabel = label;
                    break;
                }
            }
        }

        // If not found in priority zone (lines 14-16), search entire document
        if (labelLineIndex < 0) {
            for (let i = 0; i < lines.length; i++) {
                const line = lines[i].trim();
                const upperLine = line.toUpperCase();

                for (const label of birthdateLabels) {
                    if (upperLine.includes(label)) {
                        labelLineIndex = i;
                        detectedLabel = label;
                        break;
                    }
                }
                if (labelLineIndex >= 0) break;
            }
        }

        if (labelLineIndex < 0) {
            return "";
        }


        // ═══════════════════════════════════════════════════════════════════════
        // COMPREHENSIVE DATE PATTERN MATCHING
        // Accept ALL possible date formats and let normalization handle conversion
        // ═══════════════════════════════════════════════════════════════════════
        const datePatterns = [
            // NUMERIC FORMATS WITH DASHES
            { pattern: /\b(\d{4})-(\d{1,2})-(\d{1,2})\b/, name: 'YYYY-MM-DD or YYYY-DD-MM (with dashes)' },
            { pattern: /\b(\d{1,2})-(\d{1,2})-(\d{4})\b/, name: 'DD-MM-YYYY or MM-DD-YYYY (with dashes)' },
            { pattern: /\b(\d{2})-(\d{1,2})-(\d{2})\b/, name: 'YY-MM-DD (2-digit year with dashes)' },

            // NUMERIC FORMATS WITH SLASHES
            { pattern: /\b(\d{1,2})\/(\d{1,2})\/(\d{4})\b/, name: 'MM/DD/YYYY or DD/MM/YYYY (with slashes)' },
            { pattern: /\b(\d{4})\/(\d{1,2})\/(\d{1,2})\b/, name: 'YYYY/MM/DD or YYYY/DD/MM (with slashes)' },
            { pattern: /\b(\d{2})\/(\d{1,2})\/(\d{2})\b/, name: 'YY/MM/DD (2-digit year with slashes)' },

            // NUMERIC FORMATS WITH DOTS
            { pattern: /\b(\d{1,2})\.(\d{1,2})\.(\d{4})\b/, name: 'DD.MM.YYYY or MM.DD.YYYY (with dots)' },
            { pattern: /\b(\d{4})\.(\d{1,2})\.(\d{1,2})\b/, name: 'YYYY.MM.DD or YYYY.DD.MM (with dots)' },

            // NUMERIC FORMATS WITHOUT SEPARATORS
            { pattern: /\b(\d{8})\b/, name: 'YYYYMMDD or DDMMYYYY (no separators)' },

            // TEXT-BASED FORMATS - MONTH NAMES (ENGLISH)
            { pattern: /\b([A-Z]{3,9})\s+(\d{1,2})[,\s]+(\d{4})\b/i, name: 'MONTH DD, YYYY (e.g., December 25, 2003)' },
            { pattern: /\b(\d{1,2})\s+([A-Z]{3,9})[,\s]+(\d{4})\b/i, name: 'DD MONTH YYYY (e.g., 25 December 2003)' },
            { pattern: /\b([A-Z]{3,9})[\s\-\/]+(\d{1,2})[\s\-\/]+(\d{4})\b/i, name: 'MONTH-DD-YYYY with any separator' },
            { pattern: /\b(\d{1,2})[\s\-\/]+([A-Z]{3,9})[\s\-\/]+(\d{4})\b/i, name: 'DD-MONTH-YYYY with any separator' },

            // COMPACT TEXT FORMATS
            { pattern: /\b([A-Z]{3})(\d{1,2})(\d{4})\b/i, name: 'MonDDYYYY (e.g., Dec252003)' },
            { pattern: /\b(\d{1,2})([A-Z]{3})(\d{4})\b/i, name: 'DDMonYYYY (e.g., 25Dec2003)' },

            // FILIPINO MONTH FORMATS (common in PH IDs)
            { pattern: /\b(ENERO|PEBRERO|FEBRERO|MARSO|ABRIL|MAYO|HUNYO|HULYO|AGOSTO|SETYEMBRE|OKTUBRE|NOBYEMBRE|DISYEMBRE)\s+(\d{1,2})[,\s]+(\d{4})\b/i, name: 'Filipino MONTH DD, YYYY' },
            { pattern: /\b(\d{1,2})\s+(ENERO|PEBRERO|FEBRERO|MARSO|ABRIL|MAYO|HUNYO|HULYO|AGOSTO|SETYEMBRE|OKTUBRE|NOBYEMBRE|DISYEMBRE)\s+(\d{4})\b/i, name: 'Filipino DD MONTH YYYY' }
        ];

        // ═══════════════════════════════════════════════════════════════════════
        // PRIORITY SEARCH STRATEGY: Lines 14-16 first (typical date value location)
        // ═══════════════════════════════════════════════════════════════════════
        // Build search order with lines 14-16 as highest priority
        let searchOrder = [];

        // PRIORITY 1: Lines 14-16 (0-indexed: 13-15) - typical date value location
        for (let i = 13; i <= 15; i++) {
            if (i < lines.length && !searchOrder.includes(i)) {
                searchOrder.push(i);
            }
        }

        // PRIORITY 2: Add same line as label (if not already in searchOrder)
        if (!searchOrder.includes(labelLineIndex)) {
            searchOrder.push(labelLineIndex);
        }

        // PRIORITY 3: Lines near the label
        const nearbyLines = [
            labelLineIndex - 1,       // Line before label
            labelLineIndex + 1,       // Line after label
            labelLineIndex - 2,       // 2 lines before label
            labelLineIndex + 2        // 2 lines after label
        ];

        for (const lineIdx of nearbyLines) {
            if (lineIdx >= 0 && lineIdx < lines.length && !searchOrder.includes(lineIdx)) {
                searchOrder.push(lineIdx);
            }
        }


        for (const searchIndex of searchOrder) {
            if (searchIndex < 0 || searchIndex >= lines.length) continue;

            const searchLine = lines[searchIndex].trim();
            const positionDesc = searchIndex === labelLineIndex ? 'same line' :
                searchIndex < labelLineIndex ? `${labelLineIndex - searchIndex} line(s) before` :
                    `${searchIndex - labelLineIndex} line(s) after`;


            // Skip if line contains non-date keywords
            const nonDateKeywords = ['WEIGHT', 'HEIGHT', 'KG', 'CM', 'LBS', 'RESTRICTIONS', 'ADDRESS', 'BLOOD'];
            const upperSearch = searchLine.toUpperCase();
            const hasNonDateKeyword = nonDateKeywords.some(keyword => upperSearch.includes(keyword));

            if (hasNonDateKeyword) {
                continue;
            }

            // Try each date pattern
            for (const { pattern, name } of datePatterns) {
                const match = searchLine.match(pattern);
                if (match) {

                    // Extract the date value and normalize
                    const dateValue = match[0];

                    // Use the main extractBirthdate function to normalize and validate
                    // Create a temporary lines array with just this date for processing
                    const tempLines = ['DATE OF BIRTH', dateValue];
                    const normalized = extractBirthdate(tempLines, 'Driver\'s License');

                    if (normalized) {
                        return normalized;
                    } else {
                    }
                }
            }
        }

        // If we reach here, label was found but no valid date

        const errorMessage =
            `Birthdate field detected on Driver's License but value could not be read.\n\n` +
            `The OCR system found a "${detectedLabel}" label on your ID, ` +
            `but could not find a valid date value nearby.\n\n` +
            `Please ensure:\n` +
            `1. The birthdate on the ID is clear and readable\n` +
            `2. The ID image is well-lit with no glare or shadows\n` +
            `3. The entire ID is visible in the photo\n` +
            `4. The image is not blurry or distorted\n\n` +
            `Try taking a new photo with better clarity.`;

        showVerificationErrorModal(errorMessage);
        return "";
    }

    // Extract Driver's License Gender (uses unified extractGender function)
    // Note: This function is kept for backward compatibility but
    // Driver's License parsing now uses extractGender() directly
    function extractDriverLicenseGender(lines) {
        return extractGender(lines);
    }

    // Extract Driver's License City
    // Driver's License City extraction - uses unified function
    function extractDriverLicenseCity(lines) {
        return extractDavaoCity(lines);
    }

    // Parse PhilSys ID Front with precision extraction
    function parsePhilSysFront(text) {
        const lines = text.split('\n').map(line => line.trim()).filter(line => line);

        lines.forEach((line, index) => {
        });

        debugLog('PHILSYS-PARSING', { totalLines: lines.length, lines });

        let extractedData = {
            idNumber: "",
            lastName: "",
            firstName: "",
            middleName: "",
            suffix: "",
            birthdate: "",
            civilStatus: "",
            city: "",
            address: "",
            confidence: {}
        };

        // Extract PhilSys Number
        extractedData.idNumber = extractPhilSysNumber(lines);
        if (extractedData.idNumber) {
            const validation = validateIDNumber(extractedData.idNumber, 'phil-id');
            extractedData.confidence.idNumber = validation.confidence;
        }

        // Extract names
        const nameResult = extractPhilSysName(lines, text);
        extractedData.lastName = nameResult.lastName;
        extractedData.firstName = nameResult.firstName;
        extractedData.middleName = nameResult.middleName;
        extractedData.suffix = nameResult.suffix || "";

        // Validate names
        if (extractedData.lastName) {
            const validation = validateName(extractedData.lastName, 'lastName');
            extractedData.confidence.lastName = validation.confidence;
        }
        if (extractedData.firstName) {
            const validation = validateName(extractedData.firstName, 'firstName');
            extractedData.confidence.firstName = validation.confidence;
        }
        if (extractedData.middleName) {
            const validation = validateName(extractedData.middleName, 'middleName');
            extractedData.confidence.middleName = validation.confidence;
        }

        // Extract birthdate
        extractedData.birthdate = extractPhilSysBirthdate(lines);
        if (extractedData.birthdate) {
            const validation = validateBirthdate(extractedData.birthdate);
            extractedData.confidence.birthdate = validation.confidence;
        }

        // Extract address - MULTI-LINE SUPPORT (1-3 lines concatenated)
        extractedData.address = extractMultiLineAddress(lines);
        if (extractedData.address) {
        } else {
        }

        // Extract city for Davao verification (legacy - address is now preferred)
        extractedData.city = extractPhilSysCity(lines);

        // Store extracted data for debugging
        lastExtractedData = extractedData;
        debugLog('PHILSYS-EXTRACTED', extractedData);

        // Calculate overall confidence
        const confidenceValues = Object.values(extractedData.confidence).filter(v => v > 0);
        const overallConfidence = confidenceValues.length > 0
            ? Math.round(confidenceValues.reduce((a, b) => a + b, 0) / confidenceValues.length)
            : 0;

        // Log extracted data in labeled format
        logExtractedData(extractedData, 'phil-id', overallConfidence);

        // === STEP 1.5: VALIDATE EXTRACTED DATA (NO LABELS AS VALUES) ===
        const dataValidation = validateExtractedNames(extractedData);
        if (!dataValidation.valid) {
            // Hide progress bar on validation error
            if (progressBar) {
                progressBar.classList.add('hidden');
            }
            if (status) {
                status.innerHTML = '<i class="fas fa-exclamation-circle mr-2 text-red-600"></i>Extraction failed - invalid data detected';
                status.className = 'text-sm text-red-600 mt-2 font-semibold';
            }

            if (resultBox) {
                resultBox.classList.remove('hidden');
                resultBox.className = "p-4 bg-red-50 border border-red-200 rounded-xl text-sm text-red-700";
                resultBox.innerHTML = `
                    <i class="fas fa-exclamation-triangle mr-2"></i>
                    <strong>Extraction Error:</strong> Cannot read National ID data properly.
                    <br><small>The following issues were detected:</small>
                    <ul class="mt-2 ml-4 list-disc">
                        ${dataValidation.issues.map(issue => `<li>${issue}</li>`).join('')}
                    </ul>
                    <small class="block mt-2">Please ensure the ID image is clear and well-lit, then try again.</small>
                `;
            }
            return; // STOP - Do not proceed with invalid data
        }

        // === STEP 2: NAME MATCHING VALIDATION (CRITICAL) ===
        if (status) {
            status.innerHTML = '<i class="fas fa-user-check fa-pulse mr-2"></i>Validating name match...';
        }
        if (progress) progress.style.width = '90%';

        const nameValidation = validateNameMatch(
            extractedData.firstName,
            extractedData.middleName,
            extractedData.lastName,
            extractedData.suffix
        );

        if (!nameValidation.matches) {
            // Hide progress bar on name mismatch
            if (progressBar) {
                progressBar.classList.add('hidden');
            }
            if (status) {
                status.innerHTML = '<i class="fas fa-exclamation-circle mr-2 text-red-600"></i>Name validation failed';
                status.className = 'text-sm text-red-600 mt-2 font-semibold';
            }

            showNameMismatchModal(
                nameValidation.details ? nameValidation.details.extractedName : `${extractedData.lastName}, ${extractedData.firstName} ${extractedData.middleName}`,
                nameValidation.details ? nameValidation.details.registeredName : `${registeredLastName}, ${registeredFirstName} ${registeredMiddleName}`,
                nameValidation.details || {}
            );
            return; // BLOCK - Do not proceed with data extraction
        }


        // === STEP 3: DATA EXTRACTION & POPULATION ===
        if (status) {
            status.innerHTML = '<i class="fas fa-database fa-pulse mr-2"></i>Populating form fields...';
        }
        if (progress) progress.style.width = '95%';

        updateFormFieldsAdvanced(
            extractedData.idNumber,
            extractedData.firstName,
            extractedData.middleName,
            extractedData.lastName,
            extractedData.birthdate,
            "",
            extractedData.civilStatus,
            extractedData.suffix,
            "",
            text,
            'phil-id',
            overallConfidence
        );

        // Check if it's Davao City using CONCATENATED ADDRESS (supports 1-3 line addresses)
        // FALLBACK: Also check full OCR text if address extraction failed
        let isDavaoCityResult = isDavaoCity(extractedData.address);
        
        // Fallback: If address-based check failed, check the FULL OCR text
        if (!isDavaoCityResult) {
            isDavaoCityResult = isDavaoCity(text);
            if (isDavaoCityResult) {
            }
        }
        
        showDavaoVerificationResult(isDavaoCityResult, extractedData.address || text, 'national-id');
    }

    // Extract PhilSys Number
    function extractPhilSysNumber(lines) {
        // Look for PhilSys number pattern (XXXX-XXXX-XXXX-XXXX)
        for (const line of lines) {
            const philSysPattern = /\d{4}-\d{4}-\d{4}-\d{4}/;
            const match = line.match(philSysPattern);
            if (match) {
                return match[0];
            }
        }
        return "";
    }

    // Extract PhilSys Name
    function extractPhilSysName(lines, text) {
        let lastName = "", firstName = "", middleName = "", suffix = "";


        // Use independent extraction functions (works like lastName extraction)
        const lastNameResult = extractFieldValue(lines, 'lastName');
        if (lastNameResult && lastNameResult.value) {
            lastName = cleanName(lastNameResult.value);

            // Validate that lastName is not a label
            if (isExtractedTextALabel(lastName) || lastName.length < 2) {
                lastName = "";
            } else {
            }
        }

        const namesParsed = extractFirstName(lines);
        firstName = namesParsed.firstName;
        middleName = namesParsed.middleName;
        suffix = namesParsed.suffix;


        // PRIORITY: ALWAYS check for separate middle name field (National ID often has this)
        // If a separate field exists, it takes priority over parsed middle name
        const extractedMiddleName = extractMiddleName(lines);

        if (extractedMiddleName && extractedMiddleName.length >= 2) {

            // OPTIMIZATION: If separate middle name exists AND we had parsed a middle name from first name field,
            // re-parse first name to include what we thought was middle name as part of first name
            if (middleName && middleName.length >= 2) {

                // Reconstruct full first name (including what we thought was middle name)
                const fullFirstName = firstName + ' ' + middleName;
                firstName = fullFirstName.trim();
            }

            // Use the separate middle name field
            middleName = extractedMiddleName;
        } else if (!middleName || middleName.length < 2) {
        } else {
        }

        // Check for separate suffix if not found
        if (!suffix) {
            suffix = extractSuffix(lines);
            if (suffix) {
            }
        }

        return { lastName, firstName, middleName, suffix };
    }

    // Extract PhilSys Birthdate
    // PhilSys Birthdate - uses unified function
    function extractPhilSysBirthdate(lines) {
        return extractBirthdate(lines, 'National ID (PhilSys)');
    }

    // Extract PhilSys City - uses unified function with National ID specific handling
    function extractPhilSysCity(lines) {

        // Use the unified Davao City extraction
        const city = extractDavaoCity(lines);

        // Additional logging for National ID
        if (city === "Davao City") {
        } else {
        }

        return city;
    }

    // Parse PhilSys ID Back - Extract gender AND civil status
    function parsePhilSysBack(text) {
        const lines = text.split('\n').map(line => line.trim()).filter(line => line);

        lines.forEach((line, index) => {
        });

        let extractedFields = [];

        // Extract gender/sex using unified function
        const sex = extractGender(lines);
        if (sex) {
            const genderField = document.getElementById('gender') || document.getElementById('sex');
            if (genderField) genderField.value = sex;
            extractedFields.push('Gender');
        }

        // Extract civil status/marital status
        const civilStatus = extractCivilStatus(lines);
        if (civilStatus) {
            const civilStatusField = document.getElementById('civilstatus') ||
                document.getElementById('maritalstatus') ||
                document.getElementById('civil-status') ||
                document.getElementById('marital-status');
            if (civilStatusField) {
                civilStatusField.value = civilStatus;
                extractedFields.push('Civil Status');
            }
        }

        // Show result message
        if (extractedFields.length > 0 && resultBox) {
            resultBox.classList.remove('hidden');
            resultBox.className = "p-4 bg-blue-50 border border-blue-200 rounded-xl text-sm text-blue-700 slide-down";
            resultBox.innerHTML = `<i class="fas fa-info-circle mr-2"></i>${extractedFields.join(' and ')} extracted from National ID back. Please verify.`;
        }

        // ===================================================================
        // COMPLETION: Update progress bar to 100% and show completion status
        // ===================================================================

        // Set progress bar to 100%
        if (progress) {
            progress.style.width = '100%';
            progress.style.transition = 'width 0.5s ease-in-out';
        }

        // Update status to "Completed" with green styling
        if (status) {
            status.innerHTML = '<i class="fas fa-check-circle mr-2 text-green-600 animate-pulse" id="status-icon-philsys"></i><strong>Verification Complete</strong> - National ID back side processed';
            status.className = 'text-sm text-green-600 mt-2 font-semibold animate-fadeIn';

            // Stop pulse animation after 2 seconds
            setTimeout(() => {
                const statusIcon = document.getElementById('status-icon-philsys');
                if (statusIcon) {
                    statusIcon.classList.remove('animate-pulse');
                }
            }, 2000);
        }

        // Hide progress bar after a delay to show the completed state
        if (progressBar) {
            setTimeout(() => {
                progressBar.classList.add('hidden');
            }, 2000); // 2 second delay to show the completed state
        }
    }

    // Extract PhilSys Gender (uses unified extractGender function)
    // Note: This function is kept for backward compatibility but
    // PhilSys back parsing now uses extractGender() directly
    function extractPhilSysGender(lines) {
        return extractGender(lines);
    }


    // Extract Davao City
    // ═══════════════════════════════════════════════════════════════════════════════
    // UNIFIED DAVAO CITY EXTRACTION - ALL ID TYPES (Driver's License, National ID, UMID)
    // ═══════════════════════════════════════════════════════════════════════════════
    // POSITION-AGNOSTIC VALIDATION:
    //   - Accepts if "Davao City", "City of Davao", "DVO City", or "Davao" appears
    //     ANYWHERE in the complete concatenated address (across all lines)
    //   - Position doesn't matter - can be in first line, middle, or last line
    //   - Multi-line addresses are fully concatenated before searching
    //
    // DRIVER'S LICENSE PRIORITY SEARCH (NEW):
    //   - Specifically searches lines 20-25 of OCR text (typical address location on DL)
    //   - This range typically contains the ADDRESS section on Philippine Driver's Licenses
    //   - If "Davao City" is found in lines 20-25, prioritizes this section
    //   - Ensures accurate detection even if address is in a specific position
    //   - Example line 20-25 content: "123 Main St | Brgy Poblacion | Davao City"
    //
    // MULTI-LINE ADDRESS SUPPORT (especially for Driver's License):
    //   - Extracts address components from separate fields (street, barangay, city, etc.)
    //   - Extracts multi-line addresses that span 2-3 lines after ADDRESS label
    //   - Concatenates ALL components into single string for comprehensive search
    //   - Example: "123 Main St" + "Brgy Poblacion" + "Davao City" = full address
    //
    // NAME CONTAMINATION PREVENTION:
    //   - Filters out person names that may appear near address fields
    //   - Detects and rejects name patterns (e.g., "DELA CRUZ, JUAN" or "MARIA SANTOS")
    //   - Stops address extraction if NAME labels are encountered
    //   - Validates each address component to ensure it's not a person name
    //   - Ensures only legitimate address data is concatenated
    //
    // REJECTION LOGIC:
    //   - Rejects if OTHER cities are detected (Digos, Tagum, Panabo, Mati, etc.)
    //   - Even if address contains Davao province name, still rejects if another city found
    //
    // ACCEPTED KEYWORD VARIATIONS (case-insensitive):
    //   ✓ "Davao City"      ✓ "city of davao"    ✓ "DAVAO CITY"
    //   ✓ "City of Davao"   ✓ "DVO City"         ✓ "Davao"
    // ═══════════════════════════════════════════════════════════════════════════════
    function extractDavaoCity(lines) {

        // ===================================================================
        // ACCEPTED KEYWORDS: These variations will return "Davao City"
        // ===================================================================
        // IMPORTANT: These keywords can appear ANYWHERE in the concatenated address
        // Position doesn't matter - as long as one of these appears in the full
        // address text (across all lines), the ID will be ACCEPTED
        // All uppercase variations are automatically handled by toUpperCase()
        const davaoCityKeywords = [
            'DAVAO CITY',      // Most common format
            'CITY OF DAVAO',   // Official format
            'DVO CITY',        // Common abbreviation
            'DAVAO'            // Accept standalone "Davao" (refers to Davao City)
        ];

        // REJECTION KEYWORDS: Cities/Municipalities that are NOT Davao City
        // These are other cities in Davao provinces - they should be REJECTED
        const rejectionCities = [
            // Davao del Sur cities (NOT Davao City)
            'DIGOS', 'DIGOS CITY', 'BANSALAN', 'HAGONOY', 'KIBLAWAN', 'MAGSAYSAY',
            'MALALAG', 'MATANAO', 'PADADA', 'SANTA CRUZ', 'SULOP',
            // Davao del Norte cities (NOT Davao City)
            'TAGUM', 'TAGUM CITY', 'PANABO', 'PANABO CITY', 'SAMAL', 'ISLAND GARDEN CITY OF SAMAL',
            'ASUNCION', 'BRAULIO', 'CARMEN', 'KAPALONG', 'NEW CORELLA', 'SAN ISIDRO',
            'SANTO TOMAS', 'TALAINGOD',
            // Davao Oriental cities (NOT Davao City)
            'MATI', 'MATI CITY', 'BAGANGA', 'BANAYBANAY', 'BOSTON', 'CARAGA', 'CATEEL',
            'GOVERNOR GENEROSO', 'LUPON', 'MANAY', 'TARRAGONA',
            // Davao Occidental cities (NOT Davao City)
            'MALITA', 'DON MARCELINO', 'JOSE ABAD SANTOS', 'SANTA MARIA', 'SARANGANI'
        ];

        // ===================================================================
        // STEP 1: Extract ALL address components separately
        // ===================================================================

        const addressComponents = {
            street: '',
            subdivision: '',
            barangay: '',
            district: '',
            city: '',
            province: '',
            address: '' // General address field
        };

        // Extract each component using label-based extraction
        const streetResult = extractFieldValue(lines, 'street');
        if (streetResult && streetResult.value) {
            addressComponents.street = streetResult.value.trim();
        }

        const subdivisionResult = extractFieldValue(lines, 'subdivision');
        if (subdivisionResult && subdivisionResult.value) {
            addressComponents.subdivision = subdivisionResult.value.trim();
        }

        const barangayResult = extractFieldValue(lines, 'barangay');
        if (barangayResult && barangayResult.value) {
            addressComponents.barangay = barangayResult.value.trim();
        }

        const districtResult = extractFieldValue(lines, 'district');
        if (districtResult && districtResult.value) {
            addressComponents.district = districtResult.value.trim();
        }

        const cityResult = extractFieldValue(lines, 'city');
        if (cityResult && cityResult.value) {
            addressComponents.city = cityResult.value.trim();
        }

        const provinceResult = extractFieldValue(lines, 'province');
        if (provinceResult && provinceResult.value) {
            addressComponents.province = provinceResult.value.trim();
        }

        const addressResult = extractFieldValue(lines, 'address');
        if (addressResult && addressResult.value) {
            addressComponents.address = addressResult.value.trim();
        }

        // ===================================================================
        // STEP 1.5: Extract multi-line address (especially for Driver's License)
        // Driver's License often has address spanning multiple lines after ADDRESS label
        // ===================================================================

        const multiLineAddress = extractMultiLineAddress(lines);
        if (multiLineAddress && multiLineAddress.length > 0) {
            // If we have a multi-line address and no general address was found, use it
            if (!addressComponents.address || addressComponents.address.length === 0) {
                addressComponents.address = multiLineAddress;
            }
        } else {
        }

        // ===================================================================
        // STEP 2: Validate and filter address components to remove name contamination
        // ===================================================================

        // Helper function to detect if text looks like a person name
        const looksLikeName = (text) => {
            if (!text) return false;
            const cleanText = text.trim();
            const wordCount = cleanText.split(/\s+/).length;
            const hasNumbers = /\d/.test(cleanText);
            const hasComma = cleanText.includes(',');
            const isAllCaps = cleanText === cleanText.toUpperCase();

            // Common name patterns
            if (hasComma && wordCount >= 2 && wordCount <= 5 && !hasNumbers) {
                return true;
            }

            if (wordCount >= 1 && wordCount <= 3 && isAllCaps && !hasNumbers && cleanText.length < 50) {
                const addressKeywords = ['STREET', 'ST', 'BLVD', 'AVE', 'ROAD', 'RD',
                    'BLOCK', 'LOT', 'BLK', 'BARANGAY', 'BRGY', 'CITY', 'DAVAO',
                    'SUBDIVISION', 'VILLAGE', 'PUROK', 'SITIO', 'ZONE', 'DISTRICT',
                    'POBLACION', 'MATINA', 'BUHANGIN', 'TORIL', 'TALOMO', 'CALINAN',
                    'DEL', 'SUR', 'NORTE', 'ORIENTAL', 'OCCIDENTAL'];
                const upperText = cleanText.toUpperCase();
                const hasAddressKeyword = addressKeywords.some(kw => upperText.includes(kw));

                if (!hasAddressKeyword) {
                    return true;
                }
            }

            return false;
        };

        // Filter out components that look like names
        const validatedComponents = {
            street: addressComponents.street,
            subdivision: addressComponents.subdivision,
            barangay: addressComponents.barangay,
            district: addressComponents.district,
            city: addressComponents.city,
            province: addressComponents.province,
            address: addressComponents.address
        };

        // Validate each component
        Object.keys(validatedComponents).forEach(key => {
            const value = validatedComponents[key];
            if (value && looksLikeName(value)) {
                validatedComponents[key] = '';
            }
        });

        // ===================================================================
        // STEP 3: Concatenate validated address parts into one complete address
        // Format: Street, Subdivision, Barangay, District, City, Province, Address
        // ===================================================================

        const allAddressParts = [
            validatedComponents.street,
            validatedComponents.subdivision,
            validatedComponents.barangay,
            validatedComponents.district,
            validatedComponents.city,
            validatedComponents.province,
            validatedComponents.address
        ].filter(part => part && part.length > 0); // Remove empty parts

        let completeAddress = allAddressParts.join(' ');

        // ===================================================================
        // STEP 4: Priority search in lines 20-25 (Driver's License address section)
        // ===================================================================

        let addressFromLines20to25 = '';
        if (lines.length >= 20) {
            const startLine = 19; // 0-indexed, so line 20 is index 19
            const endLine = Math.min(24, lines.length - 1); // 0-indexed, so line 25 is index 24


            const targetLines = [];
            for (let i = startLine; i <= endLine; i++) {
                if (lines[i] && lines[i].trim().length > 0) {
                    targetLines.push(lines[i].trim());
                }
            }

            if (targetLines.length > 0) {
                addressFromLines20to25 = targetLines.join(' ');

                // Check if this section contains Davao City
                const upperSection = addressFromLines20to25.toUpperCase();
                const hasDavaoInSection =
                    upperSection.includes('DAVAO CITY') ||
                    upperSection.includes('CITY OF DAVAO') ||
                    upperSection.includes('DVO CITY') ||
                    upperSection.includes('DAVAO');

                if (hasDavaoInSection) {
                    // If we have Davao City in this section, prioritize it
                    if (!completeAddress || completeAddress.length === 0) {
                        completeAddress = addressFromLines20to25;
                    } else {
                        // Append to existing address for comprehensive search
                        completeAddress = completeAddress + ' ' + addressFromLines20to25;
                    }
                } else {
                }
            } else {
            }
        } else {
        }

        // ===================================================================
        // STEP 5: Fallback - scan all raw OCR lines if no address found
        // ===================================================================
        if (!completeAddress || completeAddress.length === 0) {
            completeAddress = lines.join(' ');
        }

        // ===================================================================
        // STEP 6: Check the COMPLETE address for Davao City keywords
        // ===================================================================

        if (completeAddress && completeAddress.length > 0) {
            const upperCompleteAddress = completeAddress.toUpperCase();

            // PRIORITY 1: Check for OTHER CITIES/MUNICIPALITIES FIRST (these are REJECTIONS)
            // Cities like Digos, Tagum, Panabo, Mati, etc. are NOT Davao City
            let hasRejectionCity = false;
            let rejectionCityFound = '';

            for (const rejectionCity of rejectionCities) {
                if (upperCompleteAddress.includes(rejectionCity)) {
                    hasRejectionCity = true;
                    rejectionCityFound = rejectionCity;
                    break;
                }
            }

            if (hasRejectionCity) {
                return "Not detected";
            }

            // PRIORITY 2: Check for ACCEPTED Davao City keywords
            // Includes: Davao, Davao City, City of Davao, and PROVINCE NAMES (DAVAO DEL SUR, etc.)
            let hasDavaoCityKeyword = false;
            let foundKeyword = '';

            for (const keyword of davaoCityKeywords) {
                if (upperCompleteAddress.includes(keyword)) {
                    hasDavaoCityKeyword = true;
                    foundKeyword = keyword;
                    break;
                }
            }

            if (hasDavaoCityKeyword) {
                return "Davao City";
            }

        } else {
        }

        return "Not detected";
    }

    // Legacy function - now uses unified extractDavaoCity
    function extractCityForUMID(lines) {
        return extractDavaoCity(lines);
    }

    // Parse UMID Front with precision extraction
    function parseUMIDFront(text) {
        const lines = text.split('\n').map(line => line.trim()).filter(line => line);

        debugLog('UMID-PARSING', { totalLines: lines.length, lines });

        let extractedData = {
            idNumber: "",
            lastName: "",
            firstName: "",
            middleName: "",
            suffix: "",
            birthdate: "",
            sex: "",
            civilStatus: "",
            city: "",
            address: "",
            barangay: "",
            confidence: {}
        };

        // Extract UMID Number (CRN)
        extractedData.idNumber = extractUMIDNumber(lines);
        if (extractedData.idNumber) {
            const validation = validateIDNumber(extractedData.idNumber, 'umid');
            extractedData.confidence.idNumber = validation.confidence;
        }

        // Extract names
        const nameResult = extractUMIDName(lines, text);
        extractedData.lastName = nameResult.lastName;
        extractedData.firstName = nameResult.firstName;
        extractedData.middleName = nameResult.middleName;
        extractedData.suffix = nameResult.suffix || "";

        // Validate names
        if (extractedData.lastName) {
            const validation = validateName(extractedData.lastName, 'lastName');
            extractedData.confidence.lastName = validation.confidence;
        }
        if (extractedData.firstName) {
            const validation = validateName(extractedData.firstName, 'firstName');
            extractedData.confidence.firstName = validation.confidence;
        }
        if (extractedData.middleName) {
            const validation = validateName(extractedData.middleName, 'middleName');
            extractedData.confidence.middleName = validation.confidence;
        }

        // Extract birthdate (UMID uses instant extraction - no validation needed)
        extractedData.birthdate = extractUMIDBirthdate(lines);
        if (extractedData.birthdate) {
            // For UMID, set high confidence since it's the only date on the card
            extractedData.confidence.birthdate = 95;
        } else {
        }

        // Extract gender using unified function with label matching
        extractedData.sex = extractGender(lines);
        extractedData.confidence.sex = extractedData.sex ? 90 : 0;
        if (extractedData.sex) {
        } else {
        }

        // Extract address - MULTI-LINE SUPPORT (1-4 lines concatenated for UMID)
        // ROBUST APPROACH: Try labeled extraction first, then pattern-based if needed
        extractedData.address = extractMultiLineAddress(lines);

        // If labeled extraction failed, do smart pattern-based scan of ALL lines
        if (!extractedData.address || extractedData.address.trim().length === 0) {
            extractedData.address = extractAddressFromLines(lines);
        }

        if (extractedData.address) {
        } else {
        }

        // Extract barangay (PRIORITY for address components)
        // UMID typically doesn't have "BARANGAY" label, so search through address text
        let detectedBarangay = null;

        // PRIORITY 1: Extract from position before "Davao City" (MOST RELIABLE for UMID)
        if (extractedData.address) {
            detectedBarangay = extractBarangayFromAddressPosition(extractedData.address);

            if (detectedBarangay) {
            } else {
            }
        }

        // PRIORITY 2: Full text search in extracted address (fallback)
        if (!detectedBarangay && extractedData.address) {
            detectedBarangay = extractBarangayFromAddress(extractedData.address);

            if (detectedBarangay) {
            } else {
            }
        }

        // PRIORITY 3: Try label-based extraction (rare but possible)
        if (!detectedBarangay) {
            const barangayResult = extractFieldValue(lines, 'barangay');
            if (barangayResult && barangayResult.value) {
                detectedBarangay = barangayResult.value.trim();
            } else {
            }
        }

        // PRIORITY 4: Search through ALL OCR text as last resort
        if (!detectedBarangay) {
            const fullText = text || lines.join(' ');

            // Try positional extraction on full text first
            detectedBarangay = extractBarangayFromAddressPosition(fullText);

            // If still not found, try full text search
            if (!detectedBarangay) {
                detectedBarangay = extractBarangayFromAddress(fullText);
            }

            if (detectedBarangay) {
            } else {
            }
        }

        // Store the detected barangay
        if (detectedBarangay) {
            extractedData.barangay = detectedBarangay;
        } else {
        }

        // Extract city (legacy - address is now preferred for Davao verification)
        extractedData.city = extractUMIDCity(lines);

        // Store extracted data for debugging
        lastExtractedData = extractedData;
        debugLog('UMID-EXTRACTED', extractedData);

        // Calculate overall confidence
        const confidenceValues = Object.values(extractedData.confidence).filter(v => v > 0);
        const overallConfidence = confidenceValues.length > 0
            ? Math.round(confidenceValues.reduce((a, b) => a + b, 0) / confidenceValues.length)
            : 0;

        // Log extracted data in labeled format
        logExtractedData(extractedData, 'umid', overallConfidence);

        // === STEP 1.5: VALIDATE EXTRACTED DATA (NO LABELS AS VALUES) ===
        const dataValidation = validateExtractedNames(extractedData);
        if (!dataValidation.valid) {
            // Hide progress bar on validation error
            if (progressBar) {
                progressBar.classList.add('hidden');
            }
            if (status) {
                status.innerHTML = '<i class="fas fa-exclamation-circle mr-2 text-red-600"></i>Extraction failed - invalid data detected';
                status.className = 'text-sm text-red-600 mt-2 font-semibold';
            }

            if (resultBox) {
                resultBox.classList.remove('hidden');
                resultBox.className = "p-4 bg-red-50 border border-red-200 rounded-xl text-sm text-red-700";
                resultBox.innerHTML = `
                    <i class="fas fa-exclamation-triangle mr-2"></i>
                    <strong>Extraction Error:</strong> Cannot read UMID data properly.
                    <br><small>The following issues were detected:</small>
                    <ul class="mt-2 ml-4 list-disc">
                        ${dataValidation.issues.map(issue => `<li>${issue}</li>`).join('')}
                    </ul>
                    <small class="block mt-2">Please ensure the ID image is clear and well-lit, then try again.</small>
                `;
            }
            return; // STOP - Do not proceed with invalid data
        }

        // === STEP 2: NAME MATCHING VALIDATION (CRITICAL) ===
        if (status) {
            status.innerHTML = '<i class="fas fa-user-check fa-pulse mr-2"></i>Validating name match...';
        }
        if (progress) progress.style.width = '90%';

        const nameValidation = validateNameMatch(
            extractedData.firstName,
            extractedData.middleName,
            extractedData.lastName,
            extractedData.suffix
        );

        if (!nameValidation.matches) {
            // Hide progress bar on name mismatch
            if (progressBar) {
                progressBar.classList.add('hidden');
            }
            if (status) {
                status.innerHTML = '<i class="fas fa-exclamation-circle mr-2 text-red-600"></i>Name validation failed';
                status.className = 'text-sm text-red-600 mt-2 font-semibold';
            }

            showNameMismatchModal(
                nameValidation.details ? nameValidation.details.extractedName : `${extractedData.lastName}, ${extractedData.firstName} ${extractedData.middleName}`,
                nameValidation.details ? nameValidation.details.registeredName : `${registeredLastName}, ${registeredFirstName} ${registeredMiddleName}`,
                nameValidation.details || {}
            );
            return; // BLOCK - Do not proceed with data extraction
        }


        // === STEP 3: DATA EXTRACTION & POPULATION ===
        if (status) {
            status.innerHTML = '<i class="fas fa-database fa-pulse mr-2"></i>Populating form fields...';
        }
        if (progress) progress.style.width = '95%';

        updateFormFieldsAdvanced(
            extractedData.idNumber,
            extractedData.firstName,
            extractedData.middleName,
            extractedData.lastName,
            extractedData.birthdate,
            extractedData.sex,
            extractedData.civilStatus,
            extractedData.suffix,
            extractedData.barangay,
            text,
            'umid',
            overallConfidence
        );

        // Check if it's Davao City using CONCATENATED ADDRESS (supports 1-4 line addresses)
        // The system intelligently:
        // 1. Concatenates 1-4 lines of address from UMID
        // 2. Searches for barangay in the complete address text
        // 3. Detects all Davao City variants (DAVAO CITY, CITY OF DAVAO, DVO CITY, etc.)
        // FALLBACK: Also check full OCR text if address extraction failed
        let isDavaoCityResult = isDavaoCity(extractedData.address);
        
        // Fallback: If address-based check failed, check the FULL OCR text
        if (!isDavaoCityResult) {
            isDavaoCityResult = isDavaoCity(text);
            if (isDavaoCityResult) {
            }
        }
        
        showDavaoVerificationResult(isDavaoCityResult, extractedData.address || text, 'umid');
    }

    // Extract UMID Number (CRN - Common Reference Number)
    function extractUMIDNumber(lines) {
        // Look for UMID CRN pattern: 0000-0000000-0
        // CRN = Common Reference Number (the ID number on UMID)
        for (const line of lines) {
            const umidPattern = /\d{4}-\d{7}-\d{1}/;
            const match = line.match(umidPattern);
            if (match) {
                return match[0];
            }
        }
        return "";
    }

    // Extract UMID Name - Enhanced with label-value pairing and compound name detection
    function extractUMIDName(lines, text) {
        let lastName = "", firstName = "", middleName = "", suffix = "";


        // Use independent extraction functions (works like lastName extraction)
        const lastNameResult = extractFieldValue(lines, 'lastName');
        if (lastNameResult && lastNameResult.value) {
            lastName = cleanName(lastNameResult.value);

            // Validate that lastName is not a label
            if (isExtractedTextALabel(lastName) || lastName.length < 2) {
                lastName = "";
            } else {
            }
        }

        const namesParsed = extractFirstName(lines);
        firstName = namesParsed.firstName;
        middleName = namesParsed.middleName;
        suffix = namesParsed.suffix;


        // PRIORITY: ALWAYS check for separate middle name field (UMID may have this)
        // If a separate field exists, it takes priority over parsed middle name
        const extractedMiddleName = extractMiddleName(lines);

        if (extractedMiddleName && extractedMiddleName.length >= 2) {

            // OPTIMIZATION: If separate middle name exists AND we had parsed a middle name from first name field,
            // re-parse first name to include what we thought was middle name as part of first name
            if (middleName && middleName.length >= 2) {

                // Reconstruct full first name (including what we thought was middle name)
                const fullFirstName = firstName + ' ' + middleName;
                firstName = fullFirstName.trim();
            }

            // Use the separate middle name field
            middleName = extractedMiddleName;
        } else if (!middleName || middleName.length < 2) {
        } else {
        }

        // Check for separate suffix if not found
        if (!suffix) {
            suffix = extractSuffix(lines);
            if (suffix) {
            }
        }

        // Strategy 4: Fallback - Sequential extraction (for UMIDs without labels)
        if (!lastName || !firstName) {

            const nameLines = [];

            for (let i = 0; i < lines.length; i++) {
                const line = lines[i].trim();

                // Skip lines that contain UMID indicators or keywords
                const skipKeywords = ['UMID', 'UNIFIED', 'MULTI', 'PURPOSE', 'CRN', 'COMMON', 'REFERENCE',
                    'NUMBER', 'DATE', 'BIRTH', 'SEX', 'GENDER', 'MALE', 'FEMALE', 'DAVAO', 'NAME', 'SURNAME'];
                const hasKeyword = skipKeywords.some(kw => line.toUpperCase().includes(kw));
                if (hasKeyword) continue;

                // Skip lines with dates or ID numbers
                if (/\d{4}-\d{7}-\d{1}/.test(line)) continue; // CRN pattern
                if (/\d{4}-\d{2}-\d{2}/.test(line)) continue; // Date pattern
                if (/^\d+$/.test(line)) continue; // Pure numbers

                // Look for alphabetic lines (names)
                if (/^[A-Za-z\s\-\.]+$/.test(line) && line.length > 2 && line.length < 50) {
                    nameLines.push(cleanName(line));
                }
            }

            // UMID typically displays: Last Name, First Name + Middle Name
            if (nameLines.length >= 1 && !lastName) {
                lastName = nameLines[0];
            }

            if (nameLines.length >= 2 && !firstName) {
                // Second line might be "FIRST MIDDLE" - use smart parser
                const words = nameLines[1].split(/\s+/);
                const parsed = parseFirstMiddleSuffix(words);
                firstName = parsed.firstName;
                if (!middleName) middleName = parsed.middleName;
                if (!suffix) suffix = parsed.suffix;
            }
        }

        return { lastName, firstName, middleName, suffix };
    }

    // Extract UMID Birthdate - Instant extraction without validation
    // UMID only has one date (birthdate) in YYYY/MM/DD format, so we grab it immediately
    // BYPASSES all label-based extraction and validation - just gets the date directly
    function extractUMIDBirthdate(lines) {

        // Log all lines for debugging
        lines.forEach((line, idx) => {
        });

        // UMID date pattern: YYYY/MM/DD
        const umidDatePattern = /\b(\d{4})\/(\d{2})\/(\d{2})\b/;

        // DIRECT EXTRACTION - Search through ALL text, not just clean lines
        // Join all lines into one text block to catch dates even in mixed-label lines
        const fullText = lines.join(' ');

        const match = fullText.match(umidDatePattern);

        if (match) {
            const birthdate = match[0];

            // Convert YYYY/MM/DD to YYYY-MM-DD (database format)
            const normalizedDate = birthdate.replace(/\//g, '-');

            return normalizedDate;
        }

        // Fallback: Try line-by-line if full text search failed
        for (let i = 0; i < lines.length; i++) {
            const line = lines[i].trim();
            const lineMatch = line.match(umidDatePattern);

            if (lineMatch) {
                const birthdate = lineMatch[0];

                // Convert YYYY/MM/DD to YYYY-MM-DD (database format)
                const normalizedDate = birthdate.replace(/\//g, '-');

                return normalizedDate;
            }
        }

        return "";
    }

    // Extract UMID City - Using label-value extraction
    // UMID City extraction - uses unified function
    function extractUMIDCity(lines) {
        return extractDavaoCity(lines);
    }

    // Parse UMID Back
    function parseUMIDBack(text) {
        const lines = text.split('\n').map(line => line.trim()).filter(line => line);

        // Use unified gender extraction function
        const sex = extractGender(lines);

        if (sex) {
            const genderField = document.getElementById('gender') || document.getElementById('sex');
            if (genderField) genderField.value = sex;

            if (resultBox) {
                resultBox.classList.remove('hidden');
                resultBox.className = "p-4 bg-blue-50 border border-blue-200 rounded-xl text-sm text-blue-700 slide-down";
                resultBox.innerHTML = '<i class="fas fa-info-circle mr-2"></i>Gender information extracted from UMID back. Please verify.';
            }
        }

        // ===================================================================
        // COMPLETION: Update progress bar to 100% and show completion status
        // ===================================================================

        // Set progress bar to 100%
        if (progress) {
            progress.style.width = '100%';
            progress.style.transition = 'width 0.5s ease-in-out';
        }

        // Update status to "Completed" with green styling
        if (status) {
            status.innerHTML = '<i class="fas fa-check-circle mr-2 text-green-600 animate-pulse" id="status-icon"></i><strong>Verification Complete</strong> - UMID back side processed';
            status.className = 'text-sm text-green-600 mt-2 font-semibold animate-fadeIn';

            // Stop pulse animation after 2 seconds
            setTimeout(() => {
                const statusIcon = document.getElementById('status-icon');
                if (statusIcon) {
                    statusIcon.classList.remove('animate-pulse');
                }
            }, 2000);
        }

        // Hide progress bar after a delay to show the completed state
        if (progressBar) {
            setTimeout(() => {
                progressBar.classList.add('hidden');
            }, 2000); // 2 second delay to show the completed state
        }
    }

    // Extract UMID Gender (uses unified extractGender function)
    // Note: This function is kept for backward compatibility but
    // UMID parsing now uses extractGender() directly
    function extractUMIDGender(lines) {
        return extractGender(lines);
    }

    // Legacy function - replaced by unified extractGender
    function _extractUMIDGenderOld(lines) {

        // Method 1: Label-value extraction (PREFERRED)
        const labelResult = extractFieldValue(lines, 'gender');
        if (labelResult && labelResult.value) {
            let genderValue = labelResult.value;

            // Normalize gender value
            const upperGender = genderValue.toUpperCase().trim();

            // Handle single letter codes
            if (upperGender === 'M' || upperGender === 'MALE' || upperGender.includes('MALE')) {
                return "Male";
            }
            if (upperGender === 'F' || upperGender === 'FEMALE' || upperGender.includes('FEMALE')) {
                return "Female";
            }

            // Handle Filipino terms
            if (upperGender.includes('LALAKI') || upperGender.includes('LALAKE')) {
                return "Male";
            }
            if (upperGender.includes('BABAE')) {
                return "Female";
            }

            return genderValue;
        }

        // Method 2: Fallback pattern matching (if label not found)
        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const upper = line.toUpperCase();

            // Skip lines with non-gender keywords
            if (upper.includes('UMID') || upper.includes('NAME') ||
                upper.includes('NUMBER') || upper.includes('DATE')) {
                continue;
            }

            // Pattern 1: Standalone "FEMALE" or "MALE" on a line
            if (line.trim().toUpperCase() === 'FEMALE') {
                return "Female";
            }
            if (line.trim().toUpperCase() === 'MALE') {
                return "Male";
            }

            // Pattern 2: Pattern "SEX: MALE" or "GENDER: FEMALE"
            const genderMatch = line.match(/(SEX|GENDER|KASARIAN)\s*:?\s*(MALE|FEMALE|M|F|LALAKI|BABAE)/i);
            if (genderMatch) {
                const gender = genderMatch[2].toUpperCase();
                if (gender === 'MALE' || gender === 'M' || gender === 'LALAKI') {
                    return "Male";
                }
                if (gender === 'FEMALE' || gender === 'F' || gender === 'BABAE') {
                    return "Female";
                }
            }
        }

        return "";
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // Check if city is Davao City
    // ═══════════════════════════════════════════════════════════════════════════════
    // ACCEPTS: "Davao", "Davao City", "City of Davao", "Dvo City" (any case)
    // REJECTS: Other cities/municipalities from Davao provinces (Digos, Tagum, Panabo, Mati, etc.)
    function checkIfDavaoCity(city) {
        if (!city) return false;

        const upperCity = city.toUpperCase().trim();


        // Accept variations of "Davao City" (case-insensitive)
        const davaoCityVariations = [
            'DAVAO',
            'DAVAO CITY',
            'CITY OF DAVAO',
            'DVO CITY',
            'DVO. CITY',
            'DVAO CITY',
            'DABAW CITY',
            'DAVAO CTY',
            'DAVAO CITY, PHILIPPINES',
            'DAVAO CITY PHILIPPINES'
        ];

        // Check if it matches any Davao City variation
        for (const variation of davaoCityVariations) {
            if (upperCity === variation || upperCity.includes(variation)) {
                return true;
            }
        }

        // Reject other Davao province cities/municipalities
        const otherDavaoCities = [
            'DIGOS', 'TAGUM', 'PANABO', 'MATI', 'SAMAL',
            'ISLAND GARDEN CITY OF SAMAL', 'IGACOS',
            'SAN PEDRO', 'SANTA CRUZ', 'MALITA', 'BANSALAN'
        ];

        for (const otherCity of otherDavaoCities) {
            if (upperCity.includes(otherCity)) {
                return false;
            }
        }

        return false;
    }

    /**
     * Show Davao City verification result with STRICT BLOCKING
     */
    function showDavaoVerificationResult(isDavaoCity, detectedCity, idType) {
        const idTypeNames = {
            'driver-license': 'Driver\'s License',
            'national-id': 'National ID',
            'phil-id': 'National ID',
            'umid': 'UMID'
        };

        const idTypeName = idTypeNames[idType] || 'ID';

        if (isDavaoCity) {
            // ✓ VALID: Davao City ID - Allow verification
            if (davaoVerification) {
                davaoVerification.classList.remove('hidden');
                davaoVerification.className = 'p-6 rounded-xl border-l-4 bg-green-50 border-green-200 slide-down visible';

                if (davaoResultIcon) davaoResultIcon.className = 'fas fa-check-circle text-2xl mt-1 text-green-500';
                if (davaoResultTitle) davaoResultTitle.textContent = `✓ Valid Davao City ${idTypeName}`;
                if (davaoResultMessage) davaoResultMessage.textContent = `Your ${idTypeName} has been verified as registered in Davao City.`;
                if (davaoStatusBadge) {
                    davaoStatusBadge.className = 'px-3 py-1 rounded-full text-sm font-semibold bg-green-500 text-white';
                    davaoStatusBadge.textContent = 'Verified';
                }
            }
            // Enable form fields for valid ID
            enableFormFields();


        } else {
            // ✗ INVALID: NON-Davao City ID - BLOCK with modal

            if (davaoVerification) {
                davaoVerification.classList.remove('hidden');
                davaoVerification.className = 'p-6 rounded-xl border-l-4 bg-red-50 border-red-200 slide-down visible';

                if (davaoResultIcon) davaoResultIcon.className = 'fas fa-times-circle text-2xl mt-1 text-red-500';
                if (davaoResultTitle) davaoResultTitle.textContent = `✗ ${idTypeName} Not From Davao City`;
                if (davaoResultMessage) {
                    const locationText = detectedCity !== "Not detected"
                        ? `Detected location: ${detectedCity}`
                        : 'Location not detected';
                    davaoResultMessage.textContent = `This service is only for Davao City residents. ${locationText}.`;
                }
                if (davaoStatusBadge) {
                    davaoStatusBadge.className = 'px-3 py-1 rounded-full text-sm font-semibold bg-red-500 text-white';
                    davaoStatusBadge.textContent = 'Blocked';
                }
            }

            // CRITICAL: Disable form and show blocking modal
            disableFormFields();
            clearUploadedFiles();

            // Show modal with detailed error
            showNonDavaoCityModal(detectedCity, idTypeName);
        }
    }

    /**
     * Show modal for non-Davao City blocking
     */
    function showNonDavaoCityModal(detectedCity, idTypeName) {
        const modal = document.getElementById('davaoVerificationModal');
        if (!modal) {
            alert(`Service Not Available\n\nThis service is only for Davao City residents.\n\nYour ID shows: ${detectedCity}\n\nIf you believe this is incorrect, please contact support.`);
            return;
        }

        // Update detected location text
        const detectedLocationElement = document.getElementById('detected-location');
        if (detectedLocationElement) {
            detectedLocationElement.textContent = detectedCity !== "Not detected"
                ? detectedCity
                : 'Location not detected';
        }

        // Show modal
        try {
            const bsModal = new bootstrap.Modal(modal);
            bsModal.show();
        } catch (e) {
            alert(`Service Not Available\n\nThis service is only for Davao City residents.\n\nYour ID shows: ${detectedCity}\n\nIf you believe this is incorrect, please contact support.`);
        }
    }

    // Disable all form fields
    function disableFormFields() {
        try {
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
        } catch (e) {
        }
    }

    // Enable all form fields
    function enableFormFields() {
        try {
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
        } catch (e) {
        }
    }

    // Clear uploaded files and reset upload areas
    function clearUploadedFiles() {
        try {
            // Clear file inputs
            if (fileFront) fileFront.value = '';
            if (fileBack) fileBack.value = '';

            // Hide preview images
            if (imagePreview) imagePreview.classList.add('hidden');
            if (imagePreviewBack) imagePreviewBack.classList.add('hidden');

            // Remove active class from upload areas
            if (uploadFront) uploadFront.classList.remove('active', 'has-image');
            if (uploadBack) uploadBack.classList.remove('active', 'has-image');

            // Reset upload content
            const uploadContent = document.querySelectorAll('.id-upload-content');
            uploadContent.forEach(content => {
                if (content) content.classList.remove('preview-mode');
            });

            // Clear any extracted data from form fields
            const fieldsToReset = ['idnumber', 'firstname', 'middlename', 'lastname', 'birthdate'];
            fieldsToReset.forEach(fieldId => {
                const field = document.getElementById(fieldId);
                if (field) field.value = '';
            });

            const genderField = document.getElementById('gender') || document.getElementById('sex');
            if (genderField) genderField.value = '';
        } catch (e) {
        }
    }

    // Helper functions
    // ═══════════════════════════════════════════════════════════════════════════════
    // ENHANCED: Clean name text - removes OCR noise, labels, and invalid characters
    // ═══════════════════════════════════════════════════════════════════════════════
    // Handles: mixed casing, extra spaces, newlines, punctuation, field labels
    function cleanName(text) {
        if (!text) return "";

        // === STEP 1: Normalize whitespace and punctuation ===
        // Handle OCR noise: multiple spaces, tabs, newlines
        text = text.replace(/[\t\n\r]+/g, ' ');  // Replace tabs/newlines with space
        text = text.replace(/\s+/g, ' ');         // Normalize multiple spaces to single space

        // === STEP 2: Remove common label patterns (case-insensitive) ===
        // These are field labels that sometimes get extracted with the name
        const labelPatterns = [
            /APELYIDO[\s\/:\-]*/gi,
            /APELYEDO[\s\/:\-]*/gi,
            /PANGALAN[\s\/:\-]*/gi,
            /NGALAN[\s\/:\-]*/gi,
            /SURNAME[\s\/:\-]*/gi,
            /LAST\s*NAME[\s\/:\-]*/gi,
            /FIRST\s*NAME[\s\/:\-]*/gi,
            /MIDDLE\s*NAME[\s\/:\-]*/gi,
            /GIVEN\s*NAME[\s\/:\-]*/gi,
            /FULL\s*NAME[\s\/:\-]*/gi,
            /UNANG\s*PANGALAN[\s\/:\-]*/gi,
            /HULING\s*PANGALAN[\s\/:\-]*/gi,
            /GITNANG\s*PANGALAN[\s\/:\-]*/gi,
            /BINANSAGANG[\s\/:\-]*/gi,
            /KUMPLETONG[\s\/:\-]*/gi,
            /BUONG[\s\/:\-]*/gi,
            /FAMILY\s*NAME[\s\/:\-]*/gi,
            /NAME[\s\/:\-]*/gi  // Generic "NAME" label
        ];

        for (const pattern of labelPatterns) {
            text = text.replace(pattern, ' ');
        }

        // === STEP 3: Clean special characters ===
        // Keep only: letters (including Ñ for Filipino names), spaces, and periods (for initials)
        // Remove: numbers, most punctuation, symbols
        text = text.replace(/[^a-zA-ZÑñ\s\.]/g, " ");

        // Handle periods: Keep them only if they appear to be initials (single letter followed by period)
        // Example: "JUAN M. DELA CRUZ" -> keep the period, "JUAN.DELA.CRUZ" -> remove periods
        text = text.replace(/([A-Z])\.(?=[A-Z])/g, '$1 ');  // "M.D" -> "M D"
        text = text.replace(/([a-z])\.(?=[A-Z])/g, '$1 ');  // "m.D" -> "m D"
        text = text.replace(/\.{2,}/g, ' ');                 // Multiple periods -> space

        // === STEP 4: Remove noise words (labels that appear as separate words) ===
        // These are common OCR artifacts and label words
        const noiseWords = [
            // English labels
            'NAME', 'SURNAME', 'FIRST', 'LAST', 'MIDDLE', 'GIVEN', 'FULL', 'COMPLETE',
            'HOLDER', 'CARD', 'FIRSTNAME', 'LASTNAME', 'MIDDLENAME', 'FULLNAME',
            'GIVENNAME', 'GIVENNAMES', 'FAMILYNAME',
            // Filipino/Tagalog labels
            'APELYIDO', 'APELYEDO', 'PANGALAN', 'NGALAN', 'UNANG', 'HULING', 'GITNANG',
            'MGA', 'BUONG', 'KUMPLETONG', 'BINANSAGANG', 'UNA', 'GITNA',
            // Bilingual format separators
            'NG', 'SA', 'ANG', 'NI',
            // Common OCR artifacts
            'OF', 'THE', 'AND', 'OR', 'IN', 'AT', 'TO', 'FOR'
        ];

        // Split into words and filter out noise
        const words = text.split(/\s+/).filter(word => {
            word = word.trim();
            if (word.length <= 1) return false;  // Remove single characters (except handled separately)

            const upperWord = word.toUpperCase().replace(/\./g, '');  // Remove periods for comparison

            // Keep word if it's not in the noise list
            return !noiseWords.includes(upperWord);
        });

        // === STEP 5: Rejoin and final cleanup ===
        text = words.join(' ');
        text = text.replace(/\s+/g, ' ').trim();  // Normalize spaces again

        return text;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // Validate if a single letter is a valid middle initial
    // ═══════════════════════════════════════════════════════════════════════════════
    function isValidMiddleInitial(letter) {
        if (!letter || letter.length !== 1) return false;
        return /^[A-Z]$/i.test(letter);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // NEW: Enhanced Name Format Detection Function
    // ═══════════════════════════════════════════════════════════════════════════════
    // Detects the format of a full name string and returns parsing hints
    // Supports: comma-separated, space-separated, and various Philippine ID formats
    function detectNameFormat(nameText) {
        if (!nameText || typeof nameText !== 'string') {
            return { format: 'unknown', confidence: 0 };
        }

        const text = nameText.trim();

        // === FORMAT 1: Comma-separated (LASTNAME, FIRSTNAME MIDDLENAME) ===
        // Example: "DELA CRUZ, JUAN MIGUEL JR."
        // Most reliable format - commonly used in official documents
        if (text.includes(',')) {
            const parts = text.split(',');
            if (parts.length === 2 && parts[0].trim().length > 0 && parts[1].trim().length > 0) {
                return {
                    format: 'comma-separated',
                    confidence: 95,
                    hint: 'Before comma = Last Name, After comma = First + Middle + Suffix'
                };
            }
        }

        // === FORMAT 2: Check for "lastname first name middle name" pattern ===
        // Look for indicators: all caps, 3+ words, potential compound surname at start
        const words = text.split(/\s+/).filter(w => w.length > 1);

        if (words.length >= 3) {
            // Check if first 2-3 words could be a compound surname
            const first2Words = words.slice(0, 2);
            const first3Words = words.slice(0, 3);

            if (isCompoundName(first3Words)) {
                return {
                    format: 'compound-surname-first',
                    confidence: 85,
                    hint: 'First 3 words = Compound Last Name, remaining = First + Middle + Suffix'
                };
            }

            if (isCompoundName(first2Words)) {
                return {
                    format: 'compound-surname-first',
                    confidence: 85,
                    hint: 'First 2 words = Compound Last Name, remaining = First + Middle + Suffix'
                };
            }

            // === FORMAT 3: Standard space-separated (FIRSTNAME MIDDLENAME LASTNAME) ===
            // Check if last 2-3 words could be compound surname
            const last2Words = words.slice(-2);
            const last3Words = words.slice(-3);

            if (isCompoundName(last3Words)) {
                return {
                    format: 'compound-surname-last',
                    confidence: 80,
                    hint: 'Last 3 words = Compound Last Name, beginning = First + Middle'
                };
            }

            if (isCompoundName(last2Words)) {
                return {
                    format: 'compound-surname-last',
                    confidence: 80,
                    hint: 'Last 2 words = Compound Last Name, beginning = First + Middle'
                };
            }

            // === FORMAT 4: Simple space-separated (FIRSTNAME MIDDLENAME LASTNAME) ===
            // Default assumption for 3+ words without compound surname
            return {
                format: 'space-separated',
                confidence: 60,
                hint: 'Likely: Beginning = First Name, Middle words = Middle Name, Last word = Last Name'
            };
        }

        // === FORMAT 5: Two words only ===
        if (words.length === 2) {
            return {
                format: 'two-words',
                confidence: 50,
                hint: 'Could be: First+Last or First+Middle (ambiguous)'
            };
        }

        // === FORMAT 6: Single word ===
        if (words.length === 1) {
            return {
                format: 'single-word',
                confidence: 30,
                hint: 'Insufficient data - only one name component found'
            };
        }

        return { format: 'unknown', confidence: 0 };
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // NEW: Enhanced Suffix Detection and Extraction
    // ═══════════════════════════════════════════════════════════════════════════════
    // Detects and extracts suffix from name text, handling various formats
    function extractSuffixFromName(nameText) {
        if (!nameText || typeof nameText !== 'string') {
            return { suffix: '', remainingText: nameText };
        }

        // Define valid suffixes (expanded list with variations)
        const suffixPatterns = [
            // Roman numerals (higher numbers first to avoid mismatches)
            { pattern: /\b(IV|V|VI|VII|VIII|IX|X)\b$/i, normalized: (m) => m.toUpperCase() },
            { pattern: /\b(III)\b$/i, normalized: () => 'III' },
            { pattern: /\b(II)\b$/i, normalized: () => 'II' },

            // Jr/Sr with variations
            { pattern: /\b(JR\.?|JUNIOR)\b$/i, normalized: () => 'JR' },
            { pattern: /\b(SR\.?|SENIOR)\b$/i, normalized: () => 'SR' },

            // Handle suffix with comma: "DELA CRUZ, JUAN JR."
            {
                pattern: /,\s*(JR\.?|SR\.?|II|III|IV|V|JUNIOR|SENIOR)\s*$/i, normalized: (m) => {
                    const clean = m.replace(/[,\s\.]/g, '').toUpperCase();
                    if (clean === 'JUNIOR') return 'JR';
                    if (clean === 'SENIOR') return 'SR';
                    return clean;
                }
            },
        ];

        let suffix = '';
        let remainingText = nameText.trim();

        // Try each pattern
        for (const { pattern, normalized } of suffixPatterns) {
            const match = remainingText.match(pattern);
            if (match) {
                suffix = normalized(match[1]);
                remainingText = remainingText.replace(pattern, '').trim();

                // Remove trailing comma if present
                remainingText = remainingText.replace(/,\s*$/, '').trim();
                break;
            }
        }

        return { suffix, remainingText };
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // NEW: Enhanced Full Name Parser (handles all Philippine name formats)
    // ═══════════════════════════════════════════════════════════════════════════════
    // This comprehensive function can parse any Philippine name format
    // Use this when you have a single field containing the full name
    // Example usage: const result = parsePhilippineFullName("DELA CRUZ, JUAN MIGUEL JR.");
    function parsePhilippineFullName(fullNameText) {

        // Initialize result
        let result = {
            lastName: '',
            firstName: '',
            middleName: '',
            suffix: ''
        };

        if (!fullNameText || typeof fullNameText !== 'string') {
            return result;
        }

        // === STEP 1: Clean the input text ===
        let cleanedText = cleanName(fullNameText);

        if (!cleanedText || cleanedText.trim().length === 0) {
            return result;
        }

        // === STEP 2: Extract suffix first (simplifies subsequent parsing) ===
        const { suffix, remainingText } = extractSuffixFromName(cleanedText);
        result.suffix = suffix;
        cleanedText = remainingText;

        if (suffix) {
        }

        // === STEP 3: Detect name format ===
        const formatInfo = detectNameFormat(cleanedText);

        // === STEP 4: Parse based on detected format ===
        const words = cleanedText.split(/\s+/).filter(w => w.length > 1);

        switch (formatInfo.format) {
            case 'comma-separated':
                // Format: "DELA CRUZ, JUAN MIGUEL"
                // Before comma = Last Name, After comma = First + Middle
                const parts = cleanedText.split(',').map(p => p.trim());
                result.lastName = parts[0];

                if (parts[1]) {
                    const givenNames = parts[1].split(/\s+/).filter(w => w.length > 1);
                    const parsed = parseFirstMiddleSuffix(givenNames);
                    result.firstName = parsed.firstName;
                    result.middleName = parsed.middleName;
                    // Suffix already extracted in step 2, but check if parse found one
                    if (!result.suffix && parsed.suffix) {
                        result.suffix = parsed.suffix;
                    }
                }

                break;

            case 'compound-surname-first':
                // Format: "DELA CRUZ JUAN MIGUEL" or "DE LOS SANTOS MARIA CLARA"
                // First 2-3 words = Last Name, Remaining = First + Middle

                // Try 3-word compound first
                if (words.length >= 4) {
                    const first3 = words.slice(0, 3);
                    if (isCompoundName(first3)) {
                        result.lastName = first3.join(' ');
                        const remaining = words.slice(3);
                        const parsed = parseFirstMiddleSuffix(remaining);
                        result.firstName = parsed.firstName;
                        result.middleName = parsed.middleName;
                        if (!result.suffix && parsed.suffix) result.suffix = parsed.suffix;

                        break;
                    }
                }

                // Try 2-word compound
                if (words.length >= 3) {
                    const first2 = words.slice(0, 2);
                    if (isCompoundName(first2)) {
                        result.lastName = first2.join(' ');
                        const remaining = words.slice(2);
                        const parsed = parseFirstMiddleSuffix(remaining);
                        result.firstName = parsed.firstName;
                        result.middleName = parsed.middleName;
                        if (!result.suffix && parsed.suffix) result.suffix = parsed.suffix;

                        break;
                    }
                }

            // Fallthrough to space-separated if compound not confirmed
            /* falls through */

            case 'compound-surname-last':
            case 'space-separated':
                // Format: "JUAN MIGUEL DELA CRUZ" or "MARIA DE LOS SANTOS"
                // First part = First Name, Middle part = Middle Name, Last part = Last Name

                // Try 3-word compound surname at end
                if (words.length >= 4) {
                    const last3 = words.slice(-3);
                    if (isCompoundName(last3)) {
                        result.lastName = last3.join(' ');
                        const remaining = words.slice(0, -3);
                        const parsed = parseFirstMiddleSuffix(remaining);
                        result.firstName = parsed.firstName;
                        result.middleName = parsed.middleName;
                        if (!result.suffix && parsed.suffix) result.suffix = parsed.suffix;

                        break;
                    }
                }

                // Try 2-word compound surname at end
                if (words.length >= 3) {
                    const last2 = words.slice(-2);
                    if (isCompoundName(last2)) {
                        result.lastName = last2.join(' ');
                        const remaining = words.slice(0, -2);
                        const parsed = parseFirstMiddleSuffix(remaining);
                        result.firstName = parsed.firstName;
                        result.middleName = parsed.middleName;
                        if (!result.suffix && parsed.suffix) result.suffix = parsed.suffix;

                        break;
                    }
                }

                // Standard 3+ words: First Middle Last
                if (words.length >= 3) {
                    result.lastName = words[words.length - 1];
                    const remaining = words.slice(0, -1);
                    const parsed = parseFirstMiddleSuffix(remaining);
                    result.firstName = parsed.firstName;
                    result.middleName = parsed.middleName;
                    if (!result.suffix && parsed.suffix) result.suffix = parsed.suffix;

                } else if (words.length === 2) {
                    // Ambiguous: could be First+Last or First+Middle
                    // Assume First+Last (more common)
                    result.firstName = words[0];
                    result.lastName = words[1];
                } else if (words.length === 1) {
                    result.firstName = words[0];
                }
                break;

            case 'two-words':
                // Ambiguous case
                result.firstName = words[0];
                result.lastName = words[1];
                break;

            case 'single-word':
                result.firstName = words[0];
                break;

            default:
                break;
        }


        return result;
    }

    function cleanText(text) {
        if (!text) return "";
        return text
            .replace(/[^a-zA-Z0-9\s\-,\.]/g, '')
            .replace(/\s+/g, ' ')
            .trim();
    }

    // FINAL VALIDATION: Ensure extracted values are actual data, not labels
    function validateExtractedValue(value, fieldName, isRequired = true) {

        // Layer 1: Check if value exists
        if (!value || value.trim().length === 0) {
            if (isRequired) {
                return "";
            }
            return "";
        }

        const trimmedValue = value.trim();

        // Layer 2: Check for "none" or "n/a" values (should be treated as empty)
        const invalidPlaceholders = ['NONE', 'N/A', 'NA', 'NULL', 'UNDEFINED', 'EMPTY'];
        if (invalidPlaceholders.includes(trimmedValue.toUpperCase())) {
            return "";
        }

        // Layer 3: Check minimum length (names should be at least 2 characters)
        if (trimmedValue.length < 2) {
            return "";
        }

        // Layer 4: Check if value is actually a label/indicator
        if (isExtractedTextALabel(trimmedValue)) {
            return "";
        }

        // Layer 5: For name fields, ensure it contains unusual words (actual data)
        if (fieldName.includes('Name') || fieldName.includes('name')) {
            if (!containsUnusualWords(trimmedValue)) {
                return "";
            }
        }

        // Layer 6: For suffix field, validate against allowed suffix values
        if (fieldName.toLowerCase().includes('suffix')) {
            const validSuffixes = ['JR', 'SR', 'II', 'III', 'IV', 'V', 'JUNIOR', 'SENIOR'];
            const upperValue = trimmedValue.toUpperCase();
            if (!validSuffixes.includes(upperValue)) {
                return "";
            }
            // Normalize suffix
            if (upperValue === 'JUNIOR') return 'JR';
            if (upperValue === 'SENIOR') return 'SR';
            return upperValue;
        }

        // Layer 7: Check for common label fragments that might have slipped through
        // IMPROVED: Use word boundary checking to avoid false positives
        const labelFragments = [
            'PANGALAN', 'APELYIDO', 'SURNAME', 'GIVEN', 'FIRST', 'LAST', 'MIDDLE',
            'NGALAN', 'UNANG', 'HULING', 'GITNANG', 'NAME', 'SUFFIX', 'HULAPI'
        ];
        const upperValue = trimmedValue.toUpperCase();
        const valueWords = upperValue.split(/\s+/);

        // Check if any complete word in the value is a label fragment
        for (const fragment of labelFragments) {
            // Exact match check (entire value is the fragment)
            if (upperValue === fragment) {
                return "";
            }

            // Check if any individual word matches the fragment
            if (valueWords.includes(fragment)) {
                return "";
            }
        }

        return trimmedValue;
    }

    // Advanced form fields update with civil status and suffix
    // Populates form fields with extracted ID data
    function updateFormFieldsAdvanced(idNumber, firstName, middleName, lastName, birthdate, sex, civilStatus, suffix, barangay = "", fullOcrText = "", idType = "", confidence = 0) {
        try {

            // Update ID type display with confidence
            if (idType && confidence > 0) {
                updateIDTypeDisplay(idType, confidence);
            }

            // Get all form fields with null checks
            const idField = document.getElementById('idnumber');
            const firstField = document.getElementById('firstname');
            const middleField = document.getElementById('middlename');
            const lastField = document.getElementById('lastname');
            const birthField = document.getElementById('birthdate');
            const genderField = document.getElementById('gender') || document.getElementById('sex');
            const civilStatusField = document.getElementById('civil-status');
            const suffixField = document.querySelector('select[name="Suffix"]');
            const barangayField = document.getElementById('Barangay');

            // AUTO-POPULATE: Always update fields on new upload (overwrite existing)

            if (idNumber && idField) {
                idField.value = idNumber;
            }
            if (firstName && firstField) {
                firstField.value = firstName.toUpperCase();
            }
            if (middleName && middleField) {
                middleField.value = middleName.toUpperCase();
            }
            if (lastName && lastField) {
                lastField.value = lastName.toUpperCase();
            }
            if (birthdate && birthField) {
                birthField.value = birthdate;
            } else {
            }
            
            // Store API-extracted gender for validation comparison
            if (sex) {
                window.apiExtractedGender = sex;
                console.log('📌 API-extracted gender stored:', sex);
            }
            
            // Gender validation: Compare user-selected gender with ID-extracted gender
            const userSelectedGender = genderField?.value || '';
            if (sex && userSelectedGender) {
                const genderValidation = window.validateGenderMatch ? window.validateGenderMatch(userSelectedGender, sex) : { isValid: true };
                if (!genderValidation.isValid) {
                    console.warn('⚠️ Gender mismatch detected:', genderValidation.message);
                    // Show warning but don't block - user may have selected correctly
                    const genderErrorDiv = document.getElementById('gender-error');
                    if (genderErrorDiv) {
                        genderErrorDiv.textContent = genderValidation.message;
                        genderErrorDiv.classList.remove('hidden');
                    }
                } else {
                    console.log('✅ Gender validation passed');
                    const genderErrorDiv = document.getElementById('gender-error');
                    if (genderErrorDiv) {
                        genderErrorDiv.classList.add('hidden');
                    }
                }
            }
            
            // Convert gender from ID Analyzer format (M/F) to form format (Male/Female)
            // NOTE: Gender field is NOT auto-populated - user must select manually
            // The API-extracted gender is only used for validation comparison
            if (sex && genderField) {
                let genderValue = sex.toUpperCase().trim();
                // Do NOT auto-populate gender - just log for reference
                console.log(`📌 ID shows gender: ${sex} (user selected: ${genderField.value})`);
            } else {
                if (!sex) {
                    console.log('⚠️ Gender not extracted from ID');
                } else if (!genderField) {
                    console.log('⚠️ Gender field not found in form');
                }
            }

            // Auto-populate barangay - prioritize extracted barangay, fallback to address extraction
            let detectedBarangay = barangay || extractBarangayFromAddress(fullOcrText);
            if (detectedBarangay && barangayField) {
                barangayField.value = detectedBarangay;
            }

            // Update civil status - ensure value matches form options
            // Form options: "Single", "Married", "Widowed", "Separated", "Divorced"
            if (civilStatus && civilStatusField) {
                // Capitalize first letter to match form options
                let civilStatusValue = civilStatus.charAt(0).toUpperCase() + civilStatus.slice(1).toLowerCase();

                // Validate against available options
                const validOptions = ['Single', 'Married', 'Widowed', 'Separated', 'Divorced'];
                if (validOptions.includes(civilStatusValue)) {
                    civilStatusField.value = civilStatusValue;
                    console.log(`✅ Civil Status populated: ${civilStatus} → ${civilStatusValue}`);
                } else {
                    console.log(`⚠️ Civil Status value "${civilStatus}" not recognized. Valid options: ${validOptions.join(', ')}`);
                }
            } else {
                if (!civilStatus) {
                    console.log('⚠️ Civil Status not extracted from ID');
                } else if (!civilStatusField) {
                    console.log('⚠️ Civil Status field not found in form');
                }
            }

            // Update suffix - set to detected suffix or "None" if empty
            if (suffixField) {
                // If suffix is empty or not detected, default to "None"
                suffixField.value = suffix && suffix.trim() !== '' ? suffix : 'None';
            }

            // Enable form submission (location and names validated)
            enableFormSubmission();

            // BUILD LIST OF AUTO-POPULATED FIELDS
            const populatedFields = [];
            if (idNumber) populatedFields.push('ID Number');
            if (firstName) populatedFields.push('First Name');
            if (middleName) populatedFields.push('Middle Name');
            if (lastName) populatedFields.push('Last Name');
            if (birthdate) populatedFields.push('Birthdate');
            if (sex) populatedFields.push('Gender');
            if (civilStatus) populatedFields.push('Civil Status');
            if (suffix) populatedFields.push('Suffix');
            if (detectedBarangay) populatedFields.push('Barangay');

            // Show clean success message with populated fields list
            if (resultBox) {
                resultBox.classList.remove('hidden');
                const fieldsExtracted = [idNumber, firstName, lastName, birthdate, sex, detectedBarangay].filter(f => f).length;

                // Log extraction confidence to console for debugging
                const extractionConfidence = (fieldsExtracted / 6) * 100;

                let successMessage = '';

                // Clean header without confidence display
                resultBox.className = "p-4 bg-green-50 border border-green-200 rounded-xl text-sm text-green-700 slide-down";
                successMessage = '<div class="flex items-start mb-3"><i class="fas fa-check-circle text-xl mr-2 mt-0.5"></i><div>';
                successMessage += '<strong class="text-base">ID Verification Successful</strong>';
                successMessage += '</div></div>';

                // Verification status
                successMessage += '<div class="mb-3 space-y-1">';
                successMessage += '<div><i class="fas fa-check-circle text-green-600 mr-1"></i><strong>Name Verified:</strong> Matches registered account</div>';
                successMessage += '<div><i class="fas fa-map-marker-alt text-green-600 mr-1"></i><strong>Location:</strong> Davao City ✓</div>';
                if (detectedBarangay) {
                    successMessage += `<div><i class="fas fa-home text-blue-600 mr-1"></i><strong>Barangay:</strong> ${detectedBarangay}</div>`;
                }
                successMessage += '</div>';

                // AUTO-POPULATED FIELDS LIST
                if (populatedFields.length > 0) {
                    successMessage += '<div class="mt-3 pt-3 border-t border-gray-300">';
                    successMessage += '<div class="font-semibold mb-2"><i class="fas fa-magic mr-1"></i>Auto-Populated Fields:</div>';
                    successMessage += '<div class="grid grid-cols-2 gap-1 text-xs">';

                    populatedFields.forEach(field => {
                        successMessage += `<div class="flex items-center">`;
                        successMessage += `<i class="fas fa-check text-green-600 mr-1 text-xs"></i>`;
                        successMessage += `<span>${field}</span>`;
                        successMessage += `</div>`;
                    });

                    successMessage += '</div>';
                    successMessage += '</div>';
                }

                // Instructions
                successMessage += '<div class="mt-3 pt-3 border-t border-gray-300 text-xs italic">';
                successMessage += '<i class="fas fa-info-circle mr-1"></i>';
                successMessage += 'Please review all auto-populated fields and complete any missing information before submitting.';
                successMessage += '</div>';

                resultBox.innerHTML = successMessage;
            }


            // ===================================================================
            // COMPLETION STATUS UPDATE - Show "Completed" with animation
            // ===================================================================

            // Set progress bar to 100%
            if (progress) {
                progress.style.width = '100%';
                progress.style.transition = 'width 0.5s ease-in-out';
            }

            // Update status to "Completed" with green styling (NO continuous animation)
            if (status) {
                // Use pulse animation temporarily, then remove it after 2 seconds
                status.innerHTML = '<i class="fas fa-check-circle mr-2 text-green-600 animate-pulse" id="status-icon"></i><strong>Verification Complete</strong> - ID information extracted and validated';
                status.className = 'text-sm text-green-600 mt-2 font-semibold animate-fadeIn';

                // Stop pulse animation after 2 seconds
                setTimeout(() => {
                    const statusIcon = document.getElementById('status-icon');
                    if (statusIcon) {
                        statusIcon.classList.remove('animate-pulse');
                    }
                }, 2000);
            }


            // Hide progress bar after a delay to show the completed state
            if (progressBar) {
                setTimeout(() => {
                    progressBar.classList.add('hidden');
                }, 2000); // 2 second delay to show the completed state
            }

            // ═══════════════════════════════════════════════════════════════════════════
            // STEP 2: FACE VERIFICATION (AFTER ID SCAN COMPLETES)
            // ═══════════════════════════════════════════════════════════════════════════
            // ID scan is now COMPLETE. Form fields have been populated with extracted data.
            // Now trigger face verification as the SECOND step of account verification.
            // This ensures ID data extraction happens BEFORE face matching.
            // ═══════════════════════════════════════════════════════════════════════════

            // REMOVED: triggerFaceComparison() call - Face verification is now handled by the API
            // The API's decision field (accept/review/reject) is the single source of truth
            
        } catch (e) {
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // REMOVED: triggerFaceComparison() function
    // ════════════════════════════════════════════════════════════════════════════
    // This function was making custom face verification decisions independent of the API.
    //
    // NEW FLOW:
    // - Face verification is handled by ID Analyzer API when selfie is included in /scan request
    // - The API returns a single "decision" field: "accept", "review", or "reject"
    // - This decision is checked at lines 3371-3442 BEFORE form population
    // - If decision = "reject" → User sees error and cannot proceed
    // - If decision = "review" → User sees warning and cannot proceed
    // - If decision = "accept" → Proceed to Davao City validation → Name validation → Form population
    //
    // The system now has a SINGLE SOURCE OF TRUTH: the API's decision field
    // ════════════════════════════════════════════════════════════════════════════

    // ════════════════════════════════════════════════════════════════════════════
    // REMOVED: disableFormForFaceVerification() function
    // ════════════════════════════════════════════════════════════════════════════
    // This function was disabling the form based on custom face verification logic.
    // The form is now controlled by the API's decision field:
    // - If API decision = "reject" or "review" → Error shown, form not populated
    // - If API decision = "accept" → Form populated and enabled for submission
    // ════════════════════════════════════════════════════════════════════════════

    /**
     * STRICT FILE VALIDATION - Only JPG/JPEG/PNG allowed
     */
    function validateFile(file) {
        if (!file) {
            showFileErrorModal('No File Selected', 'Please select a file to upload.');
            return false;
        }

        // STRICT: Only accept JPG, JPEG, and PNG
        const validMimeTypes = ['image/jpeg', 'image/jpg', 'image/png'];
        const validExtensions = ['.jpg', '.jpeg', '.png', '.JPG', '.JPEG', '.PNG'];
        const maxSize = 5 * 1024 * 1024; // 5MB

        // Get file extension
        const fileName = file.name.toLowerCase();
        const fileExtension = fileName.substring(fileName.lastIndexOf('.'));

        // VALIDATION 1: Check MIME type
        if (!validMimeTypes.includes(file.type)) {
            const detectedType = file.type || 'unknown';
            showFileFormatErrorModal(fileName, detectedType);
            return false;
        }

        // VALIDATION 2: Check file extension (double-check for security)
        const hasValidExtension = validExtensions.some(ext =>
            fileName.endsWith(ext.toLowerCase())
        );

        if (!hasValidExtension) {
            showFileFormatErrorModal(fileName, file.type);
            return false;
        }

        // VALIDATION 3: Check file size
        if (file.size === 0) {
            showFileErrorModal('Empty File', 'The selected file is empty. Please choose a valid image file.');
            return false;
        }

        if (file.size > maxSize) {
            const fileSizeMB = (file.size / (1024 * 1024)).toFixed(2);
            showFileErrorModal(
                'File Too Large',
                `File size (${fileSizeMB}MB) exceeds the 5MB limit. Please compress or resize the image and try again.`
            );
            return false;
        }

        // All validations passed
        return true;
    }

    /**
     * Show detailed modal for invalid file format
     */
    function showFileFormatErrorModal(fileName, detectedType) {
        const modal = document.getElementById('fileErrorModal');
        if (!modal) {
            alert(`Wrong File Format\n\nThe file you selected is not supported.\n\nPlease use JPG, JPEG, or PNG images only.`);
            return;
        }

        const titleElement = document.getElementById('file-error-title');
        const messageElement = document.getElementById('file-error-message');

        if (titleElement) {
            titleElement.innerHTML = '<i class="fas fa-file-image mr-2"></i>Invalid File Format';
        }

        if (messageElement) {
            messageElement.innerHTML = `
                <div class="space-y-3">
                    <div class="bg-red-50 border-l-4 border-red-500 p-3 rounded">
                        <p class="font-semibold text-red-800 mb-1">File Not Accepted</p>
                        <p class="text-sm text-red-700">
                            <strong>File:</strong> ${fileName}<br>
                            <strong>Detected type:</strong> ${detectedType || 'Unknown format'}
                        </p>
                    </div>

                    <div class="bg-blue-50 border-l-4 border-blue-500 p-3 rounded">
                        <p class="font-semibold text-blue-900 mb-2">
                            <i class="fas fa-check-circle mr-1"></i> Accepted Formats Only:
                        </p>
                        <ul class="text-sm text-blue-800 space-y-1">
                            <li><strong>✓ JPG</strong> / <strong>JPEG</strong> - Joint Photographic Experts Group format</li>
                            <li><strong>✓ PNG</strong> - Portable Network Graphics format</li>
                        </ul>
                    </div>

                    <div class="bg-gray-50 p-3 rounded">
                        <p class="text-sm text-gray-700 mb-2"><strong>Common Issues:</strong></p>
                        <ul class="text-sm text-gray-600 list-disc list-inside space-y-1">
                            <li>HEIC/HEIF files from iPhone - Please convert to JPG first</li>
                            <li>WebP, BMP, GIF, TIFF - Not supported, please convert to JPG or PNG</li>
                            <li>PDF files - Not accepted, please take a photo instead</li>
                        </ul>
                    </div>

                    <div class="text-center">
                        <p class="text-sm text-gray-600">
                            <i class="fas fa-info-circle mr-1"></i>
                            Most phone cameras save photos as JPG by default
                        </p>
                    </div>
                </div>
            `;
        }

        try {
            const bsModal = new bootstrap.Modal(modal);
            bsModal.show();
        } catch (e) {
            alert(`Wrong File Format\n\nThe file you selected is not supported.\n\nPlease use JPG, JPEG, or PNG images only.`);
        }
    }

    // Show File Error Modal
    function showFileErrorModal(title, message) {
        const modal = document.getElementById('fileErrorModal');
        if (!modal) {
            alert(`${title}: ${message}`);
            return;
        }

        const titleElement = document.getElementById('file-error-title');
        const messageElement = document.getElementById('file-error-message');

        if (titleElement) titleElement.textContent = title;
        if (messageElement) messageElement.textContent = message;

        try {
            const bsModal = new bootstrap.Modal(modal);
            bsModal.show();
        } catch (e) {
            alert(`${title}: ${message}`);
        }
    }

    // Show OCR Error Modal
    // Show verification error modal
    // ═══════════════════════════════════════════════════════════════════════════
    // MODAL SYSTEM - Color-Coded Error and Warning Modals
    // ═══════════════════════════════════════════════════════════════════════════
    // RED (Error): Verification failed, user cannot proceed
    // YELLOW (Warning): Issue detected, user should review and retry
    // ═══════════════════════════════════════════════════════════════════════════

    /**
     * Show ERROR modal (RED) - Compact 3-part design
     * Header (title) → Body (message + suggestions) → Footer (button)
     */
    function showErrorModal(title, message, suggestions = []) {
        const modal = document.getElementById('ocrErrorModal');
        const modalLabel = document.getElementById('ocrErrorModalLabel');
        const messageElement = document.getElementById('ocr-error-message');
        const modalHeader = modal?.querySelector('.modal-header');
        const modalFooter = modal?.querySelector('.modal-footer button');

        if (!modal || !messageElement) {
            alert(`${title}\n\n${message}${suggestions.length ? '\n\n' + suggestions.join('\n') : ''}`);
            return;
        }

        // Header: Red gradient with title
        if (modalHeader) {
            modalHeader.className = 'modal-header bg-gradient-to-r from-red-500 to-red-600 text-white py-3 px-5 border-b-0';
            modalHeader.innerHTML = `
                <h5 class="modal-title font-semibold text-base flex items-center gap-2" id="ocrErrorModalLabel">
                    <i class="fas fa-times-circle"></i> ${title}
                </h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
            `;
        }

        // Body: Message + suggestions
        let content = `<p class="text-gray-700 text-sm mb-3">${message}</p>`;
        if (suggestions.length > 0) {
            content += `<ul class="text-sm text-gray-600 space-y-1 pl-4">`;
            suggestions.forEach(s => content += `<li class="list-disc">${s}</li>`);
            content += `</ul>`;
        }
        messageElement.innerHTML = content;

        // Footer: Red button
        if (modalFooter) {
            modalFooter.className = 'w-full bg-red-600 hover:bg-red-700 text-white px-5 py-2.5 rounded-lg font-medium transition-colors text-sm';
            modalFooter.textContent = 'OK';
        }

        try {
            const bsModal = new bootstrap.Modal(modal);
            bsModal.show();
        } catch (e) {
            alert(`${title}\n\n${message}`);
        }
    }

    /**
     * Show WARNING modal (AMBER) - Compact 3-part design
     * Header (title) → Body (message + suggestions) → Footer (button)
     */
    function showWarningModal(title, message, suggestions = []) {
        const modal = document.getElementById('ocrErrorModal');
        const modalLabel = document.getElementById('ocrErrorModalLabel');
        const messageElement = document.getElementById('ocr-error-message');
        const modalHeader = modal?.querySelector('.modal-header');
        const modalFooter = modal?.querySelector('.modal-footer button');

        if (!modal || !messageElement) {
            alert(`${title}\n\n${message}${suggestions.length ? '\n\n' + suggestions.join('\n') : ''}`);
            return;
        }

        // Header: Amber gradient with title
        if (modalHeader) {
            modalHeader.className = 'modal-header bg-gradient-to-r from-amber-500 to-amber-600 text-white py-3 px-5 border-b-0';
            modalHeader.innerHTML = `
                <h5 class="modal-title font-semibold text-base flex items-center gap-2" id="ocrErrorModalLabel">
                    <i class="fas fa-exclamation-triangle"></i> ${title}
                </h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
            `;
        }

        // Body: Message + suggestions
        let content = `<p class="text-gray-700 text-sm mb-3">${message}</p>`;
        if (suggestions.length > 0) {
            content += `<ul class="text-sm text-gray-600 space-y-1 pl-4">`;
            suggestions.forEach(s => content += `<li class="list-disc">${s}</li>`);
            content += `</ul>`;
        }
        messageElement.innerHTML = content;

        // Footer: Amber button
        if (modalFooter) {
            modalFooter.className = 'w-full bg-amber-600 hover:bg-amber-700 text-white px-5 py-2.5 rounded-lg font-medium transition-colors text-sm';
            modalFooter.textContent = 'OK';
        }

        try {
            const bsModal = new bootstrap.Modal(modal);
            bsModal.show();
        } catch (e) {
            alert(`${title}\n\n${message}`);
        }
    }

    // Legacy function - redirects to new modal system
    function showVerificationErrorModal(message) {
        showErrorModal('Verification Error', message);
    }

    // Show Name Mismatch Blocking Modal (CRITICAL - Blocks verification)
    function showNameMismatchModal(extractedName, registeredName, details) {

        // Disable form submission and fields
        disableFormSubmission();
        disableFormFields();

        // Clear uploaded files
        clearUploadedFiles();

        // Show Bootstrap modal
        const modal = document.getElementById('nameMismatchModal');
        if (!modal) {
            alert(`Name Verification Failed\n\nThe name on your ID does not match your registered name.\n\nID Name: ${extractedName}\nRegistered Name: ${registeredName}\n\nPlease ensure you are using the correct ID that matches your registered account name.`);
            return;
        }

        // Update modal content
        const registeredElement = document.getElementById('name-mismatch-registered');
        const extractedElement = document.getElementById('name-mismatch-extracted');

        if (registeredElement) registeredElement.textContent = registeredName;
        if (extractedElement) extractedElement.textContent = extractedName;

        // Show modal (non-dismissible)
        try {
            const bsModal = new bootstrap.Modal(modal, {
                backdrop: 'static',
                keyboard: false
            });
            bsModal.show();
        } catch (e) {
            alert(`Name Verification Failed\n\nThe name on your ID does not match your registered name.\n\nID Name: ${extractedName}\nRegistered Name: ${registeredName}\n\nPlease ensure you are using the correct ID that matches your registered account name.`);
        }
    }

    // Show Rejected ID Modal - Specific error for non-approved ID types
    function showRejectedIdModal(rejectedIdType) {

        // Disable form submission and fields
        disableFormSubmission();
        disableFormFields();

        // Clear uploaded files
        clearUploadedFiles();

        const message = `
            <div class="p-6 bg-red-50 border-2 border-red-500 rounded-xl">
                <div class="flex items-start">
                    <i class="fas fa-ban text-red-500 text-3xl mr-4"></i>
                    <div>
                        <h3 class="text-lg font-bold text-red-700 mb-2">ID NOT ACCEPTED - Verification Blocked</h3>
                        <p class="text-red-600 mb-3"><strong>${rejectedIdType}</strong> is not accepted for identity verification.</p>

                        <div class="bg-white p-3 rounded-lg mb-3">
                            <p class="text-sm mb-2"><strong>ONLY THESE IDs ARE ACCEPTED:</strong></p>
                            <ul class="list-disc list-inside text-sm mt-1 text-gray-700">
                                <li><strong>Philippine National ID (PhilSys)</strong></li>
                                <li><strong>Driver's License (LTO)</strong></li>
                                <li><strong>UMID (Unified Multi-Purpose ID)</strong></li>
                            </ul>
                        </div>

                        <div class="bg-yellow-50 border border-yellow-300 p-3 rounded-lg mb-3">
                            <p class="text-sm text-yellow-800">
                                <i class="fas fa-exclamation-triangle mr-1"></i>
                                <strong>NOT ACCEPTED:</strong> PhilHealth, Senior Citizen, PWD, Postal ID, Voter's ID, TIN, Barangay ID, PRC ID, and all other IDs not listed above.
                            </p>
                        </div>

                        <p class="text-gray-700 text-sm">
                            Please upload one of the 3 accepted ID types to proceed with verification.
                        </p>
                    </div>
                </div>
            </div>
        `;

        if (resultBox) {
            resultBox.className = "p-4 bg-red-50 border-2 border-red-500 rounded-xl slide-down";
            resultBox.innerHTML = message;
            resultBox.classList.remove('hidden');
        }
    }

    // Show Invalid ID Type Blocking Modal
    function showInvalidIdTypeModal(detectedType) {
        // Disable form submission
        disableFormSubmission();

        const message = `
            <div class="p-6 bg-red-50 border-2 border-red-500 rounded-xl">
                <div class="flex items-start">
                    <i class="fas fa-times-circle text-red-500 text-3xl mr-4"></i>
                    <div>
                        <h3 class="text-lg font-bold text-red-700 mb-2">Invalid ID Type - Verification Blocked</h3>
                        <p class="text-red-600 mb-3">The ID you uploaded is not accepted for verification.</p>

                        <div class="bg-white p-3 rounded-lg mb-3">
                            <p class="text-sm mb-2"><strong>Detected ID:</strong> ${detectedType || 'Unknown/Unrecognized ID'}</p>
                            <p class="text-sm"><strong>Accepted IDs:</strong></p>
                            <ul class="list-disc list-inside text-sm mt-1 text-gray-700">
                                <li>Philippine National ID (PhilSys)</li>
                                <li>Driver's License</li>
                                <li>UMID</li>
                            </ul>
                        </div>

                        <p class="text-gray-700 text-sm">
                            Please upload one of the accepted ID types to proceed with verification.
                        </p>
                    </div>
                </div>
            </div>
        `;

        if (resultBox) {
            resultBox.className = "p-4 bg-red-50 border-2 border-red-500 rounded-xl slide-down";
            resultBox.innerHTML = message;
            resultBox.classList.remove('hidden');
        }

        alert(`ID Not Accepted\n\nOnly these IDs can be used for verification:\n\n✓ Philippine National ID (PhilSys)\n✓ Driver's License (LTO)\n✓ UMID\n\nPlease upload one of the accepted ID types.`);

        clearUploadedFiles();
    }

    // Disable form submission
    function disableFormSubmission() {
        const submitButton = document.querySelector('button[type="submit"]');
        if (submitButton) {
            submitButton.disabled = true;
            submitButton.classList.add('opacity-50', 'cursor-not-allowed');
            submitButton.title = 'Cannot submit - validation failed';
        }

        // Disable all form fields except name fields (so user can correct their registered name)
        formFields.forEach(fieldId => {
            if (!['firstname', 'middlename', 'lastname'].includes(fieldId)) {
                const field = document.getElementById(fieldId);
                if (field) {
                    field.disabled = true;
                    field.classList.add('opacity-50');
                }
            }
        });
    }

    // Enable form submission
    function enableFormSubmission() {
        const submitButton = document.querySelector('button[type="submit"]');
        if (submitButton) {
            submitButton.disabled = false;
            submitButton.classList.remove('opacity-50', 'cursor-not-allowed');
            submitButton.title = '';
        }

        // Re-enable all form fields
        formFields.forEach(fieldId => {
            const field = document.getElementById(fieldId);
            if (field) {
                field.disabled = false;
                field.classList.remove('opacity-50');
            }
        });
    }

    // Form validation with error handling
    const registrationForm = document.getElementById('registrationForm');
    if (registrationForm && terms) {
        registrationForm.addEventListener('submit', function (e) {
            try {
                if (!terms.checked) {
                    e.preventDefault();
                    alert('You must agree to the terms and conditions.');
                    return false;
                }
            } catch (error) {
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ✅ FIX: Re-enable submit button when users manually fill the form
        // ═══════════════════════════════════════════════════════════════
        // If users choose to fill the form manually (without OCR), ensure
        // the submit button remains enabled as they type in the fields
        registrationForm.addEventListener('input', function () {
            const submitBtn = document.querySelector('button[type="submit"]');
            if (submitBtn && submitBtn.disabled) {
                // Check if all required fields are filled
                const requiredFields = ['IDnumber', 'lastname', 'firstname', 'Barangay'];
                const allFilled = requiredFields.every(fieldId => {
                    const field = document.getElementById(fieldId);
                    return field && field.value.trim() !== '';
                });

                // If all required fields are filled, enable the submit button
                if (allFilled) {
                    submitBtn.disabled = false;
                    submitBtn.classList.remove('opacity-50', 'cursor-not-allowed');
                    submitBtn.title = '';
                }
            }
        });
    }

    // Add error listener for debugging
    window.addEventListener('error', function (e) {
    });

    // Export debug functions to window for testing
    window.OCRDebug = {
        getLastOCRText: () => lastOCRText,
        getLastExtractedData: () => lastExtractedData,
        enableDebugMode: () => {
            return 'Set DEBUG_MODE = true in the code and refresh for full debug logs.';
        },
        testExtraction: () => {
            return 'Debug report printed above';
        },
        apiInfo: () => {
            return 'API info printed above';
        },
        help: () => {
            return 'Commands listed above';
        }
    };
});