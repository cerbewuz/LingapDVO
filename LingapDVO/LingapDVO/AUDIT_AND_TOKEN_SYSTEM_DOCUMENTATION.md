# Audit Logs and Token System - Complete Documentation

## 📋 Overview

The LingapDVO system implements a **two-tier security system** using **Audit Logs** and **Tokens** to prevent fraud, cloning, duplicate submissions, and unauthorized access. This documentation explains how both systems work together.

---

## 🔐 Security Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                  USER ACTION FLOW                            │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 1: Generate Token (Frontend Page Load)                │
│  ├─ Token created when user visits registration/form page   │
│  ├─ Stored in database with metadata (IP, UserAgent, Time)  │
│  └─ Hidden field in HTML form contains token                │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 2: User Fills Out Form                                │
│  └─ Token stays in hidden field (not visible to user)       │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 3: Form Submission (Backend Validation)               │
│  ├─ Token sent with form data                               │
│  ├─ Server validates token:                                 │
│  │   ✓ Token exists in database?                            │
│  │   ✓ Token not already used?                              │
│  │   ✓ Token not expired?                                   │
│  │   ✓ Token not revoked?                                   │
│  │   ✓ IP/UserAgent matches?                                │
│  └─ All checks pass → Mark token as USED                    │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 4: Audit Log Created                                  │
│  ├─ Record the attempt (SUCCESS/FAILED/BLOCKED)             │
│  ├─ Store all metadata (IP, UserAgent, Time, Reason)        │
│  ├─ Flag suspicious activity if detected                    │
│  └─ Link to submitted form for tracking                     │
└─────────────────────────────────────────────────────────────┘
```

---

## 🎫 1. TOKEN SYSTEM

### **What are Tokens?**

Tokens are **cryptographically random 64-byte strings** that act as **one-time-use keys** for:
- **Registration forms** (RegistrationToken)
- **Form submissions** (FormSubmissionToken)

### **Token Lifecycle**

```
┌──────────────────────────────────────────────────────────┐
│  TOKEN LIFECYCLE                                          │
└──────────────────────────────────────────────────────────┘

1. CREATED
   ├─ When: User visits registration page or form page
   ├─ Data Stored:
   │   • Token (random 64-byte Base64 string)
   │   • IP Address
   │   • User Agent (browser info)
   │   • CreatedAt timestamp
   │   • ExpiresAt timestamp
   │   • IsUsed = false
   │   • IsRevoked = false
   └─ Example Token: "a8fh29fj2o8fh2o8fh2o8fh2o8fh2o8fh..."

2. ACTIVE (Waiting for Use)
   ├─ Token is embedded in form as hidden field
   ├─ User fills out the form
   └─ Token is valid for 30 minutes (registration) or 2 hours (forms)

3. VALIDATED (On Submission)
   ├─ Server receives token with form data
   ├─ Checks performed:
   │   ✓ Token exists in database?
   │   ✓ Token.IsUsed == false?
   │   ✓ Token.IsRevoked == false?
   │   ✓ Token.ExpiresAt > CurrentTime?
   │   ✓ IP/UserAgent matches (optional security check)?
   └─ If ALL checks pass → Continue to step 4
       If ANY check fails → BLOCK and create audit log

4. CONSUMED (Marked as Used)
   ├─ Token.IsUsed = true
   ├─ Token.UsedAt = current timestamp
   ├─ Token.SubmittedFormId = the form that was submitted
   └─ Token is now PERMANENTLY CONSUMED (cannot be reused)

5. REVOKED (Optional - If User Navigates Away)
   ├─ If user leaves page without submitting
   ├─ Token.IsRevoked = true
   └─ Token cannot be used anymore
