using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this ServiceCollection services)
    {
        return services
            .AddSingleton<ISnapshotSaver, SnapshotSaver>();
    }
}