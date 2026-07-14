-- ============================================================
-- UPS ReLoop Nexus - Bulk Synthetic Data Generator
-- Generates thousands of realistic, referentially-consistent rows
-- across Packages -> ReturnRequests / ImageValidationResults ->
-- InventoryPool / MatchAgentResults / DemandHistory / AgentRecommendations.
--
-- Deterministic + idempotent: all rows are tagged CreatedBy='synthetic-gen'
-- and purged at the top, so you can re-run this script safely.
--
-- Holding days are spread 0..12 on purpose so the 10-day clock, the
-- diversion agent, and the auto-approval router all have live edge cases.
--
-- Run:  sqlcmd -S "SERVER\SQLEXPRESS" -E -C -I -d ReloopTestDB -i Reloop_SyntheticBulkData.sql
-- ============================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

DECLARE @N INT = 2000;   -- synthetic packages / returns / validations to generate

-- ---------- Purge previous synthetic run (children first) ----------
DELETE FROM dbo.MatchAgentResults      WHERE CreatedBy = 'synthetic-gen';
DELETE FROM dbo.InventoryPool          WHERE CreatedBy = 'synthetic-gen';
DELETE FROM dbo.ImageValidationResults WHERE CreatedBy = 'synthetic-gen';
DELETE FROM dbo.DemandHistory          WHERE CreatedBy = 'synthetic-gen';
DELETE FROM dbo.MatchAgentResults      WHERE ReturnRequestId IN (SELECT Id FROM dbo.ReturnRequests WHERE CreatedBy = 'synthetic-gen');
DELETE FROM dbo.ReturnRequests         WHERE CreatedBy = 'synthetic-gen';
DELETE FROM dbo.AgentRecommendations   WHERE CreatedBy = 'synthetic-gen';
DELETE FROM dbo.Packages               WHERE CreatedBy = 'synthetic-gen';

-- ---------- Seed table with pre-computed attributes ----------
IF OBJECT_ID('tempdb..#Seed') IS NOT NULL DROP TABLE #Seed;
CREATE TABLE #Seed (
    n           INT PRIMARY KEY,
    pid         UNIQUEIDENTIFIER NOT NULL,   -- Package Id
    rrid        UNIQUEIDENTIFIER NOT NULL,   -- ReturnRequest Id
    ivrid       UNIQUEIDENTIFIER NOT NULL,   -- ImageValidationResult Id
    city        NVARCHAR(50)  NOT NULL,
    category    NVARCHAR(50)  NOT NULL,
    reason      NVARCHAR(200) NOT NULL,
    cond        NVARCHAR(50)  NOT NULL,
    eligible    BIT           NOT NULL,
    conf        FLOAT         NOT NULL,
    holdingDays INT           NOT NULL,
    matchScore  INT           NOT NULL,
    basePrice   DECIMAL(10,2) NOT NULL
);

