// Image Gallery with Zoom and Navigation
const images = Array.from(document.querySelectorAll(".update-image")).map(img => img.src);
let currentIndex = 0;
let currentZoom = 1;
let isDragging = false;
let startX = 0, startY = 0;
let translateX = 0, translateY = 0;
let initialDistance = 0;
let lastTapTime = 0;

const modal = document.getElementById("imageModal");
const modalImage = document.getElementById("modalImage");
const modalClose = document.getElementById("modalClose");
const prevBtn = document.getElementById("prevImageBtn");
const nextBtn = document.getElementById("nextImageBtn");
const zoomInBtn = document.getElementById("zoomInBtn");
const zoomOutBtn = document.getElementById("zoomOutBtn");
const resetZoomBtn = document.getElementById("resetZoomBtn");
const zoomIndicator = document.getElementById("zoomIndicator");

// Reset zoom when changing images
function resetZoom() {
    currentZoom = 1;
    translateX = 0;
    translateY = 0;
    updateImageTransform();
    modalImage.classList.remove('zoomed');
}

// Update image transform
function updateImageTransform() {
    modalImage.style.transform = `scale(${currentZoom}) translate(${translateX}px, ${translateY}px)`;
    zoomIndicator.textContent = Math.round(currentZoom * 100) + '%';
}

// Show image
function showImage(index) {
    currentIndex = index;
    modalImage.src = images[currentIndex];
    resetZoom();
}

// Navigate to previous image
function previousImage() {
    currentIndex = (currentIndex - 1 + images.length) % images.length;
    showImage(currentIndex);
}

// Navigate to next image
function nextImage() {
    currentIndex = (currentIndex + 1) % images.length;
    showImage(currentIndex);
}

// Zoom in
function zoomIn() {
    if (currentZoom < 3) {
        currentZoom = Math.min(currentZoom + 0.25, 3);
        updateImageTransform();
        modalImage.classList.add('zoomed');
    }
}

// Zoom out
function zoomOut() {
    if (currentZoom > 1) {
        currentZoom = Math.max(currentZoom - 0.25, 1);
        if (currentZoom === 1) {
            translateX = 0;
            translateY = 0;
            modalImage.classList.remove('zoomed');
        }
        updateImageTransform();
    }
}

// Open modal when image is clicked
document.querySelectorAll(".update-image").forEach((img, index) => {
    img.addEventListener("click", () => {
        showImage(index);
        modal.style.display = "flex";
    });
});

// Close modal
function closeModal() {
    modal.style.display = "none";
    resetZoom();
}

modalClose.addEventListener("click", closeModal);

// Click outside to close
modal.addEventListener("click", (e) => {
    if (e.target === modal) {
        closeModal();
    }
});

// Arrow button handlers
prevBtn.addEventListener("click", (e) => {
    e.stopPropagation();
    previousImage();
});

nextBtn.addEventListener("click", (e) => {
    e.stopPropagation();
    nextImage();
});

// Zoom button handlers
zoomInBtn.addEventListener("click", (e) => {
    e.stopPropagation();
    zoomIn();
});

zoomOutBtn.addEventListener("click", (e) => {
    e.stopPropagation();
    zoomOut();
});

resetZoomBtn.addEventListener("click", (e) => {
    e.stopPropagation();
    resetZoom();
});

// Keyboard navigation
document.addEventListener("keydown", (e) => {
    if (modal.style.display === "flex") {
        if (e.key === "ArrowLeft") {
            previousImage();
        } else if (e.key === "ArrowRight") {
            nextImage();
        } else if (e.key === "Escape") {
            closeModal();
        } else if (e.key === "+" || e.key === "=") {
            zoomIn();
        } else if (e.key === "-") {
            zoomOut();
        } else if (e.key === "0") {
            resetZoom();
        }
    }
});

// Mouse wheel zoom
modalImage.addEventListener("wheel", (e) => {
    e.preventDefault();
    if (e.deltaY < 0) {
        zoomIn();
    } else {
        zoomOut();
    }
});

