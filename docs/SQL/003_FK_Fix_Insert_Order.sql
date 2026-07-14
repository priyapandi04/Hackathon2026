-- ============================================================
-- 003_FK_Fix_Insert_Order.sql  [SUPERSEDED]
-- This script has been superseded by 005_FK_Remediation_Full.sql
-- which provides complete root-cause analysis, correct FK targets,
-- transaction handling, rollback strategy, SP updates and seed fixes.
-- DO NOT RE-RUN this file independently.
-- ============================================================

-- Option A: Drop the problematic FK on InventoryPool if ReturnId
-- is meant to reference ReturnRequests rather than ImageValidationResults.
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_InventoryPool_ImageValidationResults'
)
BEGIN
    ALTER TABLE [dbo].[InventoryPool]
        DROP CONSTRAINT [FK_InventoryPool_ImageValidationResults];
    PRINT 'Dropped FK_InventoryPool_ImageValidationResults';
END
GO

-- Re-create pointing at ReturnRequests (the actual parent)
-- First clean up orphan rows in InventoryPool that reference non-existent ReturnRequests.
DELETE FROM [dbo].[InventoryPool]
WHERE [ReturnId] NOT IN (SELECT [Id] FROM [dbo].[ReturnRequests]);

IF @@ROWCOUNT > 0
    PRINT 'Cleaned orphan InventoryPool rows with invalid ReturnId.';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_InventoryPool_ReturnRequests'
)
BEGIN
    ALTER TABLE [dbo].[InventoryPool]
        ADD CONSTRAINT [FK_InventoryPool_ReturnRequests]
        FOREIGN KEY ([ReturnId]) REFERENCES [dbo].[ReturnRequests]([Id]);
    PRINT 'Created FK_InventoryPool_ReturnRequests';
END
GO

-- Ensure MatchAgentResults FK is valid (seed data alignment)
-- Any orphan rows that reference non-existent ReturnRequests are cleaned up
DELETE FROM [dbo].[MatchAgentResults]
WHERE [ReturnRequestId] NOT IN (SELECT [Id] FROM [dbo].[ReturnRequests])
  AND EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MatchAgentResults_ReturnRequests');
GO

PRINT 'FK fixes applied.';
GO
