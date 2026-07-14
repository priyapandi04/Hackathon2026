-- ============================================================
-- SP 1: CreateReturnRequest
-- ========================================================
CREATE OR ALTER PROCEDURE [dbo].[usp_CreateReturnRequest]
    @PackageId    UNIQUEIDENTIFIER,
    @ReturnReason NVARCHAR(1000),
    @Location     NVARCHAR(200) = NULL,
    @ImageUrl     NVARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Id UNIQUEIDENTIFIER = NEWID();
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    INSERT INTO [dbo].[ReturnRequests]
        ([Id], [PackageId], [Reason], [Status], [Location], [ImageUrl], [CreatedAt], [IsDeleted])
    VALUES
        (@Id, @PackageId, @ReturnReason, 'Pending', @Location, @ImageUrl, @Now, 0);

    SELECT
        @Id        AS ReturnRequestId,
        @PackageId AS PackageId,
        'Pending'  AS [Status],
        @Now       AS CreatedDate;
END
GO

-- ============================================================
-- SP 2: GetReturnRequestById
-- ============================================================
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
        p.[Status] AS PackageStatus
    FROM [dbo].[ReturnRequests] rr
    INNER JOIN [dbo].[Packages] p ON p.[Id] = rr.[PackageId]
    WHERE rr.[Id] = @Id
      AND rr.[IsDeleted] = 0;
END
GO

-- ============================================================
-- SP 3: SaveImageValidationResult
-- (Inserts into Returns table — stores image validation output)
-- ============================================================
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

    DECLARE @Id UNIQUEIDENTIFIER = NEWID();
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    INSERT INTO [dbo].[ImageValidationResults]
        ([Id], [ProductId], [ProductName], [Category], [ReturnReason],
         [Condition], [Eligibility], [Confidence], [Location], [ReturnDate],
         [CreatedAt], [IsDeleted])
    VALUES
        (@Id, @ProductId, @ProductName, @Category, @ReturnReason,
         @Condition, @Eligibility, @Confidence, @Location, @Now,
         @Now, 0);

    SELECT @Id AS Id;
END
GO

-- ============================================================
-- SP 4: AddToInventoryPool
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[usp_AddToInventoryPool]
    @ReturnId   UNIQUEIDENTIFIER,
    @ProductId  NVARCHAR(100),
    @Location   NVARCHAR(200),
    @MatchScore FLOAT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM [dbo].[InventoryPool]
               WHERE [ReturnId] = @ReturnId AND [IsDeleted] = 0)
    BEGIN
        UPDATE [dbo].[InventoryPool]
        SET [MatchScore] = @MatchScore,
            [Status] = 'Available',
            [UpdatedAt] = SYSUTCDATETIME()
        WHERE [ReturnId] = @ReturnId AND [IsDeleted] = 0;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[InventoryPool]
            ([Id], [ReturnId], [ProductId], [Location], [HoldingDays], [MatchScore], [Status], [CreatedAt], [IsDeleted])
        VALUES
            (NEWID(), @ReturnId, @ProductId, @Location, 0, @MatchScore, 'Available', SYSUTCDATETIME(), 0);
    END
END
GO

-- ============================================================
-- SP 5: GetInventoryByProduct
-- ============================================================
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
    INNER JOIN [dbo].[ImageValidationResults] r ON r.[Id] = ip.[ReturnId] AND r.[IsDeleted] = 0
    WHERE ip.[IsDeleted] = 0
      AND ip.[Status] = 'Available'
      AND ip.[ProductId] = @ProductId
      AND (@Location IS NULL OR ip.[Location] = @Location)
    ORDER BY ip.[MatchScore] DESC;
END
GO

-- ============================================================
-- SP 6: GetDemandHistory
-- ============================================================
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
    WHERE [IsDeleted] = 0
      AND [ProductId] = @ProductId
      AND (@Region IS NULL OR [Region] = @Region)
    ORDER BY [DemandScore] DESC;
END
GO

