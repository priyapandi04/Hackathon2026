-- ============================================================
-- UPS ReLoop Nexus - Complete Table Schema
-- All tables inherit: Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
-- ============================================================

-- 1. Packages — Core shipment records
CREATE TABLE [dbo].[Packages] (
    [Id]                UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [TrackingNumber]    NVARCHAR(50)     NOT NULL,
    [SenderName]        NVARCHAR(200)    NOT NULL,
    [SenderAddress]     NVARCHAR(500)    NOT NULL,
    [RecipientName]     NVARCHAR(200)    NOT NULL,
    [RecipientAddress]  NVARCHAR(500)    NOT NULL,
    [Weight]            DECIMAL(10,2)    NOT NULL DEFAULT 0,
    [Status]            NVARCHAR(50)     NOT NULL,
    [AiRecommendation]  NVARCHAR(MAX)    NULL,
    [IsReturnable]      BIT              NOT NULL DEFAULT 0,
    [ReturnInitiatedAt] DATETIME2        NULL,
    [CreatedAt]         DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    [CreatedBy]         NVARCHAR(200)    NULL,
    [UpdatedAt]         DATETIME2        NULL,
    [UpdatedBy]         NVARCHAR(200)    NULL,
    [IsDeleted]         BIT              NOT NULL DEFAULT 0,
    CONSTRAINT [PK_Packages] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_Packages_TrackingNumber] ON [dbo].[Packages]([TrackingNumber]);
-- Additional indexes for dashboard & filtering
CREATE INDEX [IX_Packages_Status] ON [dbo].[Packages]([Status]) WHERE [IsDeleted] = 0;
CREATE INDEX [IX_Packages_IsReturnable] ON [dbo].[Packages]([IsReturnable]) WHERE [IsDeleted] = 0;

-- 2. ReturnRequests — Customer return initiations
CREATE TABLE [dbo].[ReturnRequests] (
    [Id]              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [PackageId]       UNIQUEIDENTIFIER NOT NULL,
    [Reason]          NVARCHAR(1000)   NOT NULL,
    [Status]          NVARCHAR(50)     NOT NULL DEFAULT 'Pending',
    [AiAnalysis]      NVARCHAR(MAX)    NULL,
    [ResolutionNotes] NVARCHAR(MAX)    NULL,
    [ResolvedAt]      DATETIME2        NULL,
    [CreatedAt]       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    [CreatedBy]       NVARCHAR(200)    NULL,
    [UpdatedAt]       DATETIME2        NULL,
    [UpdatedBy]       NVARCHAR(200)    NULL,
    [IsDeleted]       BIT              NOT NULL DEFAULT 0,
    [Location] NVARCHAR(200) NULL,
    [ImageUrl] NVARCHAR(2000) NULL,
    CONSTRAINT [PK_ReturnRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReturnRequests_Packages] FOREIGN KEY ([PackageId])
        REFERENCES [dbo].[Packages]([Id]) ON DELETE NO ACTION
);

-- Critical indexes for Dashboard queries (GROUP BY Reason, WHERE Status, date ranges)
CREATE INDEX [IX_ReturnRequests_PackageId] ON [dbo].[ReturnRequests]([PackageId]) WHERE [IsDeleted] = 0;
CREATE INDEX [IX_ReturnRequests_Status] ON [dbo].[ReturnRequests]([Status]) WHERE [IsDeleted] = 0;
CREATE INDEX [IX_ReturnRequests_CreatedAt] ON [dbo].[ReturnRequests]([CreatedAt]) WHERE [IsDeleted] = 0;
--CREATE INDEX [IX_ReturnRequests_Reason] ON [dbo].[ReturnRequests]([Reason]) WHERE [IsDeleted] = 0;



-- 3. ImageValidationResults — Validated return records (post-agent processing)
CREATE TABLE [dbo].[ImageValidationResults] (
    [Id]           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [ProductId]    NVARCHAR(100)    NOT NULL,
    [ProductName]  NVARCHAR(300)    NOT NULL,
    [Category]     NVARCHAR(100)    NOT NULL,
    [ReturnReason] NVARCHAR(1000)   NOT NULL,
    [Condition]    NVARCHAR(50)     NOT NULL,
    [Eligibility]  NVARCHAR(50)     NOT NULL,
    [Confidence]   FLOAT            NOT NULL DEFAULT 0,
    [Location]     NVARCHAR(200)    NOT NULL,
    [ReturnDate]   DATETIME2        NOT NULL,
    [CreatedAt]    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    [CreatedBy]    NVARCHAR(200)    NULL,
    [UpdatedAt]    DATETIME2        NULL,
    [UpdatedBy]    NVARCHAR(200)    NULL,
    [IsDeleted]    BIT              NOT NULL DEFAULT 0,
    CONSTRAINT [PK_ImageValidationResults] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_ImageValidationResults_ProductId] ON [dbo].[ImageValidationResults]([ProductId]) WHERE [IsDeleted] = 0;
CREATE INDEX [IX_ImageValidationResults_Category] ON [dbo].[ImageValidationResults]([Category]) WHERE [IsDeleted] = 0;
CREATE INDEX [IX_ImageValidationResults_Location] ON [dbo].[ImageValidationResults]([Location]) WHERE [IsDeleted] = 0;
CREATE INDEX [IX_ImageValidationResults_Eligibility] ON [dbo].[ImageValidationResults]([Eligibility]) WHERE [IsDeleted] = 0;

