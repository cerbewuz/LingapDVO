// ============================================================================
// FACIAL RECOGNITION MODULE - ID ANALYZER API v2 INTEGRATION
// ============================================================================
// Uses ID Analyzer API v2 Standard ID Scan for server-side face verification
// Base URL: https://api2.idanalyzer.com
//
// CORRECT FLOW (API v2 Best Practice):
// 1. Capture selfie FIRST
// 2. Upload ID with selfie in Standard ID Scan request (POST /scan)
// 3. API performs OCR + face matching in single request
// 4. Returns both document data AND face verification results
//
// Reference: https://developer.idanalyzer.com/reference/post-scan
// The 'face' parameter in scan request enables biometric verification
// ============================================================================

// Face Recognition Configuration
const FaceRecognitionConfig = {
    // Minimum similarity score to consider faces as matching (0-100)
    MATCH_THRESHOLD: 50,
    
    // API endpoints for ID Analyzer v2
    API_URL: '/api/IdAnalyzer',
    FACE_ENDPOINT: '/api/IdAnalyzer/face',
    SCAN_ENDPOINT: '/api/IdAnalyzer/scan',
    
    // Camera settings - adaptive based on device capability
    CAMERA_CONSTRAINTS: {
        // Primary constraints (ideal for good cameras)
        primary: {
            video: {
                width: { ideal: 640, max: 1280 },
                height: { ideal: 480, max: 720 },
                facingMode: 'user',
                frameRate: { ideal: 30, max: 60 }
            },
            audio: false
        },
        // Fallback constraints (for older/basic cameras)
        fallback: {
            video: {
                width: { ideal: 320, max: 640 },
                height: { ideal: 240, max: 480 },
                facingMode: 'user',
                frameRate: { ideal: 15, max: 30 }
            },
            audio: false
        },
        // Minimal constraints (last resort)
        minimal: {
            video: {
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
    selfieImageData: null,
    idImageData: null,
    videoStream: null,
    retryCount: 0,
    
    // DOM Elements (initialized on load)
    elements: {},
    
    // Initialize the module (no model loading needed - server-side processing)
    async init() {
        console.log('🔄 Initializing Face Recognition Module (ID Analyzer)...');
        
        if (this.isInitialized) {
            console.log('✅ Face Recognition already initialized');
            return true;
        }
        
        try {
            // Verify API is accessible
            const response = await fetch(`${FaceRecognitionConfig.API_URL}/health`);
            if (response.ok) {
                this.isInitialized = true;
                console.log('✅ Face Recognition Module (ID Analyzer) initialized successfully');
                return true;
            }
        } catch (error) {
            console.warn('⚠️ Could not verify API health, but continuing anyway');
        }
        
        // Initialize anyway - API might be available
        this.isInitialized = true;
        return true;
    },
    
    // Compare two faces using ID Analyzer Face Verification API
    // Endpoint: POST /api/IdAnalyzer/face
    // Reference: https://developer.idanalyzer.com/reference/post-face-2
    async compareFacesViaApi(referenceBase64, faceBase64) {
        console.log('🔐 Comparing faces via ID Analyzer Face Verification API...');
        
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
                console.error('❌ Face verification failed:', result.error);
                return {
                    match: false,
                    similarity: 0,
                    percentage: 0,
                    error: result.error
                };
            }
            
            console.log(`📊 Face verification result: Similarity=${result.similarityPercentage}%, Match=${result.isMatch}, Decision=${result.decision}`);
            
            return {
                match: result.isMatch,
                similarity: result.similarity,
                percentage: result.similarityPercentage,
                confidence: result.confidence,
                decision: result.decision
            };
        } catch (error) {
            console.error('❌ Error calling face verification API:', error);
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
        console.log('📄 Scanning ID via ID Analyzer Standard ID Scan API...');
        
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
                console.error('❌ ID scan failed:', result.error);
                return {
                    success: false,
                    error: result.error
                };
            }
            
            console.log('✅ ID scan successful');
            console.log('📋 Document type:', result.data.documentType);
            
            return {
                success: true,
                data: result.data
            };
        } catch (error) {
            console.error('❌ Error calling ID scan API:', error);
            return {
                success: false,
                error: error.message
            };
        }
    },
    
    // Store selfie image data
    storeSelfieImageData(imageData) {
        this.selfieImageData = imageData;
        console.log('💾 Selfie image data stored');
    },
    
    // Store ID image data
    storeIdImageData(imageData) {
        this.idImageData = imageData;
        console.log('💾 ID image data stored');
    },
    
    // Start webcam for selfie capture - OPTIMIZED with adaptive constraints
    async startCamera(videoElement) {
        console.log('📷 Starting camera with adaptive constraints...');
        
        // Stop any existing stream first
        this.stopCamera();
        
        // Get available cameras first
        let cameras = [];
        try {
            const devices = await navigator.mediaDevices.enumerateDevices();
            cameras = devices.filter(device => device.kind === 'videoinput');
            console.log(`📷 Found ${cameras.length} camera(s)`);
        } catch (e) {
            console.log('⚠️ Could not enumerate devices:', e);
        }
        
        // Try different constraint levels
        const constraintLevels = ['primary', 'fallback', 'minimal', 'any'];
        let lastError = null;
        
        for (const level of constraintLevels) {
            try {
                console.log(`📷 Trying ${level} constraints...`);
                const constraints = FaceRecognitionConfig.CAMERA_CONSTRAINTS[level];
                
                const stream = await navigator.mediaDevices.getUserMedia(constraints);
                
                // Get actual camera capabilities
                const videoTrack = stream.getVideoTracks()[0];
                if (videoTrack) {
                    const settings = videoTrack.getSettings();
                    console.log(`✅ Camera started with ${level} constraints`);
                    console.log(`📐 Actual resolution: ${settings.width}x${settings.height}`);
                    console.log(`🎬 Frame rate: ${settings.frameRate}`);
                    
                    // Apply optimal settings if available
                    if (videoTrack.getCapabilities) {
                        const capabilities = videoTrack.getCapabilities();
                        console.log('📷 Camera capabilities:', capabilities);
                    }
                }
                
                // Successfully got stream, now set it up
                return await this.setupVideoStream(videoElement, stream);
                
            } catch (error) {
                console.warn(`⚠️ ${level} constraints failed:`, error.name, error.message);
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
        console.log('📷 Setting up video stream...');
        
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
                
                console.log('✅ Video element ready, starting playback...');
                
                try {
                    // Try to play the video
                    await videoElement.play();
                    
                    console.log('✅ Camera playing successfully');
                    console.log(`📐 Video dimensions: ${videoElement.videoWidth}x${videoElement.videoHeight}`);
                    
                    // Additional check - ensure we have valid dimensions
                    if (videoElement.videoWidth === 0 || videoElement.videoHeight === 0) {
                        // Wait a bit more for dimensions
                        await new Promise(r => setTimeout(r, 500));
                        console.log(`📐 Video dimensions (after wait): ${videoElement.videoWidth}x${videoElement.videoHeight}`);
                    }
                    
                    resolve(true);
                } catch (err) {
                    console.error('❌ Video play error:', err);
                    reject(err);
                }
            };
            
            // Multiple event listeners for compatibility
            videoElement.oncanplay = onReady;
            videoElement.onloadedmetadata = () => {
                console.log('📷 Video metadata loaded');
            };
            videoElement.onloadeddata = () => {
                console.log('📷 Video data loaded');
                onReady();
            };
            
            videoElement.onerror = (err) => {
                if (resolved) return;
                resolved = true;
                cleanup();
                console.error('❌ Video element error:', err);
                reject(new Error('Failed to load video stream'));
            };
            
            // Periodic check for video readiness (backup)
            checkInterval = setInterval(() => {
                if (resolved) return;
                
                if (videoElement.readyState >= 2 && videoElement.videoWidth > 0) {
                    console.log('📷 Video ready via interval check');
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
                        console.log('⚠️ Timeout but video seems to be working');
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
                console.log(`📷 Stopped track: ${track.kind}`);
            });
            this.videoStream = null;
            console.log('📷 Camera stopped');
        }
    },
    
    // Capture selfie from video stream - OPTIMIZED for various resolutions
    async captureSelfie(videoElement, canvasElement) {
        console.log('📸 Capturing selfie...');
        
        // Get actual video dimensions
        const videoWidth = videoElement.videoWidth || 640;
        const videoHeight = videoElement.videoHeight || 480;
        
        console.log(`📐 Capturing from video: ${videoWidth}x${videoHeight}`);
        
        // Calculate optimal canvas size (balance quality vs performance)
        let captureWidth = videoWidth;
        let captureHeight = videoHeight;
        const maxDimension = 800;
        
        if (videoWidth > maxDimension || videoHeight > maxDimension) {
            const scale = maxDimension / Math.max(videoWidth, videoHeight);
            captureWidth = Math.round(videoWidth * scale);
            captureHeight = Math.round(videoHeight * scale);
            console.log(`📐 Scaling down to: ${captureWidth}x${captureHeight}`);
        }
        
        // Set canvas size
        canvasElement.width = captureWidth;
        canvasElement.height = captureHeight;
        
        const context = canvasElement.getContext('2d');
        
        // Enable image smoothing for better quality
        context.imageSmoothingEnabled = true;
        context.imageSmoothingQuality = 'high';
        
        // Draw video frame to canvas (mirror for selfie)
        context.save();
        context.translate(captureWidth, 0);
        context.scale(-1, 1);
        context.drawImage(videoElement, 0, 0, captureWidth, captureHeight);
        context.restore();
        
        // Get image data as base64
        const imageData = canvasElement.toDataURL('image/jpeg', 0.85);
        
        // Store selfie image data for later comparison
        this.storeSelfieImageData(imageData);
        
        console.log('✅ Selfie captured successfully');
        
        return {
            success: true,
            imageData: imageData
        };
    },
    
    // Perform face verification using ID Analyzer API
    async verifyFace() {
        console.log('🔐 Performing face verification via ID Analyzer...');
        
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
        
        if (comparison.match) {
            console.log('✅ Face verification PASSED');
            return {
                success: true,
                match: true,
                similarity: comparison.percentage,
                message: `Face verified successfully! (${comparison.percentage}% match)`
            };
        } else {
            console.log('❌ Face verification FAILED');
            return {
                success: true,
                match: false,
                similarity: comparison.percentage,
                message: `Face verification failed. Similarity: ${comparison.percentage}%`
            };
        }
    },
    
    // Reset state
    reset() {
        this.selfieImageData = null;
        this.idImageData = null;
        this.retryCount = 0;
        this.stopCamera();
        console.log('🔄 Face Recognition state reset');
    },
    
    // Store selfie data (for backward compatibility)
    storeSelfieDescriptor(descriptor, imageData) {
        this.selfieImageData = imageData;
        console.log('💾 Selfie image data stored');
    },
    
    // Compare selfie with ID face (called after ID is uploaded)
    async compareWithId(idImageSource) {
        console.log('🔐 Comparing selfie with ID face via ID Analyzer...');
        
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
    
    // Initialize UI
    init() {
        this.cacheElements();
        this.bindEvents();
        console.log('✅ Face Recognition UI initialized');
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
            this.elements.captureBtn.addEventListener('click', () => this.startCountdown());
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
        console.log('🔓 Opening Face Recognition Modal for Selfie Capture...');
        
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
            
            console.log('📷 Attempting to start camera...');
            await FaceRecognition.startCamera(this.elements.video);
            console.log('✅ Camera started successfully in UI');
            
            // Restore normal instructions after camera starts
            this.restoreNormalInstructions();
            
            this.updateStatus('Position your face in the frame and click "Capture"', 'ready');
            this.showCaptureButton();
            
        } catch (error) {
            console.error('❌ Camera error in UI:', error);
            console.error('Error name:', error.name);
            console.error('Error message:', error.message);
            
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
                    <i class="fas fa-info-circle text-blue-600"></i> Take a Selfie
                </h4>
                <ul class="mt-2 text-blue-700 text-sm list-disc list-inside space-y-1">
                    <li>Ensure good lighting on your face</li>
                    <li>Remove glasses, hats, or face coverings</li>
                    <li>Position your face within the oval frame</li>
                    <li>Look directly at the camera</li>
                    <li>Your selfie will be compared with your ID photo</li>
                </ul>
            `;
            instructionsEl.className = 'face-instructions bg-gradient-to-br from-blue-50 to-blue-100 border-blue-200';
        }
    },
    
    // Close modal
    closeModal() {
        console.log('🔒 Closing Face Recognition Modal...');
        
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
    startCountdown() {
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
                this.captureSelfie();
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
    async captureSelfie() {
        console.log('📸 Capturing selfie...');
        
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
        this.updateStatus('Selfie captured successfully! You can now upload your ID.', 'success');
        this.hideButtons();
        
        // Auto-close after delay and callback
        setTimeout(() => {
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
        console.log('🔄 Retrying...');
        
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
    
    showMatchResult(isMatch, percentage) {
        if (this.elements.matchResult) {
            this.elements.matchResult.classList.remove('hidden');
            this.elements.matchResult.className = isMatch 
                ? 'face-match-result success'
                : 'face-match-result failure';
        }
        
        // Update header
        const headerEl = document.getElementById('matchResultHeader');
        const titleEl = document.getElementById('matchResultTitle');
        
        if (headerEl) {
            headerEl.className = isMatch ? 'face-match-header success' : 'face-match-header failure';
            const icon = headerEl.querySelector('i');
            if (icon) {
                icon.className = isMatch ? 'fas fa-check-circle' : 'fas fa-times-circle';
            }
        }
        
        if (titleEl) {
            titleEl.textContent = isMatch ? 'Face Verified Successfully!' : 'Face Verification Failed';
        }
        
        if (this.elements.matchPercentage) {
            this.elements.matchPercentage.textContent = `${percentage}%`;
            this.elements.matchPercentage.className = isMatch ? 'text-green-600 font-semibold' : 'text-red-600 font-semibold';
        }
        
        if (this.elements.progressBar) {
            this.elements.progressBar.style.width = `${percentage}%`;
            this.elements.progressBar.className = isMatch 
                ? 'face-match-percentage-fill success'
                : 'face-match-percentage-fill failure';
        }
    },
    
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
// Flow: ID Upload First → Then Facial Recognition (Selfie) → Compare
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
        console.log('💾 Storing ID image for face verification...');
        this.idImageData = idImageSource;
        this.idUploaded = true;
        FaceRecognition.storeIdImageData(idImageSource);
        sessionStorage.setItem('idImageData', idImageSource);
        sessionStorage.setItem('idUploaded', 'true');
    },
    
    // Step 2: Start selfie capture (called AFTER ID upload)
    startSelfieCapture() {
        console.log('🚀 Starting Selfie Capture Flow...');
        
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
    onSelfieComplete(result) {
        console.log('✅ Selfie captured successfully', result);
        
        this.selfieCaptured = true;
        this.selfieImageData = result.imageData;
        
        // Store in session for persistence
        sessionStorage.setItem('selfieCaptured', 'true');
        sessionStorage.setItem('selfieImageData', result.imageData);
        
        // Show selfie success message
        this.showSelfieSuccessMessage();
        
        // Automatically compare with ID if ID was uploaded
        if (this.idUploaded && this.idImageData) {
            this.compareWithId(this.idImageData);
        }
    },
    
    // Called when user cancels selfie capture
    onSelfieCancelled(result) {
        console.log('❌ Selfie cancelled', result);
        
        this.selfieCaptured = false;
        
        // Show message that selfie is required
        this.showSelfieRequiredMessage();
    },
    
    // Step 3: Compare selfie with ID (called after selfie capture)
    async compareWithId(idImageSource) {
        console.log('🔐 Comparing selfie with ID...');
        
        if (!this.selfieCaptured && !FaceRecognition.selfieImageData) {
            console.warn('⚠️ No selfie captured yet');
            return {
                success: false,
                error: 'Please take a selfie first.'
            };
        }
        
        // Store ID image if not already stored
        if (idImageSource && !this.idImageData) {
            this.storeIdImage(idImageSource);
        }
        
        // Show comparing status
        this.showComparingStatus();
        
        // Compare with ID
        const result = await FaceRecognition.compareWithId(idImageSource || this.idImageData);
        
        if (!result.success) {
            this.onVerificationFailure({ reason: 'comparison_failed', message: result.error });
            return result;
        }
        
        if (result.match) {
            this.onVerificationSuccess(result);
        } else {
            this.onVerificationFailure({ reason: 'no_match', similarity: result.similarity });
        }
        
        return result;
    },
    
    // Called when verification succeeds
    onVerificationSuccess(result) {
        console.log('✅ Face Verification Flow: SUCCESS', result);
        
        this.verificationPassed = true;
        this.verificationResult = result;
        
        // Enable form submission
        this.enableAccountVerificationForm();
        
        // Show success message
        this.showSuccessMessage(result.similarity);
        
        // Store verification status
        sessionStorage.setItem('faceVerificationPassed', 'true');
        sessionStorage.setItem('faceVerificationSimilarity', result.similarity);
    },
    
    // Called when verification fails
    onVerificationFailure(result) {
        console.log('❌ Face Verification Flow: FAILURE', result);
        
        this.verificationPassed = false;
        this.verificationResult = result;
        
        // Disable form submission
        this.disableAccountVerificationForm();
        
        // Show failure message
        this.showFailureMessage(result.reason, result.similarity);
        
        // Clear verification status
        sessionStorage.removeItem('faceVerificationPassed');
        sessionStorage.removeItem('faceVerificationSimilarity');
    },
    
    // Show selfie success message
    showSelfieSuccessMessage() {
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
                                Your face has been captured. Verifying with your ID...
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
    
    // Show success message
    showSuccessMessage(similarity) {
        const container = document.getElementById('face-verification-status') || this.createStatusContainer();
        
        if (container) {
            container.innerHTML = `
                <div class="p-4 bg-green-50 border border-green-300 rounded-xl">
                    <div class="flex items-start gap-3">
                        <div class="w-10 h-10 bg-green-100 rounded-full flex items-center justify-center flex-shrink-0">
                            <i class="fas fa-user-check text-green-600 text-lg"></i>
                        </div>
                        <div>
                            <h4 class="font-semibold text-green-800">Face Verification Passed</h4>
                            <p class="text-green-700 text-sm mt-1">
                                Your identity has been verified successfully. (${similarity}% match)
                            </p>
                            <p class="text-green-600 text-xs mt-2">
                                <i class="fas fa-check mr-1"></i>You may now complete and submit the verification form.
                            </p>
                        </div>
                    </div>
                </div>
            `;
            container.classList.remove('hidden');
        }
    },
    
    // Show failure message
    showFailureMessage(reason, similarity) {
        const container = document.getElementById('face-verification-status') || this.createStatusContainer();
        
        if (container) {
            let message = 'Your face could not be matched with the ID photo.';
            if (reason === 'comparison_failed') {
                message = 'Could not detect a face in the ID photo. Please upload a clearer image.';
            } else if (similarity !== undefined) {
                message = `Face match similarity too low (${similarity}%). Please try again with better lighting or a clearer ID.`;
            }
            
            container.innerHTML = `
                <div class="p-4 bg-red-50 border border-red-300 rounded-xl">
                    <div class="flex items-start gap-3">
                        <div class="w-10 h-10 bg-red-100 rounded-full flex items-center justify-center flex-shrink-0">
                            <i class="fas fa-user-times text-red-600 text-lg"></i>
                        </div>
                        <div>
                            <h4 class="font-semibold text-red-800">Face Verification Failed</h4>
                            <p class="text-red-700 text-sm mt-1">${message}</p>
                            <p class="text-red-600 text-xs mt-2">
                                <i class="fas fa-info-circle mr-1"></i>Form submission is disabled until face verification passes.
                            </p>
                            <button type="button" onclick="FaceVerificationFlow.startSelfieCapture()" 
                                class="mt-2 px-4 py-2 bg-red-500 hover:bg-red-600 text-white rounded-lg text-sm font-medium transition-colors">
                                <i class="fas fa-redo mr-2"></i>Retake Selfie
                            </button>
                        </div>
                    </div>
                </div>
            `;
            container.classList.remove('hidden');
        }
    },
    
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
    
    // Check if selfie was already captured (page reload)
    checkPreviousState() {
        const selfieCaptured = sessionStorage.getItem('selfieCaptured');
        const verificationPassed = sessionStorage.getItem('faceVerificationPassed');
        const idUploaded = sessionStorage.getItem('idUploaded');
        
        if (verificationPassed === 'true') {
            this.verificationPassed = true;
            this.selfieCaptured = true;
            this.idUploaded = true;
            const similarity = sessionStorage.getItem('faceVerificationSimilarity') || '0';
            this.showSuccessMessage(similarity);
            this.enableAccountVerificationForm();
            return true;
        }
        
        if (idUploaded === 'true') {
            this.idUploaded = true;
            this.idImageData = sessionStorage.getItem('idImageData');
            
            if (selfieCaptured === 'true') {
                this.selfieCaptured = true;
                this.selfieImageData = sessionStorage.getItem('selfieImageData');
                // Both captured, but verification not passed - show comparing status
                this.showComparingStatus();
            } else {
                // ID uploaded but no selfie yet - prompt for selfie
                this.showSelfieRequiredMessage();
            }
            return true;
        }
        
        return false;
    },
    
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

console.log('✅ Facial Recognition Module loaded (ID Analyzer API)');
