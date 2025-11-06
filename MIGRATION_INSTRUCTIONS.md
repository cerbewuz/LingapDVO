# Database Migration Instructions

## Issue
The application is failing because the database is missing the new notification preference columns in the `RegisterAcc` table.

**Error:**
```
Invalid column name 'PreferEmailNotification'.
Invalid column name 'PreferInAppNotification'.
Invalid column name 'PreferSmsNotification'.
```

## Solution

You have **TWO OPTIONS** to fix this:

---

## Option 1: Use Entity Framework Migrations (Recommended)

### Step 1: Open Terminal/Command Prompt
Navigate to your project directory:
```bash
cd C:\Users\remen\OneDrive\Documents\GitHub\LingapDVO\LingapDVO\LingapDVO
```

### Step 2: Create Migration
Run the following command to create a migration:
```bash
dotnet ef migrations add AddNotificationPreferences
```

### Step 3: Update Database
Apply the migration to the database:
```bash
dotnet ef database update
```

### Step 4: Verify
The application should now work without errors.

---

## Option 2: Run SQL Script Manually

If you prefer to run SQL directly or if Entity Framework commands don't work:

### Step 1: Open SQL Server Management Studio (SSMS)
Or use any SQL client you prefer (Azure Data Studio, VS Code SQL extension, etc.)

### Step 2: Connect to Your Database
Connect to the same database your application uses.

### Step 3: Run the SQL Script
Execute the SQL script located at:
```
C:\Users\remen\OneDrive\Documents\GitHub\LingapDVO\AddNotificationPreferences.sql
```

**OR** copy and paste this SQL:

```sql
USE [LingapDVO]; -- Change to your database name

-- Add PreferEmailNotification column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RegisterAcc]') AND name = 'PreferEmailNotification')
BEGIN
    ALTER TABLE [dbo].[RegisterAcc] ADD [PreferEmailNotification] BIT NOT NULL DEFAULT 1;
END

-- Add PreferSmsNotification column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RegisterAcc]') AND name = 'PreferSmsNotification')
BEGIN
    ALTER TABLE [dbo].[RegisterAcc] ADD [PreferSmsNotification] BIT NOT NULL DEFAULT 0;
END

-- Add PreferInAppNotification column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RegisterAcc]') AND name = 'PreferInAppNotification')
BEGIN
    ALTER TABLE [dbo].[RegisterAcc] ADD [PreferInAppNotification] BIT NOT NULL DEFAULT 1;
END
```

### Step 4: Verify
Check that the columns were added:
```sql
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'RegisterAcc'
  AND COLUMN_NAME IN ('PreferEmailNotification', 'PreferSmsNotification', 'PreferInAppNotification');
```

You should see:
| COLUMN_NAME | DATA_TYPE | IS_NULLABLE | COLUMN_DEFAULT |
|-------------|-----------|-------------|----------------|
| PreferEmailNotification | bit | NO | ((1)) |
| PreferSmsNotification | bit | NO | ((0)) |
| PreferInAppNotification | bit | NO | ((1)) |

---

## What These Columns Do

- **PreferEmailNotification** (default: TRUE) - User will receive email notifications
- **PreferSmsNotification** (default: FALSE) - User will receive SMS notifications
- **PreferInAppNotification** (default: TRUE) - User will receive in-app notifications

Users can change these preferences from the notification settings modal in the Homepage.

---

## After Running the Migration

1. Restart your application
2. Try logging in again
3. The error should be resolved
4. Users can now manage notification preferences from Homepage → Notifications → Settings (gear icon)

---

## Troubleshooting

### "dotnet ef is not recognized"
Install Entity Framework tools:
```bash
dotnet tool install --global dotnet-ef
```

### "Cannot find connection string"
Make sure your `appsettings.json` has the correct connection string.

### "Database does not exist"
Make sure the database exists and the connection string points to the correct server/database.

### Still Getting Errors?
1. Check that the columns exist in the database
2. Verify the column names match exactly (case-sensitive in queries)
3. Restart the application after migration
4. Check the application logs for any other errors

---

**Note:** If you've already run the migration and still see errors, try:
1. Clean and rebuild the solution
2. Restart IIS/Kestrel
3. Clear browser cache and cookies