```

---

## 📊 2. REGISTRATION TOKEN SYSTEM

### **Purpose**
Prevents:
- ❌ Cloned registration forms
- ❌ Direct API calls bypassing the frontend
- ❌ Multiple registrations from same form
- ❌ Bot/automated registrations

### **Database Table: RegistrationTokens**

```sql
RegistrationToken
├─ Id (Primary Key)
├─ Token (UNIQUE string - the random token)
├─ IpAddress (IP that requested the token)
├─ UserAgent (Browser/device info)
├─ CreatedAt (When token was generated)
├─ ExpiresAt (30 minutes from creation)
├─ IsUsed (false → true when registration completes)
├─ UsedAt (NULL → timestamp when used)
├─ UsedByEmail (NULL → email that used this token)
└─ IsRevoked (false → true if manually revoked)
```

### **Flow Example: User Registration**

```
┌────────────────────────────────────────────────────────────┐
│  STEP 1: User visits /Register page                        │
└────────────────────────────────────────────────────────────┘
         │
         ├─ Controller Action: GET /Register
         │
         └─ RegistrationAuditService.GenerateRegistrationToken()
                │
                ├─ Creates random 64-byte token
                ├─ Saves to database:
                │   Token: "a8fh29fj2o8fh2o8fh..."
                │   IpAddress: "192.168.1.100"
                │   UserAgent: "Mozilla/5.0..."
                │   CreatedAt: 2025-01-15 10:00:00
                │   ExpiresAt: 2025-01-15 10:30:00
                │   IsUsed: false
                │   IsRevoked: false
                │
                └─ Returns token to view (stored in hidden input field)

┌────────────────────────────────────────────────────────────┐
│  STEP 2: User fills out registration form                  │
└────────────────────────────────────────────────────────────┘
         │
         └─ Hidden field <input name="registrationToken" value="a8fh29fj..." />

┌────────────────────────────────────────────────────────────┐
│  STEP 3: User submits form                                 │
└────────────────────────────────────────────────────────────┘
         │
         ├─ Controller Action: POST /Register
         │
         ├─ RegistrationAuditService.ValidateAndConsumeToken(token, email)
         │     │
         │     ├─ CHECK 1: Token exists in database? ✓
         │     ├─ CHECK 2: Token.IsUsed == false? ✓
         │     ├─ CHECK 3: Token.IsRevoked == false? ✓
         │     ├─ CHECK 4: Token.ExpiresAt > Now? ✓
         │     │
         │     └─ ALL CHECKS PASSED ✓
         │           │
         │           ├─ UPDATE Token:
         │           │   IsUsed = true
         │           │   UsedAt = 2025-01-15 10:05:00
         │           │   UsedByEmail = "john@example.com"
         │           │
         │           └─ RETURN (true, "Token valid")
         │
         ├─ Create audit log (SUCCESS)
         │
         └─ Continue with registration (create user account)

┌────────────────────────────────────────────────────────────┐
│  WHAT IF SOMEONE TRIES TO REUSE THE TOKEN?                 │
└────────────────────────────────────────────────────────────┘
         │
         ├─ Attacker tries to submit form again with same token
         │
         ├─ RegistrationAuditService.ValidateAndConsumeToken(token, email)
         │     │
         │     ├─ CHECK 1: Token exists? ✓
         │     ├─ CHECK 2: Token.IsUsed == false? ✗ FAILED!
         │     │                                    (IsUsed = true)
         │     │
         │     └─ BLOCK SUBMISSION
         │           │
         │           ├─ Create audit log:
         │           │   Action: BLOCKED
         │           │   Source: CLONED
         │           │   SuspiciousActivity: true
         │           │   Reason: "Token already used - possible cloning"
         │           │
         │           └─ RETURN (false, "This form has already been used")
         │
         └─ User sees error: "This registration form has already been used"
