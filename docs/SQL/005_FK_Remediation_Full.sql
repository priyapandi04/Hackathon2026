-- ============================================================
-- 005_FK_Remediation_Full.sql
-- Full FK remediation migration for UPS ReLoop Nexus.
--
-- ROOT CAUSES FIXED:
--   FK #1  FK_InventoryPool_ImageValidationResults
--          InventoryPool.ReturnId was referencing ImageValidationResults.Id
--          CORRECT: InventoryPool.ReturnId -> Returns.Id
--
--   FK #2  FK_MatchAgentResults_ReturnRequests
--          MatchAgentResults.ReturnRequestId referenced ReturnRequests.ReturnRequestId
--          (column does not exist; PK is 'Id' from BaseEntity)
--          CORRECT: MatchAgentResults.ReturnRequestId -> ReturnRequests.Id
--
-- CORRECT RELATIONSHIP MAP:
--   Packages            (PK: Id)
--     <- ReturnRequests (PK: Id, FK: PackageId    -> Packages.Id)
--          <- MatchAgentResults (PK: Id, FK: ReturnRequestId -> ReturnRequests.Id)
--   Returns             (PK: Id)  -- AI-validated physical return item
--     <- InventoryPool  (PK: Id, FK: ReturnId     -> Returns.Id)
--   AgentRecommendations (PK: Id)  -- no FK
--   DemandHistory        (PK: Id)  -- no FK
--   Buyers               (PK: Id)  -- no FK
--
-- CORRECT INSERT ORDER:
--   1. Packages
--   2. ReturnRequests  (needs Packages.Id)
--   3. Returns         (independent)
--   4. InventoryPool   (needs Returns.Id)
--   5. MatchAgentResults (needs ReturnRequests.Id)
--   6. AgentRecommendations / DemandHistory / Buyers  (independent)
--
-- IDEMPOTENT: safe to re-run. Every block checks existence before altering.
-- ============================================================

SET NOCOUNT ON;

-- ============================================================
-- STEP 0  Diagnostics — print current FK state
-- ============================================================
PRINT '=== [STEP 0] Current FK Diagnostics ==='

-- Safety guard: if [Returns] was renamed to [ImageValidationResults],
-- rename it back so all subsequent steps in this script work correctly.
-- Full remediation is in 006_Rename_ImageValidationResults_To_Returns.sql;
-- this guard makes 005 re-runnable even when 006 has not been applied yet.
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
    PRINT '  [GUARD] Renamed [ImageValidationResults] -> [Returns] before proceeding';
END

SELECT
    fk.name                              AS ConstraintName,
    OBJECT_NAME(fk.parent_object_id)     AS ChildTable,
    COL_NAME(fkc.parent_object_id,  fkc.parent_column_id)     AS ChildColumn,
    OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
    COL_NAME(fkc.referenced_object_id,fkc.referenced_column_id) AS ParentColumn
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE OBJECT_NAME(fk.parent_object_id) IN (
    'InventoryPool', 'MatchAgentResults', 'ReturnRequests', 'Returns')
ORDER BY ChildTable, ConstraintName;

GO

-- ============================================================
-- STEP 1  Fix FK_InventoryPool_ImageValidationResults
--         InventoryPool.ReturnId must reference Returns.Id
-- ============================================================
PRINT '=== [STEP 1] Fixing InventoryPool foreign key ==='

