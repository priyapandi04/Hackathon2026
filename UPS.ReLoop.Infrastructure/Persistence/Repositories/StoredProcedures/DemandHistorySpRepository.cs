namespace UPS.ReLoop.Infrastructure.Persistence.Repositories.StoredProcedures;

using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Interfaces.Repositories;

public class DemandHistorySpRepository : IDemandHistorySpRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DemandHistorySpRepository> _logger;

    public DemandHistorySpRepository(ApplicationDbContext context, ILogger<DemandHistorySpRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DemandHistoryDto>> GetAsync(string productId, string? region = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing usp_GetDemandHistory for ProductId: {ProductId}, Region: {Region}", productId, region);

        try
        {
            var parameters = new[]
            {
                new SqlParameter("@ProductId", SqlDbType.NVarChar, 100) { Value = productId },
                new SqlParameter("@Region", SqlDbType.NVarChar, 100) { Value = (object?)region ?? DBNull.Value }
            };

            var results = await _context.Database
                .SqlQueryRaw<DemandHistorySpResult>(
                    "EXEC [dbo].[usp_GetDemandHistory] @ProductId, @Region",
                    parameters)
                .ToListAsync(cancellationToken);

            return results.Select(r => new DemandHistoryDto(
                r.Id, r.ProductId, r.Region, r.OrderCount, r.DemandScore, r.CreatedAt, r.UpdatedAt
            )).ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing usp_GetDemandHistory for ProductId: {ProductId}", productId);
            throw;
        }
    }

    private class DemandHistorySpResult
    {
        public Guid Id { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public double DemandScore { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