```

---

## 📝 3. FORM SUBMISSION TOKEN SYSTEM

### **Purpose**
Prevents:
- ❌ Cloned hospital bills, funeral forms, medical forms
- ❌ Duplicate submissions (same data submitted multiple times)
- ❌ Direct API calls
- ❌ Token replay attacks

### **Database Table: FormSubmissionTokens**

```sql
FormSubmissionToken
├─ Id (Primary Key)
├─ Token (UNIQUE string)
├─ FormType (e.g., "HospitalBill", "FuneralForm", "MedicalForm")
├─ UserId (User who requested the form)
├─ IpAddress
├─ UserAgent
├─ CreatedAt
├─ ExpiresAt (2 hours from creation)
├─ IsUsed (false → true when form submitted)
├─ UsedAt (NULL → timestamp when used)
├─ SubmittedFormId (NULL → ID of submitted form)
└─ IsRevoked (false → true if manually revoked)
```

### **Flow Example: Hospital Bill Form**

```
┌────────────────────────────────────────────────────────────┐
│  STEP 1: User clicks "Fill Hospital Bill Form"             │
└────────────────────────────────────────────────────────────┘
         │
         ├─ Controller Action: GET /HospitalBill/Create?userId=123
         │
         └─ FormSubmissionAuditService.GenerateFormSubmissionToken(123, "HospitalBill")
                │
                ├─ Creates random token
                ├─ Saves to database:
                │   Token: "x9gh38dj2o8fh2o8fh..."
                │   FormType: "HospitalBill"
                │   UserId: 123
                │   IpAddress: "192.168.1.100"
                │   UserAgent: "Mozilla/5.0..."
                │   CreatedAt: 2025-01-15 14:00:00
                │   ExpiresAt: 2025-01-15 16:00:00 (2 hours)
                │   IsUsed: false
                │
                └─ Returns token (embedded in form)

┌────────────────────────────────────────────────────────────┐
│  STEP 2: User fills out hospital bill form                 │
└────────────────────────────────────────────────────────────┘
         │
         └─ Hidden field contains token

┌────────────────────────────────────────────────────────────┐
│  STEP 3: User submits form                                 │
└────────────────────────────────────────────────────────────┘
         │
         ├─ Controller Action: POST /HospitalBill/Submit
         │
         ├─ FormSubmissionAuditService.ValidateAndConsumeFormToken(
         │       token, userId, "HospitalBill", formId
         │   )
         │     │
         │     ├─ CHECK 1: Token exists? ✓
         │     ├─ CHECK 2: Token.UserId == 123? ✓
         │     ├─ CHECK 3: Token.FormType == "HospitalBill"? ✓
         │     ├─ CHECK 4: Token.IsUsed == false? ✓
         │     ├─ CHECK 5: Token.IsRevoked == false? ✓
         │     ├─ CHECK 6: Token.ExpiresAt > Now? ✓
         │     │
         │     └─ ALL CHECKS PASSED ✓
         │           │
         │           ├─ UPDATE Token:
         │           │   IsUsed = true
         │           │   UsedAt = 2025-01-15 14:30:00
         │           │   SubmittedFormId = 456
         │           │
         │           └─ RETURN (true, "Token valid")
         │
         ├─ Generate form data hash (for duplicate detection)
         │
         ├─ Check for duplicates (same data in last 30 minutes)
         │
         ├─ Create audit log (SUCCESS)
         │
         └─ Save hospital bill form to database
```

---

## 📜 4. AUDIT LOG SYSTEM

### **Purpose**
- 📊 **Track ALL attempts** (successful, failed, blocked)
- 🔍 **Detect patterns** (rapid submissions, bot activity)
- 🛡️ **Security monitoring** (identify attackers)
- 📈 **Analytics** (user behavior, common issues)

### **Two Types of Audit Logs**

#### **A. RegistrationAuditLog**

```sql
RegistrationAuditLog
├─ Id (Primary Key)
├─ IpAddress
├─ UserAgent
├─ Email (email being registered)
├─ Username
├─ FullName
├─ Action (SUCCESS / FAILED / BLOCKED)
├─ Source (WEB_FORM / CLONED / UNKNOWN)
├─ Reason (why blocked/failed)
├─ RegistrationToken (the token used)
├─ HasValidToken (true/false)
├─ SuspiciousActivity (true/false)
├─ SuspiciousReasons (detailed explanation)
├─ AttemptedAt (timestamp)
└─ RegisteredUserId (NULL if failed, user ID if success)
```

#### **B. FormSubmissionAuditLog**

```sql
FormSubmissionAuditLog
├─ Id (Primary Key)
├─ FormType (HospitalBill / FuneralForm / MedicalForm)
├─ UserId
├─ IpAddress
├─ UserAgent
├─ PatientName
├─ RequestorName
├─ Action (SUCCESS / FAILED / BLOCKED)
├─ Source (WEB_FORM / CLONED / UNKNOWN)
├─ Reason (why blocked/failed)
├─ SubmissionToken
├─ HasValidToken (true/false)
├─ SuspiciousActivity (true/false)
├─ SuspiciousReasons (detailed explanation)
├─ AttemptedAt (timestamp)
├─ SubmittedFormId (the form that was submitted)
├─ IsDuplicate (true/false)
├─ DuplicateDetails (when/where duplicate detected)
└─ FormDataHash (SHA256 hash for duplicate detection)
```

---

## 🚨 5. SUSPICIOUS ACTIVITY DETECTION

### **What Gets Flagged as Suspicious?**

```csharp
// FormSubmissionAuditService.cs - DetectSuspiciousSubmission()