BEGIN TRY
    BEGIN TRANSACTION T_InventoryPool_FK;

    -- 1a. Drop the wrong FK (if it still exists pointing at ImageValidationResults)
    IF EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_InventoryPool_ImageValidationResults'
    )
    BEGIN
        ALTER TABLE [dbo].[InventoryPool]
            DROP CONSTRAINT [FK_InventoryPool_ImageValidationResults];
        PRINT '  Dropped FK_InventoryPool_ImageValidationResults';
    END

    -- 1b. Also drop the earlier partial-fix constraint from script 003, if present,
    --     because we will recreate it below with a clean definition.
    IF EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_InventoryPool_ReturnRequests'
    )
    BEGIN
        -- This was wrong: InventoryPool.ReturnId references Returns, not ReturnRequests.
        ALTER TABLE [dbo].[InventoryPool]
            DROP CONSTRAINT [FK_InventoryPool_ReturnRequests];
        PRINT '  Dropped stale FK_InventoryPool_ReturnRequests (was pointing at wrong parent)';
    END

    -- 1c. Validate that the Returns table exists before continuing.
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Returns'
    )
    BEGIN
        RAISERROR('Table [Returns] does not exist. Cannot create FK. Aborting.', 16, 1);
    END

    -- 1d. Delete InventoryPool rows whose ReturnId has no matching row in Returns
    --     (orphan cleanup required before constraint can be added).
    DELETE FROM [dbo].[InventoryPool]
    WHERE [ReturnId] NOT IN (SELECT [Id] FROM [dbo].[Returns]);

    IF @@ROWCOUNT > 0
        PRINT '  Removed orphan InventoryPool rows with invalid ReturnId';

    -- 1e. Create the correct FK: InventoryPool.ReturnId -> Returns.Id
    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_InventoryPool_Returns'
    )
    BEGIN
        ALTER TABLE [dbo].[InventoryPool]
            ADD CONSTRAINT [FK_InventoryPool_Returns]
            FOREIGN KEY ([ReturnId])
            REFERENCES [dbo].[Returns]([Id])
            ON DELETE CASCADE;
        PRINT '  Created FK_InventoryPool_Returns  (InventoryPool.ReturnId -> Returns.Id)';
    END
    ELSE
    BEGIN
        PRINT '  FK_InventoryPool_Returns already exists — skipped';
    END

    COMMIT TRANSACTION T_InventoryPool_FK;
    PRINT '  [STEP 1] COMMITTED';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION T_InventoryPool_FK;

    DECLARE @Err1Msg  NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @Err1Sev  INT            = ERROR_SEVERITY();
    DECLARE @Err1Lin  INT            = ERROR_LINE();
    PRINT '  [STEP 1] ROLLED BACK — ' + @Err1Msg;
    RAISERROR('Step 1 failed at line %d: %s', @Err1Sev, 1, @Err1Lin, @Err1Msg);
END CATCH

GO

-- ============================================================
-- STEP 2  Fix FK_MatchAgentResults_ReturnRequests
--         MatchAgentResults.ReturnRequestId must reference ReturnRequests.Id
--         (PK column is 'Id', not 'ReturnRequestId')
-- ============================================================
PRINT '=== [STEP 2] Fixing MatchAgentResults foreign key ==='

BEGIN TRY
    BEGIN TRANSACTION T_MatchAgent_FK;

    -- 2a. Drop any existing FK on MatchAgentResults.ReturnRequestId
    --     (covers both wrong and partial-fix variants)
    DECLARE @fkName NVARCHAR(200);
    SELECT @fkName = fk.name
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    WHERE fk.parent_object_id  = OBJECT_ID('dbo.MatchAgentResults')
      AND fkc.parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.MatchAgentResults'), 'ReturnRequestId', 'ColumnId');

    IF @fkName IS NOT NULL
    BEGIN
        DECLARE @DropFK NVARCHAR(500) = 'ALTER TABLE [dbo].[MatchAgentResults] DROP CONSTRAINT [' + @fkName + ']';
        EXEC sp_executesql @DropFK;
        PRINT '  Dropped existing FK on MatchAgentResults.ReturnRequestId: ' + @fkName;
    END

    -- 2b. Validate ReturnRequests table exists.
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'ReturnRequests'
    )
    BEGIN
        RAISERROR('Table [ReturnRequests] does not exist. Cannot create FK. Aborting.', 16, 1);
    END

    -- 2c. Delete MatchAgentResults rows whose ReturnRequestId has no match in ReturnRequests.Id
    DELETE FROM [dbo].[MatchAgentResults]
    WHERE [ReturnRequestId] NOT IN (SELECT [Id] FROM [dbo].[ReturnRequests]);

    IF @@ROWCOUNT > 0
        PRINT '  Removed orphan MatchAgentResults rows with invalid ReturnRequestId';

    -- 2d. Create the correct FK: MatchAgentResults.ReturnRequestId -> ReturnRequests.Id
    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_MatchAgentResults_ReturnRequests'
          AND parent_object_id  = OBJECT_ID('dbo.MatchAgentResults')
          AND referenced_object_id = OBJECT_ID('dbo.ReturnRequests')
    )
    BEGIN
        ALTER TABLE [dbo].[MatchAgentResults]
            ADD CONSTRAINT [FK_MatchAgentResults_ReturnRequests]
            FOREIGN KEY ([ReturnRequestId])
            REFERENCES [dbo].[ReturnRequests]([Id])
            ON DELETE NO ACTION;
        PRINT '  Created FK_MatchAgentResults_ReturnRequests  (ReturnRequestId -> ReturnRequests.Id)';
    END
    ELSE
    BEGIN
        PRINT '  FK_MatchAgentResults_ReturnRequests already correct — skipped';
    END

    COMMIT TRANSACTION T_MatchAgent_FK;
    PRINT '  [STEP 2] COMMITTED';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION T_MatchAgent_FK;

    DECLARE @Err2Msg  NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @Err2Sev  INT            = ERROR_SEVERITY();
    DECLARE @Err2Lin  INT            = ERROR_LINE();
    PRINT '  [STEP 2] ROLLED BACK — ' + @Err2Msg;
    RAISERROR('Step 2 failed at line %d: %s', @Err2Sev, 1, @Err2Lin, @Err2Msg);