;WITH Numbers AS (
    SELECT TOP (@N) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT INTO #Seed (n, pid, rrid, ivrid, city, category, reason, cond, eligible, conf, holdingDays, matchScore, basePrice)
SELECT
    n, NEWID(), NEWID(), NEWID(),
    CHOOSE(n % 6 + 1, 'Chennai','Bangalore','Hyderabad','Mumbai','Delhi','Pune'),
    CHOOSE(n % 8 + 1, 'Apparel','Footwear','Electronics','Accessories','Home','Beauty','Toys','Sports'),
    CHOOSE(n % 6 + 1, 'Size too small','Changed mind','Defective item','Wrong item shipped','Not as described','Damaged in transit'),
    CHOOSE(n % 5 + 1, 'New','LikeNew','Good','Fair','Damaged'),
    CASE WHEN n % 5 = 0 THEN 0 ELSE 1 END,          -- ~80% eligible
    0.50 + (n % 50) / 100.0,                          -- confidence 0.50..0.99
    n % 13,                                           -- holding days 0..12 (exercises 10-day clock)
    n % 101,                                          -- match score 0..100
    CAST(999 + (n % 40) * 300 AS DECIMAL(10,2))       -- base price Rs.999..Rs.12,699 (exercises Rs.5000 guardrail)
FROM Numbers;

-- ---------- Packages ----------
INSERT INTO dbo.Packages
    (Id, TrackingNumber, SenderName, SenderAddress, RecipientName, RecipientAddress, Weight, Status, IsReturnable, ReturnInitiatedAt, CreatedAt, CreatedBy, IsDeleted)
SELECT
    pid,
    'SYN' + RIGHT('0000000' + CAST(n AS VARCHAR(7)), 7),
    category + ' Store ' + city,
    CAST(n AS VARCHAR(6)) + ' Market Rd, ' + city,
    'Customer ' + CAST(n AS VARCHAR(6)),
    CAST((n % 90) + 1 AS VARCHAR(6)) + ' Cross St, ' + city,
    CAST((ABS(CHECKSUM(NEWID())) % 300) / 100.0 + 0.10 AS DECIMAL(10,2)),
    'Delivered', 1,
    DATEADD(DAY, -(n % 90),        SYSUTCDATETIME()),
    DATEADD(DAY, -((n % 90) + 2),  SYSUTCDATETIME()),
    'synthetic-gen', 0
FROM #Seed;

-- ---------- ReturnRequests ----------
INSERT INTO dbo.ReturnRequests
    (Id, PackageId, Reason, Status, Location, CreatedAt, CreatedBy, IsDeleted)
SELECT
    rrid, pid, reason,
    CHOOSE(n % 4 + 1, 'Pending','Approved','Matched','ReturnToSeller'),
    city,
    DATEADD(DAY, -(n % 60), SYSUTCDATETIME()),
    'synthetic-gen', 0
FROM #Seed;

-- ---------- ImageValidationResults ----------
INSERT INTO dbo.ImageValidationResults
    (Id, ProductId, ProductName, Category, ReturnReason, Condition, Eligibility, Confidence, Location, ReturnDate, CreatedBy, IsDeleted)
SELECT
    ivrid,
    'SYN-P' + CAST(n AS VARCHAR(7)),
    category + ' Item ' + CAST(n AS VARCHAR(7)),
    category, reason, cond,
    CASE WHEN eligible = 1 THEN 'Eligible' ELSE 'NotEligible' END,
    conf, city,
    DATEADD(DAY, -(n % 60), SYSUTCDATETIME()),
    'synthetic-gen', 0
FROM #Seed;

-- ---------- InventoryPool (eligible items only) ----------
INSERT INTO dbo.InventoryPool
    (Id, ReturnId, ProductId, Location, HoldingDays, MatchScore, Status, CreatedBy, IsDeleted)
SELECT
    NEWID(), ivrid, 'SYN-P' + CAST(n AS VARCHAR(7)), city, holdingDays, matchScore,
    CASE
        WHEN holdingDays >= 10 THEN 'ReturnToSeller'
        WHEN matchScore  >= 70 THEN 'Matched'
        ELSE 'Available'
    END,
    'synthetic-gen', 0
FROM #Seed
WHERE eligible = 1;

-- ---------- MatchAgentResults (matched, eligible subset) ----------
INSERT INTO dbo.MatchAgentResults
    (Id, ReturnRequestId, ProductId, ProductName, Category, Location, Condition, MatchScore, Recommendation, Confidence, DistanceSavedKm, CostSaved, Co2Saved, Explanation, CreatedBy, IsDeleted)
SELECT
    NEWID(), rrid, 'SYN-P' + CAST(n AS VARCHAR(7)),
    category + ' Item ' + CAST(n AS VARCHAR(7)),
    category, city, cond, matchScore,
    -- Bands mirror MatchCalculator.DetermineRecommendation (80/60/40/20) exactly.
    CASE
        WHEN matchScore >= 80 THEN 'SELL_LOCAL'
        WHEN matchScore >= 60 THEN 'REDISTRIBUTE'
        WHEN matchScore >= 40 THEN 'DISCOUNT_SELL'
        WHEN matchScore >= 20 THEN 'WAREHOUSE_HOLD'
        ELSE 'LIQUIDATE'
    END,
    conf,
    matchScore * 5.5,
    matchScore * 5.5 * 1.1,
    matchScore * 5.5 * 0.0037,
    'Synthetic local-demand match for load testing.',
    'synthetic-gen', 0
FROM #Seed
WHERE eligible = 1 AND matchScore >= 40;

-- ---------- DemandHistory (unique ProductId+Region, ~300 rows) ----------
;WITH DemandNums AS (
    SELECT TOP (300) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS k
    FROM sys.all_objects
)
INSERT INTO dbo.DemandHistory
    (Id, ProductId, Region, OrderCount, DemandScore, CreatedBy, IsDeleted)
SELECT
    NEWID(),
    'SYN-DP' + CAST(k AS VARCHAR(7)),
    CHOOSE(k % 6 + 1, 'Chennai','Bangalore','Hyderabad','Mumbai','Delhi','Pune'),
    (k % 200) + 1,
    CAST((k % 100) + 1 AS DECIMAL(5,2)),              -- demand score 1..100 (0-100 scale, matches curated data)
    'synthetic-gen', 0
FROM DemandNums;

-- ---------- AgentRecommendations (audit trail, ~600 rows) ----------
INSERT INTO dbo.AgentRecommendations
    (Id, AgentName, Recommendation, Confidence, CreatedDate, CreatedAt, CreatedBy, IsDeleted)
SELECT
    NEWID(),
    CHOOSE(n % 3 + 1, 'MatchAgent','DiversionAgent','RootCauseAgent'),
    'Synthetic recommendation #' + CAST(n AS VARCHAR(7)),
    conf, SYSUTCDATETIME(), SYSUTCDATETIME(),
    'synthetic-gen', 0
FROM #Seed
WHERE n % 3 = 0;

DROP TABLE #Seed;

PRINT '============================================';
PRINT 'Synthetic bulk data generation complete.';
PRINT '============================================';
SELECT 'Packages'               AS TableName, COUNT(*) AS SyntheticRows FROM dbo.Packages               WHERE CreatedBy = 'synthetic-gen'
UNION ALL SELECT 'ReturnRequests',         COUNT(*) FROM dbo.ReturnRequests         WHERE CreatedBy = 'synthetic-gen'
UNION ALL SELECT 'ImageValidationResults', COUNT(*) FROM dbo.ImageValidationResults WHERE CreatedBy = 'synthetic-gen'
UNION ALL SELECT 'InventoryPool',          COUNT(*) FROM dbo.InventoryPool          WHERE CreatedBy = 'synthetic-gen'
UNION ALL SELECT 'MatchAgentResults',      COUNT(*) FROM dbo.MatchAgentResults      WHERE CreatedBy = 'synthetic-gen'
UNION ALL SELECT 'DemandHistory',          COUNT(*) FROM dbo.DemandHistory          WHERE CreatedBy = 'synthetic-gen'
UNION ALL SELECT 'AgentRecommendations',   COUNT(*) FROM dbo.AgentRecommendations   WHERE CreatedBy = 'synthetic-gen';
GO
