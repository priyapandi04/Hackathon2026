namespace UPS.ReLoop.Application;

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
        services.AddScoped<IReturnProcessingOrchestrator, ReturnProcessingOrchestrator>();
        return services;
    }
}