END CATCH

GO

-- ============================================================
-- STEP 3  Verify all other existing FKs are sound
-- ============================================================
PRINT '=== [STEP 3] Verifying ReturnRequests.PackageId -> Packages.Id ==='

BEGIN TRY
    BEGIN TRANSACTION T_ReturnReq_FK;

    -- Orphan cleanup: ReturnRequests whose PackageId has no parent Package
    DELETE FROM [dbo].[MatchAgentResults]
    WHERE [ReturnRequestId] IN (
        SELECT rr.[Id]
        FROM [dbo].[ReturnRequests] rr
        WHERE rr.[PackageId] NOT IN (SELECT [Id] FROM [dbo].[Packages])
    );

    DELETE FROM [dbo].[ReturnRequests]
    WHERE [PackageId] NOT IN (SELECT [Id] FROM [dbo].[Packages]);

    IF @@ROWCOUNT > 0
        PRINT '  Removed orphan ReturnRequests rows with invalid PackageId';

    -- Ensure the FK exists
    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_ReturnRequests_Packages'
          AND parent_object_id = OBJECT_ID('dbo.ReturnRequests')
    )
    BEGIN
        ALTER TABLE [dbo].[ReturnRequests]
            ADD CONSTRAINT [FK_ReturnRequests_Packages]
            FOREIGN KEY ([PackageId])
            REFERENCES [dbo].[Packages]([Id])
            ON DELETE NO ACTION;
        PRINT '  Created FK_ReturnRequests_Packages';
    END
    ELSE
    BEGIN
        PRINT '  FK_ReturnRequests_Packages already exists — OK';
    END

    COMMIT TRANSACTION T_ReturnReq_FK;
    PRINT '  [STEP 3] COMMITTED';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION T_ReturnReq_FK;

    DECLARE @Err3Msg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @Err3Sev INT            = ERROR_SEVERITY();
    DECLARE @Err3Lin INT            = ERROR_LINE();
    PRINT '  [STEP 3] ROLLED BACK — ' + @Err3Msg;
    RAISERROR('Step 3 failed at line %d: %s', @Err3Sev, 1, @Err3Lin, @Err3Msg);
END CATCH

GO

-- ============================================================
-- STEP 4  Seed data — correct insert sequence
--         Packages -> ReturnRequests -> Returns -> InventoryPool
--                                                -> MatchAgentResults
-- ============================================================
PRINT '=== [STEP 4] Seed data (correct insert sequence) ==='