// Touch events for mobile
let touchStartX = 0;
let touchStartY = 0;

// Double tap to zoom
modalImage.addEventListener("touchend", (e) => {
    const currentTime = new Date().getTime();
    const tapLength = currentTime - lastTapTime;

    if (tapLength < 300 && tapLength > 0) {
        // Double tap detected
        if (currentZoom === 1) {
            currentZoom = 2;
            modalImage.classList.add('zoomed');
        } else {
            resetZoom();
        }
        updateImageTransform();
    }
    lastTapTime = currentTime;
});

// Pinch to zoom
modalImage.addEventListener("touchstart", (e) => {
    if (e.touches.length === 2) {
        e.preventDefault();
        const touch1 = e.touches[0];
        const touch2 = e.touches[1];
        initialDistance = Math.hypot(
            touch2.clientX - touch1.clientX,
            touch2.clientY - touch1.clientY
        );
    } else if (e.touches.length === 1 && currentZoom > 1) {
        touchStartX = e.touches[0].clientX - translateX;
        touchStartY = e.touches[0].clientY - translateY;
        isDragging = true;
    }
});

modalImage.addEventListener("touchmove", (e) => {
    if (e.touches.length === 2) {
        e.preventDefault();
        const touch1 = e.touches[0];
        const touch2 = e.touches[1];
        const currentDistance = Math.hypot(
            touch2.clientX - touch1.clientX,
            touch2.clientY - touch1.clientY
        );

        const scale = currentDistance / initialDistance;
        currentZoom = Math.max(1, Math.min(3, currentZoom * scale));
        initialDistance = currentDistance;

        if (currentZoom > 1) {
            modalImage.classList.add('zoomed');
        } else {
            modalImage.classList.remove('zoomed');
            translateX = 0;
            translateY = 0;
        }
        updateImageTransform();
    } else if (e.touches.length === 1 && isDragging && currentZoom > 1) {
        e.preventDefault();
        translateX = e.touches[0].clientX - touchStartX;
        translateY = e.touches[0].clientY - touchStartY;
        updateImageTransform();
    }
});

modalImage.addEventListener("touchend", (e) => {
    if (e.touches.length === 0) {
        isDragging = false;
    }
});

// Mouse drag when zoomed
modalImage.addEventListener("mousedown", (e) => {
    if (currentZoom > 1) {
        isDragging = true;
        startX = e.clientX - translateX;
        startY = e.clientY - translateY;
        modalImage.style.cursor = 'grabbing';
    }
});

document.addEventListener("mousemove", (e) => {
    if (isDragging && currentZoom > 1) {
        translateX = e.clientX - startX;
        translateY = e.clientY - startY;
        updateImageTransform();
    }
});

document.addEventListener("mouseup", () => {
    if (isDragging) {
        isDragging = false;
        if (currentZoom > 1) {
            modalImage.style.cursor = 'move';
        } else {
            modalImage.style.cursor = 'zoom-in';
        }
    }
});

// Swipe navigation (only when not zoomed)
let swipeStartX = 0;
let swipeStartY = 0;

modal.addEventListener("touchstart", (e) => {
    if (currentZoom === 1 && e.target !== modalImage) {
        swipeStartX = e.touches[0].clientX;
        swipeStartY = e.touches[0].clientY;
    }
});

modal.addEventListener("touchend", (e) => {
    if (currentZoom === 1 && e.target !== modalImage) {
        const swipeEndX = e.changedTouches[0].clientX;
        const swipeEndY = e.changedTouches[0].clientY;
        const deltaX = swipeEndX - swipeStartX;
        const deltaY = Math.abs(swipeEndY - swipeStartY);

        // Only trigger swipe if horizontal movement is dominant
        if (Math.abs(deltaX) > 50 && deltaY < 100) {
            if (deltaX < 0) {
                nextImage();
            } else {
                previousImage();
            }
        }
    }
});

