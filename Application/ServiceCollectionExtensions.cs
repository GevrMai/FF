using Application.Services;
using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this ServiceCollection services)
    {
        return services
            .AddSingleton<IPicking, DefaultPicking>()
            .AddSingleton<IPicking, OptimizedPicking>()
            .AddSingleton<IMetricService, MetricService>()
            .AddSingleton<ITaskService, TaskService>()
            .AddSingleton<WarehouseTopology>();
    }
}