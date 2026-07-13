namespace UPS.ReLoop.Application;

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using UPS.ReLoop.Application.Interfaces;
using UPS.ReLoop.Application.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<IPackageService, PackageService>();
        services.AddScoped<IReturnRequestService, ReturnRequestService>();
        services.AddScoped<IImageValidationService, ImageValidationService>();
        services.AddScoped<IMatchAgentService, MatchAgentService>();
        services.AddScoped<IBusinessExplanationService, BusinessExplanationService>();
        services.AddScoped<IRootCauseAgentService, RootCauseAgentService>();
        services.AddSingleton<ISavingsCalculatorService, SavingsCalculatorService>();
        services.AddScoped<IDashboardService, DashboardService>();

        // ReLoop Decision Engine — differentiators (deterministic, auditable).
        services.AddSingleton<IHoldingClockService, HoldingClockService>();
        services.AddSingleton<IPolicyRetriever, PolicyRetriever>();
        services.AddSingleton<IRetailerPolicyService, RetailerPolicyService>();
        services.AddSingleton<IDiversionAgentService, DiversionAgentService>();
        services.AddSingleton<AutoApprovalMetrics>();

        // Human-in-the-loop feedback ("learns daily"). In-memory store for the MVP.
        services.AddSingleton<ConcurrentBag<FeedbackService.StoredFeedback>>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<IBuyerService, BuyerService>();

        services.AddScoped<IReturnProcessingOrchestrator, ReturnProcessingOrchestrator>();
        return services;
    }
}
