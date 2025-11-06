-- =====================================================================
-- Migration: Add Notification Preferences to RegisterAcc Table
-- Description: Adds PreferEmailNotification, PreferSmsNotification,
--              and PreferInAppNotification columns to RegisterAcc table
-- Date: 2025-01-06
-- =====================================================================

USE [LingapDVO]; -- Change this to your database name if different
GO

-- Check if columns already exist before adding them
IF NOT EXISTS (SELECT * FROM sys.columns
               WHERE object_id = OBJECT_ID(N'[dbo].[RegisterAcc]')
               AND name = 'PreferEmailNotification')
BEGIN
    ALTER TABLE [dbo].[RegisterAcc]
    ADD [PreferEmailNotification] BIT NOT NULL DEFAULT 1;
    PRINT 'Column PreferEmailNotification added successfully.';
END
ELSE
BEGIN
    PRINT 'Column PreferEmailNotification already exists.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns
               WHERE object_id = OBJECT_ID(N'[dbo].[RegisterAcc]')
               AND name = 'PreferSmsNotification')
BEGIN
    ALTER TABLE [dbo].[RegisterAcc]
    ADD [PreferSmsNotification] BIT NOT NULL DEFAULT 0;
    PRINT 'Column PreferSmsNotification added successfully.';
END
ELSE
BEGIN
    PRINT 'Column PreferSmsNotification already exists.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns
               WHERE object_id = OBJECT_ID(N'[dbo].[RegisterAcc]')
               AND name = 'PreferInAppNotification')
BEGIN
    ALTER TABLE [dbo].[RegisterAcc]
    ADD [PreferInAppNotification] BIT NOT NULL DEFAULT 1;
    PRINT 'Column PreferInAppNotification added successfully.';
END
ELSE
BEGIN
    PRINT 'Column PreferInAppNotification already exists.';
END
GO

-- Verify the changes
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'RegisterAcc'
  AND COLUMN_NAME IN ('PreferEmailNotification', 'PreferSmsNotification', 'PreferInAppNotification')
ORDER BY ORDINAL_POSITION;
GO

PRINT 'Migration completed successfully!';
GO