-- ============================================================
-- SP 7: SaveMatchResult
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[usp_SaveMatchResult]
    @ReturnRequestId UNIQUEIDENTIFIER,
    @ProductId       NVARCHAR(100),
    @ProductName     NVARCHAR(300),
    @Category        NVARCHAR(100),
    @Location        NVARCHAR(200),
    @Condition       NVARCHAR(50),
    @MatchScore      INT,
    @Recommendation  NVARCHAR(200),
    @Confidence      FLOAT,
    @DistanceSavedKm FLOAT,
    @CostSaved       FLOAT,
    @Co2Saved        FLOAT,
    @Explanation     NVARCHAR(4000),
    @MatchDetailsJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Id UNIQUEIDENTIFIER = NEWID();
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    INSERT INTO [dbo].[MatchAgentResults]
        ([Id], [ReturnRequestId], [ProductId], [ProductName], [Category],
         [Location], [Condition], [MatchScore], [Recommendation], [Confidence],
         [DistanceSavedKm], [CostSaved], [Co2Saved], [Explanation], [MatchDetailsJson],
         [CreatedAt], [IsDeleted])
    VALUES
        (@Id, @ReturnRequestId, @ProductId, @ProductName, @Category,
         @Location, @Condition, @MatchScore, @Recommendation, @Confidence,
         @DistanceSavedKm, @CostSaved, @Co2Saved, @Explanation, @MatchDetailsJson,
         @Now, 0);

    SELECT @Id AS Id;
END
GO

-- ============================================================
-- SP 8: SaveRootCauseAnalysis
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[usp_SaveRootCauseAnalysis]
    @AgentName      NVARCHAR(100) = 'RootCauseAgent',
    @RootCause      NVARCHAR(2000),
    @Frequency      NVARCHAR(200),
    @Recommendation NVARCHAR(2000),
    @Impact         NVARCHAR(2000),
    @Confidence     FLOAT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Id UNIQUEIDENTIFIER = NEWID();
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    -- Store in AgentRecommendations as audit record
    INSERT INTO [dbo].[AgentRecommendations]
        ([Id], [AgentName], [Recommendation], [Confidence], [CreatedDate], [CreatedAt], [IsDeleted])
    VALUES
        (@Id, @AgentName,
         CONCAT('RootCause: ', @RootCause, ' | Frequency: ', @Frequency,
                ' | Recommendation: ', @Recommendation, ' | Impact: ', @Impact),
         @Confidence, @Now, @Now, 0);

    SELECT @Id AS Id;
END
GO

