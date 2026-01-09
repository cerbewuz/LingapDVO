// ============================================================================
// FACIAL RECOGNITION MODULE - ID ANALYZER API v2 INTEGRATION
// ============================================================================
// Uses ID Analyzer API v2 Face Verification for server-side face comparison
// Base URL: https://api2.idanalyzer.com
//
// NEW VERIFICATION FLOW (COLLECT ALL IMAGES FIRST, THEN SUBMIT):
// ════════════════════════════════════════════════════════════════════════════
// STEP 1: COLLECT ALL REQUIRED IMAGES (NO API CALLS YET)
//    - User uploads Front ID → Stored in VerificationState.frontIdImage
//    - User uploads Back ID → Stored in VerificationState.backIdImage
//    - User captures Selfie → Stored in VerificationState.selfieImage
//    - "Verify & Submit" button becomes enabled
//
// STEP 2: USER CLICKS "VERIFY & SUBMIT" BUTTON
//    - All images sent to API in single request
//    - API performs OCR + face matching
//    - Results populate form fields
//
// This module handles SELFIE CAPTURE only.
// The actual API submission is handled by verification-idanalyzer.js
//
// Reference: https://developer.idanalyzer.com/reference/post-face-2
// ============================================================================

// Face Recognition Configuration
const FaceRecognitionConfig = {
    // API endpoints for ID Analyzer v2
    API_URL: '/api/IdAnalyzer',
    SAVE_SELFIE_ENDPOINT: '/api/IdAnalyzer/saveSelfie',  // NEW: Save selfie to disk
    FACE_ENDPOINT: '/api/IdAnalyzer/face',
    SCAN_ENDPOINT: '/api/IdAnalyzer/scan',
    
    // Camera settings - OPTIMIZED for ID Analyzer v2 API face detection
    CAMERA_CONSTRAINTS: {
        // Primary constraints - HD quality for best face detection
        primary: {
            video: {
                width: { ideal: 1280, min: 640, max: 1920 },
                height: { ideal: 720, min: 480, max: 1080 },
                facingMode: 'user',
                frameRate: { ideal: 30 }
            },
            audio: false
        },
        // Fallback constraints - Standard quality
        fallback: {
            video: {
                width: { ideal: 640, min: 320 },
                height: { ideal: 480, min: 240 },
                facingMode: 'user',
                frameRate: { ideal: 24 }
            },
            audio: false
        },
        // Minimal constraints (last resort)
        minimal: {
            video: {
                width: { min: 320 },
                height: { min: 240 },
                facingMode: 'user'
            },
            audio: false
        },
        // Any camera (absolute fallback)
        any: {
            video: true,
            audio: false
        }
    },
    
    // UI settings
    COUNTDOWN_SECONDS: 3,
    MAX_RETRIES: 3,
    
    // Performance settings
    CAMERA_TIMEOUT: 15000, // 15 seconds timeout
    VIDEO_READY_CHECK_INTERVAL: 100 // Check every 100ms
};

