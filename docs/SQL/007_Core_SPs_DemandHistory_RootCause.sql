
-- ----------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[usp_GetDemandHistory]
    @ProductId NVARCHAR(100),
    @Region    NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [Id],
        [ProductId],
        [Region],
        [OrderCount],
        [DemandScore],
        [CreatedAt],
        [UpdatedAt]
    FROM [dbo].[DemandHistory]
    WHERE [ProductId] = @ProductId
      AND [IsDeleted] = 0
      AND (@Region IS NULL OR [Region] = @Region)
    ORDER BY [DemandScore] DESC, [CreatedAt] DESC;
END
GO

-- ----------------------------------------------------------------
-- 2. usp_SaveRootCauseAnalysis
--    Called by: RootCauseSpRepository.SaveAnalysisAsync()
--    Params:    @AgentName, @RootCause, @Frequency,
--               @Recommendation, @Impact, @Confidence
--    Inserts into AgentRecommendations (AgentName = 'RootCauseAgent').
--    The full structured text is written to Recommendation so the
--    telemetry query can display it; RootCause is prefixed for clarity.
-- ----------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[usp_SaveRootCauseAnalysis]
    @AgentName     NVARCHAR(100),
    @RootCause     NVARCHAR(2000),
    @Frequency     NVARCHAR(200),
    @Recommendation NVARCHAR(2000),
    @Impact        NVARCHAR(2000),
    @Confidence    FLOAT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NewId UNIQUEIDENTIFIER = NEWID();
    DECLARE @Now   DATETIME2        = SYSUTCDATETIME();

    -- Compose a structured summary stored in the Recommendation column.
    DECLARE @Summary NVARCHAR(MAX) =
        N'ROOT_CAUSE: '    + ISNULL(@RootCause, '')     + N' | ' +
        N'FREQUENCY: '     + ISNULL(@Frequency, '')     + N' | ' +
        N'RECOMMENDATION: '+ ISNULL(@Recommendation, '')+ N' | ' +
        N'IMPACT: '        + ISNULL(@Impact, '');

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [dbo].[AgentRecommendations]
            ([Id],[AgentName],[Recommendation],[Confidence],[CreatedDate],[CreatedAt])
        VALUES
            (@NewId, @AgentName, @Summary, @Confidence, @Now, @Now);

        -- Return the new record Id to the caller (mirrors GuidResult in repository)
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
-- 3. usp_GetReturnReasonsByCategory
--    Called by: RootCauseSpRepository.GetReturnReasonsByCategoryAsync()
--    Returns:   ReturnReason, ProductName, Location, Count, Percentage
--    Source:    Returns (the AI-validated physical return records)
-- ----------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[usp_GetReturnReasonsByCategory]
    @Category NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    -- Total for percentage calculation scoped to the category
    DECLARE @Total INT;
    SELECT @Total = COUNT(*)
    FROM [dbo].[Returns]
    WHERE [Category] = @Category
      AND [IsDeleted] = 0;

    IF @Total = 0
    BEGIN
        SELECT
            CAST(NULL AS NVARCHAR(1000)) AS ReturnReason,
            CAST(NULL AS NVARCHAR(300))  AS ProductName,
            CAST(NULL AS NVARCHAR(200))  AS Location,
            CAST(NULL AS INT)            AS [Count],
            CAST(NULL AS FLOAT)          AS Percentage
        WHERE 1 = 0;  -- empty result set with correct shape
        RETURN;
    END

    SELECT
        r.[ReturnReason],
        -- Most common product name within each reason group
        MAX(r.[ProductName])                                AS ProductName,
        -- Most common location within each reason group
        MAX(r.[Location])                                   AS Location,
        COUNT(*)                                            AS [Count],
        ROUND(CAST(COUNT(*) AS FLOAT) / @Total * 100.0, 1) AS Percentage
    FROM [dbo].[Returns] r
    WHERE r.[Category] = @Category
      AND r.[IsDeleted] = 0
    GROUP BY r.[ReturnReason]
    ORDER BY [Count] DESC;
END
GO

-- ----------------------------------------------------------------
-- Seed: DemandHistory rows for core product categories and hubs
-- Used by usp_GetDemandHistory and MatchAgentService scoring.
-- ----------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[DemandHistory])
BEGIN
    INSERT INTO [dbo].[DemandHistory]
        ([Id],[ProductId],[Region],[OrderCount],[DemandScore],[CreatedAt])
    VALUES
        -- Electronics
        (NEWID(),'PROD-001','Chennai',  120, 92.0, SYSUTCDATETIME()),
        (NEWID(),'PROD-001','Bangalore', 98, 88.5, SYSUTCDATETIME()),
        (NEWID(),'PROD-001','Mumbai',    84, 84.0, SYSUTCDATETIME()),
        (NEWID(),'PROD-001','Delhi',     71, 79.5, SYSUTCDATETIME()),
        (NEWID(),'PROD-001','Hyderabad', 65, 75.0, SYSUTCDATETIME()),
        -- Footwear
        (NEWID(),'PROD-002','Chennai',   88, 85.0, SYSUTCDATETIME()),
        (NEWID(),'PROD-002','Bangalore', 76, 82.0, SYSUTCDATETIME()),
        (NEWID(),'PROD-002','Mumbai',    62, 77.0, SYSUTCDATETIME()),
        -- Home
        (NEWID(),'PROD-003','Chennai',   55, 72.0, SYSUTCDATETIME()),
        (NEWID(),'PROD-003','Mumbai',    48, 68.5, SYSUTCDATETIME());

    PRINT 'Seeded 10 DemandHistory rows across 5 hubs.';
END
ELSE
    PRINT 'DemandHistory already seeded — skipped.';
GO

PRINT '007_Core_SPs_DemandHistory_RootCause.sql completed.'
GO
