namespace UPS.ReLoop.Infrastructure.Persistence.Repositories.StoredProcedures;

using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Interfaces.Repositories;

public class InventoryPoolSpRepository : IInventoryPoolSpRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InventoryPoolSpRepository> _logger;

    public InventoryPoolSpRepository(ApplicationDbContext context, ILogger<InventoryPoolSpRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddToPoolAsync(Guid returnId, string productId, string location, double matchScore, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing usp_AddToInventoryPool for ReturnId: {ReturnId}", returnId);

        try
        {
            var parameters = new[]
            {
                new SqlParameter("@ReturnId", SqlDbType.UniqueIdentifier) { Value = returnId },
                new SqlParameter("@ProductId", SqlDbType.NVarChar, 100) { Value = productId },
                new SqlParameter("@Location", SqlDbType.NVarChar, 200) { Value = location },
                new SqlParameter("@MatchScore", SqlDbType.Float) { Value = matchScore }
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC [dbo].[usp_AddToInventoryPool] @ReturnId, @ProductId, @Location, @MatchScore",
                parameters,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing usp_AddToInventoryPool for ReturnId: {ReturnId}", returnId);
            throw;
        }
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetByProductAsync(string productId, string? location = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing usp_GetInventoryByProduct for ProductId: {ProductId}", productId);

        try
        {
            var parameters = new[]
            {
                new SqlParameter("@ProductId", SqlDbType.NVarChar, 100) { Value = productId },
                new SqlParameter("@Location", SqlDbType.NVarChar, 200) { Value = (object?)location ?? DBNull.Value }
            };

            var results = await _context.Database
                .SqlQueryRaw<InventoryItemSpResult>(
                    "EXEC [dbo].[usp_GetInventoryByProduct] @ProductId, @Location",
                    parameters)
                .ToListAsync(cancellationToken);

            return results.Select(r => new InventoryItemDto(
                r.Id, r.ReturnId, r.ProductId, r.Location, r.HoldingDays,
                r.MatchScore, r.Status, r.ProductName, r.Category, r.Condition, r.Eligibility
            )).ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing usp_GetInventoryByProduct for ProductId: {ProductId}", productId);
            throw;
        }
    }

    private class InventoryItemSpResult
    {
        public Guid Id { get; set; }
        public Guid ReturnId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int HoldingDays { get; set; }
        public double MatchScore { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string Eligibility { get; set; } = string.Empty;
    }
}
