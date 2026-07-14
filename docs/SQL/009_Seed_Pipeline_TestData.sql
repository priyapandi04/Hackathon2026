-- ============================================================
-- 009_Seed_Pipeline_TestData.sql
--
-- Inserts test data across the full pipeline so all dashboard
-- endpoints return meaningful non-zero values:
--   - Packages
--   - ReturnRequests
--   - Returns (ImageValidationResults)
--   - InventoryPool
--   - MatchAgentResults
--   - AgentRecommendations
--   - DemandHistory
--
-- IDEMPOTENT: Checks before inserting; safe to re-run.
-- CORRECT INSERT ORDER per FK constraints:
--   Packages → ReturnRequests → Returns → InventoryPool → MatchAgentResults → AgentRecommendations
-- ============================================================

SET NOCOUNT ON;
PRINT '=== 009_Seed_Pipeline_TestData.sql ==='

-- ================================================================
-- 1. PACKAGES (parent of ReturnRequests)
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Packages] WHERE [TrackingNumber] LIKE 'UPS-TEST-%')
BEGIN
    INSERT INTO [dbo].[Packages]
        ([Id],[TrackingNumber],[SenderName],[SenderAddress],[RecipientName],[RecipientAddress],[Weight],[Status],[IsReturnable],[CreatedAt])
    VALUES
        ('11111111-1111-1111-1111-111111111101','UPS-TEST-001','Acme Electronics','12 MG Road, Chennai','Ravi Kumar','45 Anna Nagar, Chennai',1.5,'Delivered',1,DATEADD(DAY,-25,SYSUTCDATETIME())),
        ('11111111-1111-1111-1111-111111111102','UPS-TEST-002','StyleCorp','88 Brigade Rd, Bangalore','Priya Sharma','10 Koramangala, Bangalore',0.8,'Delivered',1,DATEADD(DAY,-22,SYSUTCDATETIME())),
        ('11111111-1111-1111-1111-111111111103','UPS-TEST-003','HomeMart India','55 FC Road, Mumbai','Amit Patel','32 Bandra West, Mumbai',4.2,'Delivered',1,DATEADD(DAY,-20,SYSUTCDATETIME())),
        ('11111111-1111-1111-1111-111111111104','UPS-TEST-004','SportZone','7 CP, Delhi','Neha Gupta','14 Lajpat Nagar, Delhi',2.1,'Delivered',1,DATEADD(DAY,-18,SYSUTCDATETIME())),
        ('11111111-1111-1111-1111-111111111105','UPS-TEST-005','TechWorld','22 Hitech City, Hyderabad','Suresh Reddy','8 Madhapur, Hyderabad',1.0,'Delivered',1,DATEADD(DAY,-15,SYSUTCDATETIME())),
        ('11111111-1111-1111-1111-111111111106','UPS-TEST-006','FashionFirst','3 MG Road, Bangalore','Divya Rao','67 HSR Layout, Bangalore',0.6,'Delivered',1,DATEADD(DAY,-12,SYSUTCDATETIME())),
        ('11111111-1111-1111-1111-111111111107','UPS-TEST-007','GadgetHub','99 T Nagar, Chennai','Karthik S','23 Velachery, Chennai',1.8,'Delivered',1,DATEADD(DAY,-10,SYSUTCDATETIME())),
        ('11111111-1111-1111-1111-111111111108','UPS-TEST-008','BookWorm','45 Andheri, Mumbai','Meera Joshi','56 Powai, Mumbai',0.5,'Delivered',1,DATEADD(DAY,-8,SYSUTCDATETIME())),
        ('11111111-1111-1111-1111-111111111109','UPS-TEST-009','ShoeBox','11 Banjara Hills, Hyderabad','Rajesh Kumar','34 Secunderabad',1.2,'Delivered',1,DATEADD(DAY,-5,SYSUTCDATETIME())),
        ('11111111-1111-1111-1111-111111111110','UPS-TEST-010','ElectroPrime','78 Dwarka, Delhi','Anita Singh','90 Noida, Delhi NCR',2.5,'Delivered',1,DATEADD(DAY,-3,SYSUTCDATETIME()));

    PRINT '  Seeded 10 Packages';
