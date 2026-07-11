namespace UPS.ReLoop.Domain.Interfaces;

using UPS.ReLoop.Domain.Entities;

public interface IReturnRepository : IRepository<Return>
{
    Task<IReadOnlyList<Return>> GetByProductIdAsync(string productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Return>> GetByLocationAsync(string location, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Return>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

public interface IInventoryPoolRepository : IRepository<InventoryPool>
{
    Task<IReadOnlyList<InventoryPool>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryPool>> GetByReturnIdAsync(Guid returnId, CancellationToken cancellationToken = default);
}

public interface IDemandHistoryRepository : IRepository<DemandHistory>
{
    Task<IReadOnlyList<DemandHistory>> GetByProductIdAsync(string productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DemandHistory>> GetByRegionAsync(string region, CancellationToken cancellationToken = default);
    Task<DemandHistory?> GetByProductAndRegionAsync(string productId, string region, CancellationToken cancellationToken = default);
}

public interface IAgentRecommendationRepository : IRepository<AgentRecommendation>
{
    Task<IReadOnlyList<AgentRecommendation>> GetByAgentNameAsync(string agentName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgentRecommendation>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}
