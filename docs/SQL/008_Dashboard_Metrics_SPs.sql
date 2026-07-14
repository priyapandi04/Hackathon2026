
CREATE OR ALTER PROCEDURE [dbo].[usp_GetDashboardMetrics]
    @FromDate DATETIME2 = NULL,
    @ToDate   DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Normalise: default to all-time if no dates supplied
    DECLARE @From DATETIME2 = ISNULL(@FromDate, '2000-01-01');
    DECLARE @To   DATETIME2 = ISNULL(@ToDate,   SYSUTCDATETIME());

    DECLARE
        @TotalReturns    INT,
        @EligibleReturns INT,
        @LocalMatches    INT,
        @DistanceSaved   FLOAT,
        @CostSaved       FLOAT,
        @Co2Saved        FLOAT,
        @DiversionRate   FLOAT;

    -- Total return requests within the window
    SELECT @TotalReturns = COUNT(*)
    FROM [dbo].[ReturnRequests]
    WHERE [IsDeleted] = 0
      AND [CreatedAt] BETWEEN @From AND @To;

    -- Eligible items (Returns where Eligibility = 'Eligible')
    SELECT @EligibleReturns = COUNT(*)
    FROM [dbo].[Returns]
    WHERE [IsDeleted] = 0
      AND [Eligibility] = 'Eligible'
      AND [CreatedAt] BETWEEN @From AND @To;

    -- Local matches and financial/environmental savings from MatchAgentResults
    SELECT
        @LocalMatches  = COUNT(CASE WHEN MatchScore >= 70 THEN 1 END),
        @DistanceSaved = ISNULL(SUM(DistanceSavedKm), 0),
        @CostSaved     = ISNULL(SUM(CostSaved),        0),
        @Co2Saved      = ISNULL(SUM(Co2Saved),          0)
    FROM [dbo].[MatchAgentResults]
    WHERE [IsDeleted] = 0
      AND [CreatedAt] BETWEEN @From AND @To;

    -- Diversion rate: LocalMatches / TotalReturns * 100 (avoid div-by-zero)
    SET @DiversionRate = CASE
        WHEN ISNULL(@TotalReturns, 0) = 0 THEN 0.0
        ELSE ROUND(CAST(ISNULL(@LocalMatches, 0) AS FLOAT)
                   / CAST(@TotalReturns AS FLOAT) * 100.0, 2)
    END;

    -- Single-row result set — matches DashboardMetricsSpResult projection
    SELECT
        ISNULL(@TotalReturns,    0) AS TotalReturns,
        ISNULL(@EligibleReturns, 0) AS EligibleReturns,
        ISNULL(@LocalMatches,    0) AS LocalMatches,
        ISNULL(@DiversionRate,   0) AS DiversionRate,
        ISNULL(@DistanceSaved,   0) AS DistanceSavedKm,
        ISNULL(@CostSaved,       0) AS CostSaved,
        ISNULL(@Co2Saved,        0) AS Co2SavedKg;
END
GO

-- ----------------------------------------------------------------
-- 2. usp_GetDashboardRootCauseInsights
--    Called by: DashboardSpRepository.GetMetricsAsync() (second result set)
--    Returns:   Reason, Count, Percentage
--    Source:    Returns.ReturnReason — top N reasons across all returns
-- ----------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[usp_GetDashboardRootCauseInsights]
    @FromDate DATETIME2 = NULL,
    @ToDate   DATETIME2 = NULL,
    @TopN     INT       = 10
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME2 = ISNULL(@FromDate, '2000-01-01');
    DECLARE @To   DATETIME2 = ISNULL(@ToDate,   SYSUTCDATETIME());

    -- Total in window for percentage denominator
    DECLARE @Total INT;
    SELECT @Total = COUNT(*)
    FROM [dbo].[Returns]
    WHERE [IsDeleted] = 0
      AND [CreatedAt] BETWEEN @From AND @To;

    IF ISNULL(@Total, 0) = 0
    BEGIN
        -- Return empty set with correct column shape
        SELECT
            CAST('' AS NVARCHAR(1000)) AS Reason,
            0                          AS [Count],
            CAST(0 AS FLOAT)           AS Percentage
        WHERE 1 = 0;
        RETURN;
    END

    SELECT TOP (@TopN)
        [ReturnReason]                                          AS Reason,
        COUNT(*)                                                AS [Count],
        ROUND(CAST(COUNT(*) AS FLOAT) / @Total * 100.0, 1)    AS Percentage
    FROM [dbo].[Returns]
    WHERE [IsDeleted] = 0
      AND [CreatedAt] BETWEEN @From AND @To
    GROUP BY [ReturnReason]
    ORDER BY [Count] DESC;
END
GO

PRINT '008_Dashboard_Metrics_SPs.sql completed.'
GO