SUSPICIOUS FLAG 1: Missing Token
├─ User submitted form without a token
├─ Indicates: Direct API call or cloned form
└─ Action: Flag + Log reason

SUSPICIOUS FLAG 2: Rapid Submissions
├─ Same user submits >3 forms in 5 minutes
├─ Indicates: Bot or automated script
└─ Action: Flag + Log count

SUSPICIOUS FLAG 3: Multiple Failed Attempts
├─ Same IP has >5 failed attempts in 10 minutes
├─ Indicates: Brute force or repeated cloning
└─ Action: Flag + Log count

SUSPICIOUS FLAG 4: Missing/Invalid User-Agent
├─ Request has no browser info or very short User-Agent
├─ Indicates: API tool (Postman, curl) or bot
└─ Action: Flag + Log
```

### **Example: Suspicious Activity Detected**

```
Audit Log Entry:
{
    FormType: "HospitalBill",
    UserId: 123,
    Action: "BLOCKED",
    Source: "CLONED",
    SuspiciousActivity: true,
    SuspiciousReasons: "Missing token; Rapid submissions: 5 in 5 minutes",
    Reason: "Token already used - possible cloning",
    AttemptedAt: "2025-01-15 15:30:00"
}
```

---

## 🔄 6. DUPLICATE DETECTION

### **How It Works**

```csharp
// Generate hash of form data
string formHash = FormSubmissionAuditService.GenerateFormDataHash(formData);

// Check if same hash exists in last 30 minutes
var duplicate = CheckDuplicateSubmission(
    formType: "HospitalBill",
    userId: 123,
    formDataHash: formHash,
    minutesWindow: 30
);

if (duplicate.IsDuplicate) {
    // BLOCK submission
    LogFormSubmissionAttempt(
        action: "BLOCKED",
        isDuplicate: true,
        duplicateDetails: "Duplicate submitted 5 minutes ago"
    );
}
```

### **Duplicate Detection Process**

```
┌────────────────────────────────────────────────────────┐
│  User submits Hospital Bill Form                       │
└────────────────────────────────────────────────────────┘
         │
         ├─ STEP 1: Generate SHA256 hash of form data
         │   Data: { PatientName: "John", Amount: 5000, ... }
         │   Hash: "a7f9d8e6c5b4a3..."
         │
         ├─ STEP 2: Check database for matching hash
         │   SELECT * FROM FormSubmissionAuditLogs
         │   WHERE FormType = 'HospitalBill'
         │     AND UserId = 123
         │     AND FormDataHash = 'a7f9d8e6c5b4a3...'
         │     AND Action = 'SUCCESS'
         │     AND AttemptedAt > (Now - 30 minutes)
         │
         ├─ STEP 3: If match found → DUPLICATE
         │   └─ BLOCK submission
         │       └─ Log: "Duplicate form submitted at 15:00"
         │
         └─ STEP 4: If no match → ALLOW
             └─ Save hash with submission