-- 4. InventoryPool — Local inventory available for hyperlocal matching
CREATE TABLE [dbo].[InventoryPool] (
    [Id]          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [ReturnId]    UNIQUEIDENTIFIER NOT NULL,
    [ProductId]   NVARCHAR(100)    NOT NULL,
    [Location]    NVARCHAR(200)    NOT NULL,
    [HoldingDays] INT              NOT NULL DEFAULT 0,
    [MatchScore]  FLOAT            NOT NULL DEFAULT 0,
    [Status]      NVARCHAR(50)     NOT NULL,
    [CreatedAt]   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    [CreatedBy]   NVARCHAR(200)    NULL,
    [UpdatedAt]   DATETIME2        NULL,
    [UpdatedBy]   NVARCHAR(200)    NULL,
    [IsDeleted]   BIT              NOT NULL DEFAULT 0,
    CONSTRAINT [PK_InventoryPool] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryPool_ImageValidationResults] FOREIGN KEY ([ReturnId])
        REFERENCES [dbo].[ImageValidationResults]([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_InventoryPool_ProductId_Location] ON [dbo].[InventoryPool]([ProductId], [Location]) WHERE [IsDeleted] = 0;
CREATE INDEX [IX_InventoryPool_Status] ON [dbo].[InventoryPool]([Status]) WHERE [IsDeleted] = 0;

-- 5. DemandHistory — Historical demand signals per product/region
CREATE TABLE [dbo].[DemandHistory] (
    [Id]          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [ProductId]   NVARCHAR(100)    NOT NULL,
    [Region]      NVARCHAR(100)    NOT NULL,
    [OrderCount]  INT              NOT NULL DEFAULT 0,
    [DemandScore] FLOAT            NOT NULL DEFAULT 0,
    [CreatedAt]   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    [CreatedBy]   NVARCHAR(200)    NULL,
    [UpdatedAt]   DATETIME2        NULL,
    [UpdatedBy]   NVARCHAR(200)    NULL,
    [IsDeleted]   BIT              NOT NULL DEFAULT 0,
    CONSTRAINT [PK_DemandHistory] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_DemandHistory_ProductId_Region] ON [dbo].[DemandHistory]([ProductId], [Region]) WHERE [IsDeleted] = 0;

-- 6. AgentRecommendations — AI agent decision audit trail
CREATE TABLE [dbo].[AgentRecommendations] (
    [Id]             UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [AgentName]      NVARCHAR(100)    NOT NULL,
    [Recommendation] NVARCHAR(2000)   NOT NULL,
    [Confidence]     FLOAT            NOT NULL DEFAULT 0,
    [CreatedDate]    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    [CreatedAt]      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    [CreatedBy]      NVARCHAR(200)    NULL,
    [UpdatedAt]      DATETIME2        NULL,
    [UpdatedBy]      NVARCHAR(200)    NULL,
    [IsDeleted]      BIT              NOT NULL DEFAULT 0,
    CONSTRAINT [PK_AgentRecommendations] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_AgentRecommendations_AgentName] ON [dbo].[AgentRecommendations]([AgentName]) WHERE [IsDeleted] = 0;

-- ============================================================
-- Table: MatchAgentResults
-- Persists every match agent decision for audit, dashboard, and ML
-- ============================================================
CREATE TABLE [dbo].[MatchAgentResults] (
    [Id]                UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [ReturnRequestId]   UNIQUEIDENTIFIER NOT NULL,
    [ProductId]         NVARCHAR(100)    NOT NULL,
    [ProductName]       NVARCHAR(300)    NOT NULL,
    [Category]          NVARCHAR(100)    NOT NULL,
    [Location]          NVARCHAR(200)    NOT NULL,
    [Condition]         NVARCHAR(50)     NOT NULL,
    [MatchScore]        INT              NOT NULL,
    [Recommendation]    NVARCHAR(200)    NOT NULL,
    [Confidence]        FLOAT            NOT NULL,
    [DistanceSavedKm]   FLOAT            NOT NULL DEFAULT 0,
    [CostSaved]         FLOAT            NOT NULL DEFAULT 0,
    [Co2Saved]          FLOAT            NOT NULL DEFAULT 0,
    [Explanation]       NVARCHAR(4000)   NULL,
    [MatchDetailsJson]  NVARCHAR(MAX)    NULL,
    [CreatedAt]         DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    [CreatedBy]         NVARCHAR(200)    NULL,
    [UpdatedAt]         DATETIME2        NULL,
    [UpdatedBy]         NVARCHAR(200)    NULL,
    [IsDeleted]         BIT              NOT NULL DEFAULT 0,
    CONSTRAINT [PK_MatchAgentResults] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MatchAgentResults_ReturnRequests] FOREIGN KEY ([ReturnRequestId])
        REFERENCES [dbo].[ReturnRequests]([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_MatchAgentResults_ReturnRequestId] ON [dbo].[MatchAgentResults]([ReturnRequestId]) WHERE [IsDeleted] = 0;
CREATE INDEX [IX_MatchAgentResults_ProductId] ON [dbo].[MatchAgentResults]([ProductId]) WHERE [IsDeleted] = 0;
CREATE INDEX [IX_MatchAgentResults_Location_Category] ON [dbo].[MatchAgentResults]([Location], [Category]) WHERE [IsDeleted] = 0;
CREATE INDEX [IX_MatchAgentResults_MatchScore] ON [dbo].[MatchAgentResults]([MatchScore]) WHERE [IsDeleted] = 0;

--====================
CREATE TABLE [dbo].[Buyers] (
    [Id]          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    [Hub]         NVARCHAR(10)     NOT NULL,
    [Name]        NVARCHAR(100)    NOT NULL,
    [Zone]        NVARCHAR(200)    NOT NULL,
    [DistanceKm]  DECIMAL(5,1)     NOT NULL,
    [Delivery]    NVARCHAR(20)     NOT NULL,
    [Score]       INT              NOT NULL,
    [CreatedAt]   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    INDEX IX_Buyers_Hub (Hub)
);