END
ELSE
    PRINT '  Packages already seeded — skipped';
GO

-- ================================================================
-- 2. RETURN REQUESTS (FK: PackageId → Packages.Id)
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[ReturnRequests] WHERE [Id] = '22222222-2222-2222-2222-222222222201')
BEGIN
    INSERT INTO [dbo].[ReturnRequests]
        ([Id],[PackageId],[Reason],[Status],[CreatedAt])
    VALUES
        ('22222222-2222-2222-2222-222222222201','11111111-1111-1111-1111-111111111101','Defective screen on arrival','Processed',DATEADD(DAY,-24,SYSUTCDATETIME())),
        ('22222222-2222-2222-2222-222222222202','11111111-1111-1111-1111-111111111102','Wrong size shipped','Processed',DATEADD(DAY,-21,SYSUTCDATETIME())),
        ('22222222-2222-2222-2222-222222222203','11111111-1111-1111-1111-111111111103','Damaged in transit','Processed',DATEADD(DAY,-19,SYSUTCDATETIME())),
        ('22222222-2222-2222-2222-222222222204','11111111-1111-1111-1111-111111111104','Changed mind','Processed',DATEADD(DAY,-17,SYSUTCDATETIME())),
        ('22222222-2222-2222-2222-222222222205','11111111-1111-1111-1111-111111111105','Not as described','Processed',DATEADD(DAY,-14,SYSUTCDATETIME())),
        ('22222222-2222-2222-2222-222222222206','11111111-1111-1111-1111-111111111106','Size too small','Processed',DATEADD(DAY,-11,SYSUTCDATETIME())),
        ('22222222-2222-2222-2222-222222222207','11111111-1111-1111-1111-111111111107','Defective battery','Processed',DATEADD(DAY,-9,SYSUTCDATETIME())),
        ('22222222-2222-2222-2222-222222222208','11111111-1111-1111-1111-111111111108','Wrong item shipped','Processed',DATEADD(DAY,-7,SYSUTCDATETIME())),
        ('22222222-2222-2222-2222-222222222209','11111111-1111-1111-1111-111111111109','Colour mismatch','Processed',DATEADD(DAY,-4,SYSUTCDATETIME())),
        ('22222222-2222-2222-2222-222222222210','11111111-1111-1111-1111-111111111110','Missing accessories','Processed',DATEADD(DAY,-2,SYSUTCDATETIME()));

    PRINT '  Seeded 10 ReturnRequests';
END
ELSE
    PRINT '  ReturnRequests already seeded — skipped';
GO

