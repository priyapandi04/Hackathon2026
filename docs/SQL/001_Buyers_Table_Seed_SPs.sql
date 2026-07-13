-- ============================================================
-- 001_Buyers_Table_Seed_SPs.sql
-- Creates the Buyers table, seeds synthetic data,
-- and creates the usp_GetBuyersByHub stored procedure.
-- ============================================================

-- 1. Create Buyers table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Buyers')
BEGIN
    CREATE TABLE [dbo].[Buyers] (
        [Id]                      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [BuyerId]                 NVARCHAR(20)     NOT NULL,
        [Hub]                     NVARCHAR(10)     NOT NULL,
        [Name]                    NVARCHAR(200)    NOT NULL,
        [Zone]                    NVARCHAR(300)    NOT NULL,
        [DistanceKm]              FLOAT            NOT NULL,
        [EstimatedDeliveryHours]  FLOAT            NOT NULL,
        [DemandScore]             INT              NOT NULL,
        [PreferredCategory]       NVARCHAR(100)    NOT NULL DEFAULT 'Electronics',
        [Recommendation]          NVARCHAR(200)    NOT NULL DEFAULT 'Moderate Demand',
        [IsActive]                BIT              NOT NULL DEFAULT 1,
        [CreatedAt]               DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Buyers] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_Buyers_BuyerId] UNIQUE ([BuyerId]),
        CONSTRAINT [CK_Buyers_DemandScore] CHECK ([DemandScore] BETWEEN 0 AND 100)
    );

    CREATE NONCLUSTERED INDEX [IX_Buyers_Hub] ON [dbo].[Buyers] ([Hub])
        INCLUDE ([BuyerId],[Name],[Zone],[DistanceKm],[EstimatedDeliveryHours],[DemandScore],[PreferredCategory],[Recommendation]);
    PRINT 'Created table [Buyers] with index IX_Buyers_Hub.';
END
ELSE
BEGIN
    -- Migrate existing table: add new columns if they don't exist yet.
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Buyers' AND COLUMN_NAME = 'BuyerId')
        ALTER TABLE [dbo].[Buyers] ADD [BuyerId] NVARCHAR(20) NOT NULL DEFAULT 'B000';

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Buyers' AND COLUMN_NAME = 'DistanceKm')
        ALTER TABLE [dbo].[Buyers] ADD [DistanceKm] FLOAT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Buyers' AND COLUMN_NAME = 'EstimatedDeliveryHours')
        ALTER TABLE [dbo].[Buyers] ADD [EstimatedDeliveryHours] FLOAT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Buyers' AND COLUMN_NAME = 'DemandScore')
        ALTER TABLE [dbo].[Buyers] ADD [DemandScore] INT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Buyers' AND COLUMN_NAME = 'PreferredCategory')
        ALTER TABLE [dbo].[Buyers] ADD [PreferredCategory] NVARCHAR(100) NOT NULL DEFAULT 'Electronics';

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Buyers' AND COLUMN_NAME = 'Recommendation')
        ALTER TABLE [dbo].[Buyers] ADD [Recommendation] NVARCHAR(200) NOT NULL DEFAULT 'Moderate Demand';

    PRINT 'Migrated existing [Buyers] table with new columns.';
END
GO

