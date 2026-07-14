

CREATE OR ALTER PROCEDURE [dbo].[usp_GetAgentTelemetry]
AS
BEGIN
    SET NOCOUNT ON;

    -- ----------------------------------------------------------------
    -- 1. MatchAgent — sourced from [MatchAgentResults]
    -- ----------------------------------------------------------------
    SELECT
        'MatchAgent'                                                                   AS AgentName,
        COUNT(*)                                                                       AS TotalRuns,
        SUM(CASE WHEN Confidence >= 0.6 THEN 1 ELSE 0 END)                           AS SuccessfulRuns,
        ROUND(AVG(CAST(Confidence AS FLOAT)) * 100.0, 2)                              AS PrecisionRate,
        CASE WHEN COUNT(*) = 0 THEN 0.0
             ELSE ROUND(
                      CAST(SUM(CASE WHEN Confidence < 0.6 THEN 1 ELSE 0 END) AS FLOAT)
                      / NULLIF(COUNT(*), 0) * 100.0,
                  2)
        END                                                                            AS EscalationRate,
        120                                                                            AS AverageResponseTime
    FROM [dbo].[MatchAgentResults]

    UNION ALL

    -- ----------------------------------------------------------------
    -- 2. All other agents — sourced from [AgentRecommendations]
    --    Grouped by AgentName; AverageResponseTime is a deterministic
    --    business estimate per agent type (no runtime column available).
    -- ----------------------------------------------------------------
    SELECT
        AgentName,
        COUNT(*)                                                                       AS TotalRuns,
        SUM(CASE WHEN Confidence >= 0.6 THEN 1 ELSE 0 END)                           AS SuccessfulRuns,
        ROUND(AVG(CAST(Confidence AS FLOAT)) * 100.0, 2)                              AS PrecisionRate,
        CASE WHEN COUNT(*) = 0 THEN 0.0
             ELSE ROUND(
                      CAST(SUM(CASE WHEN Confidence < 0.6 THEN 1 ELSE 0 END) AS FLOAT)
                      / NULLIF(COUNT(*), 0) * 100.0,
                  2)
        END                                                                            AS EscalationRate,
        CASE AgentName
            WHEN 'ImageValidationAgent' THEN 250
            WHEN 'RootCauseAgent'       THEN 180
            WHEN 'EligibilityAgent'     THEN  95
            ELSE                              150
        END                                                                            AS AverageResponseTime
    FROM [dbo].[AgentRecommendations]
    GROUP BY AgentName;
END
GO
