-- ================================================================
-- Database Migration: Add IsArchived Column to Assistance Tables
-- Purpose: Auto-archive applications older than 1 month
-- Created: 2025-11-18
-- ================================================================

-- Add IsArchived column to HospitalAssistance table
IF NOT EXISTS (SELECT * FROM sys.columns
               WHERE object_id = OBJECT_ID(N'[dbo].[HospitalAssistance]')
               AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[HospitalAssistance]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0;

    PRINT 'IsArchived column added to HospitalAssistance table';
END
ELSE
BEGIN
    PRINT 'IsArchived column already exists in HospitalAssistance table';
END
GO

-- Add IsArchived column to OtherAssistance table
IF NOT EXISTS (SELECT * FROM sys.columns
               WHERE object_id = OBJECT_ID(N'[dbo].[OtherAssistance]')
               AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[OtherAssistance]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0;

    PRINT 'IsArchived column added to OtherAssistance table';
END
ELSE
BEGIN
    PRINT 'IsArchived column already exists in OtherAssistance table';
END
GO

-- Add IsArchived column to FuneralAssistance table
IF NOT EXISTS (SELECT * FROM sys.columns
               WHERE object_id = OBJECT_ID(N'[dbo].[FuneralAssistance]')
               AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[FuneralAssistance]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0;

    PRINT 'IsArchived column added to FuneralAssistance table';
END
ELSE
BEGIN
    PRINT 'IsArchived column already exists in FuneralAssistance table';
END
GO

-- ================================================================
-- Auto-archive existing applications older than 1 month
-- ================================================================

-- Archive HospitalAssistance records older than 1 month
UPDATE [dbo].[HospitalAssistance]
SET [IsArchived] = 1
WHERE [CreatedAt] < DATEADD(MONTH, -1, GETDATE())
  AND [IsArchived] = 0;

PRINT CONCAT('Archived ', @@ROWCOUNT, ' Hospital Assistance records');
GO

-- Archive OtherAssistance records older than 1 month
UPDATE [dbo].[OtherAssistance]
SET [IsArchived] = 1
WHERE [CreatedAt] < DATEADD(MONTH, -1, GETDATE())
  AND [IsArchived] = 0;

PRINT CONCAT('Archived ', @@ROWCOUNT, ' Other Assistance records');
GO

-- Archive FuneralAssistance records older than 1 month
UPDATE [dbo].[FuneralAssistance]
SET [IsArchived] = 1
WHERE [CreatedAt] < DATEADD(MONTH, -1, GETDATE())
  AND [IsArchived] = 0;

PRINT CONCAT('Archived ', @@ROWCOUNT, ' Funeral Assistance records');
GO

-- ================================================================
-- Create indexes for better performance on IsArchived queries
-- ================================================================

-- Create index on HospitalAssistance
IF NOT EXISTS (SELECT * FROM sys.indexes
               WHERE name = 'IX_HospitalAssistance_IsArchived_CreatedAt'
               AND object_id = OBJECT_ID(N'[dbo].[HospitalAssistance]'))
BEGIN
    CREATE INDEX [IX_HospitalAssistance_IsArchived_CreatedAt]
    ON [dbo].[HospitalAssistance] ([IsArchived], [CreatedAt] DESC);

    PRINT 'Index created on HospitalAssistance table';
END
GO

-- Create index on OtherAssistance
IF NOT EXISTS (SELECT * FROM sys.indexes
               WHERE name = 'IX_OtherAssistance_IsArchived_CreatedAt'
               AND object_id = OBJECT_ID(N'[dbo].[OtherAssistance]'))
BEGIN
    CREATE INDEX [IX_OtherAssistance_IsArchived_CreatedAt]
    ON [dbo].[OtherAssistance] ([IsArchived], [CreatedAt] DESC);

    PRINT 'Index created on OtherAssistance table';
END
GO

-- Create index on FuneralAssistance
IF NOT EXISTS (SELECT * FROM sys.indexes
               WHERE name = 'IX_FuneralAssistance_IsArchived_CreatedAt'
               AND object_id = OBJECT_ID(N'[dbo].[FuneralAssistance]'))
BEGIN
    CREATE INDEX [IX_FuneralAssistance_IsArchived_CreatedAt]
    ON [dbo].[FuneralAssistance] ([IsArchived], [CreatedAt] DESC);

    PRINT 'Index created on FuneralAssistance table';
END
GO

PRINT '================================================================';
PRINT 'Migration completed successfully!';
PRINT 'Summary:';
PRINT '- IsArchived column added to all assistance tables';
PRINT '- Existing records older than 1 month have been archived';
PRINT '- Performance indexes have been created';
PRINT '================================================================';
GO
