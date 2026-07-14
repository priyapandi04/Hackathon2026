-- ============================================================
-- 006_Rename_ImageValidationResults_To_Returns.sql
--
-- PURPOSE:
--   The [Returns] table was renamed to [ImageValidationResults] in SQL.
--   This migration renames it back to [Returns] to align with:
--     - EF entity:  Return  (mapped by convention to table "Returns")
--     - DbContext:  public DbSet<Return> Returns => Set<Return>();
--     - Domain:     InventoryPool.Return navigation -> ReturnId FK
--
-- WHAT THIS SCRIPT DOES:
--   1. Renames [ImageValidationResults] -> [Returns]
--   2. Renames the now-misleadingly-named FK constraint
--      FK_InventoryPool_ImageValidationResults -> FK_InventoryPool_Returns
--   3. Validates the FK target is correct (Returns.Id)
--   4. Recreates the stored procedures that reference Returns
--
-- RUN ORDER: before or instead of 005_FK_Remediation_Full.sql
--            (005 checks both table names safely after this script runs)
--
-- IDEMPOTENT: safe to re-run.
-- ============================================================

SET NOCOUNT ON;
PRINT '=== [006] Rename ImageValidationResults -> Returns ==='

-- ============================================================
-- STEP 1  Rename the table
-- ============================================================

BEGIN TRY
    BEGIN TRANSACTION T_Rename;

    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'ImageValidationResults'
    )
    AND NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Returns'
    )
    BEGIN
        EXEC sp_rename 'dbo.ImageValidationResults', 'Returns';
        PRINT '  Renamed [ImageValidationResults] -> [Returns]';
    END
    ELSE IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Returns'
    )
    BEGIN
        PRINT '  [Returns] already exists — rename not needed';
    END
    ELSE
    BEGIN
        RAISERROR('Neither [ImageValidationResults] nor [Returns] table found. Cannot proceed.', 16, 1);
    END

    COMMIT TRANSACTION T_Rename;
    PRINT '  [STEP 1] COMMITTED';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION T_Rename;
    DECLARE @E1 NVARCHAR(4000) = ERROR_MESSAGE();
    PRINT '  [STEP 1] ROLLED BACK — ' + @E1;
    THROW;
END CATCH

GO

-- ============================================================
-- STEP 2  Rename the misleading FK constraint
--         FK_InventoryPool_ImageValidationResults
--         -> FK_InventoryPool_Returns
-- ============================================================
PRINT '=== [STEP 2] Rename FK constraint on InventoryPool ==='

BEGIN TRY
    BEGIN TRANSACTION T_RenameFK;

    IF EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_InventoryPool_ImageValidationResults'
          AND parent_object_id = OBJECT_ID('dbo.InventoryPool')
    )
    AND NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_InventoryPool_Returns'
          AND parent_object_id = OBJECT_ID('dbo.InventoryPool')
    )
    BEGIN
        EXEC sp_rename
            'dbo.FK_InventoryPool_ImageValidationResults',
            'FK_InventoryPool_Returns',
            'OBJECT';
        PRINT '  Renamed FK_InventoryPool_ImageValidationResults -> FK_InventoryPool_Returns';
    END
    ELSE IF EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_InventoryPool_Returns'
          AND parent_object_id = OBJECT_ID('dbo.InventoryPool')
    )
    BEGIN
        PRINT '  FK_InventoryPool_Returns already correctly named — skipped';
    END
    ELSE
    BEGIN
        PRINT '  No FK found on InventoryPool referencing the old table — will recreate in next step';
    END

    COMMIT TRANSACTION T_RenameFK;
    PRINT '  [STEP 2] COMMITTED';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION T_RenameFK;
    DECLARE @E2 NVARCHAR(4000) = ERROR_MESSAGE();
    PRINT '  [STEP 2] ROLLED BACK — ' + @E2;
    THROW;
END CATCH

GO

-- ============================================================
-- STEP 3  Verify FK target is Returns.Id
--         Recreate if missing or pointing at wrong table
-- ============================================================
PRINT '=== [STEP 3] Validate / recreate FK_InventoryPool_Returns ==='

BEGIN TRY
    BEGIN TRANSACTION T_ValidateFK;

    DECLARE @fkOk BIT = 0;

    SELECT @fkOk = 1
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    WHERE fk.name = 'FK_InventoryPool_Returns'
      AND fk.parent_object_id      = OBJECT_ID('dbo.InventoryPool')
      AND fk.referenced_object_id  = OBJECT_ID('dbo.Returns')
      AND COL_NAME(fkc.parent_object_id,     fkc.parent_column_id)     = 'ReturnId'
      AND COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) = 'Id';

    IF @fkOk = 1
    BEGIN
        PRINT '  FK_InventoryPool_Returns is correct (ReturnId -> Returns.Id)';
    END
    ELSE
    BEGIN
        DECLARE @existingFk NVARCHAR(200);
        SELECT @existingFk = fk.name
        FROM sys.foreign_keys fk
        JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
        WHERE fk.parent_object_id = OBJECT_ID('dbo.InventoryPool')
          AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'ReturnId';

        IF @existingFk IS NOT NULL
        BEGIN
            DECLARE @drop NVARCHAR(500) =
                'ALTER TABLE [dbo].[InventoryPool] DROP CONSTRAINT [' + @existingFk + ']';
            EXEC sp_executesql @drop;
            PRINT '  Dropped stale FK: ' + @existingFk;
        END

        DELETE FROM [dbo].[InventoryPool]
        WHERE [ReturnId] NOT IN (SELECT [Id] FROM [dbo].[Returns]);
        IF @@ROWCOUNT > 0
            PRINT '  Removed orphan InventoryPool rows';

        ALTER TABLE [dbo].[InventoryPool]
            ADD CONSTRAINT [FK_InventoryPool_Returns]
            FOREIGN KEY ([ReturnId])
            REFERENCES [dbo].[Returns]([Id])
            ON DELETE CASCADE;

        PRINT '  Recreated FK_InventoryPool_Returns (ReturnId -> Returns.Id)';
    END

    COMMIT TRANSACTION T_ValidateFK;
    PRINT '  [STEP 3] COMMITTED';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION T_ValidateFK;
    DECLARE @E3 NVARCHAR(4000) = ERROR_MESSAGE();
    PRINT '  [STEP 3] ROLLED BACK — ' + @E3;
    THROW;