-- ================================================================
-- 3. RETURNS (FK parent for InventoryPool.ReturnId)
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Returns] WHERE [Id] = '33333333-3333-3333-3333-333333333301')
BEGIN
    INSERT INTO [dbo].[Returns]
        ([Id],[ProductId],[ProductName],[Category],[ReturnReason],[Condition],[Eligibility],[Confidence],[Location],[ReturnDate],[CreatedAt])
    VALUES
        ('33333333-3333-3333-3333-333333333301','PROD-ELEC-001','Samsung Galaxy S24','Electronics','Defective screen','Good','Eligible',0.92,'Chennai',DATEADD(DAY,-24,SYSUTCDATETIME()),DATEADD(DAY,-24,SYSUTCDATETIME())),
        ('33333333-3333-3333-3333-333333333302','PROD-APP-001','Nike Running Shoes','Apparel','Wrong size shipped','New','Eligible',0.97,'Bangalore',DATEADD(DAY,-21,SYSUTCDATETIME()),DATEADD(DAY,-21,SYSUTCDATETIME())),
        ('33333333-3333-3333-3333-333333333303','PROD-HOME-001','IKEA Standing Desk','Home','Damaged in transit','Fair','Eligible',0.78,'Mumbai',DATEADD(DAY,-19,SYSUTCDATETIME()),DATEADD(DAY,-19,SYSUTCDATETIME())),
        ('33333333-3333-3333-3333-333333333304','PROD-SPRT-001','Yoga Mat Premium','Sports','Changed mind','Excellent','Eligible',0.95,'Delhi',DATEADD(DAY,-17,SYSUTCDATETIME()),DATEADD(DAY,-17,SYSUTCDATETIME())),
        ('33333333-3333-3333-3333-333333333305','PROD-ELEC-002','Sony WH-1000XM5','Electronics','Not as described','Good','Eligible',0.88,'Hyderabad',DATEADD(DAY,-14,SYSUTCDATETIME()),DATEADD(DAY,-14,SYSUTCDATETIME())),
        ('33333333-3333-3333-3333-333333333306','PROD-APP-002','Levi Denim Jacket','Apparel','Size too small','New','Eligible',0.94,'Bangalore',DATEADD(DAY,-11,SYSUTCDATETIME()),DATEADD(DAY,-11,SYSUTCDATETIME())),
        ('33333333-3333-3333-3333-333333333307','PROD-ELEC-003','Apple AirPods Pro','Electronics','Defective battery','Good','Eligible',0.91,'Chennai',DATEADD(DAY,-9,SYSUTCDATETIME()),DATEADD(DAY,-9,SYSUTCDATETIME())),
        ('33333333-3333-3333-3333-333333333308','PROD-HOME-002','Philips Air Fryer','Home','Wrong item shipped','Excellent','Eligible',0.96,'Mumbai',DATEADD(DAY,-7,SYSUTCDATETIME()),DATEADD(DAY,-7,SYSUTCDATETIME())),
        ('33333333-3333-3333-3333-333333333309','PROD-FOOT-001','Adidas Ultraboost','Footwear','Colour mismatch','New','Eligible',0.93,'Hyderabad',DATEADD(DAY,-4,SYSUTCDATETIME()),DATEADD(DAY,-4,SYSUTCDATETIME())),
        ('33333333-3333-3333-3333-333333333310','PROD-ELEC-004','Dell XPS 15 Laptop','Electronics','Missing accessories','Good','Eligible',0.85,'Delhi',DATEADD(DAY,-2,SYSUTCDATETIME()),DATEADD(DAY,-2,SYSUTCDATETIME()));

    PRINT '  Seeded 10 Returns';
END
ELSE
    PRINT '  Returns already seeded — skipped';
GO

-- ================================================================
-- 4. INVENTORY POOL (FK: ReturnId → Returns.Id)
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryPool] WHERE [ReturnId] = '33333333-3333-3333-3333-333333333301')
BEGIN
    INSERT INTO [dbo].[InventoryPool]
        ([Id],[ReturnId],[ProductId],[Location],[HoldingDays],[MatchScore],[Status],[CreatedAt])
    VALUES
        (NEWID(),'33333333-3333-3333-3333-333333333301','PROD-ELEC-001','Chennai',   3, 92.0,'Available',DATEADD(DAY,-23,SYSUTCDATETIME())),
        (NEWID(),'33333333-3333-3333-3333-333333333302','PROD-APP-001','Bangalore',  2, 97.0,'Available',DATEADD(DAY,-20,SYSUTCDATETIME())),
        (NEWID(),'33333333-3333-3333-3333-333333333303','PROD-HOME-001','Mumbai',    5, 78.0,'Available',DATEADD(DAY,-18,SYSUTCDATETIME())),
        (NEWID(),'33333333-3333-3333-3333-333333333304','PROD-SPRT-001','Delhi',     1, 95.0,'Available',DATEADD(DAY,-16,SYSUTCDATETIME())),
        (NEWID(),'33333333-3333-3333-3333-333333333305','PROD-ELEC-002','Hyderabad', 4, 88.0,'Available',DATEADD(DAY,-13,SYSUTCDATETIME())),
        (NEWID(),'33333333-3333-3333-3333-333333333306','PROD-APP-002','Bangalore',  2, 94.0,'Available',DATEADD(DAY,-10,SYSUTCDATETIME())),
        (NEWID(),'33333333-3333-3333-3333-333333333307','PROD-ELEC-003','Chennai',   3, 91.0,'Available',DATEADD(DAY,-8,SYSUTCDATETIME())),
        (NEWID(),'33333333-3333-3333-3333-333333333308','PROD-HOME-002','Mumbai',    1, 96.0,'Sold',     DATEADD(DAY,-6,SYSUTCDATETIME())),
        (NEWID(),'33333333-3333-3333-3333-333333333309','PROD-FOOT-001','Hyderabad', 2, 93.0,'Available',DATEADD(DAY,-3,SYSUTCDATETIME())),
        (NEWID(),'33333333-3333-3333-3333-333333333310','PROD-ELEC-004','Delhi',     1, 85.0,'Available',DATEADD(DAY,-1,SYSUTCDATETIME()));

    PRINT '  Seeded 10 InventoryPool rows';
