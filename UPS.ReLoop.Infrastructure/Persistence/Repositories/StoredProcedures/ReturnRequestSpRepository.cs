namespace UPS.ReLoop.Infrastructure.Persistence.Repositories.StoredProcedures;

using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.DTOs.ReturnRequest;
using UPS.ReLoop.Application.Interfaces.Repositories;

public class ReturnRequestSpRepository : IReturnRequestSpRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReturnRequestSpRepository> _logger;

    public ReturnRequestSpRepository(ApplicationDbContext context, ILogger<ReturnRequestSpRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CreateReturnRequestSpResponse?> CreateAsync(Guid packageId, string reason, string? location = null, string? imageUrl = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing usp_CreateReturnRequest for PackageId: {PackageId}", packageId);

        try
        {
            var parameters = new[]
            {
                new SqlParameter("@PackageId", SqlDbType.UniqueIdentifier) { Value = packageId },
                new SqlParameter("@ReturnReason", SqlDbType.NVarChar, 1000) { Value = reason },
                new SqlParameter("@Location", SqlDbType.NVarChar, 200) { Value = (object?)location ?? DBNull.Value },
                new SqlParameter("@ImageUrl", SqlDbType.NVarChar, 2000) { Value = (object?)imageUrl ?? DBNull.Value }
            };

            var results = await _context.Database
                .SqlQueryRaw<CreateReturnRequestSpResult>(
                    "EXEC [dbo].[usp_CreateReturnRequest] @PackageId, @ReturnReason, @Location, @ImageUrl",
                    parameters)
                .ToListAsync(cancellationToken);

            var row = results.FirstOrDefault();
            if (row is null) return null;

            return new CreateReturnRequestSpResponse(
                row.ReturnRequestId, row.PackageId, row.Status, row.CreatedDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing usp_CreateReturnRequest for PackageId: {PackageId}", packageId);
            throw;
        }
    }

    public async Task<ReturnRequestDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing usp_GetReturnRequestById for Id: {Id}", id);

        try
        {
            var parameter = new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id };

            var results = await _context.Database
                .SqlQueryRaw<ReturnRequestDetailSpResult>(
                    "EXEC [dbo].[usp_GetReturnRequestById] @Id",
                    parameter)
                .ToListAsync(cancellationToken);

            var row = results.FirstOrDefault();
            if (row is null) return null;

            return new ReturnRequestDetailDto(
                row.Id, row.PackageId, row.Reason, row.Status,
                row.AiAnalysis, row.ResolutionNotes, row.CreatedAt, row.ResolvedAt,
                row.TrackingNumber, row.SenderName, row.RecipientName, row.PackageStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing usp_GetReturnRequestById for Id: {Id}", id);
            throw;
        }
    }

    private class CreateReturnRequestSpResult
    {
        public Guid ReturnRequestId { get; set; }
        public Guid PackageId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    private class ReturnRequestDetailSpResult
    {
        public Guid Id { get; set; }
        public Guid PackageId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AiAnalysis { get; set; }
        public string? ResolutionNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string PackageStatus { get; set; } = string.Empty;
    }
}
