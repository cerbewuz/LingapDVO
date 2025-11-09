// Enhanced Go Back Button functionality with proper URL handling
document.addEventListener("DOMContentLoaded", function () {
    const goBackButton = document.querySelector(".go-back-btn");

    if (goBackButton) {
        goBackButton.addEventListener("click", function (e) {
            e.preventDefault(); // Prevent default link behavior

            // Check for data-url attribute first, then href
            const targetUrl = goBackButton.getAttribute("data-url") || goBackButton.getAttribute("href");

            if (targetUrl && targetUrl.trim() !== "" && targetUrl !== "#") {
                window.location.href = targetUrl; // Redirect to the specific page
            } else {
                // Check if there's history to go back to
                if (window.history.length > 1) {
                    window.history.back(); // Go to the previous page
                } else {
                    // Fallback to homepage if no history
                    window.location.href = "/Homepage";
                }
            }
        });
    }
});
