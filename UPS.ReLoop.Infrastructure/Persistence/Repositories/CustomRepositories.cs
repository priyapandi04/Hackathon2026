namespace UPS.ReLoop.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using UPS.ReLoop.Domain.Entities;
using UPS.ReLoop.Domain.Interfaces;

public class ReturnRepository : Repository<Return>, IReturnRepository
{
    public ReturnRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Return>> GetByProductIdAsync(string productId, CancellationToken cancellationToken = default)
        => await _dbSet.Where(r => r.ProductId == productId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Return>> GetByLocationAsync(string location, CancellationToken cancellationToken = default)
        => await _dbSet.Where(r => r.Location == location).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Return>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => await _dbSet.Where(r => r.ReturnDate >= from && r.ReturnDate <= to).ToListAsync(cancellationToken);
}

public class InventoryPoolRepository : Repository<InventoryPool>, IInventoryPoolRepository
{
    public InventoryPoolRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<InventoryPool>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
        => await _dbSet.Where(i => i.Status == status).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InventoryPool>> GetByReturnIdAsync(Guid returnId, CancellationToken cancellationToken = default)
        => await _dbSet.Where(i => i.ReturnId == returnId).ToListAsync(cancellationToken);
}

public class DemandHistoryRepository : Repository<DemandHistory>, IDemandHistoryRepository
{
    public DemandHistoryRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<DemandHistory>> GetByProductIdAsync(string productId, CancellationToken cancellationToken = default)
        => await _dbSet.Where(d => d.ProductId == productId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DemandHistory>> GetByRegionAsync(string region, CancellationToken cancellationToken = default)
        => await _dbSet.Where(d => d.Region == region).ToListAsync(cancellationToken);

    public async Task<DemandHistory?> GetByProductAndRegionAsync(string productId, string region, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(d => d.ProductId == productId && d.Region == region, cancellationToken);
}

public class AgentRecommendationRepository : Repository<AgentRecommendation>, IAgentRecommendationRepository
{
    public AgentRecommendationRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<AgentRecommendation>> GetByAgentNameAsync(string agentName, CancellationToken cancellationToken = default)
        => await _dbSet.Where(a => a.AgentName == agentName).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AgentRecommendation>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
        => await _dbSet.OrderByDescending(a => a.CreatedDate).Take(count).ToListAsync(cancellationToken);
}