```

---

## 📈 7. PRACTICAL USE CASES

### **Use Case 1: Normal User Registration**

```
1. User visits /Register → Token generated (Token A)
2. User fills out form
3. User submits → Token A validated ✓ → SUCCESS
4. Audit log: SUCCESS, HasValidToken=true, SuspiciousActivity=false
```

### **Use Case 2: Cloned Registration Form**

```
1. Attacker copies HTML source of registration page
2. Attacker tries to submit with old Token B
3. System checks: Token B.IsUsed = true ✗
4. BLOCKED → Audit log: BLOCKED, Source=CLONED, Reason="Token already used"
```

### **Use Case 3: Direct API Call (No Token)**

```
1. Attacker uses Postman to POST /Register directly
2. No token in request
3. System checks: Token missing ✗
4. BLOCKED → Audit log: BLOCKED, SuspiciousActivity=true, Reason="Missing token"
```

### **Use Case 4: Duplicate Hospital Bill**

```
1. User submits Hospital Bill at 14:00 → Hash: ABC123 → SUCCESS
2. User tries to submit same data at 14:10 → Hash: ABC123
3. System finds matching hash from 14:00
4. BLOCKED → Audit log: BLOCKED, IsDuplicate=true, Details="Submitted 10 min ago"
```

### **Use Case 5: Bot Attack**

```
1. Bot submits 10 forms in 2 minutes
2. System detects: 10 attempts from IP 192.168.1.50 in 2 minutes
3. System flags: SuspiciousActivity=true, Reason="Rapid submissions: 10 in 2 min"
4. Admin reviews audit logs and blocks IP
```

---

## 🎯 8. SUMMARY

| Feature | Registration Tokens | Form Submission Tokens |
|---------|-------------------|----------------------|
| **Lifetime** | 30 minutes | 2 hours |
| **Purpose** | Prevent cloned registrations | Prevent cloned/duplicate forms |
| **One-Time Use** | ✓ Yes | ✓ Yes |
| **Tracks IP** | ✓ Yes | ✓ Yes |
| **Duplicate Detection** | ✓ Email/Username check | ✓ SHA256 hash check |
| **Audit Logging** | ✓ All attempts | ✓ All attempts |
| **Suspicious Activity Detection** | ✓ Yes | ✓ Yes |

---

## 🛡️ 9. SECURITY BENEFITS

✅ **Prevents Form Cloning**: Each token is one-time-use
✅ **Prevents Duplicate Submissions**: Hash-based detection
✅ **Prevents API Abuse**: Missing token = blocked
✅ **Detects Bots**: Rapid submission detection
✅ **Complete Audit Trail**: Every attempt is logged
✅ **IP Tracking**: Identifies attack sources
✅ **Time-Limited**: Tokens expire automatically

---

## 📝 10. DEVELOPER NOTES

### **How to Use in Controllers**

```csharp
// Registration Controller
public class RegistrationController : Controller
{
    private readonly RegistrationAuditService _auditService;

    // GET: Show registration form
    public IActionResult Register()
    {
        // Generate token
        var token = _auditService.GenerateRegistrationToken();
        ViewBag.RegistrationToken = token;
        return View();
    }

    // POST: Handle registration
    [HttpPost]
    public IActionResult Register(RegisterModel model)
    {
        // Validate token
        var (isValid, reason) = _auditService.ValidateAndConsumeToken(
            model.RegistrationToken,
            model.Email
        );

        if (!isValid)
        {
            // Token invalid - reject registration
            ModelState.AddModelError("", reason);
            return View(model);
        }

        // Check for duplicates
        var (isDuplicate, dupReason) = _auditService.CheckDuplicateRegistration(
            model.Email,
            model.Username
        );

        if (isDuplicate)
        {
            ModelState.AddModelError("", dupReason);
            return View(model);
        }

        // Check for suspicious activity
        var (isSuspicious, susReasons) = _auditService.DetectSuspiciousRegistration(
            model.Email,
            model.Username,
            model.RegistrationToken
        );

        // Create user account...
        var userId = CreateUser(model);

        // Log success
        _auditService.LogRegistrationAttempt(
            registrationToken: model.RegistrationToken,
            email: model.Email,
            action: "SUCCESS",
            source: "WEB_FORM",
            hasValidToken: true,
            suspiciousActivity: isSuspicious,
            suspiciousReasons: isSuspicious ? susReasons : null,
            registeredUserId: userId,
            username: model.Username,
            fullName: model.FullName
        );

        return RedirectToAction("Success");
    }
}
```

---

**Status**: ✅ Fully Documented
**Version**: 1.0
**Last Updated**: 2025-01-15

For questions or implementation help, refer to:
- `Services/RegistrationAuditService.cs`
- `Services/FormSubmissionAuditService.cs`