// Face Recognition Module - ID Analyzer API Integration
const FaceRecognition = {
    // State
    isInitialized: false,
    isProcessing: false,
    captureInProgress: false,    // NEW: Flag to prevent multiple captures
    selfieImageData: null,
    selfieFileName: null,          // NEW: Store filename of saved selfie
    idImageData: null,
    videoStream: null,
    retryCount: 0,
    
    // DOM Elements (initialized on load)
    elements: {},
    
    // Initialize the module (no model loading needed - server-side processing)
    async init() {
        
        if (this.isInitialized) {
            return true;
        }
        
        try {
            // Verify API is accessible
            const response = await fetch(`${FaceRecognitionConfig.API_URL}/health`);
            if (response.ok) {
                this.isInitialized = true;
                return true;
            }
        } catch (error) {
        }
        
        // Initialize anyway - API might be available
        this.isInitialized = true;
        return true;
    },
    
    // Compare two faces using ID Analyzer Face Verification API
    // Endpoint: POST /api/IdAnalyzer/face
    // Reference: https://developer.idanalyzer.com/reference/post-face-2
    async compareFacesViaApi(referenceBase64, faceBase64) {
        
        try {
            const response = await fetch(FaceRecognitionConfig.FACE_ENDPOINT, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    reference: referenceBase64,  // Reference face (from ID)
                    face: faceBase64             // Face to verify (selfie)
                })
            });
            
            const result = await response.json();
            
            if (!result.success) {
                return {
                    match: false,
                    similarity: 0,
                    percentage: 0,
                    error: result.error
                };
            }
            
            
            return {
                match: result.isMatch,
                similarity: result.similarity,
                percentage: result.similarityPercentage,
                confidence: result.confidence,
                decision: result.decision
            };
        } catch (error) {
            return {
                match: false,
                similarity: 0,
                percentage: 0,
                error: error.message
            };
        }
    },
    
    // Scan ID document via ID Analyzer Standard ID Scan API
    // Endpoint: POST /api/IdAnalyzer/scan
    // Reference: https://developer.idanalyzer.com/reference/post-scan
    async scanIdViaApi(documentBase64, selfieBase64 = null, backBase64 = null) {
        
        try {
            const requestBody = {
                documentImage: documentBase64
            };
            
            // Add selfie for face matching during scan
            if (selfieBase64) {
                requestBody.faceImage = selfieBase64;
            }
            
            if (backBase64) {
                requestBody.backImage = backBase64;
            }
            
            const response = await fetch(FaceRecognitionConfig.SCAN_ENDPOINT, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(requestBody)
            });
            
            const result = await response.json();
            
            if (!result.success) {
                return {
                    success: false,
                    error: result.error
                };
            }
            
            
            return {
                success: true,
                data: result.data
            };
        } catch (error) {
            return {
                success: false,
                error: error.message
            };
        }
    },
    
    // Store selfie image data
    storeSelfieImageData(imageData) {
        this.selfieImageData = imageData;
    },
    
    // Store ID image data
    storeIdImageData(imageData) {
        this.idImageData = imageData;
    },
    
    // Start webcam for selfie capture - OPTIMIZED with adaptive constraints
    async startCamera(videoElement) {
        
        // Stop any existing stream first
        this.stopCamera();
        
        // Get available cameras first
        let cameras = [];
        try {
            const devices = await navigator.mediaDevices.enumerateDevices();
            cameras = devices.filter(device => device.kind === 'videoinput');
        } catch (e) {
        }
        
        // Try different constraint levels
        const constraintLevels = ['primary', 'fallback', 'minimal', 'any'];
        let lastError = null;
        
        for (const level of constraintLevels) {
            try {
                const constraints = FaceRecognitionConfig.CAMERA_CONSTRAINTS[level];
                
                const stream = await navigator.mediaDevices.getUserMedia(constraints);
                
                // Get actual camera capabilities
                const videoTrack = stream.getVideoTracks()[0];
                if (videoTrack) {
                    const settings = videoTrack.getSettings();
                    
                    // Apply optimal settings if available
                    if (videoTrack.getCapabilities) {
                        const capabilities = videoTrack.getCapabilities();
                    }
                }
                
                // Successfully got stream, now set it up
                return await this.setupVideoStream(videoElement, stream);
                
            } catch (error) {
                lastError = error;
                
                // If it's a permission error, don't try other constraints
                if (error.name === 'NotAllowedError' || error.name === 'PermissionDeniedError') {
                    throw error;
                }
            }
        }
        
        // All constraint levels failed
        throw lastError || new Error('Failed to access camera with any constraints');
    },
    
    // Setup video stream on video element
    async setupVideoStream(videoElement, stream) {
        
        // Set the stream to video element
        videoElement.srcObject = stream;
        this.videoStream = stream;
        
        // Configure video element for optimal performance
        videoElement.setAttribute('playsinline', 'true');
        videoElement.setAttribute('autoplay', 'true');
        videoElement.muted = true;
        
        // Wait for video to be ready with improved handling
        return new Promise((resolve, reject) => {
            let resolved = false;
            let checkInterval = null;
            
            const cleanup = () => {
                if (checkInterval) {
                    clearInterval(checkInterval);
                    checkInterval = null;
                }
                videoElement.oncanplay = null;
                videoElement.onloadedmetadata = null;
                videoElement.onloadeddata = null;
                videoElement.onerror = null;
            };
            
            const onReady = async () => {
                if (resolved) return;
                resolved = true;
                cleanup();
                
                
                try {
                    // Try to play the video
                    await videoElement.play();
                    
                    
                    // Additional check - ensure we have valid dimensions
                    if (videoElement.videoWidth === 0 || videoElement.videoHeight === 0) {
                        // Wait a bit more for dimensions
                        await new Promise(r => setTimeout(r, 500));
                    }
                    
                    resolve(true);
                } catch (err) {
                    reject(err);
                }
            };
            
            // Multiple event listeners for compatibility
            videoElement.oncanplay = onReady;
            videoElement.onloadedmetadata = () => {
            };
            videoElement.onloadeddata = () => {
                onReady();
            };
            
            videoElement.onerror = (err) => {
                if (resolved) return;
                resolved = true;
                cleanup();
                reject(new Error('Failed to load video stream'));
            };
            
            // Periodic check for video readiness (backup)
            checkInterval = setInterval(() => {
                if (resolved) return;
                
                if (videoElement.readyState >= 2 && videoElement.videoWidth > 0) {
                    onReady();
                }
            }, FaceRecognitionConfig.VIDEO_READY_CHECK_INTERVAL);
            
            // Timeout
            setTimeout(() => {
                if (!resolved) {
                    resolved = true;
                    cleanup();
                    
                    // Check if video is actually working despite timeout
                    if (videoElement.videoWidth > 0 && videoElement.readyState >= 2) {
                        videoElement.play().then(() => resolve(true)).catch(reject);
                    } else {
                        reject(new Error('Camera timeout - video not loading'));
                    }
                }
            }, FaceRecognitionConfig.CAMERA_TIMEOUT);
        });
    },
    
    // Stop webcam
    stopCamera() {
        if (this.videoStream) {
            this.videoStream.getTracks().forEach(track => {
                track.stop();
            });
            this.videoStream = null;
        }
    },
    
    // Capture selfie from video stream - ORIGINAL QUALITY PRESERVED for ID Analyzer v2 API
    // CRITICAL: No resizing, rescaling, or repositioning - send original image to API
    // FIXED: Added captureInProgress flag to prevent multiple captures
    async captureSelfie(videoElement, canvasElement) {
        // Prevent multiple simultaneous captures
        if (this.captureInProgress) {
            return {
                success: false,
                error: 'Capture already in progress'
            };
        }
        
        this.captureInProgress = true;

        // Get actual video dimensions - USE ORIGINAL SIZE WITHOUT MODIFICATION
        const videoWidth = videoElement.videoWidth || 640;
        const videoHeight = videoElement.videoHeight || 480;


        // ID Analyzer v2 API Requirements for Face Detection:
        // - Minimum resolution: 640x480 (recommended: 1280x720 or higher)
        // - Face must be clearly visible and well-lit
        // - High quality JPEG (0.92+ quality)
        // - DO NOT mirror the image (causes face detection issues)
        // - CRITICAL: Send ORIGINAL image without any modifications

        // ORIGINAL QUALITY: Use video's native resolution without any scaling
        // The ID Analyzer API should receive the image in its original form
        const captureWidth = videoWidth;
        const captureHeight = videoHeight;


        // Set canvas size to EXACT video dimensions (no scaling)
        canvasElement.width = captureWidth;
        canvasElement.height = captureHeight;

        const context = canvasElement.getContext('2d');

        // Disable image smoothing to preserve original pixel data
        context.imageSmoothingEnabled = false;

        // CRITICAL: Draw video frame at ORIGINAL size WITHOUT any transformation
        // No mirroring, no scaling, no repositioning - exact 1:1 copy
        context.drawImage(videoElement, 0, 0, captureWidth, captureHeight);

        // CRITICAL: Use maximum JPEG quality (1.0) to preserve original image data
        // No compression artifacts - send to API exactly as captured
        const imageData = canvasElement.toDataURL('image/jpeg', 1.0);


        // Validate image was captured
        if (!imageData || imageData.length < 1000) {
            this.captureInProgress = false;  // Reset flag on failure
            return {
                success: false,
                error: 'Failed to capture valid selfie image'
            };
        }

        // Store selfie image data in memory (Base64)
        // No disk save - sent directly to API as Base64
        // Only saved to disk AFTER API returns "accept" decision
        this.storeSelfieImageData(imageData);

        // Reset flag on success (allow retry later if needed)
        this.captureInProgress = false;

        console.log('✅ Selfie captured and stored as Base64 (no disk save)');
        console.log('   Length:', imageData.length, 'chars');

        return {
            success: true,
            imageData: imageData
        };
    },

    // NEW: Save selfie to server (/wwwroot/UsersImg)
    async saveSelfieToServer(imageData) {

        try {
            const response = await fetch(FaceRecognitionConfig.SAVE_SELFIE_ENDPOINT, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    imageData: imageData
                })
            });

            const result = await response.json();

            if (!result.success) {
                return {
                    success: false,
                    error: result.error
                };
            }


            return {
                success: true,
                fileName: result.fileName,
                filePath: result.filePath,
                fileSize: result.fileSize
            };
        } catch (error) {
            return {
                success: false,
                error: error.message
            };
        }
    },
    
    // Perform face verification using ID Analyzer API
    async verifyFace() {
        
        if (!this.selfieImageData) {
            return {
                success: false,
                error: 'Selfie not captured. Please take a selfie.'
            };
        }
        
        if (!this.idImageData) {
            return {
                success: false,
                error: 'ID face not captured. Please upload your ID first.'
            };
        }
        
        // Compare faces via API
        const comparison = await this.compareFacesViaApi(this.idImageData, this.selfieImageData);

        if (comparison.error) {
            return {
                success: false,
                error: comparison.error
            };
        }

        // Return raw API response without custom decision logic
        // The API's decision field is the single source of truth
        return {
            success: true,
            similarity: comparison.percentage,
            confidence: comparison.confidence,
            decision: comparison.decision,
            message: 'Face verification completed. Decision: ' + (comparison.decision || 'pending')
        };
    },
    
    // Reset state
    reset() {
        this.selfieImageData = null;
        this.idImageData = null;
        this.retryCount = 0;
        this.stopCamera();
    },
    
    // Store selfie data (for backward compatibility)
    storeSelfieDescriptor(descriptor, imageData) {
        this.selfieImageData = imageData;
    },
    
    // Compare selfie with ID face (called after ID is uploaded)
    async compareWithId(idImageSource) {
        
        if (!this.selfieImageData) {
            return {
                success: false,
                error: 'Selfie not captured. Please take a selfie first.'
            };
        }
        
        // Store ID image
        this.storeIdImageData(idImageSource);
        
        // Compare faces via API
        return this.verifyFace();
    }
};

