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
    INNER JOIN [dbo].[Returns] r ON r.[Id] = ip.[ReturnId] AND r.[IsDeleted] = 0
    WHERE ip.[ProductId] = @ProductId
      AND ip.[IsDeleted] = 0
      AND (@Location IS NULL OR ip.[Location] = @Location)
    ORDER BY ip.[MatchScore] DESC;
END
GO