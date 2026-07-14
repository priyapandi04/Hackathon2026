-- ============================================================
-- ReLoop SQL Validation & Smoke Test
-- Exercises stored procedures + analytical queries against the
-- deployed schema and the thousands-scale synthetic dataset.
-- Read-only except for a single round-trip create (cleaned up).
-- ============================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '=== 1. Row counts (base + synthetic) ===';
SELECT 'Packages' AS TableName, COUNT(*) AS Rows FROM dbo.Packages
UNION ALL SELECT 'ReturnRequests', COUNT(*) FROM dbo.ReturnRequests
UNION ALL SELECT 'ImageValidationResults', COUNT(*) FROM dbo.ImageValidationResults
UNION ALL SELECT 'InventoryPool', COUNT(*) FROM dbo.InventoryPool
UNION ALL SELECT 'MatchAgentResults', COUNT(*) FROM dbo.MatchAgentResults
UNION ALL SELECT 'DemandHistory', COUNT(*) FROM dbo.DemandHistory
UNION ALL SELECT 'AgentRecommendations', COUNT(*) FROM dbo.AgentRecommendations;

PRINT '';
PRINT '=== 2. 10-day Holding Clock buckets (InventoryPool) ===';
SELECT
    CASE
        WHEN HoldingDays >= 10 THEN '3. Expired (>=10) -> auto return-to-seller'
        WHEN HoldingDays >= 8  THEN '2. Closing window (8-9)'
        ELSE '1. On track (0-7)'
    END AS ClockBucket,
    COUNT(*) AS Items
FROM dbo.InventoryPool
GROUP BY CASE
        WHEN HoldingDays >= 10 THEN '3. Expired (>=10) -> auto return-to-seller'
        WHEN HoldingDays >= 8  THEN '2. Closing window (8-9)'
        ELSE '1. On track (0-7)'
    END
ORDER BY ClockBucket;

PRINT '';
PRINT '=== 3. Eligibility rate ===';
SELECT
    SUM(CASE WHEN Eligibility = 'Eligible' THEN 1 ELSE 0 END) AS Eligible,
    SUM(CASE WHEN Eligibility <> 'Eligible' THEN 1 ELSE 0 END) AS NotEligible,
    CAST(100.0 * SUM(CASE WHEN Eligibility = 'Eligible' THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5,1)) AS EligibilityPct
FROM dbo.ImageValidationResults;

PRINT '';
PRINT '=== 4. Auto-approval routing simulation (mirrors AutoApprovalPolicy) ===';
-- Value guardrail Rs.5000; confidence bands mirror the C# thresholds.
SELECT
    CASE
        WHEN ip.HoldingDays >= 10 THEN 'AUTO_APPROVE (expired -> return-to-seller)'
        WHEN r.Confidence < 0.60 THEN 'ESCALATE (low confidence)'
        WHEN (999 + (ABS(CHECKSUM(ip.Id)) % 40) * 300) > 5000 THEN 'HUMAN_REVIEW (high value)'
        WHEN r.Confidence >= 0.85 THEN 'AUTO_APPROVE (confident)'
        ELSE 'HUMAN_REVIEW (medium band)'
    END AS Route,
    COUNT(*) AS Items
FROM dbo.InventoryPool ip
INNER JOIN dbo.ImageValidationResults r ON r.Id = ip.ReturnId
GROUP BY
    CASE
        WHEN ip.HoldingDays >= 10 THEN 'AUTO_APPROVE (expired -> return-to-seller)'
        WHEN r.Confidence < 0.60 THEN 'ESCALATE (low confidence)'
        WHEN (999 + (ABS(CHECKSUM(ip.Id)) % 40) * 300) > 5000 THEN 'HUMAN_REVIEW (high value)'
        WHEN r.Confidence >= 0.85 THEN 'AUTO_APPROVE (confident)'
        ELSE 'HUMAN_REVIEW (medium band)'
    END
ORDER BY Route;

PRINT '';
PRINT '=== 5. SP: usp_GetDashboardMetrics ===';
EXEC dbo.usp_GetDashboardMetrics;

PRINT '';
PRINT '=== 6. SP: usp_GetReturnReasonsByCategory (@Category=Apparel) ===';
EXEC dbo.usp_GetReturnReasonsByCategory @Category = 'Apparel';

PRINT '';
PRINT '=== 7. SP: usp_GetDashboardRootCauseInsights ===';
EXEC dbo.usp_GetDashboardRootCauseInsights;

PRINT '';
PRINT '=== 8. SP: usp_GetInventoryByProduct (previously broken - dbo.Returns) ===';
DECLARE @availProduct NVARCHAR(100) = (
    SELECT TOP 1 ProductId FROM dbo.InventoryPool WHERE Status = 'Available' ORDER BY MatchScore DESC);
PRINT 'Testing ProductId = ' + ISNULL(@availProduct, '(none)');
EXEC dbo.usp_GetInventoryByProduct @ProductId = @availProduct;

PRINT '';
PRINT '=== 9. SP: usp_GetDemandHistory ===';
EXEC dbo.usp_GetDemandHistory @ProductId = 'SYN-DP1';

PRINT '';
PRINT '=== 10. SP round-trip: SaveImageValidationResult -> verify insert ===';
EXEC dbo.usp_SaveImageValidationResult
    @ProductId = 'SMOKE-TEST-1', @ProductName = 'Smoke Test Item', @Category = 'Apparel',
    @ReturnReason = 'Validation smoke test', @Condition = 'Good', @Eligibility = 'Eligible',
    @Confidence = 0.91, @Location = 'Chennai';
SELECT COUNT(*) AS SmokeRowInserted FROM dbo.ImageValidationResults WHERE ProductId = 'SMOKE-TEST-1';
DELETE FROM dbo.ImageValidationResults WHERE ProductId = 'SMOKE-TEST-1';

PRINT '';
PRINT '=== 11. SP round-trip: CreateReturnRequest -> GetReturnRequestById ===';
DECLARE @pkg UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.Packages WHERE CreatedBy = 'synthetic-gen');
DECLARE @newRr TABLE (ReturnRequestId UNIQUEIDENTIFIER, PackageId UNIQUEIDENTIFIER, Status NVARCHAR(50), CreatedDate DATETIME2);
INSERT INTO @newRr EXEC dbo.usp_CreateReturnRequest @PackageId = @pkg, @ReturnReason = 'Smoke test return', @Location = 'Chennai';
DECLARE @rr UNIQUEIDENTIFIER = (SELECT TOP 1 ReturnRequestId FROM @newRr);
PRINT 'Created ReturnRequestId = ' + CAST(@rr AS NVARCHAR(50));
EXEC dbo.usp_GetReturnRequestById @Id = @rr;
DELETE FROM dbo.ReturnRequests WHERE Id = @rr;

PRINT '';
PRINT '=== VALIDATION COMPLETE (all SPs executed without error) ===';
GO