// ============================================================================
// FACIAL RECOGNITION MODAL UI CONTROLLER
// Uses ID Analyzer API for server-side face comparison
// ============================================================================

const FaceRecognitionUI = {
    // State
    isModalOpen: false,
    countdownInterval: null,
    onSelfieCompleteCallback: null,
    onCloseCallback: null,
    pendingIdImage: null,
    isCapturing: false,  // NEW: Flag to prevent multiple captures
    
    // Initialize UI
    init() {
        this.cacheElements();
        this.bindEvents();
    },
    
    // Cache DOM elements
    cacheElements() {
        this.elements = {
            modal: document.getElementById('facialRecognitionModal'),
            video: document.getElementById('faceVideo'),
            canvas: document.getElementById('faceCanvas'),
            captureBtn: document.getElementById('captureFaceBtn'),
            retryBtn: document.getElementById('retryFaceBtn'),
            closeBtn: document.getElementById('closeFaceModal'),
            statusText: document.getElementById('faceStatus'),
            statusIcon: document.getElementById('faceStatusIcon'),
            countdown: document.getElementById('faceCountdown'),
            countdownNumber: document.getElementById('countdownNumber'),
            previewContainer: document.getElementById('selfiePreviewContainer'),
            previewImage: document.getElementById('selfiePreview'),
            idPreview: document.getElementById('idFacePreview'),
            matchResult: document.getElementById('faceMatchResult'),
            matchPercentage: document.getElementById('matchPercentage'),
            progressBar: document.getElementById('faceProgressBar'),
            instructions: document.getElementById('faceInstructions'),
            verifyingOverlay: document.getElementById('verifyingOverlay')
        };
    },
    
    // Bind event handlers
    bindEvents() {
        if (this.elements.captureBtn) {
            this.elements.captureBtn.addEventListener('click', () => {
                // Prevent multiple clicks during countdown or capture
                if (this.isCapturing || this.countdownInterval) {
                    return;
                }
                this.startCountdown();
            });
        }
        
        if (this.elements.retryBtn) {
            this.elements.retryBtn.addEventListener('click', () => this.retry());
        }
        
        if (this.elements.closeBtn) {
            this.elements.closeBtn.addEventListener('click', () => this.handleClose());
        }
        
        // Close on backdrop click
        if (this.elements.modal) {
            this.elements.modal.addEventListener('click', (e) => {
                if (e.target === this.elements.modal) {
                    if (!FaceRecognition.isProcessing) {
                        this.handleClose();
                    }
                }
            });
        }
    },
    
    // Handle close button
    handleClose() {
        if (FaceRecognition.selfieImageData) {
            // Selfie already captured, safe to close
            this.closeModal();
        } else {
            // No selfie yet, show warning
            if (confirm('You need to take a selfie for identity verification. Are you sure you want to cancel?')) {
                this.closeModal();
                if (this.onCloseCallback) {
                    this.onCloseCallback({ cancelled: true });
                }
            }
        }
    },
    
    // Open modal for SELFIE CAPTURE (called AFTER ID Upload)
    async openForSelfie(onSelfieComplete, onClose) {
        
        this.onSelfieCompleteCallback = onSelfieComplete;
        this.onCloseCallback = onClose;
        
        // Reset UI but keep ID image data
        this.resetUI();
        
        // Show modal
        if (this.elements.modal) {
            this.elements.modal.classList.remove('hidden');
            this.elements.modal.classList.add('flex');
            document.body.style.overflow = 'hidden';
            this.isModalOpen = true;
        }
        
        // Hide ID preview section initially
        if (this.elements.previewContainer) {
            this.elements.previewContainer.classList.add('hidden');
        }
        
        // Initialize Face Recognition if not done
        if (!FaceRecognition.isInitialized) {
            this.updateStatus('Initializing face verification...', 'loading');
            const initialized = await FaceRecognition.init();
            if (!initialized) {
                this.updateStatus('Failed to initialize. Please refresh and try again.', 'error');
                this.showRetryButton();
                return;
            }
        }
        
        // Show permission info and start camera directly
        this.showCameraPermissionRequest();
        
        // Small delay to let user see the message
        await new Promise(resolve => setTimeout(resolve, 800));
        
        // Start camera directly (this will trigger browser permission if needed)
        await this.startCameraWithRetry();
    },
    
    // Show camera permission request UI
    showCameraPermissionRequest() {
        // Update modal body to show permission request
        const instructionsEl = document.getElementById('faceInstructions');
        if (instructionsEl) {
            instructionsEl.innerHTML = `
                <h4 class="text-blue-800 font-semibold flex items-center gap-2">
                    <i class="fas fa-video text-blue-600"></i> Camera Access Required
                </h4>
                <div class="mt-3 space-y-2">
                    <p class="text-blue-700 text-sm">
                        To verify your identity, we need access to your camera to take a selfie.
                    </p>
                    <div class="bg-white/50 rounded-lg p-3 mt-2">
                        <p class="text-blue-800 text-sm font-medium mb-2">When your browser asks for camera permission:</p>
                        <ol class="text-blue-700 text-sm list-decimal list-inside space-y-1">
                            <li>Click <strong>"Allow"</strong> in the browser popup</li>
                            <li>If you don't see a popup, check your browser's address bar</li>
                            <li>Look for a camera icon 📷 or lock icon 🔒</li>
                        </ol>
                    </div>
                </div>
            `;
            instructionsEl.className = 'face-instructions bg-gradient-to-br from-blue-50 to-blue-100 border-blue-200';
        }
        
        this.updateStatus('Requesting camera access...', 'loading');
        this.hideButtons();
    },
    
    // Show message when camera is blocked
    showCameraBlockedMessage() {
        const instructionsEl = document.getElementById('faceInstructions');
        if (instructionsEl) {
            instructionsEl.innerHTML = `
                <h4 class="text-red-800 font-semibold flex items-center gap-2">
                    <i class="fas fa-video-slash text-red-600"></i> Camera Access Blocked
                </h4>
                <div class="mt-3 space-y-2">
                    <p class="text-red-700 text-sm">
                        Camera access has been blocked. Please enable it to continue with identity verification.
                    </p>
                    <div class="bg-white/50 rounded-lg p-3 mt-2">
                        <p class="text-red-800 text-sm font-medium mb-2">To enable camera access:</p>
                        <div class="text-red-700 text-sm space-y-2">
                            <div class="flex items-start gap-2">
                                <span class="font-bold">Chrome:</span>
                                <span>Click the lock icon 🔒 in the address bar → Site settings → Camera → Allow</span>
                            </div>
                            <div class="flex items-start gap-2">
                                <span class="font-bold">Firefox:</span>
                                <span>Click the lock icon → Connection secure → More information → Permissions → Camera</span>
                            </div>
                            <div class="flex items-start gap-2">
                                <span class="font-bold">Edge:</span>
                                <span>Click the lock icon → Permissions for this site → Camera → Allow</span>
                            </div>
                        </div>
                    </div>
                    <p class="text-red-600 text-xs mt-2">
                        <i class="fas fa-info-circle mr-1"></i>After enabling, click "Try Again" below.
                    </p>
                </div>
            `;
            instructionsEl.className = 'face-instructions bg-gradient-to-br from-red-50 to-red-100 border-red-200';
        }
        
        this.updateStatus('Camera access is blocked. Please enable it in your browser settings.', 'error');
        this.showRetryButton();
    },
    
    // Start camera with retry mechanism
    async startCameraWithRetry() {
        this.updateStatus('Starting camera...', 'loading');
        
        try {
            // Check if mediaDevices is available
            if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
                throw { name: 'NotSupportedError', message: 'Camera not supported in this browser' };
            }
            
            await FaceRecognition.startCamera(this.elements.video);
            
            // Restore normal instructions after camera starts
            this.restoreNormalInstructions();

            this.updateStatus('Face the camera directly - Make sure your face is well-lit and clearly visible', 'ready');
            this.showCaptureButton();
            
        } catch (error) {
            
            // Check error type - handle various browser error formats
            const errorName = error.name || '';
            const errorMessage = (error.message || '').toLowerCase();
            
            // Check for permission denied errors
            if (errorName === 'NotAllowedError' || 
                errorName === 'PermissionDeniedError' ||
                errorMessage.includes('permission') ||
                errorMessage.includes('denied') ||
                errorMessage.includes('not allowed')) {
                this.showCameraBlockedMessage();
            } 
            // Check for no camera found
            else if (errorName === 'NotFoundError' ||
                     errorName === 'DevicesNotFoundError' ||
                     errorMessage.includes('not found') ||
                     errorMessage.includes('no video')) {
                this.showNoCameraMessage();
            } 
            // Check for camera in use
            else if (errorName === 'NotReadableError' ||
                     errorName === 'TrackStartError' ||
                     errorMessage.includes('in use') ||
                     errorMessage.includes('could not start')) {
                this.showCameraInUseMessage();
            } 
            // Check for timeout
            else if (errorMessage.includes('timeout')) {
                this.updateStatus('Camera is taking too long to respond. Please try again.', 'error');
                this.showRetryButton();
            }
            // Generic error
            else {
                this.updateStatus('Failed to access camera: ' + (error.message || 'Unknown error'), 'error');
                this.showRetryButton();
            }
        }
    },
    
    // Show no camera found message
    showNoCameraMessage() {
        const instructionsEl = document.getElementById('faceInstructions');
        if (instructionsEl) {
            instructionsEl.innerHTML = `
                <h4 class="text-orange-800 font-semibold flex items-center gap-2">
                    <i class="fas fa-exclamation-triangle text-orange-600"></i> No Camera Found
                </h4>
                <div class="mt-3 space-y-2">
                    <p class="text-orange-700 text-sm">
                        We couldn't find a camera on your device.
                    </p>
                    <div class="bg-white/50 rounded-lg p-3 mt-2">
                        <p class="text-orange-800 text-sm font-medium mb-2">Please check:</p>
                        <ul class="text-orange-700 text-sm list-disc list-inside space-y-1">
                            <li>Is your webcam connected properly?</li>
                            <li>Is the camera being used by another application?</li>
                            <li>Try using a device with a built-in camera (laptop/phone)</li>
                        </ul>
                    </div>
                </div>
            `;
            instructionsEl.className = 'face-instructions bg-gradient-to-br from-orange-50 to-orange-100 border-orange-200';
        }
        
        this.updateStatus('No camera detected. Please connect a camera and try again.', 'error');
        this.showRetryButton();
    },
    
    // Show camera in use message
    showCameraInUseMessage() {
        const instructionsEl = document.getElementById('faceInstructions');
        if (instructionsEl) {
            instructionsEl.innerHTML = `
                <h4 class="text-yellow-800 font-semibold flex items-center gap-2">
                    <i class="fas fa-video text-yellow-600"></i> Camera In Use
                </h4>
                <div class="mt-3 space-y-2">
                    <p class="text-yellow-700 text-sm">
                        Your camera appears to be in use by another application.
                    </p>
                    <div class="bg-white/50 rounded-lg p-3 mt-2">
                        <p class="text-yellow-800 text-sm font-medium mb-2">Please try:</p>
                        <ul class="text-yellow-700 text-sm list-disc list-inside space-y-1">
                            <li>Close other apps using the camera (Zoom, Teams, etc.)</li>
                            <li>Close other browser tabs that might be using the camera</li>
                            <li>Restart your browser and try again</li>
                        </ul>
                    </div>
                </div>
            `;
            instructionsEl.className = 'face-instructions bg-gradient-to-br from-yellow-50 to-yellow-100 border-yellow-200';
        }
        
        this.updateStatus('Camera is in use. Close other apps and try again.', 'error');
        this.showRetryButton();
    },
    
    // Restore normal instructions after camera access granted
    restoreNormalInstructions() {
        const instructionsEl = document.getElementById('faceInstructions');
        if (instructionsEl) {
            instructionsEl.innerHTML = `
                <h4 class="text-blue-800 font-semibold flex items-center gap-2">
                    <i class="fas fa-camera text-blue-600"></i> Take a Clear Selfie for Face Verification
                </h4>
                <div class="mt-3 space-y-2">
                    <p class="text-blue-700 text-sm font-medium">
                        <i class="fas fa-exclamation-circle text-blue-600 mr-1"></i>
                        Important: Your face must be clearly visible for verification
                    </p>
                    <div class="bg-white/60 rounded-lg p-3 mt-2">
                        <p class="text-blue-800 text-xs font-semibold mb-2">✓ DO:</p>
                        <ul class="text-blue-700 text-xs list-disc list-inside space-y-1 ml-2">
                            <li><strong>Face the camera directly</strong> - look straight ahead</li>
                            <li><strong>Good lighting</strong> - make sure your face is well-lit (avoid shadows)</li>
                            <li><strong>Entire face visible</strong> - forehead to chin must be in frame</li>
                            <li><strong>Neutral expression</strong> - don't smile too much, keep eyes open</li>
                            <li><strong>Remove accessories</strong> - take off glasses, hats, masks</li>
                        </ul>
                        <p class="text-red-700 text-xs font-semibold mb-1 mt-2">✗ DON'T:</p>
                        <ul class="text-red-700 text-xs list-disc list-inside space-y-1 ml-2">
                            <li>Look away or tilt your head</li>
                            <li>Cover any part of your face</li>
                            <li>Take photos in dim lighting or with backlighting</li>
                            <li>Move during capture</li>
                        </ul>
                    </div>
                    <p class="text-blue-600 text-xs mt-2">
                        <i class="fas fa-shield-check mr-1"></i>
                        This selfie will be matched with the photo on your ID
                    </p>
                </div>
            `;
            instructionsEl.className = 'face-instructions bg-gradient-to-br from-blue-50 to-blue-100 border-blue-200';
        }
    },
    
    // Close modal
    closeModal() {
        
        FaceRecognition.stopCamera();
        this.clearCountdown();
        
        if (this.elements.modal) {
            this.elements.modal.classList.add('hidden');
            this.elements.modal.classList.remove('flex');
            document.body.style.overflow = '';
            this.isModalOpen = false;
        }
    },
    
    // Start countdown before capture
    // FIXED: Added safeguard to prevent multiple countdowns
    startCountdown() {
        // Prevent multiple countdowns or captures
        if (this.countdownInterval || this.isCapturing) {
            return;
        }
        
        let count = FaceRecognitionConfig.COUNTDOWN_SECONDS;
        
        this.hideButtons();
        if (this.elements.countdown) {
            this.elements.countdown.classList.remove('hidden');
        }
        if (this.elements.countdownNumber) {
            this.elements.countdownNumber.textContent = count;
        }
        
        this.updateStatus('Get ready! Taking photo in...', 'countdown');
        
        this.countdownInterval = setInterval(() => {
            count--;
            
            if (count > 0) {
                if (this.elements.countdownNumber) {
                    this.elements.countdownNumber.textContent = count;
                }
            } else {
                this.clearCountdown();
                if (this.elements.countdown) {
                    this.elements.countdown.classList.add('hidden');
                }
                // Only capture if not already capturing
                if (!this.isCapturing) {
                    this.captureSelfie();
                }
            }
        }, 1000);
    },
    
    // Clear countdown
    clearCountdown() {
        if (this.countdownInterval) {
            clearInterval(this.countdownInterval);
            this.countdownInterval = null;
        }
    },
    
    // Capture selfie only (Selfie-First Flow) - ID Analyzer version
    // FIXED: Added isCapturing flag to prevent multiple captures
    async captureSelfie() {
        // Prevent multiple simultaneous captures
        if (this.isCapturing) {
            return;
        }
        
        this.isCapturing = true;
        
        FaceRecognition.isProcessing = true;
        this.updateStatus('Capturing photo...', 'loading');
        this.showVerifyingOverlay();
        
        // Capture selfie
        const captureResult = await FaceRecognition.captureSelfie(
            this.elements.video,
            this.elements.canvas
        );
        
        FaceRecognition.isProcessing = false;
        this.hideVerifyingOverlay();
        
        if (!captureResult.success) {
            this.isCapturing = false;  // Reset flag on failure
            this.updateStatus(captureResult.error || 'Failed to capture selfie', 'error');
            this.showRetryButton();
            FaceRecognition.retryCount++;
            
            if (FaceRecognition.retryCount >= FaceRecognitionConfig.MAX_RETRIES) {
                this.updateStatus('Maximum attempts reached. Please refresh and try again.', 'error');
                setTimeout(() => {
                    this.closeModal();
                    if (this.onCloseCallback) {
                        this.onCloseCallback({ maxRetries: true });
                    }
                }, 2000);
            }
            return;
        }
        
        // Store selfie image data (no face descriptor needed with ID Analyzer)
        FaceRecognition.storeSelfieDescriptor(null, captureResult.imageData);
        
        // Show success and preview
        if (this.elements.previewImage) {
            this.elements.previewImage.src = captureResult.imageData;
        }
        
        // Stop camera
        FaceRecognition.stopCamera();
        
        // Show success message
        this.updateStatus('Selfie captured successfully!', 'success');
        this.hideButtons();
        
        // Auto-close after delay and callback
        // Image is stored as Base64 in memory (no disk save)
        setTimeout(() => {
            this.isCapturing = false;  // Reset flag after success
            this.closeModal();
            if (this.onSelfieCompleteCallback) {
                this.onSelfieCompleteCallback({
                    success: true,
                    imageData: captureResult.imageData
                });
            }
        }, 1500);
    },
    
    // Retry camera/capture
    async retry() {
        
        // Reset capture flags to allow new capture
        this.isCapturing = false;
        this.clearCountdown();
        
        this.hideMatchResult();
         if (this.elements.previewContainer) {
            this.elements.previewContainer.classList.add('hidden');
        }
        
        // Restart camera
        await this.startCameraWithRetry();
    },
    
    // UI Helper Methods
    updateStatus(message, type) {
        if (this.elements.statusText) {
            this.elements.statusText.textContent = message;
        }
        
        if (this.elements.statusIcon) {
            const iconClasses = {
                loading: 'fas fa-spinner fa-spin text-blue-500',
                ready: 'fas fa-camera text-green-500',
                countdown: 'fas fa-clock text-yellow-500',
                success: 'fas fa-check-circle text-green-500',
                error: 'fas fa-exclamation-circle text-red-500'
            };
            this.elements.statusIcon.className = iconClasses[type] || iconClasses.ready;
        }
    },
    
    showCaptureButton() {
        if (this.elements.captureBtn) {
            this.elements.captureBtn.classList.remove('hidden');
        }
        if (this.elements.retryBtn) {
            this.elements.retryBtn.classList.add('hidden');
        }
    },
    
    showRetryButton() {
        if (this.elements.captureBtn) {
            this.elements.captureBtn.classList.add('hidden');
        }
        if (this.elements.retryBtn) {
            this.elements.retryBtn.classList.remove('hidden');
        }
    },
    
    hideButtons() {
        if (this.elements.captureBtn) {
            this.elements.captureBtn.classList.add('hidden');
        }
        if (this.elements.retryBtn) {
            this.elements.retryBtn.classList.add('hidden');
        }
    },
    
    showSelfiePreview(imageData) {
        if (this.elements.previewImage) {
            this.elements.previewImage.src = imageData;
        }
        if (this.elements.previewContainer) {
            this.elements.previewContainer.classList.remove('hidden');
        }
    },
    
    hideSelfiePreview() {
        if (this.elements.previewContainer) {
            this.elements.previewContainer.classList.add('hidden');
        }
    },
    
    // REMOVED: showMatchResult() - This function made custom decisions about face verification
    // The system now follows ONLY the API's decision field (accept/review/reject)
    
    hideMatchResult() {
        if (this.elements.matchResult) {
            this.elements.matchResult.classList.add('hidden');
        }
    },
    
    showVerifyingOverlay() {
        if (this.elements.verifyingOverlay) {
            this.elements.verifyingOverlay.classList.remove('hidden');
        }
    },
    
    hideVerifyingOverlay() {
        if (this.elements.verifyingOverlay) {
            this.elements.verifyingOverlay.classList.add('hidden');
        }
    },
    
    resetUI() {
        // Reset capture flags
        this.isCapturing = false;
        
        this.hideButtons();
        this.hideSelfiePreview();
        this.hideMatchResult();
        this.hideVerifyingOverlay();
        this.clearCountdown();
        
        if (this.elements.countdown) {
            this.elements.countdown.classList.add('hidden');
        }
        
        this.updateStatus('Initializing...', 'loading');
    }
};

