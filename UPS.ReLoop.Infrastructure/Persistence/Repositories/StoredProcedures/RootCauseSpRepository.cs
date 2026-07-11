namespace UPS.ReLoop.Infrastructure.Persistence.Repositories.StoredProcedures;

using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Interfaces.Repositories;

public class RootCauseSpRepository : IRootCauseSpRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RootCauseSpRepository> _logger;

    public RootCauseSpRepository(ApplicationDbContext context, ILogger<RootCauseSpRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> SaveAnalysisAsync(SaveRootCauseParams parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing usp_SaveRootCauseAnalysis: {RootCause}", parameters.RootCause);

        try
        {
            var sqlParams = new[]
            {
                new SqlParameter("@AgentName", SqlDbType.NVarChar, 100) { Value = "RootCauseAgent" },
                new SqlParameter("@RootCause", SqlDbType.NVarChar, 2000) { Value = parameters.RootCause },
                new SqlParameter("@Frequency", SqlDbType.NVarChar, 200) { Value = parameters.Frequency },
                new SqlParameter("@Recommendation", SqlDbType.NVarChar, 2000) { Value = parameters.Recommendation },
                new SqlParameter("@Impact", SqlDbType.NVarChar, 2000) { Value = parameters.Impact },
                new SqlParameter("@Confidence", SqlDbType.Float) { Value = parameters.Confidence }
            };

            var results = await _context.Database
                .SqlQueryRaw<GuidResult>(
                    "EXEC [dbo].[usp_SaveRootCauseAnalysis] @AgentName, @RootCause, @Frequency, @Recommendation, @Impact, @Confidence",
                    sqlParams)
                .ToListAsync(cancellationToken);

            return results.FirstOrDefault()?.Id ?? Guid.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing usp_SaveRootCauseAnalysis");
            throw;
        }
    }

    public async Task<IReadOnlyList<ReturnReasonByCategoryDto>> GetReturnReasonsByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing usp_GetReturnReasonsByCategory for Category: {Category}", category);

        try
        {
            var parameter = new SqlParameter("@Category", SqlDbType.NVarChar, 100) { Value = category };

            var results = await _context.Database
                .SqlQueryRaw<ReturnReasonSpResult>(
                    "EXEC [dbo].[usp_GetReturnReasonsByCategory] @Category",
                    parameter)
                .ToListAsync(cancellationToken);

            return results.Select(r => new ReturnReasonByCategoryDto(
                r.ReturnReason, r.ProductName, r.Location, r.Count, r.Percentage
            )).ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing usp_GetReturnReasonsByCategory for Category: {Category}", category);
            throw;
        }
    }

    private class ReturnReasonSpResult
    {
        public string ReturnReason { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    private class GuidResult
    {
        public Guid Id { get; set; }
    }
}