END CATCH

GO

-- ============================================================
-- STEP 4  Recreate stored procedures that referenced [Returns]
-- ============================================================
PRINT '=== [STEP 4] Recreate SPs that reference [Returns] ==='

GO

-- usp_SaveImageValidationResult
CREATE OR ALTER PROCEDURE [dbo].[usp_SaveImageValidationResult]
    @ProductId    NVARCHAR(100),
    @ProductName  NVARCHAR(300),
    @Category     NVARCHAR(100),
    @ReturnReason NVARCHAR(1000),
    @Condition    NVARCHAR(50),
    @Eligibility  NVARCHAR(50),
    @Confidence   FLOAT,
    @Location     NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NewId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [dbo].[Returns]
            ([Id],[ProductId],[ProductName],[Category],[ReturnReason],
             [Condition],[Eligibility],[Confidence],[Location],[ReturnDate],[CreatedAt])
        VALUES
            (@NewId, @ProductId, @ProductName, @Category, @ReturnReason,
             @Condition, @Eligibility, @Confidence, @Location,
             SYSUTCDATETIME(), SYSUTCDATETIME());

        SELECT @NewId AS Id;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- usp_AddToInventoryPool
CREATE OR ALTER PROCEDURE [dbo].[usp_AddToInventoryPool]
    @ReturnId   UNIQUEIDENTIFIER,
    @ProductId  NVARCHAR(100),
    @Location   NVARCHAR(200),
    @MatchScore FLOAT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ReturnIdStr NVARCHAR(50) = CAST(@ReturnId AS NVARCHAR(50));

    IF NOT EXISTS (
        SELECT 1 FROM [dbo].[Returns]
        WHERE [Id] = @ReturnId AND [IsDeleted] = 0
    )
    BEGIN
        RAISERROR('Return %s does not exist or is deleted. Cannot add to InventoryPool.', 16, 1, @ReturnIdStr);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (
            SELECT 1 FROM [dbo].[InventoryPool]
            WHERE [ReturnId] = @ReturnId AND [ProductId] = @ProductId
        )
        BEGIN
            UPDATE [dbo].[InventoryPool]
            SET    [MatchScore] = @MatchScore,
                   [Location]  = @Location,
                   [UpdatedAt] = SYSUTCDATETIME()
            WHERE  [ReturnId] = @ReturnId AND [ProductId] = @ProductId;
        END
        ELSE
        BEGIN
            INSERT INTO [dbo].[InventoryPool]
                ([Id],[ReturnId],[ProductId],[Location],[HoldingDays],[MatchScore],[Status],[CreatedAt])
            VALUES
                (NEWID(), @ReturnId, @ProductId, @Location, 0, @MatchScore, 'Available', SYSUTCDATETIME());
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- usp_GetInventoryByProduct
CREATE OR ALTER PROCEDURE [dbo].[usp_GetInventoryByProduct]
    @ProductId NVARCHAR(100),
    @Location  NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ip.[Id],
        ip.[ReturnId],
        ip.[ProductId],
        ip.[Location],
        ip.[HoldingDays],
        ip.[MatchScore],
        ip.[Status],
        r.[ProductName],
        r.[Category],
        r.[Condition],
        r.[Eligibility]
    FROM [dbo].[InventoryPool] ip
    INNER JOIN [dbo].[Returns] r ON r.[Id] = ip.[ReturnId] AND r.[IsDeleted] = 0
    WHERE ip.[ProductId] = @ProductId
      AND ip.[IsDeleted] = 0
      AND (@Location IS NULL OR ip.[Location] = @Location)
    ORDER BY ip.[MatchScore] DESC;
END
GO

-- ============================================================
-- STEP 5  Final validation report
-- ============================================================
PRINT '=== [STEP 5] Final validation ==='

SELECT
    TABLE_NAME  AS [Table],
    TABLE_TYPE  AS [Type]
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME IN ('Returns', 'ImageValidationResults');

SELECT
    fk.name                                                                 AS ConstraintName,
    OBJECT_NAME(fk.parent_object_id)                                        AS ChildTable,
    COL_NAME(fkc.parent_object_id,     fkc.parent_column_id)               AS ChildColumn,
    OBJECT_NAME(fk.referenced_object_id)                                    AS ParentTable,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id)           AS ParentColumn,
    fk.delete_referential_action_desc                                       AS OnDelete
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.InventoryPool');

PRINT '=== Migration 006_Rename_ImageValidationResults_To_Returns.sql completed ==='
GO