BEGIN TRY
    BEGIN TRANSACTION T_Seed;

    -- ---- 4a. Packages (prerequisite for everything) ----
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Packages])
    BEGIN
        DECLARE @PkgId1 UNIQUEIDENTIFIER = NEWID();
        DECLARE @PkgId2 UNIQUEIDENTIFIER = NEWID();
        DECLARE @PkgId3 UNIQUEIDENTIFIER = NEWID();

        INSERT INTO [dbo].[Packages]
            ([Id],[TrackingNumber],[SenderName],[SenderAddress],
             [RecipientName],[RecipientAddress],[Weight],[Status],[IsReturnable],[CreatedAt])
        VALUES
            (@PkgId1,'UPS-SEED-001','Acme Corp','123 Sender St, Chennai',
             'Bob Smith','45 Return Ave, Chennai',2.5,'Delivered',1,SYSUTCDATETIME()),
            (@PkgId2,'UPS-SEED-002','Global Traders','88 Export Road, Bangalore',
             'Alice Rao','10 Receiver Lane, Bangalore',1.2,'Delivered',1,SYSUTCDATETIME()),
            (@PkgId3,'UPS-SEED-003','Premium Goods','7 Dispatch Blvd, Mumbai',
             'Vikram Kumar','32 Hub Street, Mumbai',3.8,'Delivered',1,SYSUTCDATETIME());

        PRINT '  Seeded 3 Packages';
    END
    ELSE
    BEGIN
        SELECT TOP 1
            @PkgId1 = [Id] FROM [dbo].[Packages] ORDER BY [CreatedAt];
        SELECT TOP 1
            @PkgId2 = [Id] FROM [dbo].[Packages] WHERE [Id] <> @PkgId1 ORDER BY [CreatedAt];
        SELECT TOP 1
            @PkgId3 = [Id] FROM [dbo].[Packages] WHERE [Id] NOT IN (@PkgId1,@PkgId2) ORDER BY [CreatedAt];
        PRINT '  Packages already seeded — reusing existing Ids';
    END

    -- ---- 4b. ReturnRequests (needs Packages.Id) ----
    IF NOT EXISTS (SELECT 1 FROM [dbo].[ReturnRequests])
    BEGIN
        DECLARE @RrId1 UNIQUEIDENTIFIER = NEWID();
        DECLARE @RrId2 UNIQUEIDENTIFIER = NEWID();
        DECLARE @RrId3 UNIQUEIDENTIFIER = NEWID();

        INSERT INTO [dbo].[ReturnRequests]
            ([Id],[PackageId],[Reason],[Status],[CreatedAt])
        VALUES
            (@RrId1, @PkgId1, 'Item defective on arrival',  'Processed', SYSUTCDATETIME()),
            (@RrId2, @PkgId2, 'Wrong item shipped',         'Processed', SYSUTCDATETIME()),
            (@RrId3, @PkgId3, 'Changed mind after delivery','Processed', SYSUTCDATETIME());

        PRINT '  Seeded 3 ReturnRequests';
    END
    ELSE
    BEGIN
        SELECT TOP 1 @RrId1 = [Id] FROM [dbo].[ReturnRequests] ORDER BY [CreatedAt];
        SELECT TOP 1 @RrId2 = [Id] FROM [dbo].[ReturnRequests] WHERE [Id] <> @RrId1 ORDER BY [CreatedAt];
        SELECT TOP 1 @RrId3 = [Id] FROM [dbo].[ReturnRequests] WHERE [Id] NOT IN (@RrId1,@RrId2) ORDER BY [CreatedAt];
        PRINT '  ReturnRequests already seeded — reusing existing Ids';
    END

    -- ---- 4c. Returns (independent — AI-validated physical return) ----
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Returns])
    BEGIN
        DECLARE @RetId1 UNIQUEIDENTIFIER = NEWID();
        DECLARE @RetId2 UNIQUEIDENTIFIER = NEWID();
        DECLARE @RetId3 UNIQUEIDENTIFIER = NEWID();

        INSERT INTO [dbo].[Returns]
            ([Id],[ProductId],[ProductName],[Category],[ReturnReason],
             [Condition],[Eligibility],[Confidence],[Location],[ReturnDate],[CreatedAt])
        VALUES
            (@RetId1,'PROD-001','Samsung Galaxy S21','Electronics','Defective screen',
             'Good','Eligible',0.92,'Chennai',SYSUTCDATETIME(),SYSUTCDATETIME()),
            (@RetId2,'PROD-002','Nike Air Max','Footwear','Wrong size shipped',
             'New','Eligible',0.97,'Bangalore',SYSUTCDATETIME(),SYSUTCDATETIME()),
            (@RetId3,'PROD-003','IKEA Bookshelf','Home','Damaged in transit',
             'Fair','Eligible',0.78,'Mumbai',SYSUTCDATETIME(),SYSUTCDATETIME());

        PRINT '  Seeded 3 Returns';
    END
    ELSE
    BEGIN
        SELECT TOP 1 @RetId1 = [Id] FROM [dbo].[Returns] ORDER BY [CreatedAt];
        SELECT TOP 1 @RetId2 = [Id] FROM [dbo].[Returns] WHERE [Id] <> @RetId1 ORDER BY [CreatedAt];
        SELECT TOP 1 @RetId3 = [Id] FROM [dbo].[Returns] WHERE [Id] NOT IN (@RetId1,@RetId2) ORDER BY [CreatedAt];
        PRINT '  Returns already seeded — reusing existing Ids';
    END

    -- ---- 4d. InventoryPool (FK: ReturnId -> Returns.Id — CORRECT parent) ----
    IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryPool])
    BEGIN
        INSERT INTO [dbo].[InventoryPool]
            ([Id],[ReturnId],[ProductId],[Location],[HoldingDays],[MatchScore],[Status],[CreatedAt])
        VALUES
            (NEWID(), @RetId1, 'PROD-001', 'Chennai',   2, 91.5, 'Available', SYSUTCDATETIME()),
            (NEWID(), @RetId2, 'PROD-002', 'Bangalore', 1, 97.0, 'Available', SYSUTCDATETIME()),
            (NEWID(), @RetId3, 'PROD-003', 'Mumbai',    4, 78.2, 'Available', SYSUTCDATETIME());

        PRINT '  Seeded 3 InventoryPool rows (ReturnId -> Returns.Id)';
    END
    ELSE
        PRINT '  InventoryPool already seeded — skipped';

    -- ---- 4e. MatchAgentResults (FK: ReturnRequestId -> ReturnRequests.Id — CORRECT column) ----
    IF NOT EXISTS (SELECT 1 FROM [dbo].[MatchAgentResults])
    BEGIN
        INSERT INTO [dbo].[MatchAgentResults]
            ([Id],[ReturnRequestId],[ProductId],[ProductName],[Category],
             [Location],[Condition],[MatchScore],[Recommendation],[Confidence],
             [DistanceSavedKm],[CostSaved],[Co2Saved],[Explanation],[MatchDetailsJson],[CreatedAt])
        VALUES
            (NEWID(), @RrId1, 'PROD-001','Samsung Galaxy S21','Electronics',
             'Chennai','Good',91,'Local Resale',0.92,
             14.2, 4200.0, 3.8,'Strong local demand in Chennai hub','[]', SYSUTCDATETIME()),
            (NEWID(), @RrId2, 'PROD-002','Nike Air Max','Footwear',
             'Bangalore','New',97,'Local Resale',0.97,
             18.5, 5100.0, 4.9,'Matched to premium buyer in Koramangala','[]', SYSUTCDATETIME()),
            (NEWID(), @RrId3, 'PROD-003','IKEA Bookshelf','Home',
             'Mumbai','Fair',78,'Local Resale',0.78,
             22.1, 3300.0, 6.1,'Fair condition accepted by bulk buyer','[]', SYSUTCDATETIME());

        PRINT '  Seeded 3 MatchAgentResults rows (ReturnRequestId -> ReturnRequests.Id)';
    END
    ELSE
        PRINT '  MatchAgentResults already seeded — skipped';

    -- ---- 4f. AgentRecommendations (independent — no FK) ----
    IF NOT EXISTS (SELECT 1 FROM [dbo].[AgentRecommendations])
    BEGIN
        INSERT INTO [dbo].[AgentRecommendations]
            ([Id],[AgentName],[Recommendation],[Confidence],[CreatedDate],[CreatedAt])
        VALUES
            (NEWID(),'ImageValidationAgent','Item is in Good condition, eligible for resale',0.92,SYSUTCDATETIME(),SYSUTCDATETIME()),
            (NEWID(),'EligibilityAgent',    'Approved for local resale channel',             0.95,SYSUTCDATETIME(),SYSUTCDATETIME()),
            (NEWID(),'RootCauseAgent',      'Return caused by logistics damage — carrier SLA breach',0.88,SYSUTCDATETIME(),SYSUTCDATETIME());

        PRINT '  Seeded 3 AgentRecommendations rows';
    END
    ELSE
        PRINT '  AgentRecommendations already seeded — skipped';

    COMMIT TRANSACTION T_Seed;
    PRINT '  [STEP 4] COMMITTED — seed data inserted in correct order';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION T_Seed;

    DECLARE @Err4Msg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @Err4Sev INT            = ERROR_SEVERITY();
    DECLARE @Err4Lin INT            = ERROR_LINE();
    PRINT '  [STEP 4] ROLLED BACK — ' + @Err4Msg;
    RAISERROR('Step 4 failed at line %d: %s', @Err4Sev, 1, @Err4Lin, @Err4Msg);
