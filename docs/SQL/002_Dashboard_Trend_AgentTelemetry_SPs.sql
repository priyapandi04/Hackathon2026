-- ============================================================
-- 002_Dashboard_Trend_AgentTelemetry_SPs.sql
-- Stored procedures for dashboard trend & agent telemetry.
-- ============================================================

-- 1. usp_GetDashboardTrend — daily 30-day time-series for the trend chart
CREATE OR ALTER PROCEDURE [dbo].[usp_GetDashboardTrend]
    @Days INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Days AS (
        SELECT CAST(DATEADD(DAY, -n, CAST(SYSUTCDATETIME() AS DATE)) AS DATE) AS [Date]
        FROM (SELECT TOP (@Days) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS n
              FROM sys.objects) nums
    )
    SELECT
        d.[Date],
        ISNULL(COUNT(DISTINCT rr.Id), 0)      AS [Returns],
        COUNT(mar.Id)                          AS LocalMatches,
        ISNULL(SUM(mar.CostSaved), 0)         AS CostSaved,
        ISNULL(SUM(mar.DistanceSavedKm), 0)   AS DistanceSavedKm,
        ISNULL(SUM(mar.Co2Saved), 0)           AS Co2SavedKg
    FROM Days d
    LEFT JOIN [dbo].[ReturnRequests] rr
        ON CAST(rr.CreatedAt AS DATE) = d.[Date]
        AND rr.IsDeleted = 0
    LEFT JOIN [dbo].[MatchAgentResults] mar
        ON CAST(mar.CreatedAt AS DATE) = d.[Date]
    GROUP BY d.[Date]
    ORDER BY d.[Date];
END
GO

-- 2. usp_GetAgentTelemetry — agent performance from MatchAgentResults + AgentRecommendations
CREATE OR ALTER PROCEDURE [dbo].[usp_GetAgentTelemetry]
AS
BEGIN
    SET NOCOUNT ON;

    -- Match Agent metrics from MatchAgentResults
    SELECT
        'Demand Match Agent'            AS AgentName,
        COUNT(*)                        AS DecisionsMade,
        AVG(CAST(Confidence AS FLOAT)) * 100 AS Precision,
        CASE WHEN COUNT(*) = 0 THEN 0
             ELSE CAST(SUM(CASE WHEN Confidence < 0.6 THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) * 100
        END                             AS EscalationRate
    FROM [dbo].[MatchAgentResults]

    UNION ALL

    -- Other agents from AgentRecommendations
    SELECT
        AgentName,
        COUNT(*)                        AS DecisionsMade,
        AVG(CAST(Confidence AS FLOAT)) * 100 AS Precision,
        CASE WHEN COUNT(*) = 0 THEN 0
             ELSE CAST(SUM(CASE WHEN Confidence < 0.6 THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) * 100
        END                             AS EscalationRate
    FROM [dbo].[AgentRecommendations]
    GROUP BY AgentName;
END
GO
