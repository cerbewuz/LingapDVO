# Go Back Button - Customization Guide

## Overview
The Go Back Button is a uniform, customizable navigation element that appears across all pages in the LingapDVO application. It provides consistent user experience with automatic mobile-responsive behavior and flexible navigation options.

## Features
- **Uniform styling** across all pages
- **Mobile-responsive** - hides on scroll down, shows on scroll up (mobile only)
- **Always visible** on desktop screens
- **Customizable behavior** via data attributes
- **Fallback navigation** to homepage when no history exists

## Basic Usage

### Default Behavior (Auto Mode)
By default, the button uses browser history to go back. If no history exists, it redirects to `/Homepage`.

```html
<button id="goBackBtn">
    <i class="fas fa-arrow-left"></i>
    <span>Go Back</span>
</button>
```

## Customization Options

### 1. Custom URL Navigation
Navigate to a specific URL instead of using browser history.

```html
<button id="goBackBtn" data-back-url="/Dashboard">
    <i class="fas fa-arrow-left"></i>
    <span>Go Back</span>
</button>
```

**Use cases:**
- Return to a specific dashboard after form submission
- Navigate to a parent page in a multi-step form
- Go to a specific page when history navigation is unreliable

### 2. Force History Navigation
Always use browser history, with fallback to homepage.

```html
<button id="goBackBtn" data-back-action="history">
    <i class="fas fa-arrow-left"></i>
    <span>Go Back</span>
</button>
```

**Use cases:**
- Standard navigation where you want to preserve browsing context
- When the previous page is always relevant

### 3. Custom JavaScript Function
Call a custom JavaScript function for complete control over navigation behavior.

```html
<button id="goBackBtn" data-back-custom="myCustomBackFunction">
    <i class="fas fa-arrow-left"></i>
    <span>Go Back</span>
</button>

<script>
    function myCustomBackFunction() {
        // Custom logic here
        if (confirm('Are you sure you want to leave?')) {
            window.location.href = '/Dashboard';
        }
    }
</script>
```

**Use cases:**
- Show confirmation dialogs before leaving
- Save form data before navigating
- Complex navigation logic based on application state
- Analytics tracking before navigation

## Data Attributes Reference

| Attribute | Values | Priority | Description |
|-----------|--------|----------|-------------|
| `data-back-custom` | Function name (string) | 1 (Highest) | Calls a custom JavaScript function |
| `data-back-url` | URL path (string) | 2 | Navigates to specified URL |
| `data-back-action` | `"auto"`, `"history"`, `"url"` | 3 | Sets navigation behavior mode |

### Priority Order
1. **Custom Function** (`data-back-custom`) - Highest priority
2. **Custom URL** (`data-back-url`) - Medium priority
3. **Action Mode** (`data-back-action`) - Lowest priority
4. **Default** (auto mode) - No attributes set

## Examples

### Example 1: Admin Dashboard Return
```html
<!-- Always return to admin dashboard -->
<button id="goBackBtn" data-back-url="/Adminuser/Admin">
    <i class="fas fa-arrow-left"></i>
    <span>Go Back</span>
</button>
```

### Example 2: Form with Unsaved Changes
```html
<button id="goBackBtn" data-back-custom="confirmLeave">
    <i class="fas fa-arrow-left"></i>
    <span>Go Back</span>
</button>

<script>
    let hasUnsavedChanges = false;

    // Track form changes
    document.querySelector('form').addEventListener('input', function() {
        hasUnsavedChanges = true;
    });

    function confirmLeave() {
        if (hasUnsavedChanges) {
            if (confirm('You have unsaved changes. Are you sure you want to leave?')) {
                window.history.back();
            }
        } else {
            window.history.back();
        }
    }
</script>
```

### Example 3: Multi-Step Form Navigation
```html
<!-- Step 2 of 3 - go back to step 1 -->
<button id="goBackBtn" data-back-url="/Application/Step1">
    <i class="fas fa-arrow-left"></i>
    <span>Go Back</span>
</button>
```

### Example 4: Conditional Navigation
```html
<button id="goBackBtn" data-back-custom="smartBack">
    <i class="fas fa-arrow-left"></i>
    <span>Go Back</span>
</button>

<script>
    function smartBack() {
        const userRole = '@User.IsInRole("Admin")';

        if (userRole === 'True') {
            window.location.href = '/Adminuser/Admin';
        } else {
            window.location.href = '/Dashboard/Homepage';
        }
    }
</script>
```

## Mobile Behavior

### Automatic Hide/Show on Scroll
On mobile devices (screen width ≤ 768px), the button automatically:
- **Hides** when scrolling down
- **Shows** when scrolling up
- **Always visible** on desktop

This behavior is built-in and requires no configuration.

### CSS Classes
The button behavior uses these CSS classes:
- `.go-back-visible` - Button is shown
- `.go-back-hidden` - Button is hidden (mobile scroll)

## Required Files

### JavaScript
- `/wwwroot/js/go-back-button.js` - Main functionality

### CSS
- `/wwwroot/css/go-back-button.css` - Styling and animations

### HTML Element
```html
<button id="goBackBtn">
    <i class="fas fa-arrow-left"></i>
    <span>Go Back</span>
</button>
```

## Troubleshooting

### Button Not Working
1. Check if `go-back-button.js` is loaded
2. Verify the button has `id="goBackBtn"`
3. Check browser console for errors

### Custom Function Not Called
1. Ensure the function is defined in global scope (window)
2. Check the function name matches `data-back-custom` value exactly
3. Verify the function is defined before the button is clicked

### Button Not Hiding on Mobile
1. Check if `go-back-button.css` is loaded
2. Verify screen width is ≤ 768px
3. Check if CSS classes are applied in browser DevTools

## Browser Console Logging

The button logs its behavior to the console for debugging:
```
Go Back Button: Initialized
Go Back Button: Scroll behavior and click handler attached
Go Back Button: Navigating to previous page
Go Back Button: Calling custom function - myCustomFunction
Go Back Button: Navigating to custom URL - /Dashboard
```

## Best Practices

1. **Use `data-back-url`** for predictable navigation patterns
2. **Use `data-back-custom`** when you need confirmation dialogs or complex logic
3. **Keep custom functions simple** and focused on navigation
4. **Test on mobile** to ensure scroll behavior works as expected
5. **Provide fallback behavior** in custom functions for edge cases

## Integration with Layouts

### _Layout.cshtml
For pages using the default layout, ensure the layout includes:
```html
<link rel="stylesheet" href="~/css/go-back-button.css">
<script src="~/js/go-back-button.js"></script>
```

### _FormsLayout.cshtml
Already includes the button and required scripts:
```html
<button id="goBackBtn">
    <i class="fas fa-arrow-left"></i>
    <span>Go Back</span>
</button>
<script src="~/js/go-back-button.js"></script>
```

### Pages with Layout = null
Add the button manually:
```html
<!DOCTYPE html>
<html>
<head>
    <link rel="stylesheet" href="~/css/go-back-button.css">
</head>
<body>
    <button id="goBackBtn">
        <i class="fas fa-arrow-left"></i>
        <span>Go Back</span>
    </button>

    <script src="~/js/go-back-button.js"></script>
</body>
</html>
```

## Support

For issues or questions, check:
1. Browser console for error messages
2. This documentation for usage examples
3. Source code in `/wwwroot/js/go-back-button.js` for detailed implementation