// ============================================================================
// INTEGRATION WITH ACCOUNT VERIFICATION FLOW
// ════════════════════════════════════════════════════════════════════════════
// NEW FLOW: COLLECT ALL IMAGES FIRST, THEN SUBMIT
// ════════════════════════════════════════════════════════════════════════════
// 
// This module handles SELFIE CAPTURE for account verification.
// The actual API submission is handled by verification-idanalyzer.js
//
// Flow:
// 1. User uploads Front ID → Stored in VerificationState.frontIdImage
// 2. User uploads Back ID → Stored in VerificationState.backIdImage
// 3. User captures Selfie (this module) → Stored in VerificationState.selfieImage
// 4. User clicks "Verify & Submit" → submitVerification() sends all to API
// 5. API returns OCR data + face match result
// 6. Form fields populated, face result displayed
//
// ============================================================================

const FaceVerificationFlow = {
    // State
    selfieImageData: null,
    idImageData: null,
    selfieCaptured: false,
    idUploaded: false,
    verificationPassed: false,
    verificationResult: null,
    
    // Step 1: Store ID image (called AFTER ID upload)
    storeIdImage(idImageSource) {
        this.idImageData = idImageSource;
        this.idUploaded = true;
        FaceRecognition.storeIdImageData(idImageSource);
        sessionStorage.setItem('idImageData', idImageSource);
        sessionStorage.setItem('idUploaded', 'true');
    },
    
    // Step 2: Start selfie capture (called AFTER ID upload)
    startSelfieCapture() {
        
        this.selfieCaptured = false;
        this.verificationPassed = false;
        
        // Initialize UI if not done
        FaceRecognitionUI.init();
        
        // Open modal for selfie capture
        FaceRecognitionUI.openForSelfie(
            (result) => this.onSelfieComplete(result),
            (result) => this.onSelfieCancelled(result)
        );
    },
    
    // Called when selfie is captured successfully
    // NEW FLOW: Store in VerificationState instead of immediately comparing
    onSelfieComplete(result) {
        
        this.selfieCaptured = true;
        this.selfieImageData = result.imageData;
        this.selfieFileName = result.fileName;  // Store filename for API submission
        
        // Store in session for persistence
        sessionStorage.setItem('selfieCaptured', 'true');
        sessionStorage.setItem('selfieImageData', result.imageData);
        if (result.fileName) {
            sessionStorage.setItem('selfieFileName', result.fileName);
        }
        
        // ═══════════════════════════════════════════════════════════════════════════
        // NEW FLOW: Store selfie in VerificationState (NO API CALL YET)
        // Store both imageData and fileName for file-based API submission
        // ═══════════════════════════════════════════════════════════════════════════
        if (typeof VerificationState !== 'undefined') {
            VerificationState.setSelfie(result.imageData);
            VerificationState.selfieFileName = result.fileName;  // Store filename
        }
        
        // Show selfie success message (no auto-compare)
        this.showSelfieSuccessMessage();
        
        // NOTE: NO automatic comparison - user must click "Verify & Submit" button
        // The old flow was: if (this.idUploaded && this.idImageData) { this.compareWithId(...) }
        // New flow: Wait for user to click verify button which calls submitVerification()
    },
    
    // Called when user cancels selfie capture
    onSelfieCancelled(result) {
        
        this.selfieCaptured = false;
        
        // Show message that selfie is required
        this.showSelfieRequiredMessage();
    },
    
    // REMOVED: compareWithId() - This function made custom verification decisions
    // The system now follows ONLY the API's decision field (accept/review/reject)
    // Face verification is handled as part of the combined ID scan API call

    // REMOVED: onVerificationSuccess() - This function made custom "success" decisions
    // The system now follows ONLY the API's decision field (accept/review/reject)

    // REMOVED: onVerificationFailure() - This function made custom "failure" decisions
    // The system now follows ONLY the API's decision field (accept/review/reject)
    
    // Show selfie success message
    // NEW FLOW: Also update the selfie preview in the upload area
    showSelfieSuccessMessage() {
        // Update selfie preview in upload area
        const selfiePreview = document.getElementById('selfie-preview');
        const selfieCaptureArea = document.getElementById('selfie-capture-area');
        
        if (selfiePreview && this.selfieImageData) {
            selfiePreview.src = this.selfieImageData;
            selfiePreview.classList.remove('hidden');
        }
        
        if (selfieCaptureArea) {
            selfieCaptureArea.classList.add('has-image', 'active');
            const content = selfieCaptureArea.querySelector('.selfie-capture-content');
            if (content) {
                // Update text to show selfie captured
                const titleEl = content.querySelector('.font-semibold');
                if (titleEl) {
                    titleEl.innerHTML = '<i class="fas fa-check-circle text-green-500 mr-2"></i>Selfie Captured';
                }
                const descEl = content.querySelector('.text-gray-500');
                if (descEl) {
                    descEl.textContent = 'Click to retake selfie';
                }
            }
        }
        
        const container = document.getElementById('face-verification-status') || this.createStatusContainer();
        
        if (container) {
            container.innerHTML = `
                <div class="p-4 bg-green-50 border border-green-300 rounded-xl">
                    <div class="flex items-start gap-3">
                        <div class="w-10 h-10 bg-green-100 rounded-full flex items-center justify-center flex-shrink-0">
                            <i class="fas fa-camera-retro text-green-600 text-lg"></i>
                        </div>
                        <div>
                            <h4 class="font-semibold text-green-800">Selfie Captured</h4>
                            <p class="text-green-700 text-sm mt-1">
                                Your selfie has been captured. Click "Verify & Extract ID Data" to proceed.
                            </p>
                        </div>
                    </div>
                </div>
            `;
            container.classList.remove('hidden');
        }
    },
    
    // Show selfie required message (after ID upload)
    showSelfieRequiredMessage() {
        const container = document.getElementById('face-verification-status') || this.createStatusContainer();
        
        if (container) {
            container.innerHTML = `
                <div class="p-4 bg-blue-50 border border-blue-300 rounded-xl">
                    <div class="flex items-start gap-3">
                        <div class="w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center flex-shrink-0">
                            <i class="fas fa-user-check text-blue-600 text-lg"></i>
                        </div>
                        <div>
                            <h4 class="font-semibold text-blue-800">Face Verification Required</h4>
                            <p class="text-blue-700 text-sm mt-1">
                                Please take a selfie to verify your identity matches the ID you uploaded.
                            </p>
                            <button type="button" onclick="FaceVerificationFlow.startSelfieCapture()" 
                                class="mt-2 px-4 py-2 bg-blue-500 hover:bg-blue-600 text-white rounded-lg text-sm font-medium transition-colors">
                                <i class="fas fa-camera mr-2"></i>Take Selfie
                            </button>
                        </div>
                    </div>
                </div>
            `;
            container.classList.remove('hidden');
        }
    },
    
    // Show comparing status
    showComparingStatus() {
        const container = document.getElementById('face-verification-status') || this.createStatusContainer();
        
        if (container) {
            container.innerHTML = `
                <div class="p-4 bg-blue-50 border border-blue-300 rounded-xl">
                    <div class="flex items-center gap-3">
                        <div class="w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center flex-shrink-0">
                            <i class="fas fa-spinner fa-spin text-blue-600 text-lg"></i>
                        </div>
                        <div>
                            <h4 class="font-semibold text-blue-800">Verifying Identity...</h4>
                            <p class="text-blue-700 text-sm mt-1">
                                Comparing your selfie with the ID photo. Please wait...
                            </p>
                        </div>
                    </div>
                </div>
            `;
            container.classList.remove('hidden');
        }
    },
    
    // Enable ID upload section
    enableIdUpload() {
        const uploadArea = document.querySelector('.upload-area');
        const uploadInput = document.getElementById('idImage');
        
        if (uploadArea) {
            uploadArea.classList.remove('opacity-50', 'pointer-events-none');
        }
        if (uploadInput) {
            uploadInput.disabled = false;
        }
    },
    
    // Enable account verification form
    enableAccountVerificationForm() {
        const form = document.getElementById('registrationForm');
        const submitBtn = form?.querySelector('button[type="submit"]');
        
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.classList.remove('opacity-50', 'cursor-not-allowed');
        }
        
        // Enable all form fields
        const formFields = form?.querySelectorAll('input, select, textarea');
        formFields?.forEach(field => {
            field.disabled = false;
            field.classList.remove('opacity-50');
        });
    },
    
    // Disable account verification form
    disableAccountVerificationForm() {
        const form = document.getElementById('registrationForm');
        const submitBtn = form?.querySelector('button[type="submit"]');
        
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.classList.add('opacity-50', 'cursor-not-allowed');
        }
        
        // Disable all form fields except file inputs
        const formFields = form?.querySelectorAll('input:not([type="file"]), select, textarea');
        formFields?.forEach(field => {
            field.disabled = true;
            field.classList.add('opacity-50');
        });
    },
    
    // REMOVED: showSuccessMessage() - This function made custom "Face Verification Passed" decisions
    // The system now follows ONLY the API's decision field (accept/review/reject)

    // REMOVED: showFailureMessage() - This function made custom "Face Verification Failed" decisions
    // The system now follows ONLY the API's decision field (accept/review/reject)
    
    // Create status container if it doesn't exist
    createStatusContainer() {
        let container = document.getElementById('face-verification-status');
        
        if (!container) {
            // Find the ID upload section and insert before it
            const uploadSection = document.querySelector('.upload-area')?.closest('.space-y-4') || 
                                  document.querySelector('.space-y-6');
            
            if (uploadSection) {
                container = document.createElement('div');
                container.id = 'face-verification-status';
                container.className = 'mb-4';
                uploadSection.insertBefore(container, uploadSection.firstChild);
            }
        }
        
        return container;
    },
    
    // REMOVED: checkPreviousState() - This function made custom verification state decisions
    // The system now follows ONLY the API's decision field (accept/review/reject)
    // Face verification state is managed by the API response, not client-side session storage
    
    // Reset the flow
    reset() {
        this.selfieCaptured = false;
        this.selfieImageData = null;
        this.idUploaded = false;
        this.idImageData = null;
        this.verificationPassed = false;
        this.verificationResult = null;
        
        FaceRecognition.reset();
        
        sessionStorage.removeItem('selfieCaptured');
        sessionStorage.removeItem('selfieImageData');
        sessionStorage.removeItem('idUploaded');
        sessionStorage.removeItem('idImageData');
        sessionStorage.removeItem('faceVerificationPassed');
        sessionStorage.removeItem('faceVerificationSimilarity');
    }
};

// Export for global access
window.FaceRecognition = FaceRecognition;
window.FaceRecognitionUI = FaceRecognitionUI;
window.FaceVerificationFlow = FaceVerificationFlow;

// Auto-initialize UI when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    FaceRecognitionUI.init();
    
    // DO NOT auto-check previous state or show any UI on page load
    // Face verification should ONLY be triggered after successful ID upload
    // The FaceVerificationFlow.checkPreviousState() call is removed to prevent
    // showing face verification UI before ID is uploaded
    
    // Clear any stale session data on fresh page load
    // Only preserve if user is in the middle of verification
    const urlParams = new URLSearchParams(window.location.search);
    if (!urlParams.has('resume')) {
        // Fresh page load - don't show anything
        const statusContainer = document.getElementById('face-verification-status');
        if (statusContainer) {
            statusContainer.classList.add('hidden');
            statusContainer.innerHTML = '';
        }
    }
});