END
ELSE
    PRINT '  InventoryPool already seeded — skipped';
GO

-- ================================================================
-- 5. MATCH AGENT RESULTS (FK: ReturnRequestId → ReturnRequests.Id)
--    These drive: usp_GetDashboardMetrics, usp_GetDashboardTrend,
--                 usp_GetAgentTelemetry, /api/debug/matches
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[MatchAgentResults] WHERE [ReturnRequestId] = '22222222-2222-2222-2222-222222222201')
BEGIN
    INSERT INTO [dbo].[MatchAgentResults]
        ([Id],[ReturnRequestId],[ProductId],[ProductName],[Category],[Location],[Condition],
         [MatchScore],[Recommendation],[Confidence],[DistanceSavedKm],[CostSaved],[Co2Saved],
         [Explanation],[MatchDetailsJson],[CreatedAt])
    VALUES
        (NEWID(),'22222222-2222-2222-2222-222222222201','PROD-ELEC-001','Samsung Galaxy S24','Electronics','Chennai','Good',
         92,'Local Resale',0.92, 14.2, 4200.00, 3.8,
         'Strong local demand in Chennai for flagship phones','[]',DATEADD(DAY,-23,SYSUTCDATETIME())),

        (NEWID(),'22222222-2222-2222-2222-222222222202','PROD-APP-001','Nike Running Shoes','Apparel','Bangalore','New',
         97,'Local Resale',0.97, 18.5, 5100.00, 4.9,
         'Premium buyer matched in Koramangala within 2 km','[]',DATEADD(DAY,-20,SYSUTCDATETIME())),

        (NEWID(),'22222222-2222-2222-2222-222222222203','PROD-HOME-001','IKEA Standing Desk','Home','Mumbai','Fair',
         78,'Local Resale',0.78, 22.1, 3300.00, 6.1,
         'Bulk buyer in Bandra accepted fair-condition furniture','[]',DATEADD(DAY,-18,SYSUTCDATETIME())),

        (NEWID(),'22222222-2222-2222-2222-222222222204','PROD-SPRT-001','Yoga Mat Premium','Sports','Delhi','Excellent',
         95,'Local Resale',0.95, 11.8, 2800.00, 3.1,
         'Immediate match at UPS Access Point, Connaught Place','[]',DATEADD(DAY,-16,SYSUTCDATETIME())),

        (NEWID(),'22222222-2222-2222-2222-222222222205','PROD-ELEC-002','Sony WH-1000XM5','Electronics','Hyderabad','Good',
         88,'Local Resale',0.88, 16.4, 4800.00, 4.3,
         'Tech-savvy buyer pool in Hitech City; sold within 2 days','[]',DATEADD(DAY,-13,SYSUTCDATETIME())),

        (NEWID(),'22222222-2222-2222-2222-222222222206','PROD-APP-002','Levi Denim Jacket','Apparel','Bangalore','New',
         94,'Local Resale',0.94, 12.6, 3600.00, 3.4,
         'Fashion-forward HSR Layout buyer pool absorbs quickly','[]',DATEADD(DAY,-10,SYSUTCDATETIME())),

        (NEWID(),'22222222-2222-2222-2222-222222222207','PROD-ELEC-003','Apple AirPods Pro','Electronics','Chennai','Good',
         91,'Local Resale',0.91, 9.8, 5500.00, 2.6,
         'High-demand premium audio category in T. Nagar','[]',DATEADD(DAY,-8,SYSUTCDATETIME())),

        (NEWID(),'22222222-2222-2222-2222-222222222208','PROD-HOME-002','Philips Air Fryer','Home','Mumbai','Excellent',
         96,'Local Resale',0.96, 8.2, 2900.00, 2.2,
         'Kitchen appliance demand spiking in Powai area','[]',DATEADD(DAY,-6,SYSUTCDATETIME())),

        (NEWID(),'22222222-2222-2222-2222-222222222209','PROD-FOOT-001','Adidas Ultraboost','Footwear','Hyderabad','New',
         93,'Local Resale',0.93, 15.1, 4100.00, 4.0,
         'Sportswear buyers in Banjara Hills; instant pickup','[]',DATEADD(DAY,-3,SYSUTCDATETIME())),

        (NEWID(),'22222222-2222-2222-2222-222222222210','PROD-ELEC-004','Dell XPS 15 Laptop','Electronics','Delhi','Good',
         85,'Local Resale',0.85, 20.3, 8200.00, 5.4,
         'Corporate buyer in Noida matched for refurb channel','[]',DATEADD(DAY,-1,SYSUTCDATETIME()));

    PRINT '  Seeded 10 MatchAgentResults';