-- ============================================================
-- SP 9: GetDashboardMetrics
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetDashboardMetrics]
    @FromDate DATETIME2 = NULL,
    @ToDate   DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- KPI Counts with real savings from MatchAgentResults
    SELECT
        COUNT(DISTINCT rr.[Id])                                           AS TotalReturns,
        SUM(CASE WHEN rr.Status IN ('Eligible','Matched','Diverted') THEN 1 ELSE 0 END) AS EligibleReturns,
        SUM(CASE WHEN rr.Status = 'Matched' THEN 1 ELSE 0 END)          AS LocalMatches,
        CASE
            WHEN COUNT(*) > 0
            THEN ROUND(CAST(SUM(CASE WHEN rr.Status = 'Matched' THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) * 100, 2)
            ELSE 0
        END                                                               AS DiversionRate,
        ISNULL(SUM(mar.[DistanceSavedKm]), 0)                            AS DistanceSavedKm,
        ISNULL(SUM(mar.[CostSaved]), 0)                                  AS CostSaved,
        ISNULL(SUM(mar.[Co2Saved]), 0)                                   AS Co2SavedKg
    FROM [dbo].[ReturnRequests] rr
    LEFT JOIN [dbo].[MatchAgentResults] mar ON mar.[ReturnRequestId] = rr.[Id] AND mar.[IsDeleted] = 0
    WHERE rr.[IsDeleted] = 0
      AND (@FromDate IS NULL OR rr.[CreatedAt] >= @FromDate)
      AND (@ToDate   IS NULL OR rr.[CreatedAt] <= @ToDate);

    -- Root Cause Insights (Top 10)
    SELECT TOP 10
        rr.[Reason],
        COUNT(*)   AS [Count],
        ROUND(CAST(COUNT(*) AS FLOAT) / NULLIF((
            SELECT COUNT(*) FROM [dbo].[ReturnRequests]
            WHERE [IsDeleted] = 0
              AND (@FromDate IS NULL OR [CreatedAt] >= @FromDate)
              AND (@ToDate   IS NULL OR [CreatedAt] <= @ToDate)
        ), 0) * 100, 2) AS [Percentage]
    FROM [dbo].[ReturnRequests] rr
    WHERE rr.[IsDeleted] = 0
      AND (@FromDate IS NULL OR rr.[CreatedAt] >= @FromDate)
      AND (@ToDate   IS NULL OR rr.[CreatedAt] <= @ToDate)
    GROUP BY rr.[Reason]
    ORDER BY COUNT(*) DESC;
END
GO

-- ============================================================
-- SP: Dashboard Root Cause Insights (split from usp_GetDashboardMetrics)
-- Required because EF Core SqlQueryRaw only reads a single result set
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetDashboardRootCauseInsights]
    @FromDate DATETIME2 = NULL,
    @ToDate   DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 10
        [Reason],
        COUNT(*) AS [Count],
        ROUND(CAST(COUNT(*) AS FLOAT) / NULLIF((
            SELECT COUNT(*) FROM [dbo].[ReturnRequests]
            WHERE [IsDeleted] = 0
              AND (@FromDate IS NULL OR [CreatedAt] >= @FromDate)
              AND (@ToDate   IS NULL OR [CreatedAt] <= @ToDate)
        ), 0) * 100, 2) AS [Percentage]
    FROM [dbo].[ReturnRequests]
    WHERE [IsDeleted] = 0
      AND (@FromDate IS NULL OR [CreatedAt] >= @FromDate)
      AND (@ToDate   IS NULL OR [CreatedAt] <= @ToDate)
    GROUP BY [Reason]
    ORDER BY COUNT(*) DESC;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_GetReturnReasonsByCategory]
    @Category NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Total INT;

    SELECT @Total = COUNT(*)
    FROM [dbo].[ReturnRequests]
    WHERE [IsDeleted] = 0;

    SELECT
        rr.[Reason]       AS ReturnReason,
        p.[SenderName]    AS ProductName,
        ISNULL(rr.[Location], 'Unknown') AS [Location],
        COUNT(*)          AS [Count],
        CASE
            WHEN @Total > 0
            THEN ROUND(CAST(COUNT(*) AS FLOAT) / @Total * 100, 1)
            ELSE 0
        END               AS [Percentage]
    FROM [dbo].[ReturnRequests] rr
    INNER JOIN [dbo].[Packages] p ON p.[Id] = rr.[PackageId]
    WHERE rr.[IsDeleted] = 0
      AND p.[Status] LIKE '%' + @Category + '%'
    GROUP BY rr.[Reason], p.[SenderName], rr.[Location]
    ORDER BY COUNT(*) DESC;
END
GO

-- ======================================================================================================================================================

---- SP: Dashboard with REAL savings from persisted match results
---- Replaces estimated savings with actual agent-computed values
---- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetDashboardMetrics_v2]
    @FromDate DATETIME2 = NULL,
    @ToDate   DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(DISTINCT rr.[Id])                                           AS TotalReturns,
        SUM(CASE WHEN rr.Status IN ('Eligible','Matched','Diverted') THEN 1 ELSE 0 END) AS EligibleReturns,
        SUM(CASE WHEN rr.Status = 'Matched' THEN 1 ELSE 0 END)          AS LocalMatches,
        CASE
            WHEN COUNT(*) > 0
            THEN ROUND(CAST(SUM(CASE WHEN rr.Status = 'Matched' THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) * 100, 2)
            ELSE 0
        END                                                               AS DiversionRate,
        ISNULL(SUM(mar.[DistanceSavedKm]), 0)                            AS DistanceSavedKm,
        ISNULL(SUM(mar.[CostSaved]), 0)                                  AS CostSaved,
        ISNULL(SUM(mar.[Co2Saved]), 0)                                   AS Co2SavedKg
    FROM [dbo].[ReturnRequests] rr
    LEFT JOIN [dbo].[MatchAgentResults] mar ON mar.[ReturnRequestId] = rr.[Id] AND mar.[IsDeleted] = 0
    WHERE rr.[IsDeleted] = 0
      AND (@FromDate IS NULL OR rr.[CreatedAt] >= @FromDate)
      AND (@ToDate   IS NULL OR rr.[CreatedAt] <= @ToDate);
END
GO

--====================
CREATE or Alter PROCEDURE [dbo].[usp_GetBuyersByHub]
    @Hub NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT [Name], [Zone], 
           CAST([DistanceKm] AS NVARCHAR(10)) + ' km' AS [Distance],
           [Delivery], [Score]
    FROM [dbo].[Buyers]
    WHERE [Hub] = @Hub
    ORDER BY [Score] DESC;
END
GO