-- 2. Seed synthetic buyer data (idempotent — skips if data exists)
-- Drop and re-seed if the old schema data is present (no BuyerId populated).
IF EXISTS (SELECT 1 FROM [dbo].[Buyers] WHERE [BuyerId] = 'B000' OR [BuyerId] IS NULL)
    DELETE FROM [dbo].[Buyers];
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Buyers])
BEGIN
    INSERT INTO [dbo].[Buyers] ([BuyerId],[Hub],[Name],[Zone],[DistanceKm],[EstimatedDeliveryHours],[DemandScore],[PreferredCategory],[Recommendation]) VALUES
    -- Chennai (CHN)
    ('B001', 'CHN', 'Retail Outlet Chennai',     'T. Nagar, Chennai',         2.1,  2.5,  96, 'Electronics',  'High Demand'),
    ('B002', 'CHN', 'Smart Bazaar Adyar',        'Adyar, Chennai',            3.4,  3.25, 91, 'Apparel',      'High Demand'),
    ('B003', 'CHN', 'TechMart Velachery',        'Velachery, Chennai',         5.2,  4.0,  87, 'Electronics',  'High Demand'),
    ('B004', 'CHN', 'HomeStyle Porur',           'Porur, Chennai',             7.8,  5.5,  82, 'Home',         'Moderate Demand'),
    ('B005', 'CHN', 'QuickSell Tambaram',        'Tambaram, Chennai',          9.1,  6.0,  78, 'Footwear',     'Moderate Demand'),
    -- Bangalore (BLR)
    ('B006', 'BLR', 'Urban Store Indiranagar',   'Indiranagar, Bangalore',     1.8,  2.0,  94, 'Electronics',  'High Demand'),
    ('B007', 'BLR', 'StyleHub Koramangala',      'Koramangala, Bangalore',     3.1,  3.0,  88, 'Apparel',      'High Demand'),
    ('B008', 'BLR', 'GadgetZone Whitefield',     'Whitefield, Bangalore',      6.2,  4.5,  83, 'Electronics',  'High Demand'),
    ('B009', 'BLR', 'FashionFirst HSR',          'HSR Layout, Bangalore',      8.4,  5.0,  77, 'Apparel',      'Moderate Demand'),
    ('B010', 'BLR', 'ValueMart E-City',          'Electronic City, Bangalore', 11.2, 6.5,  71, 'Sports',       'Moderate Demand'),
    -- Mumbai (MUM)
    ('B011', 'MUM', 'Metro Deals Bandra',        'Bandra, Mumbai',             2.5,  2.75, 91, 'Electronics',  'High Demand'),
    ('B012', 'MUM', 'TrendSetters Andheri',      'Andheri, Mumbai',            4.0,  3.5,  85, 'Apparel',      'High Demand'),
    ('B013', 'MUM', 'DigiStore Powai',           'Powai, Mumbai',              7.1,  5.0,  79, 'Electronics',  'Moderate Demand'),
    ('B014', 'MUM', 'ClearanceHub Thane',        'Thane, Mumbai',              9.3,  5.75, 74, 'Home',         'Moderate Demand'),
    ('B015', 'MUM', 'BargainBox Navi Mumbai',    'Navi Mumbai',                12.0, 7.0,  68, 'Footwear',     'Low Demand'),
    -- Delhi (DEL)
    ('B016', 'DEL', 'PrimePick CP',              'Connaught Place, Delhi',     3.2,  3.25, 86, 'Electronics',  'High Demand'),
    ('B017', 'DEL', 'DealsDen Lajpat Nagar',     'Lajpat Nagar, Delhi',        5.8,  4.5,  79, 'Apparel',      'Moderate Demand'),
    ('B018', 'DEL', 'SmartSave Dwarka',          'Dwarka, Delhi',              9.0,  5.5,  71, 'Home',         'Moderate Demand'),
    ('B019', 'DEL', 'NCR Outlet Noida',          'Noida, Delhi NCR',           11.5, 6.25, 65, 'Electronics',  'Low Demand'),
    ('B020', 'DEL', 'MegaMart Gurgaon',          'Gurgaon, Delhi NCR',         14.2, 7.5,  59, 'Sports',       'Low Demand'),
    -- Hyderabad (HYD)
    ('B021', 'HYD', 'TechWorld Hitech City',     'Hitech City, Hyderabad',     2.2,  2.25, 93, 'Electronics',  'High Demand'),
    ('B022', 'HYD', 'LuxeMart Banjara Hills',   'Banjara Hills, Hyderabad',   4.5,  3.5,  86, 'Apparel',      'High Demand'),
    ('B023', 'HYD', 'GadgetGuru Madhapur',       'Madhapur, Hyderabad',        6.8,  4.75, 80, 'Electronics',  'Moderate Demand'),
    ('B024', 'HYD', 'HomeNeeds Secunderabad',    'Secunderabad',               9.5,  5.5,  73, 'Home',         'Moderate Demand'),
    ('B025', 'HYD', 'BudgetBuy Kukatpally',      'Kukatpally, Hyderabad',      11.8, 6.75, 67, 'Footwear',     'Low Demand');

    PRINT 'Seeded 25 synthetic buyers (5 per hub).';
END
GO

-- 3. Stored Procedure: usp_GetBuyersByHub
CREATE OR ALTER PROCEDURE [dbo].[usp_GetBuyersByHub]
    @Hub NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT [BuyerId], [Name], [Hub], [Zone], [DistanceKm],
           [EstimatedDeliveryHours], [DemandScore], [PreferredCategory], [Recommendation]
    FROM [dbo].[Buyers]
    WHERE [Hub] = @Hub AND [IsActive] = 1
    ORDER BY [DemandScore] DESC;
END
GO