END
ELSE
    PRINT '  MatchAgentResults already seeded — skipped';
GO

-- ================================================================
-- 6. AGENT RECOMMENDATIONS (no FK — drives usp_GetAgentTelemetry)
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[AgentRecommendations] WHERE [AgentName] = 'ImageValidationAgent' AND [Recommendation] LIKE '%TEST-SEED%')
BEGIN
    INSERT INTO [dbo].[AgentRecommendations]
        ([Id],[AgentName],[Recommendation],[Confidence],[CreatedDate],[CreatedAt])
    VALUES
        (NEWID(),'ImageValidationAgent','TEST-SEED: Item in Good condition, eligible for resale',0.92,DATEADD(DAY,-23,SYSUTCDATETIME()),DATEADD(DAY,-23,SYSUTCDATETIME())),
        (NEWID(),'ImageValidationAgent','TEST-SEED: Item in Excellent condition, premium resale',0.97,DATEADD(DAY,-20,SYSUTCDATETIME()),DATEADD(DAY,-20,SYSUTCDATETIME())),
        (NEWID(),'ImageValidationAgent','TEST-SEED: Fair condition, eligible with discount',0.78,DATEADD(DAY,-18,SYSUTCDATETIME()),DATEADD(DAY,-18,SYSUTCDATETIME())),
        (NEWID(),'ImageValidationAgent','TEST-SEED: Excellent condition, full-price resale',0.95,DATEADD(DAY,-16,SYSUTCDATETIME()),DATEADD(DAY,-16,SYSUTCDATETIME())),
        (NEWID(),'ImageValidationAgent','TEST-SEED: Good condition after inspection',0.88,DATEADD(DAY,-13,SYSUTCDATETIME()),DATEADD(DAY,-13,SYSUTCDATETIME())),
        (NEWID(),'EligibilityAgent','TEST-SEED: Approved for local resale channel',0.94,DATEADD(DAY,-22,SYSUTCDATETIME()),DATEADD(DAY,-22,SYSUTCDATETIME())),
        (NEWID(),'EligibilityAgent','TEST-SEED: Approved with condition note',0.91,DATEADD(DAY,-19,SYSUTCDATETIME()),DATEADD(DAY,-19,SYSUTCDATETIME())),
        (NEWID(),'EligibilityAgent','TEST-SEED: Approved — premium category',0.96,DATEADD(DAY,-15,SYSUTCDATETIME()),DATEADD(DAY,-15,SYSUTCDATETIME())),
        (NEWID(),'RootCauseAgent','TEST-SEED: ROOT_CAUSE: Size chart error | FREQUENCY: 40% | RECOMMENDATION: Update size guide',0.85,DATEADD(DAY,-17,SYSUTCDATETIME()),DATEADD(DAY,-17,SYSUTCDATETIME())),
        (NEWID(),'RootCauseAgent','TEST-SEED: ROOT_CAUSE: Packaging damage | FREQUENCY: 27% | RECOMMENDATION: Upgrade packaging',0.82,DATEADD(DAY,-12,SYSUTCDATETIME()),DATEADD(DAY,-12,SYSUTCDATETIME()));

    PRINT '  Seeded 10 AgentRecommendations';
