// ============================================================================
// ACCOUNT VERIFICATION - OCR DATA EXTRACTION WITH KEY-VALUE PAIRING
// ============================================================================
//
// This script performs OCR (Optical Character Recognition) on Philippine ID cards
// and extracts data using a sophisticated KEY-VALUE PAIRING system.
//
// KEY-VALUE PAIRING ARCHITECTURE:
// -------------------------------
// 1. FIELD_LABELS constant (lines ~183-319):
//    - Defines all possible label variations for each field (English + Filipino/Tagalog)
//    - Example: firstName: ['FIRST NAME', 'FIRSTNAME', 'GIVEN NAME', 'PANGALAN', 'UNANG PANGALAN']
//    - Example: gender: ['SEX', 'GENDER', 'KASARIAN', 'SEX/KASARIAN']
//    - Comprehensive coverage for Philippine National ID, Driver's License, SSS ID, UMID
//
// 2. extractFieldValue() function (lines ~321-420):
//    - Searches OCR text for labels and extracts associated values
//    - Removes bilingual label fragments (e.g., "KASARIAN/SEX: M" → extracts "M")
//    - Handles separators: colons (:), slashes (/), dashes (-)
//    - Returns: { value: "JOHN DOE", method: "same-line" }
//
// 3. Individual extraction functions:
//    - extractFirstName(), extractGender(), extractLastName(), etc.
//    - Each uses extractFieldValue() for consistent label-based extraction
//
// 4. ID-specific parsers:
//    - parseDriverLicenseFront(), parsePhilSysFront(), parseSSSFront(), parseUMIDFront()
//    - Combine individual extraction functions into complete ID parsing
//
// 5. Output formatting:
//    - formatExtractedDataWithLabels() → ["First Name: JOHN", "Gender: Male"]
//    - getExtractedDataLabeled() → { 'First Name': 'JOHN', 'Gender': 'Male' }
//    - logExtractedData() → Displays formatted key-value pairs in console
//
// EXAMPLE FLOW:
// -------------
// OCR Text: "FIRST NAME: JOHN DOE\nSEX: M"
//   ↓
// extractFieldValue(lines, 'firstName') → { value: "JOHN DOE", method: "same-line" }
// extractGender(lines) → "Male"
//   ↓
// extractedData = { firstName: "JOHN DOE", sex: "Male", ... }
//   ↓
// logExtractedData() outputs:
//   "First Name: JOHN DOE"
//   "Gender: Male"
//
// This ensures proper matching of labels/indicators with extracted data for
// easy parsing, validation, and field population.
// ============================================================================

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

    // Check if critical elements exist
    if (!documentType || !uploadFront || !uploadBack || !fileFront || !fileBack) {
        console.error('Critical form elements are missing. Please check the HTML structure.');
        return;
    }

    // Form fields to disable when license is invalid
    const formFields = [
        'idnumber', 'phonenumber', 'lastname', 'firstname', 'middlename',
        'sex', 'birthdate', 'BlkLotStreet', 'SubVill', 'Barangay', 'District',
        'SecurityQuestions', 'Securityanswer'
    ];

    // Track if OCR is enabled - start with true to allow auto-detection
    let ocrEnabled = true;

    // Debug mode for testing and calibration
    const DEBUG_MODE = false; // Set to true to see detailed extraction logs

    // Store last OCR result for debugging
    let lastOCRText = "";
    let lastExtractedData = {};

    // Rate limiting for API calls
    let lastAPICallTime = 0;
    const MIN_TIME_BETWEEN_CALLS = 1000; // 1 second minimum between calls

    // Get registered user's name from window object (passed from ViewBag)
    // This comes from RegisterAcc table columns: FirstName, MiddleName, LastName, Suffix
    let registeredFirstName = window.registeredUserName?.firstName || "";
    let registeredMiddleName = window.registeredUserName?.middleName || "";
    let registeredLastName = window.registeredUserName?.lastName || "";
    let registeredSuffix = window.registeredUserName?.suffix || "";

    console.log('===========================================');
    console.log('=== REGISTERED USER NAME (from RegisterAcc table) ===');
    console.log('FirstName (column):', registeredFirstName);
    console.log('MiddleName (column):', registeredMiddleName);
    console.log('LastName (column):', registeredLastName);
    console.log('Suffix (column):', registeredSuffix);
    console.log('Full Name:', `${registeredLastName}, ${registeredFirstName} ${registeredMiddleName} ${registeredSuffix}`.trim());
    console.log('===========================================');

    console.log('\n===========================================');
    console.log('=== OCR LABEL RECOGNITION ===');
    console.log('Filipino/Tagalog Label Support: ENABLED');
    console.log('Bilingual Format Handling: ENABLED');
    console.log('Supported ID Types:');
    console.log('  • Philippine National ID (PhilSys)');
    console.log('  • Driver\'s License');
    console.log('  • SSS ID');
    console.log('  • UMID');
    console.log('Label Examples:');
    console.log('  • PANGALAN/GIVEN NAME → extracts name only');
    console.log('  • KASARIAN/SEX: M → extracts "M" (normalized to "Male")');
    console.log('  • PETSA NG KAPANGANAKAN → extracts birthdate');
    console.log('===========================================\n');

    // List of known Filipino compound/double middle names and last names
    const COMPOUND_NAMES = [
        // Common compound middle names (with "De/Del/Dela/Delos/De Los")
        'DE LA CRUZ', 'DELA CRUZ', 'DE LOS REYES', 'DELOS REYES',
        'DE LA ROSA', 'DELA ROSA', 'DE LA TORRE', 'DELA TORRE',
        'DE LOS SANTOS', 'DELOS SANTOS', 'DE LOS ANGELES', 'DELOS ANGELES',
        'DE LEON', 'DELEON', 'DE GUZMAN', 'DEGUZMAN',
        'DE CASTRO', 'DECASTRO', 'DE JESUS', 'DEJESUS',
        'DEL ROSARIO', 'DELROSARIO', 'DEL MUNDO', 'DELMUNDO',
        'DEL CARMEN', 'DELCARMEN', 'DEL PILAR', 'DELPILAR',

        // Santa/San compound names
        'SANTA MARIA', 'SANTAMARIA', 'SANTA CRUZ', 'SANTACRUZ',
        'SANTA ROSA', 'SANTAROSA', 'SANTA ANA', 'SANTAANA',
        'SAN JOSE', 'SANJOSE', 'SAN JUAN', 'SANJUAN',
        'SAN PEDRO', 'SANPEDRO', 'SAN DIEGO', 'SANDIEGO',

        // Other compound names
        'VILLA NUEVA', 'VILLANUEVA', 'VILLA REAL', 'VILLAREAL',
        'VILLA MAYOR', 'VILLAMAYOR', 'VILLA VERDE', 'VILLAVERDE',
        'MONTE MAYOR', 'MONTEMAYOR', 'MONTE REAL', 'MONTEREAL',
        'BUEN CAMINO', 'BUENCAMINO', 'BUEN DIA', 'BUENDIA',

        // De/Del variations (standalone)
        'DE', 'DEL', 'DELA', 'DELOS', 'DE LOS', 'DE LA'
    ];

    // Check if a word sequence is a known compound name
    function isCompoundName(words) {
        if (!words || words.length === 0) return false;

        // Normalize to uppercase and join
        const normalized = words.map(w => w.toUpperCase().trim()).join(' ');

        // Check exact match
        if (COMPOUND_NAMES.includes(normalized)) {
            return true;
        }

        // Check if starts with compound name prefix
        for (const compound of COMPOUND_NAMES) {
            if (normalized === compound || normalized.startsWith(compound + ' ')) {
                return true;
            }
        }

        return false;
    }

    // ========================================================================
    // FIELD LABEL MAPPINGS - Core of Key-Value Pairing System
    // ========================================================================
    // This constant defines all possible labels/indicators that may appear in ID cards
    // for each data field. Used by extractFieldValue() to find and extract data.
    // Supports English, Filipino, and common abbreviations for maximum compatibility.
    // ========================================================================
    const FIELD_LABELS = {
        lastName: [
            // English variations
            'LAST NAME', 'LASTNAME', 'SURNAME', 'FAMILY NAME', 'LAST',
            // Filipino/Tagalog variations
            'APELYIDO', 'APELYEDO', 'HULING PANGALAN', 'HULING NGALAN',
            // Common on Philippine IDs
            'PANGALAN (APELYIDO)', 'APELYIDO/SURNAME'
        ],
        firstName: [
            // English variations
            'FIRST NAME', 'FIRSTNAME', 'GIVEN NAME', 'GIVEN NAMES', 'GIVENNAME', 'FIRST',
            // Filipino/Tagalog variations
            'PANGALAN', 'UNANG PANGALAN', 'MGA PANGALAN', 'PANGALAN (UNA)',
            // Common on Philippine National ID
            'PANGALAN/GIVEN NAME', 'PANGALAN/NAME', 'GIVEN', 'UNANG NGALAN',
            // SSS/UMID variations
            'BINANSAGANG PANGALAN'
        ],
        middleName: [
            // English variations
            'MIDDLE NAME', 'MIDDLENAME', 'MIDDLE INITIAL', 'MIDDLE', 'M.I.',
            // Filipino/Tagalog variations
            'GITNANG PANGALAN', 'GITNANG APELYIDO', 'GITNANG NGALAN',
            // Common on Philippine IDs
            'PANGALAN (GITNA)', 'PANGALAN/MIDDLE NAME',
            // National ID specific (PhilSys)
            'GITNANG', 'MID NAME', 'M NAME',
            // Variations with slash
            'MIDDLE/GITNANG', 'GITNANG/MIDDLE'
        ],
        fullName: [
            // English variations
            'FULL NAME', 'COMPLETE NAME', 'NAME', 'HOLDER NAME',
            // Filipino/Tagalog variations
            'BUONG PANGALAN', 'KUMPLETONG PANGALAN', 'PANGALAN NG MAY-ARI',
            // Common on Philippine IDs
            'PANGALAN/NAME', 'PANGALAN NG CARD HOLDER'
        ],
        birthdate: [
            // English variations
            'DATE OF BIRTH', 'BIRTH DATE', 'BIRTHDATE', 'DOB', 'BIRTHDAY', 'BIRTH',
            'DATE OF BIRTH/PETSA', 'BIRTH (MM/DD/YYYY)',
            // Filipino/Tagalog variations
            'PETSA NG KAPANGANAKAN', 'KAPANGANAKAN', 'PETSA NG PAGSILANG',
            'ARAW NG KAPANGANAKAN', 'IPINANGANAK',
            // Common on Philippine National ID
            'KAPANGANAKAN/DATE OF BIRTH', 'PETSA/DATE'
        ],
        gender: [
            // English variations
            'SEX', 'GENDER', 'SEX/KASARIAN',
            // Filipino/Tagalog variations
            'KASARIAN', 'KASARIAN/SEX',
            // Common on Philippine IDs
            'KASARIAN (SEX)', 'SEX (M/F)', 'KASARIAN/GENDER'
        ],
        address: [
            // English variations
            'ADDRESS', 'RESIDENCE', 'RESIDENTIAL ADDRESS', 'HOME ADDRESS',
            'PRESENT ADDRESS', 'PERMANENT ADDRESS', 'STREET ADDRESS',
            // Filipino/Tagalog variations
            'TIRAHAN', 'LUGAR', 'LUGAR NG TIRAHAN', 'TIRAHAN/ADDRESS',
            'TAHANAN', 'ADDRESS/TIRAHAN',
            // Common on Philippine National ID
            'KASALUKUYANG TIRAHAN', 'PERMANENTENG TIRAHAN'
        ],
        city: [
            // English variations
            'CITY', 'CITY/MUNICIPALITY', 'MUNICIPALITY', 'CITY/MUNIC',
            // Filipino/Tagalog variations
            'LUNGSOD', 'BAYAN', 'MUNISIPALIDAD', 'LUNGSOD/BAYAN',
            // Common on Philippine IDs
            'LUNGSOD/CITY', 'CITY OR MUNICIPALITY', 'LUNSOD/MUNISIPALIDAD'
        ],
        idNumber: [
            // English variations
            'ID NUMBER', 'ID NO', 'ID #', 'NUMBER', 'NO.', 'ID',
            // Driver's License specific
            'LICENSE NO', 'LICENSE NUMBER', 'DL NO', 'LICENSE #',
            'LISENSYA BLGD', 'DRIVER LICENSE NO',
            // SSS specific
            'SSS NUMBER', 'SSS NO', 'SS NO', 'SOCIAL SECURITY',
            'SSS #', 'SSS NUM',
            // PhilSys/National ID specific
            'PHILSYS NUMBER', 'PSN', 'PHIL-SYS NO', 'PHILSYS NO',
            'PCN', 'NATIONAL ID NUMBER', 'NATIONAL ID NO',
            // UMID specific
            'UMID NUMBER', 'UMID NO', 'CRN', 'CARD REFERENCE NUMBER',
            'UMID CRN'
        ],
        nationality: [
            // English variations
            'NATIONALITY', 'CITIZEN', 'CITIZENSHIP',
            // Filipino/Tagalog variations
            'NASYONALIDAD', 'PAGKAMAMAMAYAN', 'BANSA',
            // Common on Philippine IDs
            'NASYONALIDAD/NATIONALITY', 'CITIZENSHIP/NASYONALIDAD'
        ],
        civilStatus: [
            // English variations
            'CIVIL STATUS', 'MARITAL STATUS', 'STATUS', 'MAR STATUS',
            // Filipino/Tagalog variations
            'KATAYUANG SIBIL', 'LAGAY NG PAG-AASAWA', 'KATAYUAN',
            'ESTADO SIBIL',
            // Common on Philippine National ID
            'KATAYUANG SIBIL/CIVIL STATUS', 'CIVIL STATUS/KATAYUAN'
        ],
        suffix: [
            // English variations
            'SUFFIX', 'NAME SUFFIX', 'EXT', 'EXTENSION',
            // Filipino/Tagalog variations
            'HULAPI', 'KARUGTONG', 'SUFFIX/HULAPI'
        ],
        bloodType: [
            // English variations
            'BLOOD TYPE', 'BLOOD', 'BLOOD GROUP', 'BT',
            // Filipino/Tagalog variations
            'URI NG DUGO', 'DUGO', 'BLOOD TYPE/URI NG DUGO'
        ],
        height: [
            // English variations
            'HEIGHT', 'HT', 'HGT',
            // Filipino/Tagalog variations
            'TAAS', 'TANGKAD', 'HEIGHT/TAAS'
        ],
        weight: [
            // English variations
            'WEIGHT', 'WT', 'WGT',
            // Filipino/Tagalog variations
            'TIMBANG', 'BIGAT', 'WEIGHT/TIMBANG'
        ],
        placeOfBirth: [
            // English variations
            'PLACE OF BIRTH', 'BIRTH PLACE', 'POB', 'BIRTHPLACE',
            // Filipino/Tagalog variations
            'LUGAR NG KAPANGANAKAN', 'PINANGANAK SA', 'LUGAR NG PAGSILANG',
            // Common on Philippine IDs
            'LUGAR NG KAPANGANAKAN/PLACE OF BIRTH', 'PLACE OF BIRTH/LUGAR'
        ]
    };

    // Helper function to check if text contains "unusual words" (actual data, not labels)
    // Philippine IDs have labels on top, values below - values are unusual/unique words
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

        // Check if text is mostly unusual words (actual data)
        const words = upperText.split(/\s+/).filter(w => w.length > 1);

        // If no words, reject
        if (words.length === 0) return false;

        // IMPROVED MATCHING: Use exact word matching, not substring matching
        // This prevents false positives like "EMENDO" being rejected
        const labelWordsCount = words.filter(word => {
            // Exact match check
            if (commonLabelWords.includes(word)) {
                return true;
            }

            // For longer words (6+ chars), check if they're mostly a label word
            // This catches variations like "NAMES" or "SURNAME:"
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
            console.log(`    ✓ Single word not matching any label: "${text}"`);
            return true;
        }

        // If less than 30% of words are label-related, it's likely actual data
        const unusualWordPercentage = ((words.length - labelWordsCount) / words.length) * 100;

        if (unusualWordPercentage >= 70) {
            console.log(`    ✓ Contains unusual words (${Math.round(unusualWordPercentage)}% non-label): "${text}"`);
            return true;
        }

        console.log(`    ✗ Too many label words (${Math.round(100 - unusualWordPercentage)}% labels): "${text}"`);
        return false;
    }

    // Helper function to check if extracted text is actually a label (should be rejected)
    // This prevents labels like "PANGALAN", "APELYIDO" from being returned as name values
    function isExtractedTextALabel(text) {
        if (!text || text.length < 2) return true;

        const allLabels = Object.values(FIELD_LABELS).flat();
        const upperText = text.toUpperCase().trim();

        // Check if the entire text is a label (exact match)
        if (allLabels.some(label => upperText === label)) {
            console.log(`    ⚠ Rejected: "${text}" is an exact label match`);
            return true;
        }

        // Check if text is mostly labels (more than 60% is label words)
        // IMPROVED: Use exact matching to prevent false positives
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
            console.log(`    ⚠ Rejected: "${text}" is ${Math.round(labelWords.length/words.length*100)}% label words`);
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
            console.log(`    ⚠ Rejected: "${text}" is a label-only term`);
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
            console.log(`    ⚠ Rejected: "${text}" matches label-only pattern`);
            return true;
        }

        // Additional check: if ALL words in the text are exact label-only terms
        // This is more conservative than substring replacement to avoid false positives
        const allWordsAreLabels = words.every(word => labelOnlyTerms.includes(word));

        if (allWordsAreLabels && words.length > 0) {
            console.log(`    ⚠ Rejected: "${text}" - all words are label-only terms`);
            return true;
        }

        // Passed all checks - this is likely actual data, not a label
        return false;
    }

    // Validate extracted name data to ensure no labels are present
    // Returns { valid: boolean, issues: string[] }
    function validateExtractedNames(extractedData) {
        const issues = [];

        console.log('\n========== VALIDATING EXTRACTED NAME DATA ==========');

        // Check firstName
        if (!extractedData.firstName || extractedData.firstName.trim() === '') {
            issues.push('First Name is empty');
        } else if (isExtractedTextALabel(extractedData.firstName)) {
            issues.push(`First Name contains label: "${extractedData.firstName}"`);
        } else if (extractedData.firstName.length < 2) {
            issues.push(`First Name too short: "${extractedData.firstName}"`);
        } else {
            console.log(`✓ First Name valid: "${extractedData.firstName}"`);
        }

        // Check lastName
        if (!extractedData.lastName || extractedData.lastName.trim() === '') {
            issues.push('Last Name is empty');
        } else if (isExtractedTextALabel(extractedData.lastName)) {
            issues.push(`Last Name contains label: "${extractedData.lastName}"`);
        } else if (extractedData.lastName.length < 2) {
            issues.push(`Last Name too short: "${extractedData.lastName}"`);
        } else {
            console.log(`✓ Last Name valid: "${extractedData.lastName}"`);
        }

        // Check middleName (can be empty in some cases, but if present must be valid)
        if (extractedData.middleName && extractedData.middleName.trim() !== '') {
            if (isExtractedTextALabel(extractedData.middleName)) {
                issues.push(`Middle Name contains label: "${extractedData.middleName}"`);
            } else if (extractedData.middleName.length < 2) {
                issues.push(`Middle Name too short: "${extractedData.middleName}"`);
            } else {
                console.log(`✓ Middle Name valid: "${extractedData.middleName}"`);
            }
        } else {
            console.log(`⚠ Middle Name empty (this may be acceptable)`);
        }

        // Check suffix (optional, so only validate if present)
        if (extractedData.suffix && extractedData.suffix.trim() !== '') {
            if (isExtractedTextALabel(extractedData.suffix)) {
                issues.push(`Suffix contains label: "${extractedData.suffix}"`);
            } else {
                console.log(`✓ Suffix valid: "${extractedData.suffix}"`);
            }
        } else {
            console.log(`✓ Suffix empty (acceptable)`);
        }

        const valid = issues.length === 0;

        if (valid) {
            console.log('✓ ALL NAME FIELDS VALIDATED SUCCESSFULLY');
        } else {
            console.error('✗ NAME VALIDATION FAILED:');
            issues.forEach(issue => console.error(`  - ${issue}`));
        }

        console.log('====================================================\n');

        return { valid, issues };
    }

    // Smart label-value extraction function - Core of Key-Value Pairing
    // PHILIPPINE ID FORMAT: Labels are on TOP, values are BELOW
    // Extraction priority: 1) Next-line, 2) Following-line, 3) Same-line (last resort)
    // Returns: { value: string, method: string } or null if not found
    function extractFieldValue(lines, fieldType) {
        const labels = FIELD_LABELS[fieldType];
        if (!labels) {
            console.log(`✗ No labels defined for fieldType: ${fieldType}`);
            return null;
        }

        console.log(`\n--- Searching for ${fieldType} ---`);
        console.log(`Labels to search:`, labels);
        console.log(`Total lines available: ${lines.length}`);
        console.log(`Strategy: Philippine ID format (label on top, value below)`);

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i].trim();
            const upperLine = line.toUpperCase();

            // Check if line contains any of the field labels
            for (const label of labels) {
                if (upperLine.includes(label)) {
                    console.log(`✓ Found label "${label}" at line ${i + 1}: "${line}"`);
                    console.log(`  Philippine ID Format: Label is on TOP, value should be BELOW`);

                    // ===================================================================
                    // PRIORITY METHOD 1: Value on NEXT line (Philippine ID standard format)
                    // Most Philippine IDs have: Label on line X, Value on line X+1
                    // ===================================================================
                    if (i + 1 < lines.length) {
                        const nextLine = lines[i + 1].trim();
                        console.log(`  [Method 1 - PRIORITY] Checking next line ${i + 2}: "${nextLine}"`);

                        // Validate: Not a label, contains unusual words (actual data)
                        const isNextLineLabel = isExtractedTextALabel(nextLine);
                        const hasUnusualWords = containsUnusualWords(nextLine);

                        if (!isNextLineLabel && nextLine && nextLine.length > 1) {
                            console.log(`  ✓✓✓ VALUE FOUND on next line (Philippine ID format): "${nextLine}"`);
                            return { value: nextLine, method: 'next-line' };
                        } else {
                            console.log(`  ✗ Next line invalid (isLabel: ${isNextLineLabel}, hasData: ${hasUnusualWords})`);
                        }
                    } else {
                        console.log(`  [Method 1] No next line available`);
                    }

                    // ===================================================================
                    // METHOD 2: Value on FOLLOWING line (skip empty lines)
                    // Some IDs have empty line between label and value
                    // ===================================================================
                    console.log(`  [Method 2] Trying following lines (2-3 lines ahead)...`);
                    for (let j = i + 2; j < Math.min(i + 4, lines.length); j++) {
                        const followingLine = lines[j].trim();
                        console.log(`    Checking line ${j + 1}: "${followingLine}"`);

                        if (followingLine && followingLine.length > 1) {
                            const isLabel = isExtractedTextALabel(followingLine);
                            const hasUnusualWords = containsUnusualWords(followingLine);

                            if (!isLabel && hasUnusualWords) {
                                console.log(`    ✓✓ VALUE FOUND ${j - i} lines after: "${followingLine}"`);
                                return { value: followingLine, method: 'following-line' };
                            } else {
                                console.log(`    ✗ Line invalid (isLabel: ${isLabel}, hasData: ${hasUnusualWords})`);
                            }
                        }
                    }

                    // ===================================================================
                    // METHOD 3: Value on SAME line (LAST RESORT - uncommon for Philippine IDs)
                    // Only use if label and value are on same line (e.g., "NAME: JUAN")
                    // ===================================================================
                    console.log(`  [Method 3 - LAST RESORT] Checking same line...`);
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
                        console.log(`  ✓ VALUE FOUND on same line (unusual): "${valueOnSameLine}"`);
                        return { value: valueOnSameLine, method: 'same-line' };
                    } else {
                        console.log(`  ✗ Same line invalid (isLabel: ${isStillALabel}, hasData: ${hasUnusualWords})`);
                    }

                    console.log(`  ✗✗✗ NO VALID VALUE FOUND for label "${label}"`);
                    break; // Try next label variant
                }
            }
        }

        console.log(`✗ No ${fieldType} found - none of the labels matched`);
        console.log(`--- End search for ${fieldType} ---\n`);
        return null;
    }

    // Independent extraction function for First Name (works like extractFieldValue)
    // Returns object with firstName, middleName, and suffix since they're often together
    function extractFirstName(lines) {
        console.log('Extracting First Name independently...');

        // Try using extractFieldValue first
        const result = extractFieldValue(lines, 'firstName');
        if (result && result.value) {
            const rawValue = cleanName(result.value);
            console.log(`✓ Found raw given names via ${result.method}:`, rawValue);

            // Additional validation: reject if still contains labels
            if (isExtractedTextALabel(rawValue)) {
                console.log('✗ Extracted text is actually a label, rejecting:', rawValue);
                return { firstName: "", middleName: "", suffix: "" };
            }

            // Parse to separate first name from middle name and suffix
            const words = rawValue.split(/\s+/).filter(w => w.length > 0);
            if (words.length > 0) {
                const parsed = parseFirstMiddleSuffix(words);
                console.log('  Extracted First Name:', parsed.firstName);
                console.log('  Extracted Middle Name:', parsed.middleName);
                if (parsed.suffix) console.log('  Extracted Suffix:', parsed.suffix);

                // Final validation: ensure we have actual name data
                if (!parsed.firstName || parsed.firstName.length < 2) {
                    console.log('✗ First name too short or invalid');
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

        console.log('✗ First Name extraction failed');
        return { firstName: "", middleName: "", suffix: "" };
    }

    // Independent extraction function for Middle Name (works like extractFieldValue)
    function extractMiddleName(lines, alreadyParsedFromFirstName = "") {
        console.log('Extracting Middle Name independently...');

        // If already extracted from first name field, use that
        if (alreadyParsedFromFirstName) {
            console.log('✓ Using middle name from first name field:', alreadyParsedFromFirstName);
            return alreadyParsedFromFirstName;
        }

        // Try using extractFieldValue for separate middle name field
        const result = extractFieldValue(lines, 'middleName');
        if (result && result.value) {
            let value = cleanName(result.value);
            console.log(`✓ Found raw middle name via ${result.method}:`, value);

            // Additional validation: reject if still contains labels
            if (isExtractedTextALabel(value)) {
                console.log('✗ Extracted middle name is actually a label, rejecting:', value);
                return "";
            }

            // Validate length
            if (value.length < 2) {
                console.log('✗ Middle name too short:', value);
                return "";
            }

            console.log(`✓✓ Valid middle name extracted: "${value}"`);
            return value;
        }

        console.log('✗ Middle Name extraction failed - no label found or no value');
        return "";
    }

    // Independent extraction function for Suffix (works like extractFieldValue)
    function extractSuffix(lines, alreadyParsedFromFirstName = "") {
        console.log('Extracting Suffix independently...');

        // If already extracted from first name field, use that
        if (alreadyParsedFromFirstName) {
            console.log('✓ Using suffix from first name field:', alreadyParsedFromFirstName);
            return alreadyParsedFromFirstName;
        }

        // Try using extractFieldValue for labeled suffix field FIRST
        const result = extractFieldValue(lines, 'suffix');
        if (result && result.value) {
            let value = cleanName(result.value);
            console.log(`✓ Found suffix via label extraction (${result.method}):`, value);

            // Validate it's actually a suffix (not a label or other text)
            const validSuffixes = ['JR', 'SR', 'II', 'III', 'IV', 'V', 'JUNIOR', 'SENIOR'];
            const upperValue = value.toUpperCase().trim();

            if (validSuffixes.includes(upperValue)) {
                // Normalize
                if (upperValue === 'JUNIOR') value = 'JR';
                if (upperValue === 'SENIOR') value = 'SR';
                console.log('✓✓ Valid suffix:', value);
                return value;
            } else {
                console.log('✗ Extracted value is not a valid suffix:', value);
            }
        }

        // Fallback: Try finding suffix patterns in lines (Jr, Sr, II, III, IV, V)
        // BUT with VERY STRICT context validation to prevent false positives
        console.log('Label extraction failed, trying pattern search with STRICT validation...');
        const suffixPatterns = /\b(JR|SR|II|III|IV|V|JUNIOR|SENIOR)\b/i;

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const match = line.match(suffixPatterns);

            if (match) {
                let suffix = match[1].toUpperCase();
                console.log(`Found potential suffix "${suffix}" in line ${i + 1}: "${line}"`);

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
                    console.log('✗ Suffix found in label/instruction context, rejecting');
                    continue;
                }

                // REJECTION 2: Reject if it's a Driver's License restriction code
                // Driver's License restrictions are numbered I, II, III, etc.
                const isRestrictionCode = /RESTRICTION|RESTRICTIONS|COND|CONDITIONS|CODE/i.test(upperLine);
                if (isRestrictionCode) {
                    console.log('✗ Suffix found in restriction code context (Driver\'s License), rejecting');
                    continue;
                }

                // REJECTION 3: Reject if line contains numbers (likely ID numbers, dates, or codes)
                const hasNumbers = /\d/.test(line);
                if (hasNumbers && suffix.match(/^(II|III|IV|V)$/)) {
                    console.log('✗ Suffix found in line with numbers (likely code/ID), rejecting');
                    continue;
                }

                // REJECTION 4: Check if line contains actual name-like words (3+ letter words)
                const nameWords = line.match(/\b[A-Z][a-z]{2,}\b/g) || [];
                const hasMultipleNameWords = nameWords.length >= 2;

                if (!hasMultipleNameWords) {
                    console.log('✗ Suffix not found near actual names (needs at least 2 name-like words), rejecting');
                    continue;
                }

                // REJECTION 5: For "III" specifically, require very strong name context
                if (suffix === 'III') {
                    // Must have at least 3 name-like words (e.g., "DELA CRUZ JOSE III")
                    if (nameWords.length < 3) {
                        console.log('✗ "III" requires at least 3 name-like words for context, rejecting');
                        continue;
                    }

                    // Must be at the end of the line or followed only by spaces
                    const suffixIndex = line.toUpperCase().lastIndexOf('III');
                    const afterSuffix = line.substring(suffixIndex + 3).trim();
                    if (afterSuffix.length > 0) {
                        console.log('✗ "III" not at end of name, likely not a suffix, rejecting');
                        continue;
                    }
                }

                // VALIDATION PASSED: This appears to be a legitimate suffix
                // Normalize
                if (suffix === 'JUNIOR') suffix = 'JR';
                if (suffix === 'SENIOR') suffix = 'SR';

                console.log('✓✓ Valid suffix found with proper name context:', suffix);
                console.log('   Name words detected:', nameWords.join(', '));
                return suffix;
            }
        }

        console.log('✗ No suffix found (pattern search completed with no valid matches)');
        return "";
    }

    // Independent extraction function for Civil Status (works like extractFieldValue)
    function extractCivilStatus(lines) {
        console.log('Extracting Civil Status independently...');

        // Try using extractFieldValue first for labeled fields
        const result = extractFieldValue(lines, 'civilStatus');
        if (result && result.value) {
            let value = result.value.toUpperCase().trim();
            console.log(`✓ Found civil status field via ${result.method}:`, value);

            // Remove any remaining Filipino/English label fragments
            value = value
                .replace(/KATAYUANG SIBIL/g, '')
                .replace(/CIVIL STATUS/g, '')
                .replace(/MARITAL STATUS/g, '')
                .replace(/STATUS/g, '')
                .replace(/KATAYUAN/g, '')
                .replace(/[\/:]/g, '')
                .trim();

            console.log(`  After label removal: "${value}"`);

            // Normalize the value to standard civil status values
            // Single
            if (value.includes('SINGLE') || value.includes('SOLO') || value.includes('WALANG ASAWA')) {
                console.log('  Normalized to: Single');
                return 'Single';
            }
            // Married
            if (value.includes('MARRIED') || value.includes('KASAL') || value.includes('MAY ASAWA')) {
                console.log('  Normalized to: Married');
                return 'Married';
            }
            // Widowed
            if (value.includes('WIDOW') || value.includes('BIYUDO') || value.includes('BIYUDA')) {
                console.log('  Normalized to: Widowed');
                return 'Widowed';
            }
            // Separated
            if (value.includes('SEPARATED') || value.includes('HIWALAY')) {
                console.log('  Normalized to: Separated');
                return 'Separated';
            }
            // Divorced (rare in Philippines but possible)
            if (value.includes('DIVORCED') || value.includes('DIBORSYADO')) {
                console.log('  Normalized to: Divorced');
                return 'Divorced';
            }

            console.log('  ✗ Could not normalize civil status value:', value);
            // Return the cleaned value even if not normalized
            return value;
        }

        // Fallback: Search all lines for civil status patterns
        console.log('Label extraction failed, searching for civil status patterns...');
        for (const line of lines) {
            const upper = line.toUpperCase().trim();

            // Check for specific civil status values
            if (/\b(SINGLE|SOLO|WALANG ASAWA)\b/.test(upper)) {
                console.log('✓ Found civil status pattern (Single) in line:', line);
                return 'Single';
            }
            if (/\b(MARRIED|KASAL|MAY ASAWA)\b/.test(upper)) {
                console.log('✓ Found civil status pattern (Married) in line:', line);
                return 'Married';
            }
            if (/\b(WIDOW|WIDOWED|BIYUDO|BIYUDA)\b/.test(upper)) {
                console.log('✓ Found civil status pattern (Widowed) in line:', line);
                return 'Widowed';
            }
            if (/\b(SEPARATED|HIWALAY)\b/.test(upper)) {
                console.log('✓ Found civil status pattern (Separated) in line:', line);
                return 'Separated';
            }
        }

        console.log('✗ Civil Status not found');
        return "";
    }

    // Independent extraction function for Gender/Sex (works like extractFieldValue)
    function extractGender(lines) {
        console.log('Extracting Gender/Sex independently...');

        // Try using extractFieldValue first for labeled fields
        const result = extractFieldValue(lines, 'gender');
        if (result && result.value) {
            let value = result.value.toUpperCase().trim();
            console.log(`✓ Found gender field via ${result.method}:`, value);

            // Remove any remaining Filipino/English label fragments
            value = value
                .replace(/KASARIAN/g, '')
                .replace(/GENDER/g, '')
                .replace(/SEX/g, '')
                .replace(/[\/:]/g, '')
                .trim();

            console.log(`  After label removal: "${value}"`);

            // Normalize the value to Male/Female
            // Check for Male indicators (M, MALE, LALAKI)
            if (/\bM\b/.test(value) || value.includes('MALE') || value.includes('LALAKI')) {
                console.log('  Normalized to: Male');
                return 'Male';
            }
            // Check for Female indicators (F, FEMALE, BABAE)
            if (/\bF\b/.test(value) || value.includes('FEMALE') || value.includes('BABAE')) {
                console.log('  Normalized to: Female');
                return 'Female';
            }

            console.log('  ✗ Could not normalize gender value:', value);
        }

        // Fallback: Search all lines for gender patterns
        console.log('Label extraction failed, searching for gender patterns...');
        for (const line of lines) {
            const upper = line.toUpperCase().trim();

            // Check for explicit gender indicators
            // Pattern: "SEX: M", "GENDER: MALE", "M", "MALE" (standalone or with label)
            if (/\bSEX\b.*\bM\b/.test(upper) ||
                /\bGENDER\b.*\bM\b/.test(upper) ||
                /\bKASARIAN\b.*\bM\b/.test(upper) ||
                /\bSEX\b.*\bMALE\b/.test(upper) ||
                /\bGENDER\b.*\bMALE\b/.test(upper) ||
                /\bSEX\b.*\bLALAKI\b/.test(upper) ||
                upper === 'M' ||
                upper === 'MALE' ||
                upper === 'LALAKI') {
                console.log('✓ Found gender pattern (Male) in line:', line);
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
                console.log('✓ Found gender pattern (Female) in line:', line);
                return 'Female';
            }
        }

        console.log('✗ Gender not found');
        return "";
    }

    // Parse FIRST MIDDLE SUFFIX from array of words with compound name detection
    function parseFirstMiddleSuffix(words, preferAllAsFirstName = false) {
        let firstName = '';
        let middleName = '';
        let suffix = '';

        if (words.length === 0) return { firstName, middleName, suffix };

        console.log('Parsing first/middle/suffix from words:', words);
        console.log('Prefer all as first name:', preferAllAsFirstName);

        // Check for suffix at the end
        const suffixPatterns = ['JR', 'SR', 'II', 'III', 'IV', 'V', 'JUNIOR', 'SENIOR'];
        const lastWord = words[words.length - 1].toUpperCase();
        if (suffixPatterns.includes(lastWord)) {
            suffix = lastWord === 'JUNIOR' ? 'JR' : (lastWord === 'SENIOR' ? 'SR' : lastWord);
            words = words.slice(0, -1); // Remove suffix from words
            console.log('Found suffix in name field:', suffix);
        }

        if (words.length === 0) return { firstName, middleName, suffix };

        // OPTIMIZATION: If middle name exists in a separate field, treat all words as first name
        // This supports multiple first names like "JETLANE MARSHALL" or "MARIA CLARA ROSA"
        if (preferAllAsFirstName) {
            firstName = words.join(' ');
            console.log(`✓ All words as first name (separate middle name field exists): "${firstName}"`);
            return { firstName, middleName: '', suffix };
        }

        // Check if last 2-3 words form a compound middle name
        if (words.length >= 3) {
            // Try 3 words compound (e.g., "DE LOS SANTOS")
            const last3 = words.slice(-3);
            if (isCompoundName(last3)) {
                middleName = last3.join(' ');
                firstName = words.slice(0, -3).join(' ');
                console.log(`✓ Found 3-word compound middle name: "${middleName}"`);
                console.log(`  First Name (multiple words): "${firstName}"`);
                return { firstName, middleName, suffix };
            }
        }

        if (words.length >= 2) {
            // Try 2 words compound (e.g., "DELA CRUZ", "SANTA MARIA")
            const last2 = words.slice(-2);
            if (isCompoundName(last2)) {
                middleName = last2.join(' ');
                firstName = words.slice(0, -2).join(' ');
                console.log(`✓ Found 2-word compound middle name: "${middleName}"`);
                console.log(`  First Name (multiple words): "${firstName}"`);
                return { firstName, middleName, suffix };
            }
        }

        // Standard parsing - last word is middle name, rest is first name
        // This allows multiple first names: "JETLANE MARSHALL RODRIGUEZ" → first="JETLANE MARSHALL", middle="RODRIGUEZ"
        if (words.length >= 2) {
            middleName = words[words.length - 1];
            firstName = words.slice(0, -1).join(' ');
            console.log(`Standard parsing (last word as middle, rest as first name):`);
            console.log(`  First Name (supports multiple words): "${firstName}"`);
            console.log(`  Middle Name: "${middleName}"`);
        } else {
            // Only one word - it's the first name, no middle name
            firstName = words[0];
            console.log(`Single word - First Name: "${firstName}", no middle name in field`);
        }

        return { firstName, middleName, suffix };
    }

    // Complete list of Davao City barangays (182 total)
    const DAVAO_CITY_BARANGAYS = [
        "1-A", "2-A", "3-A", "4-A", "5-A", "6-A", "7-A", "8-A", "9-A", "10-A",
        "11-B", "12-B", "13-B", "14-B", "15-B", "16-B", "17-B", "18-B", "19-B", "20-B",
        "21-C", "22-C", "23-C", "24-C", "25-C", "26-C", "27-C", "28-C", "29-C", "30-C",
        "31-D", "32-D", "33-D", "34-D", "35-D", "36-D", "37-D", "38-D", "39-D", "40-D",
        "Agdao", "Angalan", "Bucana", "Bunawan", "Angliongto", "Baguio", "Bangkal",
        "Buhangin", "BUHANGIN (POB.)", "Cabantian", "Communal", "Dumoy", "Ilang", "Indangan", "Lasang",
        "Leon Garcia", "Magtuod", "Mahayag", "Matina Aplaya", "Matina Crossing",
        "Matina Pangi", "Malagos", "Matina Biao", "New Valencia",
        "Talomo", "Catalunan Grande", "Catalunan Pequeño", "Bato", "Balingian",
        "Baganihan", "Baliok", "Crossing Bayabas", "Daliao", "Lampianao",
        "Lizada", "Ma-a", "Mudiang", "Mulig", "Rafael Castillo", "Riverside",
        "Tacunan", "Talandang", "Tawan-Tawan", "Tibungco", "Toril", "Ulas",
        "Wilfredo Aquino",
        "Tugbok", "Bago Aplaya", "Bago Gallera", "Bago Oshiro", "Calinan",
        "Catigan", "Colosas", "Dacudao", "Dalag", "Dalagdag", "Daliaon Plantation",
        "Dominga", "Eden", "Fatima", "Gumitan", "Lacson", "Lamanan", "Langub",
        "Los Amigos", "Magsaysay", "Malabog", "Malagamot", "Mandug", "Mapula",
        "Marilog", "Megkawayan", "New Carmen", "Pampanga", "Panacan", "Pangyan",
        "Paquibato", "Paradise Embak", "Salaysay", "Salapawan", "San Isidro",
        "Santo Niño", "Sibulan", "Sasa", "Sirawan", "Subasta", "Suawan",
        "Sumimao", "Tagakpan", "Tagluno", "Tamugan", "Tapak", "Tawantawan",
        "Tigatto", "Lubogan", "Vicente Hizon Sr.", "Wangan", "Waan", "Wines"
    ];

    // Allowed ID types
    const ALLOWED_ID_TYPES = ['phil-id', 'driver-license', 'sss-id', 'umid'];

    // ID type display element
    const idTypeDisplay = document.getElementById('id-type-display');

    // Go Back button functionality
    const goBackBtn = document.getElementById('goBackBtn');
    if (goBackBtn) {
        goBackBtn.addEventListener('click', function () {
            window.history.back();
        });
    }

    // Initialize status message
    status.innerHTML = '<i class="fas fa-upload mr-2"></i>Upload your ID - AI will auto-detect the type';

    // Debug logger
    function debugLog(category, data) {
        if (DEBUG_MODE) {
            console.log(`[OCR-${category}]`, data);
        }
    }

    // ========================================================================
    // KEY-VALUE PAIRING FUNCTIONS
    // ========================================================================
    // These functions ensure proper matching of labels/indicators with extracted data
    // for easy parsing, validation, and field population.
    //
    // EXTRACTION FLOW:
    // 1. extractFieldValue() finds labels in OCR text and extracts values
    // 2. Individual extraction functions (extractFirstName, extractGender, etc.) use extractFieldValue()
    // 3. ID-specific parsers (parseDriverLicenseFront, etc.) combine extracted fields
    // 4. formatExtractedDataWithLabels() formats output as "Label: Value" strings
    // 5. getExtractedDataLabeled() returns object with labeled keys for validation
    // 6. logExtractedData() displays extracted data in labeled format
    //
    // This ensures consistency and traceability throughout the extraction pipeline.
    // ========================================================================

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
                'sss-id': 'SSS ID',
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
        console.log('\n========== EXTRACTED DATA (KEY-VALUE PAIRS) ==========');
        const formattedData = formatExtractedDataWithLabels(extractedData, idType);
        formattedData.forEach(line => console.log(line));
        console.log(`Overall Confidence: ${overallConfidence}%`);
        console.log('=======================================================\n');
    }

    // Get extracted data as labeled key-value pairs for easy validation
    // USE THIS FUNCTION when you need to pass extracted data for validation or field population
    // Returns data in format: { 'Label': 'Value', ... }
    // Example: { 'First Name': 'JETLANCE MARSHALL', 'Gender': 'Male', ... }
    // This ensures proper matching of labels/indicators with extracted data
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
            'sss-id': 'SSS ID (Social Security System)',
            'umid': 'UMID (Unified Multi-Purpose ID)'
        };

        const name = idTypeNames[idType] || 'Unknown ID';

        // Log confidence to console for debugging
        console.log(`Detected ID Type: ${name} (${confidence}% confidence)`);

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

        // Enable OCR for all supported ID types OR when empty (for auto-detection)
        if (!selectedValue || selectedValue === 'phil-id' || selectedValue === 'driver-license' || selectedValue === 'sss-id' || selectedValue === 'umid') {
            ocrEnabled = true;
            uploadFront.classList.remove('disabled');
            uploadBack.classList.remove('disabled');
            documentWarning.classList.add('hidden');

            if (!selectedValue) {
                status.innerHTML = '<i class="fas fa-info-circle mr-2"></i>Upload your ID - we\'ll detect the type automatically';
            } else {
                status.innerHTML = '<i class="fas fa-info-circle mr-2"></i>You can now upload ID images';
            }

            // Show Davao verification for all ID types
            davaoVerification.classList.add('hidden');
            enableFormFields();
        } else {
            ocrEnabled = false;
            uploadFront.classList.add('disabled');
            uploadBack.classList.add('disabled');
            documentWarning.classList.remove('hidden');
            status.innerHTML = '<i class="fas fa-info-circle mr-2"></i>OCR not available for selected document type';
            resultBox.classList.add('hidden');
            davaoVerification.classList.add('hidden');
            enableFormFields();
        }
    });

    // Setup uploaders for both front and back
    setupUploader(uploadFront, fileFront, imagePreview, false);
    setupUploader(uploadBack, fileBack, imagePreviewBack, true);

    function setupUploader(uploadArea, fileInput, preview, isBack = false) {
        if (!uploadArea || !fileInput || !preview) {
            console.error('Missing uploader elements');
            return;
        }

        try {
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
        } catch (e) {
            console.error('Error setting up uploader:', e);
        }
    }

    // Handle file upload and processing
    function handleFileUpload(file, preview, section, isBack) {
        if (!validateFile(file)) return;

        const reader = new FileReader();
        reader.onload = function (e) {
            // Show image preview immediately and replace upload box content
            preview.src = e.target.result;
            preview.classList.remove('hidden');

            // Hide upload instructions and show preview prominently
            const uploadContent = section.querySelector('.id-upload-content');
            if (uploadContent) {
                uploadContent.classList.add('preview-mode');
            }
            section.classList.add('active', 'has-image');

            // Process with OCR if enabled
            if (ocrEnabled) {
                preprocessAndOCR(e.target.result, preview, isBack);
            }
        };
        reader.readAsDataURL(file);
    }

    // Minimal preprocessing - OCR.space has better built-in algorithms (Async)
    async function preprocessAndOCR(imageSrc, preview, isBack) {
        try {
            console.log('Starting image preprocessing...');

            // OCR.space handles image preprocessing automatically
            // Just pass the original image for best results
            // The API does: auto-rotation, deskewing, noise reduction, contrast enhancement

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

            // Get image as data URL (JPEG for smaller size, 92% quality)
            const processedImage = canvas.toDataURL('image/jpeg', 0.92);

            console.log('Image preprocessing complete');

            // Send to OCR.space API asynchronously
            await processImageWithOCR(processedImage, isBack);
        } catch (error) {
            console.error('Error in preprocessing:', error);
            status.innerHTML = '<i class="fas fa-exclamation-triangle mr-2 text-red-500"></i>Image preprocessing failed. Please try again.';
            status.className = 'text-sm text-red-600 mt-2';
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

    // Advanced text cleaning to remove noise and garbage
    function cleanOCRText(text) {
        if (!text) return "";

        debugLog('RAW-OCR', text);

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
            case 'sss-id':
                // Format: 00-0000000-0
                pattern = /^\d{2}-\d{7}-\d{1}$/;
                expectedLength = [12]; // with dashes
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

    // Validate name match with SIMILARITY comparison (prioritizes full name matching)
    function validateNameMatch(extractedFirstName, extractedMiddleName, extractedLastName, extractedSuffix = "") {
        // Normalize names for comparison
        const normalizeForComparison = (name) => {
            if (!name) return "";
            return name.trim().toUpperCase().replace(/[^A-Z]/g, '');
        };

        const extractedFirst = normalizeForComparison(extractedFirstName);
        const extractedMiddle = normalizeForComparison(extractedMiddleName);
        const extractedLast = normalizeForComparison(extractedLastName);
        const extractedSuff = normalizeForComparison(extractedSuffix);

        const regFirst = normalizeForComparison(registeredFirstName);
        const regMiddle = normalizeForComparison(registeredMiddleName);
        const regLast = normalizeForComparison(registeredLastName);

        console.log('=== NAME MATCHING VALIDATION - DETAILED ===');
        console.log('Extracted Names (normalized):', { first: extractedFirst, middle: extractedMiddle, last: extractedLast, suffix: extractedSuff });
        console.log('Registered Names (normalized):', { first: regFirst, middle: regMiddle, last: regLast });
        console.log('Registered Names (raw):', {
            firstName: registeredFirstName,
            middleName: registeredMiddleName,
            lastName: registeredLastName,
            suffix: registeredSuffix
        });

        debugLog('NAME-MATCH-VALIDATION', {
            extracted: { first: extractedFirst, middle: extractedMiddle, last: extractedLast, suffix: extractedSuff },
            registered: { first: regFirst, middle: regMiddle, last: regLast }
        });

        // CRITICAL: If registered name is empty, this is a configuration error
        if (!regFirst && !regMiddle && !regLast) {
            console.error('⚠️ CRITICAL: No registered name found in database!');
            console.error('This means ViewBag.RegisteredFirstName/MiddleName/LastName are empty');
            console.error('Check if user is logged in and Controller is passing ViewBag correctly');

            // Still block if no registered name - this is suspicious
            return {
                matches: false,
                reason: 'No registered name found in database. Please contact support.',
                confidence: 0,
                details: {
                    firstMatches: false,
                    middleMatches: false,
                    lastMatches: false,
                    firstSimilarity: 0,
                    middleSimilarity: 0,
                    lastSimilarity: 0,
                    overallSimilarity: 0,
                    extractedName: `${extractedLastName}, ${extractedFirstName} ${extractedMiddleName} ${extractedSuffix}`.trim(),
                    registeredName: 'Not found in database'
                }
            };
        }

        // Calculate SIMILARITY for each name component (Last → First → Middle → Suffix order)
        const lastSimilarity = calculateSimilarity(extractedLast, regLast);
        const firstSimilarity = calculateSimilarity(extractedFirst, regFirst);
        let middleSimilarity = 100; // Default to 100 if no middle name to compare

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

        // Determine if names match based on similarity threshold
        const SIMILARITY_THRESHOLD = 80; // 80% similarity required

        const lastMatches = lastSimilarity >= SIMILARITY_THRESHOLD;
        const firstMatches = firstSimilarity >= SIMILARITY_THRESHOLD;
        const middleMatches = middleSimilarity >= SIMILARITY_THRESHOLD;

        // Calculate overall similarity (weighted: Last name is most important)
        const overallSimilarity = Math.round((lastSimilarity * 0.4 + firstSimilarity * 0.4 + middleSimilarity * 0.2));

        // Log name similarity scores to console for debugging
        console.log('Name Matching Validation:', {
            extractedName: `${extractedLastName}, ${extractedFirstName} ${extractedMiddleName}`.trim(),
            registeredName: `${registeredLastName}, ${registeredFirstName} ${registeredMiddleName}`.trim(),
            similarity: {
                lastName: `${lastSimilarity}%`,
                firstName: `${firstSimilarity}%`,
                middleName: `${middleSimilarity}%`,
                overall: `${overallSimilarity}%`
            },
            threshold: `${SIMILARITY_THRESHOLD}%`,
            matches: lastMatches && firstMatches && middleMatches
        });

        debugLog('NAME-SIMILARITY-SCORES', {
            last: `${lastSimilarity}% (${extractedLast} vs ${regLast})`,
            first: `${firstSimilarity}% (${extractedFirst} vs ${regFirst})`,
            middle: `${middleSimilarity}% (${extractedMiddle} vs ${regMiddle})`,
            overall: `${overallSimilarity}%`
        });

        // All critical components must match
        const allMatch = lastMatches && firstMatches && middleMatches;

        if (!allMatch) {
            let reason = 'Name mismatch: ';
            const issues = [];

            if (!lastMatches) issues.push(`Last name (${lastSimilarity}% similar)`);
            if (!firstMatches) issues.push(`First name (${firstSimilarity}% similar)`);
            if (!middleMatches) issues.push(`Middle name (${middleSimilarity}% similar)`);

            reason += issues.join(', ');

            return {
                matches: false,
                reason: reason.trim(),
                confidence: overallSimilarity,
                details: {
                    firstMatches,
                    middleMatches,
                    lastMatches,
                    firstSimilarity,
                    middleSimilarity,
                    lastSimilarity,
                    overallSimilarity,
                    extractedName: `${extractedLastName}, ${extractedFirstName} ${extractedMiddleName} ${extractedSuffix}`.trim(),
                    registeredName: `${registeredLastName}, ${registeredFirstName} ${registeredMiddleName}`.trim()
                }
            };
        }

        return {
            matches: true,
            reason: `Names match (${overallSimilarity}% similarity)`,
            confidence: overallSimilarity
        };
    }

    // Extract barangay from address text
    function extractBarangayFromAddress(text) {
        if (!text) return null;

        const upperText = text.toUpperCase();

        // Try to find a match in our barangay list
        for (const barangay of DAVAO_CITY_BARANGAYS) {
            const upperBarangay = barangay.toUpperCase();

            // Check for exact match
            if (upperText.includes(upperBarangay)) {
                return barangay;
            }

            // Check for match with common abbreviations
            const withBrgy = `BRGY ${upperBarangay}`;
            const withBarangay = `BARANGAY ${upperBarangay}`;

            if (upperText.includes(withBrgy) || upperText.includes(withBarangay)) {
                return barangay;
            }
        }

        debugLog('BARANGAY-EXTRACTION', {
            searchText: text,
            found: null
        });

        return null;
    }

    // Process image with OCR.space API (much better than Tesseract for IDs)
    // Async function for OCR processing with error recovery
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

        status.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Processing with advanced ID recognition...';
        progressBar.classList.remove('hidden');
        progress.style.width = '10%';

        // Realistic progress simulation
        const progressSteps = [20, 35, 50, 65, 80, 95, 100];
        let currentStep = 0;

        const progressInterval = setInterval(() => {
            if (currentStep < progressSteps.length) {
                progress.style.width = `${progressSteps[currentStep]}%`;
                currentStep++;
            } else {
                clearInterval(progressInterval);
            }
        }, 300);

        const apiKey = 'K87899142388957';
        const apiUrl = 'https://api.ocr.space/parse/image';

        // Create form data with OPTIMIZED settings for Philippine IDs
        const formData = new FormData();
        formData.append('base64Image', imageSrc);
        formData.append('language', 'eng');
        formData.append('isOverlayRequired', 'false');
        formData.append('detectOrientation', 'true');
        formData.append('scale', 'true');
        formData.append('OCREngine', '2');
        formData.append('isTable', 'false');
        formData.append('filetype', 'PNG');
        formData.append('iscreatesearchablepdf', 'false');
        formData.append('issearchablepdfhidetextlayer', 'false');
        formData.append('detectCheckbox', 'false');
        formData.append('checkboxtemplate', '0');
        formData.append('apikey', apiKey);

        fetch(apiUrl, {
            method: 'POST',
            body: formData
        })
            .then(response => response.json())
            .then(result => {
                clearInterval(progressInterval);
                progress.style.width = '100%';

                if (result.OCRExitCode !== 1 || !result.ParsedResults || result.ParsedResults.length === 0) {
                    throw new Error(result.ErrorMessage || 'OCR processing failed');
                }

                const text = result.ParsedResults[0].ParsedText;

                // Keep progress bar visible at 100% while analyzing
                // DON'T hide it here - let the final completion hide it
                setTimeout(() => {
                    status.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Analyzing ID type...';
                }, 500);

                // Clean OCR text to remove noise
                const cleanedText = cleanOCRText(text);
                lastOCRText = cleanedText;

                // Log all extracted OCR text to console for debugging
                console.log('========================================');
                console.log('===  OCR EXTRACTION COMPLETE  ===');
                console.log('========================================');
                console.log('Raw OCR Text:');
                console.log(text);
                console.log('----------------------------------------');
                console.log('Cleaned OCR Text:');
                console.log(cleanedText);
                console.log('========================================');

                // === STEP 1: ID TYPE DETECTION ===
                console.log('\n=== STEP 1: ID TYPE DETECTION ===');
                const detectedIdType = detectIdType(cleanedText);
                const selectedIdType = documentType.value;

                // Log detected ID type to console for debugging
                console.log('ID Detection Result:', {
                    detected: detectedIdType ? getIdTypeName(detectedIdType) : 'None',
                    selected: selectedIdType ? getIdTypeName(selectedIdType) : 'None'
                });
                debugLog('ID-DETECTION', { detected: detectedIdType, selected: selectedIdType });

                // VALIDATION: Check if detected ID type is in allowed list
                if (detectedIdType && !ALLOWED_ID_TYPES.includes(detectedIdType)) {
                    console.error('✗ STEP 1 FAILED: Detected ID type not in allowed list:', detectedIdType);
                    showInvalidIdTypeModal(getIdTypeName(detectedIdType));
                    return;
                }

                // If no ID type selected yet, auto-select
                if (!selectedIdType && detectedIdType) {
                    documentType.value = detectedIdType;
                    const detectionConfidence = 90;
                    updateIDTypeDisplay(detectedIdType, detectionConfidence);
                    status.innerHTML = `<i class="fas fa-check-circle mr-2"></i>Auto-detected: ${getIdTypeName(detectedIdType)}`;
                }

                // Use detected type if selection is empty (auto-detection mode)
                const idTypeToProcess = selectedIdType || detectedIdType;

                // Final check: If still no valid ID type, show error with detailed guidance
                if (!idTypeToProcess || !ALLOWED_ID_TYPES.includes(idTypeToProcess)) {
                    console.error('✗ STEP 1 FAILED: Unable to detect valid ID type');
                    console.error('OCR Text Preview:', cleanedText.substring(0, 500));

                    let errorMessage = 'Unable to detect ID type from the uploaded image.\n\n';
                    errorMessage += 'Please ensure:\n';
                    errorMessage += '1. The image is clear and well-lit\n';
                    errorMessage += '2. All text on the ID is readable\n';
                    errorMessage += '3. The entire ID is visible in the image\n';
                    errorMessage += '4. You are using one of these accepted IDs:\n';
                    errorMessage += '   • Philippine National ID (PhilSys)\n';
                    errorMessage += '   • Driver\'s License (LTO)\n';
                    errorMessage += '   • SSS ID\n';
                    errorMessage += '   • UMID (Unified Multi-Purpose ID)\n\n';
                    errorMessage += 'Tips:\n';
                    errorMessage += '• Make sure the ID text is not blurry\n';
                    errorMessage += '• Avoid shadows and glare\n';
                    errorMessage += '• Try taking a new photo with better lighting';

                    showOCRErrorModal(errorMessage);
                    return;
                }

                console.log('✓ STEP 1 PASSED: ID Type =', getIdTypeName(idTypeToProcess));
                console.log('===================================\n');

                // === STEP 2 & 3: Parse ID, Validate Name, Extract Data ===
                // Each parser will handle Steps 2 & 3 internally
                if (idTypeToProcess === "driver-license") {
                    if (!isBack) {
                        parseDriverLicenseFront(cleanedText);
                    }
                } else if (idTypeToProcess === "phil-id") {
                    if (isBack) {
                        parsePhilSysBack(cleanedText);
                    } else {
                        parsePhilSysFront(cleanedText);
                    }
                } else if (idTypeToProcess === "sss-id") {
                    if (!isBack) {
                        parseSSSFront(cleanedText);
                    }
                } else if (idTypeToProcess === "umid") {
                    if (!isBack) {
                        parseUMIDFront(cleanedText);
                    } else {
                        parseUMIDBack(cleanedText);
                    }
                } else {
                    console.error('✗ VALIDATION FAILED: Unknown ID type');
                    showOCRErrorModal('Unable to detect ID type. Please ensure the image is clear and contains a valid Philippine ID.');
                }

            })
            .catch(err => {
                clearInterval(progressInterval);
                progressBar.classList.add('hidden');

                let errorMessage = 'Unable to process the ID image. ';

                if (err.message.includes('network') || err.message.includes('fetch')) {
                    errorMessage = 'Network error. Please check your internet connection and try again.';
                    status.innerHTML = '<i class="fas fa-wifi mr-2"></i>Network error';
                } else if (err.message.includes('rate') || err.message.includes('limit')) {
                    errorMessage = 'API rate limit reached. Please wait a moment and try again.';
                    status.innerHTML = '<i class="fas fa-stopwatch mr-2"></i>Rate limit reached';
                } else if (err.message.includes('size') || err.message.includes('large')) {
                    errorMessage = 'Image file is too large. Please use a smaller image (max 1MB).';
                    status.innerHTML = '<i class="fas fa-file-image mr-2"></i>File too large';
                } else if (err.message.includes('format')) {
                    errorMessage = 'Invalid image format. Please use JPEG or PNG.';
                    status.innerHTML = '<i class="fas fa-image mr-2"></i>Invalid format';
                } else {
                    status.innerHTML = '<i class="fas fa-exclamation-triangle mr-2"></i>Recognition failed';
                    errorMessage = 'Unable to process the ID image. Please ensure:\n\n' +
                        '1. Image is clear and well-lit\n' +
                        '2. All text on the ID is readable\n' +
                        '3. ID is fully visible in the frame\n' +
                        '4. Image is not blurry or distorted\n\n' +
                        'Error details: ' + err.message;
                }

                showOCRErrorModal(errorMessage);

                debugLog('OCR-API-ERROR', {
                    message: err.message,
                    stack: err.stack,
                    timestamp: new Date().toISOString()
                });
            });
    }

    // Get friendly ID type name
    function getIdTypeName(idType) {
        const idTypeNames = {
            'driver-license': 'Driver\'s License',
            'phil-id': 'National ID',
            'sss-id': 'SSS ID',
            'umid': 'UMID'
        };
        return idTypeNames[idType] || idType;
    }

    // Enhanced ID Type Detection with Multiple Detection Methods
    function detectIdType(text) {
        if (!text) return null;

        const upperText = text.toUpperCase();
        console.log('Starting ID Type Detection...');
        console.log('Text length:', text.length);

        // === METHOD 1: STRONG KEYWORD DETECTION (High confidence) ===
        // Check for unique, definitive keywords first

        // UMID - Most specific keywords
        if (upperText.includes('UMID') ||
            upperText.includes('UNIFIED MULTI-PURPOSE') ||
            upperText.includes('UNIFIED MULTIPURPOSE') ||
            upperText.includes('UNIFIED ID')) {
            console.log('✓ UMID detected (Strong keyword match)');
            return 'umid';
        }

        // PhilSys/National ID - Unique identifiers
        if (upperText.includes('PHILSYS') ||
            upperText.includes('PHILIPPINE IDENTIFICATION SYSTEM') ||
            upperText.includes('PHILIPPINE IDENTIFICATION CARD') ||
            upperText.includes('PCN')) {
            console.log('✓ National ID detected (PhilSys keyword)');
            return 'phil-id';
        }

        // Driver's License - Unique identifiers
        if (upperText.includes('LAND TRANSPORTATION OFFICE') ||
            upperText.includes('LICENSE NO') ||
            upperText.includes('RESTRICTION CODE') ||
            (upperText.includes('LTO') && (upperText.includes('LICENSE') || upperText.includes('RESTRICTION')))) {
            console.log('✓ Driver\'s License detected (LTO keyword)');
            return 'driver-license';
        }

        // === METHOD 2: ID NUMBER PATTERN DETECTION ===
        // Each ID has a unique number format

        // UMID CRN: 0000-0000000-0 (4 digits, 7 digits, 1 digit)
        if (/\d{4}-\d{7}-\d{1}/.test(text)) {
            console.log('✓ UMID detected (CRN pattern: 0000-0000000-0)');
            return 'umid';
        }

        // National ID: 0000-0000-0000-0000 (16 digits in 4 groups)
        if (/\d{4}-\d{4}-\d{4}-\d{4}/.test(text)) {
            console.log('✓ National ID detected (16-digit pattern)');
            return 'phil-id';
        }

        // Driver's License: L00-00-000000 (Letter followed by numbers)
        if (/[A-Z]\d{2}-\d{2}-\d{6}/.test(text)) {
            console.log('✓ Driver\'s License detected (License number pattern)');
            return 'driver-license';
        }

        // SSS: 00-0000000-0 (2 digits, 7 digits, 1 digit)
        if (/\d{2}-\d{7}-\d{1}/.test(text)) {
            // Could be SSS or confused with UMID
            // Additional check: SSS IDs typically have "SSS" or "SOCIAL SECURITY" text
            if (upperText.includes('SSS') || upperText.includes('SOCIAL SECURITY')) {
                console.log('✓ SSS ID detected (SSS pattern + SSS keyword)');
                return 'sss-id';
            }
        }

        // === METHOD 3: COMPREHENSIVE INDICATOR SCORING ===
        console.log('Using comprehensive indicator scoring...');

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
            // Medium weight (3 points)
            'PSA': 3,
            'STATISTICS AUTHORITY': 3,
            'FULL NAME': 3,
            // Low weight (1 point)
            'REPUBLIC': 1,
            'PHILIPPINES': 1
        };

        // SSS ID indicators (weighted)
        const sssIndicators = {
            // High weight (5 points)
            'SOCIAL SECURITY SYSTEM': 5,
            'SSS NUMBER': 5,
            'SSS ID': 5,
            // Medium weight (3 points)
            'EMPLOYEE': 3,
            'MEMBER': 3,
            'CRN': 3,
            // Low weight (1 point)
            'SSS': 1
        };

        // UMID indicators (weighted)
        const umidIndicators = {
            // High weight (5 points) - already checked above, but keep for completeness
            'UMID CARD': 5,
            'UNIFIED MULTI': 5,
            // Medium weight (3 points)
            'GSIS': 3,
            'SSS': 1 // Low because SSS appears on both SSS ID and UMID
        };

        let driverLicenseScore = 0;
        let nationalIdScore = 0;
        let sssScore = 0;
        let umidScore = 0;

        // Calculate weighted scores
        Object.entries(driverLicenseIndicators).forEach(([indicator, weight]) => {
            if (upperText.includes(indicator)) {
                driverLicenseScore += weight;
                console.log(`  Driver's License: +${weight} for "${indicator}"`);
            }
        });

        Object.entries(nationalIdIndicators).forEach(([indicator, weight]) => {
            if (upperText.includes(indicator)) {
                nationalIdScore += weight;
                console.log(`  National ID: +${weight} for "${indicator}"`);
            }
        });

        Object.entries(sssIndicators).forEach(([indicator, weight]) => {
            if (upperText.includes(indicator)) {
                sssScore += weight;
                console.log(`  SSS ID: +${weight} for "${indicator}"`);
            }
        });

        Object.entries(umidIndicators).forEach(([indicator, weight]) => {
            if (upperText.includes(indicator)) {
                umidScore += weight;
                console.log(`  UMID: +${weight} for "${indicator}"`);
            }
        });

        const scores = {
            'driver-license': driverLicenseScore,
            'phil-id': nationalIdScore,
            'sss-id': sssScore,
            'umid': umidScore
        };

        console.log('Final Scores:', scores);

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
            console.log(`✓ ID Type detected: ${getIdTypeName(detectedType)} (Score: ${maxScore})`);
            return detectedType;
        }

        // === METHOD 4: FALLBACK SINGLE KEYWORD DETECTION ===
        console.log('Using fallback detection...');

        if (upperText.includes('LICENSE')) {
            console.log('✓ Fallback: Driver\'s License (contains "LICENSE")');
            return 'driver-license';
        }

        if (upperText.includes('NATIONAL') || upperText.includes('IDENTIFICATION')) {
            console.log('✓ Fallback: National ID (contains "NATIONAL" or "IDENTIFICATION")');
            return 'phil-id';
        }

        if (upperText.includes('SSS')) {
            // Disambiguate between SSS and UMID
            if (upperText.includes('UNIFIED') || upperText.includes('UMID')) {
                console.log('✓ Fallback: UMID (contains both "SSS" and "UNIFIED/UMID")');
                return 'umid';
            } else {
                console.log('✓ Fallback: SSS ID (contains "SSS" only)');
                return 'sss-id';
            }
        }

        console.log('✗ Unable to detect ID type');
        return null;
    }

    // Parse Driver's License Front with precision extraction
    function parseDriverLicenseFront(text) {
        const lines = text.split('\n').map(line => line.trim()).filter(line => line);

        console.log('\n========================================');
        console.log('=== DRIVER\'S LICENSE FULL OCR TEXT ===');
        console.log('========================================');
        console.log('Total Lines:', lines.length);
        console.log('All Lines:');
        lines.forEach((line, index) => {
            console.log(`  ${index + 1}: "${line}"`);
        });
        console.log('========================================\n');

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

        // Extract birthdate
        extractedData.birthdate = extractDriverLicenseBirthdate(lines);
        if (extractedData.birthdate) {
            const validation = validateBirthdate(extractedData.birthdate);
            extractedData.confidence.birthdate = validation.confidence;
        }

        // Extract gender using unified function with label matching
        extractedData.sex = extractGender(lines);
        extractedData.confidence.sex = extractedData.sex ? 90 : 0;

        // Extract city for Davao verification
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
            console.error('✗ EXTRACTION FAILED: Labels detected as values!');
            console.error('Issues:', dataValidation.issues);

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
            return; // STOP - Do not proceed with invalid data
        }

        // === STEP 2: NAME MATCHING VALIDATION (CRITICAL) ===
        console.log('=== STEP 2: NAME MATCHING VALIDATION ===');
        const nameValidation = validateNameMatch(
            extractedData.firstName,
            extractedData.middleName,
            extractedData.lastName,
            extractedData.suffix
        );

        if (!nameValidation.matches) {
            console.error('✗ STEP 2 FAILED: Name mismatch detected');
            showNameMismatchModal(
                nameValidation.details ? nameValidation.details.extractedName : `${extractedData.lastName}, ${extractedData.firstName} ${extractedData.middleName}`,
                nameValidation.details ? nameValidation.details.registeredName : `${registeredLastName}, ${registeredFirstName} ${registeredMiddleName}`,
                nameValidation.details || {}
            );
            return; // BLOCK - Do not proceed with data extraction
        }

        console.log('✓ STEP 2 PASSED: Name matches registered account');
        console.log('=====================================\n');

        // === STEP 3: DATA EXTRACTION & POPULATION ===
        updateFormFieldsAdvanced(
            extractedData.idNumber,
            extractedData.firstName,
            extractedData.middleName,
            extractedData.lastName,
            extractedData.birthdate,
            extractedData.sex,
            extractedData.civilStatus,
            extractedData.suffix,
            text,
            'driver-license',
            overallConfidence
        );

        // Check if it's Davao City
        const isDavaoCity = checkIfDavaoCity(extractedData.city);
        showDavaoVerificationResult(isDavaoCity, extractedData.city, 'driver-license');
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

    // Extract Driver's License Name
    function extractDriverLicenseName(lines, text) {
        let lastName = "", firstName = "", middleName = "", suffix = "";

        console.log('=== Extracting Driver\'s License Name ===');
        console.log('Total lines to search:', lines.length);
        console.log('Sample lines:', lines.slice(0, Math.min(10, lines.length)));

        // Strategy 1: Try comma format FIRST (most reliable for Driver's License)
        console.log('Trying comma format (LAST, FIRST MIDDLE)...');
        for (const line of lines) {
            if (line.includes(',') && line.length > 5) {
                const parts = line.split(',').map(p => p.trim());
                if (parts.length >= 2 && parts[0].length > 1 && parts[1].length > 1) {
                    const hasLetters = /[A-Za-z]/.test(parts[0]) && /[A-Za-z]/.test(parts[1]);
                    const notTooManyNumbers = (parts[0].match(/\d/g) || []).length < 3;

                    if (hasLetters && notTooManyNumbers) {
                        // Clean the parts first
                        const part0Clean = cleanName(parts[0]).toUpperCase();
                        const part1Clean = cleanName(parts[1]).toUpperCase();

                        // VALIDATION 1: Skip if either part is too long (likely contains full name)
                        const part0Words = part0Clean.split(/\s+/).filter(w => w.length > 0);
                        const part1Words = part1Clean.split(/\s+/).filter(w => w.length > 0);

                        if (part0Words.length > 4 || part1Words.length > 4) {
                            console.log('⚠ Skipping line: Parts contain too many words (likely malformed)');
                            console.log('  Part0 words:', part0Words.length, '| Part1 words:', part1Words.length);
                            continue;
                        }

                        // VALIDATION 2: DUPLICATION CHECK - If both parts are identical, it's duplicated
                        // Example: "Espanola Shean Andrei Yee, Espanola Shean Andrei Yee"
                        if (part0Clean === part1Clean) {
                            console.log('⚠ DUPLICATE DETECTED: Both parts identical - REJECTING this line');
                            console.log('  Duplicate line:', line);
                            console.log('  This appears to be a display artifact, not actual name format');
                            console.log('  Skipping to find proper comma-separated name...');
                            continue; // Skip this line, look for proper format
                        }

                        // VALIDATION 3: Check if part0 (should be last name) contains part1 (first name)
                        // This would indicate wrong format or malformed data
                        if (part0Clean.includes(part1Clean) || part1Clean.includes(part0Clean)) {
                            console.log('⚠ Parts contain each other - likely malformed');
                            console.log('  Part0:', part0Clean);
                            console.log('  Part1:', part1Clean);
                            continue;
                        }

                        // VALIDATION 4: Proper comma format - part0 should be 1-2 words (last name)
                        // Part1 should be 1-3 words (first + middle name, possibly suffix)
                        if (part0Words.length > 2) {
                            console.log('⚠ Last name part has too many words:', part0Words.length);
                            console.log('  This might be a full name, not just last name');
                            continue;
                        }

                        // VALID COMMA FORMAT FOUND
                        lastName = part0Clean;
                        console.log('✓ Last Name from comma format:', lastName);

                        // Parse given names part
                        const parsed = parseFirstMiddleSuffix(part1Words);
                        firstName = parsed.firstName;
                        middleName = parsed.middleName;
                        suffix = parsed.suffix;

                        console.log('✓ Successfully parsed comma format:');
                        console.log('  Last Name:', lastName);
                        console.log('  First Name:', firstName);
                        console.log('  Middle Name:', middleName);
                        if (suffix) console.log('  Suffix:', suffix);

                        break; // Found valid comma format, stop searching
                    }
                }
            }
        }

        // Strategy 2: Use independent extraction functions ONLY if comma format failed
        if (!lastName) {
            console.log('Comma format failed, trying independent field extraction...');
            const lastNameResult = extractFieldValue(lines, 'lastName');
            if (lastNameResult && lastNameResult.value) {
                const extractedLastName = cleanName(lastNameResult.value);

                // VALIDATION: Last name should be 1-2 words, not a full name
                const lastNameWords = extractedLastName.split(/\s+/).filter(w => w.length > 0);
                if (lastNameWords.length > 2) {
                    console.log(`✗ Extracted lastName contains too many words (${lastNameWords.length}):`, extractedLastName);
                    console.log('  This appears to be a full name, not just last name - REJECTING');
                    lastName = "";
                } else if (isExtractedTextALabel(extractedLastName) || extractedLastName.length < 2) {
                    console.log(`✗ Extracted lastName is invalid or a label:`, extractedLastName);
                    lastName = "";
                } else {
                    lastName = extractedLastName;
                    console.log(`✓ Last Name extracted independently via ${lastNameResult.method}:`, lastName);
                }
            }
        }

        if (!firstName) {
            console.log('Extracting first name independently...');
            const namesParsed = extractFirstName(lines);

            // VALIDATION: First name should not contain the last name
            if (namesParsed.firstName && lastName) {
                const firstNameUpper = namesParsed.firstName.toUpperCase();
                const lastNameUpper = lastName.toUpperCase();

                if (firstNameUpper.includes(lastNameUpper)) {
                    console.log('✗ First name contains last name - DATA CONTAMINATION DETECTED');
                    console.log('  First name:', namesParsed.firstName);
                    console.log('  Last name:', lastName);
                    console.log('  REJECTING to prevent duplication');
                    // Clear all extracted data - something is wrong
                    firstName = "";
                    middleName = "";
                    suffix = "";
                } else {
                    firstName = namesParsed.firstName;
                    if (!middleName) middleName = namesParsed.middleName;
                    if (!suffix) suffix = namesParsed.suffix;
                }
            } else {
                firstName = namesParsed.firstName;
                if (!middleName) middleName = namesParsed.middleName;
                if (!suffix) suffix = namesParsed.suffix;
            }
        }

        console.log('=== After extractFirstName ===');
        console.log('firstName:', firstName);
        console.log('middleName:', middleName);
        console.log('suffix:', suffix);

        // PRIORITY: ALWAYS check for separate middle name field (Driver's License may have this)
        // If a separate field exists, it takes priority over parsed middle name
        console.log('=== Checking for separate middle name field (Driver\'s License format) ===');
        const extractedMiddleName = extractMiddleName(lines);

        if (extractedMiddleName && extractedMiddleName.length >= 2) {
            console.log('✓ Found separate middle name field:', extractedMiddleName);

            // OPTIMIZATION: If separate middle name exists AND we had parsed a middle name from first name field,
            // re-parse first name to include what we thought was middle name as part of first name
            if (middleName && middleName.length >= 2) {
                console.log('⚠ Middle name was parsed from first name field, but separate field exists');
                console.log('  Re-parsing: treating all words in first name field as first name only');

                // Reconstruct full first name (including what we thought was middle name)
                const fullFirstName = firstName + ' ' + middleName;
                firstName = fullFirstName.trim();
                console.log('  Updated first name to include all words:', firstName);
            }

            // Use the separate middle name field
            middleName = extractedMiddleName;
            console.log('✓ Using middle name from separate field:', middleName);
        } else if (!middleName || middleName.length < 2) {
            console.log('✗ No separate middle name field found and no middle name parsed from first name field');
        } else {
            console.log('✓ No separate middle name field, using parsed middle name from first name field:', middleName);
        }

        // Check for separate suffix if not found
        if (!suffix) {
            console.log('=== Checking for separate suffix field ===');
            suffix = extractSuffix(lines);
            if (suffix) {
                console.log('✓ Suffix found via independent extraction:', suffix);
            }
        }

        console.log('=== Driver\'s License Name Extraction Complete ===');
        console.log('BEFORE FINAL VALIDATION:');
        console.log('  Last Name: "' + lastName + '"');
        console.log('  First Name: "' + firstName + '"');
        console.log('  Middle Name: "' + middleName + '"');
        console.log('  Suffix: "' + suffix + '"');

        // === FINAL VALIDATION: Prevent cross-contamination ===
        console.log('=== FINAL VALIDATION: Checking for data contamination ===');

        // Check 1: Ensure lastName doesn't contain firstName or vice versa
        if (lastName && firstName) {
            const lastNameUpper = lastName.toUpperCase();
            const firstNameUpper = firstName.toUpperCase();

            if (lastNameUpper.includes(firstNameUpper) || firstNameUpper.includes(lastNameUpper)) {
                console.log('✗ CRITICAL ERROR: lastName and firstName contain each other');
                console.log('  This indicates extraction failure - clearing all data');
                lastName = "";
                firstName = "";
                middleName = "";
                suffix = "";
            }
        }

        // Check 2: Ensure lastName is not too long (max 2 words for compound last names)
        if (lastName) {
            const lastNameWords = lastName.split(/\s+/).filter(w => w.length > 0);
            if (lastNameWords.length > 2) {
                console.log('✗ CRITICAL ERROR: lastName has too many words:', lastNameWords.length);
                console.log('  This appears to be a full name, not just last name');
                lastName = "";
            }
        }

        // Check 3: Ensure firstName + middleName + lastName don't form a duplicated pattern
        if (firstName && lastName) {
            const fullName = `${firstName} ${middleName} ${lastName}`.trim().toUpperCase();
            const words = fullName.split(/\s+/);

            // Check if we have duplicated name patterns (like "SHEAN ANDREI YEE SHEAN ANDREI YEE")
            if (words.length >= 4) {
                const half = Math.floor(words.length / 2);
                const firstHalf = words.slice(0, half).join(' ');
                const secondHalf = words.slice(half).join(' ');

                if (firstHalf === secondHalf) {
                    console.log('✗ CRITICAL ERROR: Duplicate pattern detected in full name');
                    console.log('  First half:', firstHalf);
                    console.log('  Second half:', secondHalf);
                    console.log('  Clearing all data to prevent duplication');
                    lastName = "";
                    firstName = "";
                    middleName = "";
                    suffix = "";
                }
            }
        }

        console.log('=== FINAL VALIDATED Results ===');
        console.log('  Last Name: "' + lastName + '"');
        console.log('  First Name: "' + firstName + '"');
        console.log('  Middle Name: "' + middleName + '"');
        console.log('  Suffix: "' + suffix + '"');
        console.log('');
        return { lastName, firstName, middleName, suffix };
    }

    function extractBirthdate(lines, idType = 'generic') {
        console.log(`=== Extracting Birthdate (${idType}) ===`);

        // Method 1: Label-value extraction (PRIORITY)
        // Only extract if "Birthdate" or "Date of Birth" label exists
        const labelResult = extractFieldValue(lines, 'birthdate');
        if (labelResult && labelResult.value) {
            let dateValue = labelResult.value.trim();
            console.log(`✓ Found birthdate via label (${labelResult.method}):`, dateValue);

            // ===================================================================
            // ENHANCED BIRTHDATE FORMAT NORMALIZATION
            // Normalize all formats to yyyy-mm-dd (required by HTML5 date input)
            // Supports text-based dates: "December 25, 2001", "Dec 25, 2001", etc.
            // ===================================================================

            // Format 1: MM/DD/YYYY (American format) → Convert to yyyy-mm-dd
            let match = dateValue.match(/\b(\d{1,2})\/(\d{1,2})\/(\d{4})\b/);
            if (match) {
                const [_, month, day, year] = match;
                const normalized = `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
                console.log('  Converted MM/DD/YYYY → yyyy-mm-dd:', normalized);
                return normalized;
            }

            // Format 2: YYYY-MM-DD (ISO format - already correct) → Normalize with padding
            match = dateValue.match(/\b(\d{4})-(\d{1,2})-(\d{1,2})\b/);
            if (match) {
                const [_, year, month, day] = match;
                const normalized = `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
                console.log('  Normalized yyyy-mm-dd:', normalized);
                return normalized;
            }

            // Format 3: DD-MM-YYYY (European with dashes) → Convert to yyyy-mm-dd
            match = dateValue.match(/\b(\d{1,2})-(\d{1,2})-(\d{4})\b/);
            if (match) {
                const [_, part1, part2, year] = match;
                // If part1 > 12, it's DD-MM-YYYY; otherwise ambiguous (assume MM-DD-YYYY)
                if (parseInt(part1) > 12) {
                    const normalized = `${year}-${part2.padStart(2, '0')}-${part1.padStart(2, '0')}`;
                    console.log('  Converted DD-MM-YYYY → yyyy-mm-dd:', normalized);
                    return normalized;
                } else {
                    const normalized = `${year}-${part1.padStart(2, '0')}-${part2.padStart(2, '0')}`;
                    console.log('  Converted MM-DD-YYYY → yyyy-mm-dd:', normalized);
                    return normalized;
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
                    console.log(`  Converted "${monthName} ${day}, ${year}" → yyyy-mm-dd:`, normalized);
                    return normalized;
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
                    console.log(`  Converted "${day} ${monthName} ${year}" → yyyy-mm-dd:`, normalized);
                    return normalized;
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
                    console.log(`  Converted "${day} ${filMonth} ${year}" → yyyy-mm-dd:`, normalized);
                    return normalized;
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
                            console.log(`  Converted "${dateValue}" → yyyy-mm-dd:`, normalized);
                            return normalized;
                        }
                    }
                }
            }

            // Format 8: Simple YYYYMMDD (no separators) → Convert to yyyy-mm-dd
            match = dateValue.match(/\b(\d{4})(\d{2})(\d{2})\b/);
            if (match) {
                const [_, year, month, day] = match;
                const normalized = `${year}-${month}-${day}`;
                console.log('  Converted YYYYMMDD → yyyy-mm-dd:', normalized);
                return normalized;
            }

            console.log('  Warning: Date format not recognized, returning as-is:', dateValue);
            return dateValue;
        }

        // Method 2: Pattern matching fallback ONLY if label was found but value was on different line
        // This prevents extracting random dates that aren't birthdate
        console.log('✗ No birthdate label found - skipping pattern matching');
        console.log('  (Birthdate requires "Date of Birth" or "Birthdate" indicator on ID)');
        return "";
    }

    // Driver's License Birthdate - uses unified function
    function extractDriverLicenseBirthdate(lines) {
        return extractBirthdate(lines, 'Driver\'s License');
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

        console.log('\n========================================');
        console.log('=== NATIONAL ID FULL OCR TEXT ===');
        console.log('========================================');
        console.log('Total Lines:', lines.length);
        console.log('All Lines:');
        lines.forEach((line, index) => {
            console.log(`  ${index + 1}: "${line}"`);
        });
        console.log('========================================\n');

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

        // Extract city for Davao verification
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
            console.error('✗ EXTRACTION FAILED: Labels detected as values!');
            console.error('Issues:', dataValidation.issues);

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
            return; // STOP - Do not proceed with invalid data
        }

        // === STEP 2: NAME MATCHING VALIDATION (CRITICAL) ===
        console.log('=== STEP 2: NAME MATCHING VALIDATION ===');
        const nameValidation = validateNameMatch(
            extractedData.firstName,
            extractedData.middleName,
            extractedData.lastName,
            extractedData.suffix
        );

        if (!nameValidation.matches) {
            console.error('✗ STEP 2 FAILED: Name mismatch detected');
            showNameMismatchModal(
                nameValidation.details ? nameValidation.details.extractedName : `${extractedData.lastName}, ${extractedData.firstName} ${extractedData.middleName}`,
                nameValidation.details ? nameValidation.details.registeredName : `${registeredLastName}, ${registeredFirstName} ${registeredMiddleName}`,
                nameValidation.details || {}
            );
            return; // BLOCK - Do not proceed with data extraction
        }

        console.log('✓ STEP 2 PASSED: Name matches registered account');
        console.log('=====================================\n');

        // === STEP 3: DATA EXTRACTION & POPULATION ===
        updateFormFieldsAdvanced(
            extractedData.idNumber,
            extractedData.firstName,
            extractedData.middleName,
            extractedData.lastName,
            extractedData.birthdate,
            "",
            extractedData.civilStatus,
            extractedData.suffix,
            text,
            'phil-id',
            overallConfidence
        );

        // Check if it's Davao City for National ID
        const isDavaoCity = checkIfDavaoCity(extractedData.city);
        showDavaoVerificationResult(isDavaoCity, extractedData.city, 'national-id');
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

        console.log('=== Extracting PhilSys Name ===');
        console.log('Total lines to search:', lines.length);
        console.log('Sample lines:', lines.slice(0, Math.min(10, lines.length)));

        // Use independent extraction functions (works like lastName extraction)
        const lastNameResult = extractFieldValue(lines, 'lastName');
        if (lastNameResult && lastNameResult.value) {
            lastName = cleanName(lastNameResult.value);

            // Validate that lastName is not a label
            if (isExtractedTextALabel(lastName) || lastName.length < 2) {
                console.log(`✗ Extracted lastName is invalid or a label:`, lastName);
                lastName = "";
            } else {
                console.log(`✓ Last Name extracted independently via ${lastNameResult.method}:`, lastName);
            }
        }

        const namesParsed = extractFirstName(lines);
        firstName = namesParsed.firstName;
        middleName = namesParsed.middleName;
        suffix = namesParsed.suffix;

        console.log('=== After extractFirstName ===');
        console.log('firstName:', firstName);
        console.log('middleName (from first name field):', middleName);
        console.log('suffix:', suffix);

        // PRIORITY: ALWAYS check for separate middle name field (National ID often has this)
        // If a separate field exists, it takes priority over parsed middle name
        console.log('=== Checking for separate middle name field (National ID format) ===');
        const extractedMiddleName = extractMiddleName(lines);

        if (extractedMiddleName && extractedMiddleName.length >= 2) {
            console.log('✓ Found separate middle name field:', extractedMiddleName);

            // OPTIMIZATION: If separate middle name exists AND we had parsed a middle name from first name field,
            // re-parse first name to include what we thought was middle name as part of first name
            if (middleName && middleName.length >= 2) {
                console.log('⚠ Middle name was parsed from first name field, but separate field exists');
                console.log('  Re-parsing: treating all words in first name field as first name only');

                // Reconstruct full first name (including what we thought was middle name)
                const fullFirstName = firstName + ' ' + middleName;
                firstName = fullFirstName.trim();
                console.log('  Updated first name to include all words:', firstName);
            }

            // Use the separate middle name field
            middleName = extractedMiddleName;
            console.log('✓ Using middle name from separate field:', middleName);
        } else if (!middleName || middleName.length < 2) {
            console.log('✗ No separate middle name field found and no middle name parsed from first name field');
        } else {
            console.log('✓ No separate middle name field, using parsed middle name from first name field:', middleName);
        }

        // Check for separate suffix if not found
        if (!suffix) {
            console.log('=== Checking for separate suffix field ===');
            suffix = extractSuffix(lines);
            if (suffix) {
                console.log('✓ Suffix found via independent extraction:', suffix);
            }
        }

        console.log('=== PhilSys Name Extraction Complete ===');
        console.log('FINAL Results:');
        console.log('  Last Name: "' + lastName + '"');
        console.log('  First Name: "' + firstName + '"');
        console.log('  Middle Name: "' + middleName + '"');
        console.log('  Suffix: "' + suffix + '"');
        console.log('');
        return { lastName, firstName, middleName, suffix };
    }

    // Extract PhilSys Birthdate
    // PhilSys Birthdate - uses unified function
    function extractPhilSysBirthdate(lines) {
        return extractBirthdate(lines, 'National ID (PhilSys)');
    }

    // Extract PhilSys City - uses unified function
    function extractPhilSysCity(lines) {
        return extractDavaoCity(lines);
    }

    // Parse PhilSys ID Back - Extract gender AND civil status
    function parsePhilSysBack(text) {
        const lines = text.split('\n').map(line => line.trim()).filter(line => line);

        console.log('\n========================================');
        console.log('=== NATIONAL ID BACK - FULL OCR TEXT ===');
        console.log('========================================');
        console.log('Total Lines:', lines.length);
        console.log('All Lines:');
        lines.forEach((line, index) => {
            console.log(`  ${index + 1}: "${line}"`);
        });
        console.log('========================================\n');

        let extractedFields = [];

        // Extract gender/sex using unified function
        const sex = extractGender(lines);
        if (sex) {
            console.log('✓ Gender extracted from back:', sex);
            const genderField = document.getElementById('gender') || document.getElementById('sex');
            if (genderField) genderField.value = sex;
            extractedFields.push('Gender');
        }

        // Extract civil status/marital status
        const civilStatus = extractCivilStatus(lines);
        if (civilStatus) {
            console.log('✓ Civil Status extracted from back:', civilStatus);
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
        if (extractedFields.length > 0) {
            resultBox.classList.remove('hidden');
            resultBox.className = "p-4 bg-blue-50 border border-blue-200 rounded-xl text-sm text-blue-700 slide-down";
            resultBox.innerHTML = `<i class="fas fa-info-circle mr-2"></i>${extractedFields.join(' and ')} extracted from National ID back. Please verify.`;
        }
    }

    // Extract PhilSys Gender (uses unified extractGender function)
    // Note: This function is kept for backward compatibility but
    // PhilSys back parsing now uses extractGender() directly
    function extractPhilSysGender(lines) {
        return extractGender(lines);
    }

    // Parse SSS ID Front with precision extraction
    function parseSSSFront(text) {
        const lines = text.split('\n').map(line => line.trim()).filter(line => line);

        debugLog('SSS-PARSING', { totalLines: lines.length, lines });

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
            confidence: {}
        };

        // Extract SSS Number
        extractedData.idNumber = extractSSSNumber(lines);
        if (extractedData.idNumber) {
            const validation = validateIDNumber(extractedData.idNumber, 'sss-id');
            extractedData.confidence.idNumber = validation.confidence;
        }

        // Extract names
        const nameResult = extractSSSName(lines, text);
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
        extractedData.birthdate = extractSSSBirthdate(lines);
        if (extractedData.birthdate) {
            const validation = validateBirthdate(extractedData.birthdate);
            extractedData.confidence.birthdate = validation.confidence;
        }

        // Extract gender using unified function with label matching
        extractedData.sex = extractGender(lines);
        extractedData.confidence.sex = extractedData.sex ? 90 : 0;

        // Extract city
        extractedData.city = extractSSSCity(lines);

        // Store extracted data for debugging
        lastExtractedData = extractedData;
        debugLog('SSS-EXTRACTED', extractedData);

        // Calculate overall confidence
        const confidenceValues = Object.values(extractedData.confidence).filter(v => v > 0);
        const overallConfidence = confidenceValues.length > 0
            ? Math.round(confidenceValues.reduce((a, b) => a + b, 0) / confidenceValues.length)
            : 0;

        // Log extracted data in labeled format
        logExtractedData(extractedData, 'sss-id', overallConfidence);

        // === STEP 1.5: VALIDATE EXTRACTED DATA (NO LABELS AS VALUES) ===
        const dataValidation = validateExtractedNames(extractedData);
        if (!dataValidation.valid) {
            console.error('✗ EXTRACTION FAILED: Labels detected as values!');
            console.error('Issues:', dataValidation.issues);

            resultBox.classList.remove('hidden');
            resultBox.className = "p-4 bg-red-50 border border-red-200 rounded-xl text-sm text-red-700";
            resultBox.innerHTML = `
                <i class="fas fa-exclamation-triangle mr-2"></i>
                <strong>Extraction Error:</strong> Cannot read SSS ID data properly.
                <br><small>The following issues were detected:</small>
                <ul class="mt-2 ml-4 list-disc">
                    ${dataValidation.issues.map(issue => `<li>${issue}</li>`).join('')}
                </ul>
                <small class="block mt-2">Please ensure the ID image is clear and well-lit, then try again.</small>
            `;
            return; // STOP - Do not proceed with invalid data
        }

        // === STEP 2: NAME MATCHING VALIDATION (CRITICAL) ===
        console.log('=== STEP 2: NAME MATCHING VALIDATION ===');
        const nameValidation = validateNameMatch(
            extractedData.firstName,
            extractedData.middleName,
            extractedData.lastName,
            extractedData.suffix
        );

        if (!nameValidation.matches) {
            console.error('✗ STEP 2 FAILED: Name mismatch detected');
            showNameMismatchModal(
                nameValidation.details ? nameValidation.details.extractedName : `${extractedData.lastName}, ${extractedData.firstName} ${extractedData.middleName}`,
                nameValidation.details ? nameValidation.details.registeredName : `${registeredLastName}, ${registeredFirstName} ${registeredMiddleName}`,
                nameValidation.details || {}
            );
            return; // BLOCK - Do not proceed with data extraction
        }

        console.log('✓ STEP 2 PASSED: Name matches registered account');
        console.log('=====================================\n');

        // === STEP 3: DATA EXTRACTION & POPULATION ===
        updateFormFieldsAdvanced(
            extractedData.idNumber,
            extractedData.firstName,
            extractedData.middleName,
            extractedData.lastName,
            extractedData.birthdate,
            extractedData.sex,
            extractedData.civilStatus,
            extractedData.suffix,
            text,
            'sss-id',
            overallConfidence
        );

        // Check if it's Davao City
        const isDavaoCity = checkIfDavaoCity(extractedData.city);
        showDavaoVerificationResult(isDavaoCity, extractedData.city, 'sss-id');
    }

    // Extract SSS Number
    function extractSSSNumber(lines) {
        // Look for SSS number pattern (XX-XXXXXXX-X)
        for (const line of lines) {
            const sssPattern = /\d{2}-\d{7}-\d{1}/;
            const match = line.match(sssPattern);
            if (match) {
                return match[0];
            }
        }
        return "";
    }

    // Extract SSS Name
    function extractSSSName(lines, text) {
        let lastName = "", firstName = "", middleName = "", suffix = "";

        console.log('=== Extracting SSS Name ===');
        console.log('Total lines to search:', lines.length);
        console.log('Sample lines:', lines.slice(0, Math.min(10, lines.length)));

        // Use independent extraction functions (works like lastName extraction)
        const lastNameResult = extractFieldValue(lines, 'lastName');
        if (lastNameResult && lastNameResult.value) {
            lastName = cleanName(lastNameResult.value);

            // Validate that lastName is not a label
            if (isExtractedTextALabel(lastName) || lastName.length < 2) {
                console.log(`✗ Extracted lastName is invalid or a label:`, lastName);
                lastName = "";
            } else {
                console.log(`✓ Last Name extracted independently via ${lastNameResult.method}:`, lastName);
            }
        }

        const namesParsed = extractFirstName(lines);
        firstName = namesParsed.firstName;
        middleName = namesParsed.middleName;
        suffix = namesParsed.suffix;

        console.log('=== After extractFirstName ===');
        console.log('firstName:', firstName);
        console.log('middleName (from first name field):', middleName);
        console.log('suffix:', suffix);

        // PRIORITY: ALWAYS check for separate middle name field (SSS ID may have this)
        // If a separate field exists, it takes priority over parsed middle name
        console.log('=== Checking for separate middle name field (SSS ID format) ===');
        const extractedMiddleName = extractMiddleName(lines);

        if (extractedMiddleName && extractedMiddleName.length >= 2) {
            console.log('✓ Found separate middle name field:', extractedMiddleName);

            // OPTIMIZATION: If separate middle name exists AND we had parsed a middle name from first name field,
            // re-parse first name to include what we thought was middle name as part of first name
            if (middleName && middleName.length >= 2) {
                console.log('⚠ Middle name was parsed from first name field, but separate field exists');
                console.log('  Re-parsing: treating all words in first name field as first name only');

                // Reconstruct full first name (including what we thought was middle name)
                const fullFirstName = firstName + ' ' + middleName;
                firstName = fullFirstName.trim();
                console.log('  Updated first name to include all words:', firstName);
            }

            // Use the separate middle name field
            middleName = extractedMiddleName;
            console.log('✓ Using middle name from separate field:', middleName);
        } else if (!middleName || middleName.length < 2) {
            console.log('✗ No separate middle name field found and no middle name parsed from first name field');
        } else {
            console.log('✓ No separate middle name field, using parsed middle name from first name field:', middleName);
        }

        // Check for separate suffix if not found
        if (!suffix) {
            console.log('=== Checking for separate suffix field ===');
            suffix = extractSuffix(lines);
            if (suffix) {
                console.log('✓ Suffix found via independent extraction:', suffix);
            }
        }

        // Fallback - Sequential extraction (for SSS IDs without labels)
        if (!lastName || !firstName) {
            console.log('Label extraction incomplete, trying sequential extraction...');
            const nameLines = [];
            for (let i = 0; i < lines.length; i++) {
                const line = lines[i];
                // Look for lines that appear to be names (no numbers, reasonable length)
                if (line.length > 2 && line.length < 40 && !/\d/.test(line) && !/ID|SSS|SOCIAL|SECURITY|NUMBER|SYSTEM/.test(line.toUpperCase())) {
                    nameLines.push(line);
                    if (nameLines.length >= 3) break;
                }
            }

            if (!lastName && nameLines.length >= 1) {
                lastName = cleanName(nameLines[0]);
                console.log('✓ Sequential - Last Name:', lastName);
            }
            if (!firstName && nameLines.length >= 2) {
                const givenNamesRaw = cleanName(nameLines[1]);
                console.log('✓ Sequential - Given Names:', givenNamesRaw);

                const words = givenNamesRaw.split(/\s+/).filter(w => w.length > 0);
                if (words.length > 0) {
                    const parsed = parseFirstMiddleSuffix(words);
                    firstName = parsed.firstName;
                    if (!middleName) middleName = parsed.middleName;
                    if (!suffix) suffix = parsed.suffix;
                    console.log('  Parsed - First Name:', firstName);
                    console.log('  Parsed - Middle Name:', middleName);
                    if (suffix) console.log('  Parsed - Suffix:', suffix);
                }
            }
        }

        console.log('=== SSS Name Extraction Complete ===');
        console.log('FINAL Results:');
        console.log('  Last Name: "' + lastName + '"');
        console.log('  First Name: "' + firstName + '"');
        console.log('  Middle Name: "' + middleName + '"');
        console.log('  Suffix: "' + suffix + '"');
        console.log('');
        return { lastName, firstName, middleName, suffix };
    }

    // Extract SSS Birthdate
    // SSS Birthdate - uses unified function
    function extractSSSBirthdate(lines) {
        return extractBirthdate(lines, 'SSS ID');
    }

    // Extract SSS Gender (uses unified extractGender function)
    // Note: This function is kept for backward compatibility but
    // SSS parsing now uses extractGender() directly
    function extractSSSGender(lines) {
        return extractGender(lines);
    }

    // Extract SSS City
    // Unified city extraction for ALL ID types (Driver's License, National ID, SSS, UMID)
    // STRICT: Only accepts specific Davao City keywords as complete phrases
    // LOGIC: If "Davao City" keyword exists, accept (even if "Davao del Sur" also exists)
    //        If only "Davao del Sur" exists (without Davao City keyword), reject
    function extractDavaoCity(lines) {
        console.log('=== Extracting Davao City with STRICT keyword matching ===');

        // STRICT KEYWORDS: Only these exact variations will return "Davao City"
        // Case-insensitive matching
        const davaoCityKeywords = [
            'DAVAO CITY',
            'CITY OF DAVAO',
            'DVO CITY'
        ];

        // REJECTION KEYWORDS: These should NOT be accepted as Davao City
        // BUT: If line contains BOTH Davao City keyword AND rejection keyword, ACCEPT it
        // Example: "Davao City Davao del Sur" → ACCEPT (Davao City is explicitly mentioned)
        // Example: "Digos City Davao del Sur" → REJECT (no Davao City keyword)
        const rejectionKeywords = [
            'DAVAO DEL SUR',
            'DAVAO DEL NORTE',
            'DAVAO ORIENTAL',
            'DAVAO REGION',
            'DAVAO DE ORO'
        ];

        // Search through ALL lines for Davao City keywords as complete phrases
        for (let i = 0; i < lines.length; i++) {
            const line = lines[i].trim();
            const upper = line.toUpperCase();

            // Skip empty lines
            if (!line || line.length < 3) continue;

            // Skip lines with ID-specific indicators (to avoid detecting keywords in ID type labels)
            if (upper.includes('LICENSE') || upper.includes('DRIVER') ||
                upper.includes('SSS') || upper.includes('UMID') ||
                upper.includes('PHILSYS') || upper.includes('NATIONAL ID')) {
                continue;
            }

            console.log(`Checking line ${i + 1}: "${line}"`);

            // PRIORITY 1: Check for Davao City keywords FIRST
            // If Davao City keyword exists, accept immediately (even if rejection keyword also exists)
            let hasDavaoCityKeyword = false;
            let foundKeyword = '';

            for (const keyword of davaoCityKeywords) {
                if (upper.includes(keyword)) {
                    hasDavaoCityKeyword = true;
                    foundKeyword = keyword;
                    break;
                }
            }

            if (hasDavaoCityKeyword) {
                // Check if rejection keyword also exists
                let hasRejectionKeyword = false;
                let rejectionFound = '';

                for (const rejection of rejectionKeywords) {
                    if (upper.includes(rejection)) {
                        hasRejectionKeyword = true;
                        rejectionFound = rejection;
                        break;
                    }
                }

                if (hasRejectionKeyword) {
                    console.log(`  ℹ️ Contains both "${foundKeyword}" AND "${rejectionFound}"`);
                    console.log(`  ✓✓✓ ACCEPTED: Davao City explicitly mentioned (province/region is just context)`);
                } else {
                    console.log(`  ✓✓✓ FOUND Davao City keyword: "${foundKeyword}"`);
                }

                console.log(`  Line content: "${line}"`);
                return "Davao City";
            }

            // PRIORITY 2: If NO Davao City keyword, check for rejection keywords
            // If rejection keyword exists without Davao City keyword, skip this line
            for (const rejection of rejectionKeywords) {
                if (upper.includes(rejection)) {
                    console.log(`  ✗ REJECTED: Contains "${rejection}" but no Davao City keyword`);
                    console.log(`  (This is not Davao City - it's another municipality/city in the province)`);
                    break; // Skip this line
                }
            }
        }

        console.log('✗ Davao City not detected (no matching keywords found)');
        return "Not detected";
    }

    // Legacy function - now uses unified extractDavaoCity
    function extractCityForSSSAndUMID(lines) {
        return extractDavaoCity(lines);
    }

    // SSS City extraction - uses unified function
    function extractSSSCity(lines) {
        return extractDavaoCity(lines);
    }

    // Parse UMID Front with precision extraction
    function parseUMIDFront(text) {
        const lines = text.split('\n').map(line => line.trim()).filter(line => line);

        console.log('\n=== PARSING UMID (CRN) ===');
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

        // Extract birthdate
        extractedData.birthdate = extractUMIDBirthdate(lines);
        if (extractedData.birthdate) {
            const validation = validateBirthdate(extractedData.birthdate);
            extractedData.confidence.birthdate = validation.confidence;
        }

        // Extract gender using unified function with label matching (same as SSS)
        extractedData.sex = extractGender(lines);
        extractedData.confidence.sex = extractedData.sex ? 90 : 0;

        // Extract city
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
            console.error('✗ EXTRACTION FAILED: Labels detected as values!');
            console.error('Issues:', dataValidation.issues);

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
            return; // STOP - Do not proceed with invalid data
        }

        // === STEP 2: NAME MATCHING VALIDATION (CRITICAL) ===
        console.log('=== STEP 2: NAME MATCHING VALIDATION ===');
        const nameValidation = validateNameMatch(
            extractedData.firstName,
            extractedData.middleName,
            extractedData.lastName,
            extractedData.suffix
        );

        if (!nameValidation.matches) {
            console.error('✗ STEP 2 FAILED: Name mismatch detected');
            showNameMismatchModal(
                nameValidation.details ? nameValidation.details.extractedName : `${extractedData.lastName}, ${extractedData.firstName} ${extractedData.middleName}`,
                nameValidation.details ? nameValidation.details.registeredName : `${registeredLastName}, ${registeredFirstName} ${registeredMiddleName}`,
                nameValidation.details || {}
            );
            return; // BLOCK - Do not proceed with data extraction
        }

        console.log('✓ STEP 2 PASSED: Name matches registered account');
        console.log('=====================================\n');

        // === STEP 3: DATA EXTRACTION & POPULATION ===
        updateFormFieldsAdvanced(
            extractedData.idNumber,
            extractedData.firstName,
            extractedData.middleName,
            extractedData.lastName,
            extractedData.birthdate,
            extractedData.sex,
            extractedData.civilStatus,
            extractedData.suffix,
            text,
            'umid',
            overallConfidence
        );

        // Check if it's Davao City
        const isDavaoCity = checkIfDavaoCity(extractedData.city);
        showDavaoVerificationResult(isDavaoCity, extractedData.city, 'umid');
    }

    // Extract UMID Number (CRN - Common Reference Number)
    function extractUMIDNumber(lines) {
        // Look for UMID CRN pattern: 0000-0000000-0
        // CRN = Common Reference Number (the ID number on UMID)
        for (const line of lines) {
            const umidPattern = /\d{4}-\d{7}-\d{1}/;
            const match = line.match(umidPattern);
            if (match) {
                console.log('Detected UMID CRN:', match[0]);
                return match[0];
            }
        }
        return "";
    }

    // Extract UMID Name - Enhanced with label-value pairing and compound name detection
    function extractUMIDName(lines, text) {
        let lastName = "", firstName = "", middleName = "", suffix = "";

        console.log('=== Extracting UMID Name ===');
        console.log('Total lines to search:', lines.length);
        console.log('Sample lines:', lines.slice(0, Math.min(10, lines.length)));

        // Use independent extraction functions (works like lastName extraction)
        const lastNameResult = extractFieldValue(lines, 'lastName');
        if (lastNameResult && lastNameResult.value) {
            lastName = cleanName(lastNameResult.value);

            // Validate that lastName is not a label
            if (isExtractedTextALabel(lastName) || lastName.length < 2) {
                console.log(`✗ Extracted lastName is invalid or a label:`, lastName);
                lastName = "";
            } else {
                console.log(`✓ Last Name extracted independently via ${lastNameResult.method}:`, lastName);
            }
        }

        const namesParsed = extractFirstName(lines);
        firstName = namesParsed.firstName;
        middleName = namesParsed.middleName;
        suffix = namesParsed.suffix;

        console.log('=== After extractFirstName ===');
        console.log('firstName:', firstName);
        console.log('middleName (from first name field):', middleName);
        console.log('suffix:', suffix);

        // PRIORITY: ALWAYS check for separate middle name field (UMID may have this)
        // If a separate field exists, it takes priority over parsed middle name
        console.log('=== Checking for separate middle name field (UMID format) ===');
        const extractedMiddleName = extractMiddleName(lines);

        if (extractedMiddleName && extractedMiddleName.length >= 2) {
            console.log('✓ Found separate middle name field:', extractedMiddleName);

            // OPTIMIZATION: If separate middle name exists AND we had parsed a middle name from first name field,
            // re-parse first name to include what we thought was middle name as part of first name
            if (middleName && middleName.length >= 2) {
                console.log('⚠ Middle name was parsed from first name field, but separate field exists');
                console.log('  Re-parsing: treating all words in first name field as first name only');

                // Reconstruct full first name (including what we thought was middle name)
                const fullFirstName = firstName + ' ' + middleName;
                firstName = fullFirstName.trim();
                console.log('  Updated first name to include all words:', firstName);
            }

            // Use the separate middle name field
            middleName = extractedMiddleName;
            console.log('✓ Using middle name from separate field:', middleName);
        } else if (!middleName || middleName.length < 2) {
            console.log('✗ No separate middle name field found and no middle name parsed from first name field');
        } else {
            console.log('✓ No separate middle name field, using parsed middle name from first name field:', middleName);
        }

        // Check for separate suffix if not found
        if (!suffix) {
            console.log('=== Checking for separate suffix field ===');
            suffix = extractSuffix(lines);
            if (suffix) {
                console.log('✓ Suffix found via independent extraction:', suffix);
            }
        }

        // Strategy 4: Fallback - Sequential extraction (for UMIDs without labels)
        if (!lastName || !firstName) {
            console.log('Label extraction incomplete, trying sequential extraction...');

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
                    console.log('Potential name line:', line);
                }
            }

            // UMID typically displays: Last Name, First Name + Middle Name
            if (nameLines.length >= 1 && !lastName) {
                lastName = nameLines[0];
                console.log('Extracted Last Name (sequential):', lastName);
            }

            if (nameLines.length >= 2 && !firstName) {
                // Second line might be "FIRST MIDDLE" - use smart parser
                const words = nameLines[1].split(/\s+/);
                const parsed = parseFirstMiddleSuffix(words);
                firstName = parsed.firstName;
                if (!middleName) middleName = parsed.middleName;
                if (!suffix) suffix = parsed.suffix;
                console.log('✓ Sequential - First Name:', firstName);
                console.log('✓ Sequential - Middle Name:', middleName);
                if (suffix) console.log('✓ Sequential - Suffix:', suffix);
            }
        }

        console.log('=== UMID Name Extraction Complete ===');
        console.log('FINAL Results:');
        console.log('  Last Name: "' + lastName + '"');
        console.log('  First Name: "' + firstName + '"');
        console.log('  Middle Name: "' + middleName + '"');
        console.log('  Suffix: "' + suffix + '"');
        console.log('');
        return { lastName, firstName, middleName, suffix };
    }

    // Extract UMID Birthdate - Using label-value extraction
    // UMID Birthdate - uses unified function
    function extractUMIDBirthdate(lines) {
        return extractBirthdate(lines, 'UMID');
    }

    // Extract UMID City - Using label-value extraction
    // UMID City extraction - uses unified function
    function extractUMIDCity(lines) {
        return extractDavaoCity(lines);
    }

    // Parse UMID Back
    function parseUMIDBack(text) {
        const lines = text.split('\n').map(line => line.trim()).filter(line => line);

        // Use unified gender extraction function (same as SSS)
        const sex = extractGender(lines);

        if (sex) {
            const genderField = document.getElementById('gender') || document.getElementById('sex');
            if (genderField) genderField.value = sex;

            resultBox.classList.remove('hidden');
            resultBox.className = "p-4 bg-blue-50 border border-blue-200 rounded-xl text-sm text-blue-700 slide-down";
            resultBox.innerHTML = '<i class="fas fa-info-circle mr-2"></i>Gender information extracted from UMID back. Please verify.';
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
        console.log('Extracting gender from UMID using label-value system...');

        // Method 1: Label-value extraction (PREFERRED)
        const labelResult = extractFieldValue(lines, 'gender');
        if (labelResult && labelResult.value) {
            let genderValue = labelResult.value;
            console.log(`✓ Found gender via label (${labelResult.method}):`, genderValue);

            // Normalize gender value
            const upperGender = genderValue.toUpperCase().trim();

            // Handle single letter codes
            if (upperGender === 'M' || upperGender === 'MALE' || upperGender.includes('MALE')) {
                console.log('  Normalized to: Male');
                return "Male";
            }
            if (upperGender === 'F' || upperGender === 'FEMALE' || upperGender.includes('FEMALE')) {
                console.log('  Normalized to: Female');
                return "Female";
            }

            // Handle Filipino terms
            if (upperGender.includes('LALAKI') || upperGender.includes('LALAKE')) {
                console.log('  Normalized to: Male (from Filipino)');
                return "Male";
            }
            if (upperGender.includes('BABAE')) {
                console.log('  Normalized to: Female (from Filipino)');
                return "Female";
            }

            return genderValue;
        }

        // Method 2: Fallback pattern matching (if label not found)
        console.log('Label-value extraction failed, trying pattern matching...');
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
                console.log('✓ Found gender: Female (standalone pattern)');
                return "Female";
            }
            if (line.trim().toUpperCase() === 'MALE') {
                console.log('✓ Found gender: Male (standalone pattern)');
                return "Male";
            }

            // Pattern 2: Pattern "SEX: MALE" or "GENDER: FEMALE"
            const genderMatch = line.match(/(SEX|GENDER|KASARIAN)\s*:?\s*(MALE|FEMALE|M|F|LALAKI|BABAE)/i);
            if (genderMatch) {
                const gender = genderMatch[2].toUpperCase();
                if (gender === 'MALE' || gender === 'M' || gender === 'LALAKI') {
                    console.log('✓ Found gender: Male (pattern match)');
                    return "Male";
                }
                if (gender === 'FEMALE' || gender === 'F' || gender === 'BABAE') {
                    console.log('✓ Found gender: Female (pattern match)');
                    return "Female";
                }
            }
        }

        console.log('✗ Gender not detected');
        return "";
    }

    // Check if city is Davao City
    function checkIfDavaoCity(city) {
        if (!city || city === "Not detected") return false;

        const davaoPatterns = [
            'DAVAO CITY',
            'CITY OF DAVAO',
            'DAVAO',
            'DVO CITY'
        ];

        const normalizedCity = city.toUpperCase().trim();

        for (const pattern of davaoPatterns) {
            if (normalizedCity.includes(pattern)) {
                return true;
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
            'sss-id': 'SSS ID',
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

            console.log('✓ DAVAO CITY verification PASSED');

        } else {
            // ✗ INVALID: NON-Davao City ID - BLOCK with modal
            console.error('✗ NON-DAVAO CITY detected - BLOCKING application');

            if (davaoVerification) {
                davaoVerification.classList.remove('hidden');
                davaoVerification.className = 'p-6 rounded-xl border-l-4 bg-red-50 border-red-200 slide-down visible';

                if (davaoResultIcon) davaoResultIcon.className = 'fas fa-times-circle text-2xl mt-1 text-red-500';
                if (davaoResultTitle) davaoResultTitle.textContent = `✗ ${idTypeName} Not From Davao City`;
                if (davaoResultMessage) {
                    const locationText = detectedCity !== "Not detected"
                        ? `Detected location: ${detectedCity}`
                        : 'City could not be detected on the ID';
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
            console.error('Davao verification modal not found');
            alert(`VERIFICATION BLOCKED\n\nThis service is only available for Davao City residents.\n\nDetected location: ${detectedCity}\n\nYou cannot proceed with account verification.`);
            return;
        }

        // Update modal content
        const titleElement = modal.querySelector('.modal-title');
        const bodyElement = modal.querySelector('.modal-body');

        if (titleElement) {
            titleElement.innerHTML = '<i class="fas fa-map-marker-alt mr-2"></i>Verification Blocked - Non-Davao City Resident';
        }

        if (bodyElement) {
            const locationInfo = detectedCity !== "Not detected"
                ? `<div class="alert alert-danger mb-3">
                     <i class="fas fa-exclamation-triangle mr-2"></i>
                     <strong>Detected Location:</strong> ${detectedCity}
                   </div>`
                : `<div class="alert alert-warning mb-3">
                     <i class="fas fa-question-circle mr-2"></i>
                     City could not be detected on the ID
                   </div>`;

            bodyElement.innerHTML = `
                <div class="text-center mb-4">
                    <i class="fas fa-ban text-red-500" style="font-size: 4rem;"></i>
                </div>

                ${locationInfo}

                <h5 class="font-bold mb-3">Account Verification Blocked</h5>

                <p class="mb-3">
                    This account verification service is <strong>exclusively for Davao City residents</strong>.
                    Your ${idTypeName} indicates you are not a resident of Davao City.
                </p>

                <div class="bg-blue-50 border-l-4 border-blue-500 p-3 mb-3">
                    <p class="text-sm font-semibold text-blue-900 mb-2">
                        <i class="fas fa-info-circle mr-1"></i> If you ARE a Davao City resident:
                    </p>
                    <ul class="text-sm text-blue-800 list-disc list-inside space-y-1">
                        <li>Ensure your ID image is clear and well-lit</li>
                        <li>Make sure the address section is fully visible</li>
                        <li>Upload a higher quality image</li>
                        <li>Use an ID that shows your current Davao City address</li>
                    </ul>
                </div>

                <div class="bg-gray-50 p-3 rounded">
                    <p class="text-sm text-gray-700 mb-1">
                        <strong>Accepted locations:</strong> Davao City only
                    </p>
                    <p class="text-sm text-gray-600">
                        <strong>Not accepted:</strong> Cebu City, Manila, Quezon City, or any other location outside Davao City
                    </p>
                </div>
            `;
        }

        try {
            const bsModal = new bootstrap.Modal(modal);
            bsModal.show();
        } catch (e) {
            console.error('Error showing modal:', e);
            alert(`VERIFICATION BLOCKED\n\nThis service is only for Davao City residents.\nDetected: ${detectedCity}`);
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
            console.error('Error disabling form fields:', e);
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
            console.error('Error enabling form fields:', e);
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
            console.error('Error clearing uploaded files:', e);
        }
    }

    // Helper functions
    function cleanName(text) {
        if (!text) return "";

        // First pass: Remove label patterns before character filtering
        const labelPatterns = [
            /APELYIDO[\s\/:]*/gi,
            /APELYEDO[\s\/:]*/gi,
            /PANGALAN[\s\/:]*/gi,
            /NGALAN[\s\/:]*/gi,
            /SURNAME[\s\/:]*/gi,
            /LAST\s*NAME[\s\/:]*/gi,
            /FIRST\s*NAME[\s\/:]*/gi,
            /MIDDLE\s*NAME[\s\/:]*/gi,
            /GIVEN\s*NAME[\s\/:]*/gi,
            /FULL\s*NAME[\s\/:]*/gi,
            /UNANG\s*PANGALAN[\s\/:]*/gi,
            /HULING\s*PANGALAN[\s\/:]*/gi,
            /GITNANG\s*PANGALAN[\s\/:]*/gi,
            /BINANSAGANG[\s\/:]*/gi,
            /KUMPLETONG[\s\/:]*/gi,
            /BUONG[\s\/:]*/gi
        ];

        for (const pattern of labelPatterns) {
            text = text.replace(pattern, ' ');
        }

        // Remove numbers and special characters, keep only letters and spaces
        text = text.replace(/[^a-zA-ZÑñ\s]/g, " ");  // Include Ñ for Filipino names

        // Remove common OCR mistakes and noise words (English + Filipino labels)
        const noiseWords = [
            // English labels
            'NAME', 'SURNAME', 'FIRST', 'LAST', 'MIDDLE', 'GIVEN', 'FULL', 'COMPLETE',
            'HOLDER', 'CARD', 'FIRSTNAME', 'LASTNAME', 'MIDDLENAME', 'FULLNAME',
            'GIVENNAME', 'GIVENNAMES', 'FAMILYNAME',
            // Filipino/Tagalog labels
            'APELYIDO', 'APELYEDO', 'PANGALAN', 'NGALAN', 'UNANG', 'HULING', 'GITNANG',
            'MGA', 'BUONG', 'KUMPLETONG', 'BINANSAGANG', 'UNA', 'GITNA',
            // Bilingual format separators
            'NG', 'SA', 'ANG', 'NI'
        ];

        const words = text.split(/\s+/).filter(word => {
            const upperWord = word.toUpperCase();
            // Keep word if it's longer than 1 char and not a noise word
            return word.length > 1 && !noiseWords.includes(upperWord);
        });

        text = words.join(' ');

        // Trim and normalize spaces
        return text.replace(/\s+/g, " ").trim();
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
        console.log(`[FINAL VALIDATION] Checking ${fieldName}: "${value}"`);

        // Layer 1: Check if value exists
        if (!value || value.trim().length === 0) {
            if (isRequired) {
                console.log(`✗ ${fieldName} is empty (required field)`);
                return "";
            }
            console.log(`✓ ${fieldName} is empty (optional field)`);
            return "";
        }

        const trimmedValue = value.trim();

        // Layer 2: Check for "none" or "n/a" values (should be treated as empty)
        const invalidPlaceholders = ['NONE', 'N/A', 'NA', 'NULL', 'UNDEFINED', 'EMPTY'];
        if (invalidPlaceholders.includes(trimmedValue.toUpperCase())) {
            console.log(`✗ ${fieldName} contains placeholder value "${trimmedValue}", treating as empty`);
            return "";
        }

        // Layer 3: Check minimum length (names should be at least 2 characters)
        if (trimmedValue.length < 2) {
            console.log(`✗ ${fieldName} too short: "${trimmedValue}"`);
            return "";
        }

        // Layer 4: Check if value is actually a label/indicator
        if (isExtractedTextALabel(trimmedValue)) {
            console.log(`✗ ${fieldName} is a label/indicator, rejecting: "${trimmedValue}"`);
            return "";
        }

        // Layer 5: For name fields, ensure it contains unusual words (actual data)
        if (fieldName.includes('Name') || fieldName.includes('name')) {
            if (!containsUnusualWords(trimmedValue)) {
                console.log(`✗ ${fieldName} appears to be label text, not actual data: "${trimmedValue}"`);
                return "";
            }
        }

        // Layer 6: For suffix field, validate against allowed suffix values
        if (fieldName.toLowerCase().includes('suffix')) {
            const validSuffixes = ['JR', 'SR', 'II', 'III', 'IV', 'V', 'JUNIOR', 'SENIOR'];
            const upperValue = trimmedValue.toUpperCase();
            if (!validSuffixes.includes(upperValue)) {
                console.log(`✗ ${fieldName} is not a valid suffix: "${trimmedValue}"`);
                return "";
            }
            // Normalize suffix
            if (upperValue === 'JUNIOR') return 'JR';
            if (upperValue === 'SENIOR') return 'SR';
            console.log(`✓ ${fieldName} PASSED validation (valid suffix): "${upperValue}"`);
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
                console.log(`✗ ${fieldName} is exactly a label fragment "${fragment}": "${trimmedValue}"`);
                return "";
            }

            // Check if any individual word matches the fragment
            if (valueWords.includes(fragment)) {
                console.log(`✗ ${fieldName} contains label fragment as complete word "${fragment}": "${trimmedValue}"`);
                return "";
            }
        }

        console.log(`✓ ${fieldName} PASSED validation: "${trimmedValue}"`);
        return trimmedValue;
    }

    // Advanced form fields update with civil status and suffix
    // FINAL VALIDATION is performed before populating fields
    function updateFormFieldsAdvanced(idNumber, firstName, middleName, lastName, birthdate, sex, civilStatus, suffix, fullOcrText = "", idType = "", confidence = 0) {
        try {
            console.log('\n=== STEP 3: DATA EXTRACTION & FIELD POPULATION ===');

            // ========================================
            // FINAL VALIDATION LAYER - Validate all name fields
            // ========================================
            console.log('\n=== FINAL VALIDATION LAYER ===');
            firstName = validateExtractedValue(firstName, 'First Name', true);
            middleName = validateExtractedValue(middleName, 'Middle Name', false);
            lastName = validateExtractedValue(lastName, 'Last Name', true);
            suffix = validateExtractedValue(suffix, 'Suffix', false);

            // Check if required fields passed validation
            if (!firstName || !lastName) {
                console.error('✗ VALIDATION FAILED: Required name fields are missing or invalid');
                console.error('  First Name:', firstName || '(empty)');
                console.error('  Last Name:', lastName || '(empty)');

                const resultBox = document.getElementById('result-box');
                if (resultBox) {
                    resultBox.innerHTML = '<strong>Validation Error:</strong> Unable to extract valid first name and last name. Please ensure the ID is clear and try again.';
                    resultBox.style.color = 'red';
                }
                return;
            }
            console.log('✓ VALIDATION PASSED: All required fields are valid');
            console.log('========================================\n');

            // VALIDATION 1: Check if ID type is allowed
            if (idType && !ALLOWED_ID_TYPES.includes(idType)) {
                console.error('Invalid ID type detected:', idType);
                showInvalidIdTypeModal(getIdTypeName(idType));
                return;
            }

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
            console.log('Auto-populating fields...');

            if (idNumber && idField) {
                idField.value = idNumber;
                console.log('✓ ID Number:', idNumber);
            }
            if (firstName && firstField) {
                firstField.value = firstName.toUpperCase();
                console.log('✓ First Name:', firstName);
            }
            if (middleName && middleField) {
                middleField.value = middleName.toUpperCase();
                console.log('✓ Middle Name:', middleName);
            }
            if (lastName && lastField) {
                lastField.value = lastName.toUpperCase();
                console.log('✓ Last Name:', lastName);
            }
            if (birthdate && birthField) {
                birthField.value = birthdate;
                console.log('✓ Birthdate:', birthdate);
            }
            if (sex && genderField) {
                genderField.value = sex;
                console.log('✓ Gender:', sex);
            }

            // Auto-populate barangay if detected
            const detectedBarangay = extractBarangayFromAddress(fullOcrText);
            if (detectedBarangay && barangayField) {
                barangayField.value = detectedBarangay;
                console.log('✓ Barangay auto-populated:', detectedBarangay);
            }

            // Update civil status if field exists
            if (civilStatus && civilStatusField) {
                civilStatusField.value = civilStatus;
                console.log('✓ Civil Status:', civilStatus);
            }

            // Update suffix if field exists and value is not "None"
            if (suffix && suffixField) {
                suffixField.value = suffix;
                console.log('✓ Suffix:', suffix);
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
                console.log(`Extraction Confidence: ${extractionConfidence}%`);

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

            console.log('=== INTELLIGENT VALIDATION COMPLETED SUCCESSFULLY ===');
            console.log('Auto-populated fields:', populatedFields);

            // Ensure progress bar is at 100% and update status to show completion
            if (progress) {
                progress.style.width = '100%';
            }

            if (status) {
                status.innerHTML = '<i class="fas fa-check-circle mr-2 text-green-600"></i><strong>Completed</strong> - Data extraction successful';
            }

            // Hide progress bar after a delay to show the completed state
            if (progressBar) {
                setTimeout(() => {
                    progressBar.classList.add('hidden');
                }, 2000); // 2 second delay to show the completed state
            }
        } catch (e) {
            console.error('Error updating form fields:', e);
        }
    }

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
        console.log('✓ File validation passed:', fileName, `(${(file.size / 1024).toFixed(2)}KB)`);
        return true;
    }

    /**
     * Show detailed modal for invalid file format
     */
    function showFileFormatErrorModal(fileName, detectedType) {
        const modal = document.getElementById('fileErrorModal');
        if (!modal) {
            alert(`INVALID FILE FORMAT\n\nFile: ${fileName}\nDetected type: ${detectedType}\n\nOnly JPG, JPEG, and PNG formats are accepted.`);
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
            console.error('Error showing modal:', e);
            alert(`INVALID FILE FORMAT\n\nOnly JPG, JPEG, and PNG files are accepted.\n\nFile: ${fileName}\nType: ${detectedType}`);
        }
    }

    // Show File Error Modal
    function showFileErrorModal(title, message) {
        const modal = document.getElementById('fileErrorModal');
        if (!modal) {
            console.error('File Error Modal not found');
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
            console.error('Error showing modal:', e);
            alert(`${title}: ${message}`);
        }
    }

    // Show OCR Error Modal
    function showOCRErrorModal(message) {
        const modal = document.getElementById('ocrErrorModal');
        if (!modal) {
            console.error('OCR Error Modal not found');
            alert(`OCR Error: ${message}`);
            return;
        }

        const messageElement = document.getElementById('ocr-error-message');
        if (messageElement) messageElement.textContent = message;

        try {
            const bsModal = new bootstrap.Modal(modal);
            bsModal.show();
        } catch (e) {
            console.error('Error showing modal:', e);
            alert(`OCR Error: ${message}`);
        }
    }

    // Show Name Mismatch Blocking Modal (CRITICAL - Blocks verification)
    function showNameMismatchModal(extractedName, registeredName, details) {
        console.error('===  NAME MISMATCH - BLOCKING VERIFICATION ===');
        console.error('Extracted Name:', extractedName);
        console.error('Registered Name:', registeredName);
        console.error('Match Details:', details);
        console.error('==============================================');

        // Disable form submission and fields
        disableFormSubmission();
        disableFormFields();

        // Clear uploaded files
        clearUploadedFiles();

        // Show Bootstrap modal
        const modal = document.getElementById('nameMismatchModal');
        if (!modal) {
            console.error('Name Mismatch Modal not found');
            alert(`VERIFICATION BLOCKED: Name Mismatch\n\nID Name: ${extractedName}\nRegistered Name: ${registeredName}\n\nYou cannot proceed until the names match.`);
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
            console.error('Error showing modal:', e);
            alert(`VERIFICATION BLOCKED: Name Mismatch\n\nID Name: ${extractedName}\nRegistered Name: ${registeredName}\n\nYou cannot proceed until the names match.`);
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
                                <li>SSS ID</li>
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

        alert(`VERIFICATION BLOCKED: Invalid ID Type\n\nOnly these IDs are accepted:\n- Philippine National ID\n- Driver's License\n- SSS ID\n- UMID\n\nPlease upload one of the accepted ID types.`);

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
                console.error('Form validation error:', error);
            }
        });
    }

    // Add error listener for debugging
    window.addEventListener('error', function (e) {
        console.error('Global error:', e.error);
    });

    // Export debug functions to window for testing
    window.OCRDebug = {
        getLastOCRText: () => lastOCRText,
        getLastExtractedData: () => lastExtractedData,
        enableDebugMode: () => {
            console.log('Debug mode enabled. OCR details will be logged.');
            return 'Set DEBUG_MODE = true in the code and refresh for full debug logs.';
        },
        testExtraction: () => {
            console.log('=== OCR DEBUG REPORT ===');
            console.log('Engine: OCR.space API (Cloud-based)');
            console.log('\nLast OCR Text:');
            console.log(lastOCRText);
            console.log('\nLast Extracted Data:');
            console.log(lastExtractedData);
            console.log('\nConfidence Scores:');
            console.log(lastExtractedData.confidence || 'No confidence data');
            console.log('======================');
            return 'Debug report printed above';
        },
        apiInfo: () => {
            console.log('=== OCR API Information ===');
            console.log('Provider: OCR.space');
            console.log('Engine: Engine 2 (Optimized for structured documents)');
            console.log('Free Tier: 25,000 requests/month');
            console.log('Rate Limit: 10 requests per 10 seconds');
            console.log('Max File Size: 1MB');
            console.log('Get your own key: https://ocr.space/ocrapi');
            console.log('========================');
            return 'API info printed above';
        },
        help: () => {
            console.log('=== OCR Debug Commands ===');
            console.log('OCRDebug.getLastOCRText()       - Get raw OCR text from last scan');
            console.log('OCRDebug.getLastExtractedData() - Get extracted data from last scan');
            console.log('OCRDebug.testExtraction()       - Print full debug report');
            console.log('OCRDebug.apiInfo()              - Show OCR API information');
            console.log('OCRDebug.help()                 - Show this help');
            console.log('==========================');
            return 'Commands listed above';
        }
    };

    console.log('%c✅ Account Verification OCR Initialized', 'color: #32cd32; font-weight: bold; font-size: 14px;');
    console.log('%c🚀 Using OCR.space API - Superior ID recognition', 'color: #3b82f6; font-size: 12px;');
    console.log('%c🔍 Debug Tools: Type OCRDebug.help() for commands', 'color: gray; font-size: 11px;');
});