// DOM Content Loaded event listeners
document.addEventListener('DOMContentLoaded', function() {
    // Remove loading screen immediately - no artificial delay
    const loadingScreen = document.getElementById('loadingScreen');
    if (loadingScreen) {
        loadingScreen.classList.add('hide');
    }

    // Navbar scroll effect
    const navbar = document.getElementById('navbar');
    window.addEventListener('scroll', function() {
        if (window.scrollY > 50) {
            navbar.classList.add('scrolled');
        } else {
            navbar.classList.remove('scrolled');
        }
    });

    // Mobile menu toggle
    const menuToggle = document.getElementById('menuToggle');
    const mobileMenu = document.getElementById('mobileMenu');
    const mobileOverlay = document.getElementById('mobileOverlay');
    const mobileMenuClose = document.getElementById('mobileMenuClose');
    const mobileNavLinks = document.querySelectorAll('.mobile-nav-link');

    function openMobileMenu() {
        mobileMenu.classList.add('active');
        mobileOverlay.classList.add('active');
        document.body.style.overflow = 'hidden';
    }

    function closeMobileMenu() {
        mobileMenu.classList.remove('active');
        mobileOverlay.classList.remove('active');
        document.body.style.overflow = '';
    }

    menuToggle.addEventListener('click', openMobileMenu);
    mobileMenuClose.addEventListener('click', closeMobileMenu);
    mobileOverlay.addEventListener('click', closeMobileMenu);

    mobileNavLinks.forEach(link => {
        link.addEventListener('click', closeMobileMenu);
    });

    // Smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const href = this.getAttribute('href');
            if (href === '#') return;

            e.preventDefault();
            const target = document.querySelector(href);
            if (target) {
                const offsetTop = target.offsetTop - 80;
                window.scrollTo({
                    top: offsetTop,
                    behavior: 'smooth'
                });
            }
        });
    });

    // Image Modal/Lightbox functionality
    const imageModal = document.getElementById('imageModal');
    const modalImage = document.getElementById('modalImage');
    const modalCaption = document.getElementById('modalCaption');
    const modalClose = document.getElementById('modalClose');
    const gridItems = document.querySelectorAll('.grid-item');

    // Open modal when grid item is clicked
    gridItems.forEach(item => {
        item.addEventListener('click', function() {
            const img = this.querySelector('img');
            const badge = this.querySelector('.grid-badge');

            modalImage.src = img.src;
            modalImage.alt = img.alt;
            modalCaption.textContent = badge ? badge.textContent : img.alt;

            imageModal.classList.add('active');
            document.body.style.overflow = 'hidden';
        });
    });

    // Close modal when close button is clicked
    modalClose.addEventListener('click', function(e) {
        e.stopPropagation();
        imageModal.classList.remove('active');
        document.body.style.overflow = '';
    });

    // Close modal when clicking outside the image
    imageModal.addEventListener('click', function(e) {
        if (e.target === imageModal) {
            imageModal.classList.remove('active');
            document.body.style.overflow = '';
        }
    });

    // Close modal with Escape key
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && imageModal.classList.contains('active')) {
            imageModal.classList.remove('active');
            document.body.style.overflow = '';
        }
    });

    // Process Section Scroll Animation
    const processTimeline = document.querySelector('.process-timeline');
    const processSteps = document.querySelectorAll('.process-step');

            const processObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                                // Animate timeline line
                if (entry.target.classList.contains('process-timeline')) {
                    entry.target.classList.add('animate-line');
                                    }

                // Animate process steps
                if (entry.target.classList.contains('process-step')) {
                    entry.target.classList.add('animate-in');
                                    }
            }
        });
    }, {
        threshold: 0.1,
        rootMargin: '0px 0px -10px 0px'
    });

    // Observe timeline
    if (processTimeline) {
        processObserver.observe(processTimeline);
            }

    // Observe each step
    processSteps.forEach((step, index) => {
        processObserver.observe(step);
            });

    // Counter animation
    const counters = document.querySelectorAll('.counter');
    const speed = 200;
    let hasAnimated = false;

    const animateCounters = () => {
        if (hasAnimated) return;

        const heroStats = document.querySelector('.hero-stats');
        if (!heroStats) return;

        const rect = heroStats.getBoundingClientRect();
        const isVisible = rect.top < window.innerHeight && rect.bottom >= 0;

        if (isVisible) {
            hasAnimated = true;
            counters.forEach(counter => {
                const target = +counter.getAttribute('data-target');
                const increment = target / speed;

                const updateCount = () => {
                    const count = +counter.innerText;
                    if (count < target) {
                        counter.innerText = Math.ceil(count + increment);
                        setTimeout(updateCount, 10);
                    } else {
                        counter.innerText = target.toLocaleString();
                    }
                };

                updateCount();
            });
        }
    };

    window.addEventListener('scroll', animateCounters);
    animateCounters(); // Check on load

    // Start Application button - redirect to login with modal info
    const startAppBtn = document.getElementById('startApplicationBtn');
    if (startAppBtn) {
        startAppBtn.addEventListener('click', function(e) {
            e.preventDefault();

            // Show a quick info alert before redirecting
            const proceed = confirm(
                "Paano Magsimula:\n\n" +
                "1. Lumikha ng Account sa registration page\n" +
                "2. Mag-login gamit ang inyong credentials\n" +
                "3. I-verify ang account sa pag-upload ng Valid ID\n" +
                "4. Piliin ang uri ng tulong at kumpletuhin ang form\n\n" +
                "Click OK para magpatuloy sa Login page"
            );

            if (proceed) {
                window.location.href = '/Login';
            }
        });
    }

    // Parallax effect for hero background
    window.addEventListener('scroll', function() {
        const scrolled = window.pageYOffset;
        const parallax = document.querySelector('.hero-background');
        if (parallax) {
            parallax.style.transform = 'translateY(' + scrolled * 0.5 + 'px)';
        }
    });

    // Card mouse tracking effect
    const cards = document.querySelectorAll('.feature-card');
    cards.forEach(card => {
        card.addEventListener('mousemove', function(e) {
            const rect = card.getBoundingClientRect();
            const x = ((e.clientX - rect.left) / rect.width) * 100;
            const y = ((e.clientY - rect.top) / rect.height) * 100;
            card.style.setProperty('--mouse-x', x + '%');
            card.style.setProperty('--mouse-y', y + '%');
        });
    });

    // Smooth reveal on scroll
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
            }
        });
    }, observerOptions);

    // Observe elements for scroll animations
    document.querySelectorAll('.section-header, .feature-card, .process-step, .fade-in-up, .fade-in').forEach(el => {
        observer.observe(el);
    });

    // Add subtle rotation effect to feature icons on hover
    document.querySelectorAll('.feature-icon').forEach(icon => {
        const card = icon.closest('.feature-card');
        if (card) {
            card.addEventListener('mouseenter', () => {
                icon.style.transform = 'rotateY(360deg)';
            });
            card.addEventListener('mouseleave', () => {
                icon.style.transform = 'rotateY(0deg)';
            });
        }
    });

    // Add ripple effect on button click
    document.querySelectorAll('.btn-primary-custom, .btn-secondary-custom').forEach(button => {
        button.addEventListener('click', function(e) {
            const ripple = document.createElement('span');
            const rect = this.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            const x = e.clientX - rect.left - size / 2;
            const y = e.clientY - rect.top - size / 2;

            ripple.style.cssText = `
                position: absolute;
                width: ${size}px;
                height: ${size}px;
                border-radius: 50%;
                background: rgba(255, 255, 255, 0.6);
                left: ${x}px;
                top: ${y}px;
                pointer-events: none;
                animation: ripple 0.6s ease-out;
            `;

            this.appendChild(ripple);
            setTimeout(() => ripple.remove(), 600);
        });
    });
});