END
ELSE
    PRINT '  AgentRecommendations already seeded — skipped';
GO

-- ================================================================
-- 7. DEMAND HISTORY (no FK — drives usp_GetDemandHistory for MatchAgent scoring)
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[DemandHistory] WHERE [ProductId] = 'PROD-ELEC-001' AND [Region] = 'Chennai')
BEGIN
    INSERT INTO [dbo].[DemandHistory]
        ([Id],[ProductId],[Region],[OrderCount],[DemandScore],[CreatedAt])
    VALUES
        (NEWID(),'PROD-ELEC-001','Chennai',   135, 94.0, DATEADD(DAY,-30,SYSUTCDATETIME())),
        (NEWID(),'PROD-ELEC-001','Bangalore', 112, 89.0, DATEADD(DAY,-30,SYSUTCDATETIME())),
        (NEWID(),'PROD-ELEC-002','Hyderabad',  98, 86.0, DATEADD(DAY,-30,SYSUTCDATETIME())),
        (NEWID(),'PROD-ELEC-003','Chennai',   145, 96.0, DATEADD(DAY,-30,SYSUTCDATETIME())),
        (NEWID(),'PROD-ELEC-004','Delhi',      78, 79.0, DATEADD(DAY,-30,SYSUTCDATETIME())),
        (NEWID(),'PROD-APP-001','Bangalore',  105, 88.0, DATEADD(DAY,-30,SYSUTCDATETIME())),
        (NEWID(),'PROD-APP-002','Bangalore',   92, 84.0, DATEADD(DAY,-30,SYSUTCDATETIME())),
        (NEWID(),'PROD-HOME-001','Mumbai',     65, 72.0, DATEADD(DAY,-30,SYSUTCDATETIME())),
        (NEWID(),'PROD-HOME-002','Mumbai',     88, 82.0, DATEADD(DAY,-30,SYSUTCDATETIME())),
        (NEWID(),'PROD-SPRT-001','Delhi',      72, 76.0, DATEADD(DAY,-30,SYSUTCDATETIME())),
        (NEWID(),'PROD-FOOT-001','Hyderabad',  81, 80.0, DATEADD(DAY,-30,SYSUTCDATETIME()));

    PRINT '  Seeded 11 DemandHistory rows';
END
ELSE
    PRINT '  DemandHistory already seeded — skipped';
GO

-- ================================================================
-- 8. VERIFICATION — confirm data feeds all dashboard SPs
-- ================================================================
PRINT ''
PRINT '=== Verification Counts ==='
SELECT 'Packages'              AS [Table], COUNT(*) AS [Rows] FROM [dbo].[Packages]              UNION ALL
SELECT 'ReturnRequests',                   COUNT(*)           FROM [dbo].[ReturnRequests]         UNION ALL
SELECT 'Returns',                          COUNT(*)           FROM [dbo].[Returns]                UNION ALL
SELECT 'InventoryPool',                    COUNT(*)           FROM [dbo].[InventoryPool]          UNION ALL
SELECT 'MatchAgentResults',                COUNT(*)           FROM [dbo].[MatchAgentResults]      UNION ALL
SELECT 'AgentRecommendations',             COUNT(*)           FROM [dbo].[AgentRecommendations]   UNION ALL
SELECT 'DemandHistory',                    COUNT(*)           FROM [dbo].[DemandHistory]
ORDER BY [Table];

-- Quick test: run the dashboard metrics SP to confirm non-zero output
PRINT ''
PRINT '=== usp_GetDashboardMetrics output ==='
EXEC [dbo].[usp_GetDashboardMetrics];

PRINT ''
PRINT '=== usp_GetAgentTelemetry output ==='
EXEC [dbo].[usp_GetAgentTelemetry];

PRINT ''
PRINT '009_Seed_Pipeline_TestData.sql completed successfully.'
GO