END CATCH

GO

-- ============================================================
-- STEP 5  Stored Procedures — corrected with FK-aware logic
--         Each SP uses TRY/CATCH with explicit ROLLBACK.
-- ============================================================
PRINT '=== [STEP 5] Recreating stored procedures ==='

GO

-- ----------------------------------------------------------------
-- usp_CreateReturnRequest
-- Insert sequence: Packages must exist first (FK PackageId -> Packages.Id)
-- ----------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[usp_CreateReturnRequest]
    @PackageId   UNIQUEIDENTIFIER,
    @ReturnReason NVARCHAR(1000),
    @Location    NVARCHAR(200) = NULL,
    @ImageUrl    NVARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Guard: validate parent Package exists before insert
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Packages] WHERE [Id] = @PackageId AND [IsDeleted] = 0)
    BEGIN
        RAISERROR('Package does not exist or is deleted. ReturnRequest cannot be created.', 16, 1);
        RETURN;
    END

    DECLARE @NewId UNIQUEIDENTIFIER = NEWID();
    DECLARE @Now   DATETIME2        = SYSUTCDATETIME();

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [dbo].[ReturnRequests]
            ([Id],[PackageId],[Reason],[Status],[CreatedAt])
        VALUES
            (@NewId, @PackageId, @ReturnReason, 'Pending', @Now);

        -- Return the created row (mirrors EF projection)
        SELECT
            [Id]          AS ReturnRequestId,
            [PackageId],
            [Status],
            [CreatedAt]   AS CreatedDate
        FROM [dbo].[ReturnRequests]
        WHERE [Id] = @NewId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ----------------------------------------------------------------
