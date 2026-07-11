namespace UPS.ReLoop.Infrastructure.Persistence.Repositories.StoredProcedures;

using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Interfaces.Repositories;

public class MatchResultSpRepository : IMatchResultSpRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MatchResultSpRepository> _logger;

    public MatchResultSpRepository(ApplicationDbContext context, ILogger<MatchResultSpRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> SaveAsync(SaveMatchResultParams parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing usp_SaveMatchResult for ReturnRequestId: {ReturnRequestId}, Score: {Score}",
            parameters.ReturnRequestId, parameters.MatchScore);

        try
        {
            var sqlParams = new[]
            {
                new SqlParameter("@ReturnRequestId", SqlDbType.UniqueIdentifier) { Value = parameters.ReturnRequestId },
                new SqlParameter("@ProductId", SqlDbType.NVarChar, 100) { Value = parameters.ProductId },
                new SqlParameter("@ProductName", SqlDbType.NVarChar, 300) { Value = parameters.ProductName },
                new SqlParameter("@Category", SqlDbType.NVarChar, 100) { Value = parameters.Category },
                new SqlParameter("@Location", SqlDbType.NVarChar, 200) { Value = parameters.Location },
                new SqlParameter("@Condition", SqlDbType.NVarChar, 50) { Value = parameters.Condition },
                new SqlParameter("@MatchScore", SqlDbType.Int) { Value = parameters.MatchScore },
                new SqlParameter("@Recommendation", SqlDbType.NVarChar, 200) { Value = parameters.Recommendation },
                new SqlParameter("@Confidence", SqlDbType.Float) { Value = parameters.Confidence },
                new SqlParameter("@DistanceSavedKm", SqlDbType.Float) { Value = parameters.DistanceSavedKm },
                new SqlParameter("@CostSaved", SqlDbType.Float) { Value = parameters.CostSaved },
                new SqlParameter("@Co2Saved", SqlDbType.Float) { Value = parameters.Co2Saved },
                new SqlParameter("@Explanation", SqlDbType.NVarChar, 4000) { Value = parameters.Explanation },
                new SqlParameter("@MatchDetailsJson", SqlDbType.NVarChar, -1) { Value = parameters.MatchDetailsJson }
            };

            var results = await _context.Database
                .SqlQueryRaw<GuidResult>(
                    "EXEC [dbo].[usp_SaveMatchResult] @ReturnRequestId, @ProductId, @ProductName, @Category, @Location, @Condition, @MatchScore, @Recommendation, @Confidence, @DistanceSavedKm, @CostSaved, @Co2Saved, @Explanation, @MatchDetailsJson",
                    sqlParams)
                .ToListAsync(cancellationToken);

            return results.FirstOrDefault()?.Id ?? Guid.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing usp_SaveMatchResult for ReturnRequestId: {ReturnRequestId}", parameters.ReturnRequestId);
            throw;
        }
    }

    private class GuidResult
    {
        public Guid Id { get; set; }
    }
}
