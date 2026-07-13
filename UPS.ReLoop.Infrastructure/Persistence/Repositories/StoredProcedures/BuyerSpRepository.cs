namespace UPS.ReLoop.Infrastructure.Persistence.Repositories.StoredProcedures;

using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.DTOs.Buyers;
using UPS.ReLoop.Application.Interfaces.Repositories;

public class BuyerSpRepository : IBuyerSpRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BuyerSpRepository> _logger;

    public BuyerSpRepository(ApplicationDbContext context, ILogger<BuyerSpRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BuyerDto>> GetByHubAsync(string hub, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing usp_GetBuyersByHub for Hub: {Hub}", hub);

        var parameters = new[]
        {
            new SqlParameter("@Hub", SqlDbType.NVarChar, 10) { Value = hub }
        };

        var results = await _context.Database
            .SqlQueryRaw<BuyerSpResult>(
                "EXEC [dbo].[usp_GetBuyersByHub] @Hub",
                parameters)
            .ToListAsync(cancellationToken);

        return results.Select(r => new BuyerDto(
            r.BuyerId,
            r.Name,
            hub,
            r.Zone,
            r.DistanceKm,
            r.EstimatedDeliveryHours,
            r.DemandScore,
            r.PreferredCategory,
            r.Recommendation)).ToList().AsReadOnly();
    }
}

internal class BuyerSpResult
{
    public string BuyerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public double EstimatedDeliveryHours { get; set; }
    public int DemandScore { get; set; }
    public string PreferredCategory { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}
