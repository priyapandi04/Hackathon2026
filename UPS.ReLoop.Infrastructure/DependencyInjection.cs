namespace UPS.ReLoop.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UPS.ReLoop.Application.Interfaces;
using UPS.ReLoop.Application.Interfaces.Repositories;
using UPS.ReLoop.Domain.Interfaces;
using UPS.ReLoop.Infrastructure.Configuration;
using UPS.ReLoop.Infrastructure.Persistence;
using UPS.ReLoop.Infrastructure.Persistence.Repositories;
using UPS.ReLoop.Infrastructure.Persistence.Repositories.StoredProcedures;
using UPS.ReLoop.Infrastructure.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ISqlStoredProcedureExecutor, SqlStoredProcedureExecutor>();

        // Custom EF repositories
        services.AddScoped<IReturnRepository, ReturnRepository>();
        services.AddScoped<IInventoryPoolRepository, InventoryPoolRepository>();
        services.AddScoped<IDemandHistoryRepository, DemandHistoryRepository>();
        services.AddScoped<IAgentRecommendationRepository, AgentRecommendationRepository>();

        // Stored Procedure repositories
        services.AddScoped<IReturnRequestSpRepository, ReturnRequestSpRepository>();
        services.AddScoped<IImageValidationSpRepository, ImageValidationSpRepository>();
        services.AddScoped<IInventoryPoolSpRepository, InventoryPoolSpRepository>();
        services.AddScoped<IDemandHistorySpRepository, DemandHistorySpRepository>();
        services.AddScoped<IMatchResultSpRepository, MatchResultSpRepository>();
        services.AddScoped<IRootCauseSpRepository, RootCauseSpRepository>();
        services.AddScoped<IDashboardSpRepository, DashboardSpRepository>();
        services.AddScoped<IBuyerSpRepository, BuyerSpRepository>();

        services.Configure<AzureOpenAiSettings>(configuration.GetSection(AzureOpenAiSettings.SectionName));
        services.AddScoped<AzureOpenAiService>();
        services.AddScoped<IAiService>(sp => sp.GetRequiredService<AzureOpenAiService>());
        services.AddScoped<IOpenAIService>(sp => sp.GetRequiredService<AzureOpenAiService>());

        return services;
    }
}
