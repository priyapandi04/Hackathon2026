namespace UPS.ReLoop.Infrastructure.Persistence.Repositories.StoredProcedures;

using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Interfaces.Repositories;

public class ImageValidationSpRepository : IImageValidationSpRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ImageValidationSpRepository> _logger;

    public ImageValidationSpRepository(ApplicationDbContext context, ILogger<ImageValidationSpRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> SaveResultAsync(ImageValidationResultParams parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing usp_SaveImageValidationResult for ProductId: {ProductId}", parameters.ProductId);

        try
        {
            var sqlParams = new[]
            {
                new SqlParameter("@ProductId", SqlDbType.NVarChar, 100) { Value = parameters.ProductId },
                new SqlParameter("@ProductName", SqlDbType.NVarChar, 300) { Value = parameters.ProductName },
                new SqlParameter("@Category", SqlDbType.NVarChar, 100) { Value = parameters.Category },
                new SqlParameter("@ReturnReason", SqlDbType.NVarChar, 1000) { Value = parameters.ReturnReason },
                new SqlParameter("@Condition", SqlDbType.NVarChar, 50) { Value = parameters.Condition },
                new SqlParameter("@Eligibility", SqlDbType.NVarChar, 50) { Value = parameters.Eligibility },
                new SqlParameter("@Confidence", SqlDbType.Float) { Value = parameters.Confidence },
                new SqlParameter("@Location", SqlDbType.NVarChar, 200) { Value = parameters.Location }
            };

            var results = await _context.Database
                .SqlQueryRaw<GuidResult>(
                    "EXEC [dbo].[usp_SaveImageValidationResult] @ProductId, @ProductName, @Category, @ReturnReason, @Condition, @Eligibility, @Confidence, @Location",
                    sqlParams)
                .ToListAsync(cancellationToken);

            return results.FirstOrDefault()?.Id ?? Guid.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing usp_SaveImageValidationResult for ProductId: {ProductId}", parameters.ProductId);
            throw;
        }
    }

    private class GuidResult
    {
        public Guid Id { get; set; }
    }
}