-- usp_GetReturnRequestById
-- ----------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[usp_GetReturnRequestById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rr.[Id],
        rr.[PackageId],
        rr.[Reason],
        rr.[Status],
        rr.[AiAnalysis],
        rr.[ResolutionNotes],
        rr.[CreatedAt],
        rr.[ResolvedAt],
        p.[TrackingNumber],
        p.[SenderName],
        p.[RecipientName],
        p.[Status]   AS PackageStatus
    FROM [dbo].[ReturnRequests] rr
    INNER JOIN [dbo].[Packages] p ON p.[Id] = rr.[PackageId]
    WHERE rr.[Id] = @Id
      AND rr.[IsDeleted] = 0;
END
GO

-- ----------------------------------------------------------------
-- usp_SaveMatchResult
-- FK: ReturnRequestId -> ReturnRequests.Id  (column 'Id', NOT 'ReturnRequestId')
-- ----------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[usp_SaveMatchResult]
    @ReturnRequestId   UNIQUEIDENTIFIER,
    @ProductId         NVARCHAR(100),
    @ProductName       NVARCHAR(300),
    @Category          NVARCHAR(100),
    @Location          NVARCHAR(200),
    @Condition         NVARCHAR(50),
    @MatchScore        INT,
    @Recommendation    NVARCHAR(200),
    @Confidence        FLOAT,
    @DistanceSavedKm   FLOAT,
    @CostSaved         FLOAT,
    @Co2Saved          FLOAT,
    @Explanation       NVARCHAR(4000),
    @MatchDetailsJson  NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    -- Guard: ReturnRequest.Id (PK) must exist — FK target is Id, not a separate column
    IF NOT EXISTS (
        SELECT 1 FROM [dbo].[ReturnRequests]
        WHERE [Id] = @ReturnRequestId AND [IsDeleted] = 0
    )
    BEGIN
        RAISERROR('ReturnRequest does not exist or is deleted. MatchAgentResult cannot be saved.', 16, 1);
        RETURN;
    END

    DECLARE @NewId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [dbo].[MatchAgentResults]
            ([Id],[ReturnRequestId],[ProductId],[ProductName],[Category],
             [Location],[Condition],[MatchScore],[Recommendation],[Confidence],
             [DistanceSavedKm],[CostSaved],[Co2Saved],[Explanation],[MatchDetailsJson],[CreatedAt])
        VALUES
            (@NewId,@ReturnRequestId,@ProductId,@ProductName,@Category,
             @Location,@Condition,@MatchScore,@Recommendation,@Confidence,
             @DistanceSavedKm,@CostSaved,@Co2Saved,@Explanation,@MatchDetailsJson,
             SYSUTCDATETIME());

        -- Return the generated Id to the caller (mirrors GuidResult in repository)
        SELECT @NewId AS Id;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ----------------------------------------------------------------
-- usp_AddToInventoryPool
-- FK: ReturnId -> Returns.Id  (NOT ImageValidationResults, NOT ReturnRequests)
-- ----------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[usp_AddToInventoryPool]
    @ReturnId   UNIQUEIDENTIFIER,
    @ProductId  NVARCHAR(100),
    @Location   NVARCHAR(200),
    @MatchScore FLOAT
AS
BEGIN
    SET NOCOUNT ON;

    -- Guard: Returns.Id (PK) must exist — correct FK parent is Returns, not ImageValidationResults
    IF NOT EXISTS (
        SELECT 1 FROM [dbo].[Returns]
        WHERE [Id] = @ReturnId AND [IsDeleted] = 0
    )
    BEGIN
        RAISERROR('Return does not exist or is deleted. Cannot add to InventoryPool.', 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Upsert: if this ReturnId + ProductId already exists, update; else insert.
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

-- ----------------------------------------------------------------
-- usp_GetInventoryByProduct
-- JOIN path: InventoryPool.ReturnId -> Returns.Id (corrected join)
-- ----------------------------------------------------------------
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
        -- Pull product detail from the correctly joined Returns row
        r.[ProductName],
        r.[Category],
        r.[Condition],
        r.[Eligibility]
    FROM [dbo].[InventoryPool] ip
    -- Correct JOIN: ReturnId references Returns.Id
    INNER JOIN [dbo].[Returns] r ON r.[Id] = ip.[ReturnId] AND r.[IsDeleted] = 0
    WHERE ip.[ProductId] = @ProductId
      AND ip.[IsDeleted] = 0
      AND (@Location IS NULL OR ip.[Location] = @Location)
    ORDER BY ip.[MatchScore] DESC;
END
GO

-- ----------------------------------------------------------------
-- usp_SaveImageValidationResult
-- ImageValidationResults is a standalone audit log — no FK to InventoryPool.
-- InventoryPool.ReturnId -> Returns.Id is the correct relationship.
-- ----------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[usp_SaveImageValidationResult]
    @ProductId   NVARCHAR(100),
    @ProductName NVARCHAR(300),
    @Category    NVARCHAR(100),
    @ReturnReason NVARCHAR(1000),
    @Condition   NVARCHAR(50),
    @Eligibility NVARCHAR(50),
    @Confidence  FLOAT,
    @Location    NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NewId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Insert into Returns (the correct parent of InventoryPool)
        -- ImageValidationResults is a write-only audit log; InventoryPool does NOT reference it.
        INSERT INTO [dbo].[Returns]
            ([Id],[ProductId],[ProductName],[Category],[ReturnReason],
             [Condition],[Eligibility],[Confidence],[Location],[ReturnDate],[CreatedAt])
        VALUES
            (@NewId, @ProductId, @ProductName, @Category, @ReturnReason,
             @Condition, @Eligibility, @Confidence, @Location,
             SYSUTCDATETIME(), SYSUTCDATETIME());

        -- Return the new Returns.Id so the application layer can chain usp_AddToInventoryPool
        SELECT @NewId AS Id;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ============================================================
-- STEP 6  Post-migration validation
-- ============================================================
PRINT '=== [STEP 6] Post-migration FK validation ==='

SELECT
    fk.name                              AS ConstraintName,
    OBJECT_NAME(fk.parent_object_id)     AS ChildTable,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id)     AS ChildColumn,
    OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
    COL_NAME(fkc.referenced_object_id,fkc.referenced_column_id) AS ParentColumn,
    fk.delete_referential_action_desc    AS OnDelete
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE OBJECT_NAME(fk.parent_object_id) IN (
    'InventoryPool','MatchAgentResults','ReturnRequests')
ORDER BY ChildTable, ConstraintName;

GO

-- Row counts after migration
SELECT
    'Packages'             AS TableName, COUNT(*) AS Rows FROM [dbo].[Packages]         UNION ALL
SELECT 'ReturnRequests',                            COUNT(*) FROM [dbo].[ReturnRequests]  UNION ALL
SELECT 'Returns',                                   COUNT(*) FROM [dbo].[Returns]          UNION ALL
SELECT 'InventoryPool',                             COUNT(*) FROM [dbo].[InventoryPool]    UNION ALL
SELECT 'MatchAgentResults',                         COUNT(*) FROM [dbo].[MatchAgentResults] UNION ALL
SELECT 'AgentRecommendations',                      COUNT(*) FROM [dbo].[AgentRecommendations]
ORDER BY TableName;

PRINT '=== Migration 005_FK_Remediation_Full.sql completed successfully ==='
GO
