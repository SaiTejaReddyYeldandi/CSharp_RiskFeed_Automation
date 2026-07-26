using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RiskFeed.Core.Abstractions;
using RiskFeed.Infrastructure.Persistence;

namespace RiskFeed.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRiskFeedInfrastructure(
        this IServiceCollection services)
    {
        services.AddSingleton(serviceProvider =>
        {
            IConfiguration resolvedConfiguration =
                serviceProvider.GetRequiredService<IConfiguration>();
            string connectionString =
                resolvedConfiguration.GetConnectionString("RiskFeedDb")
                ?? throw new InvalidOperationException(
                    "Connection string 'RiskFeedDb' is not configured.");
            NpgsqlDataSourceBuilder dataSourceBuilder = new(connectionString);
            dataSourceBuilder.EnableParameterLogging(false);
            return dataSourceBuilder.Build();
        });
        services.AddScoped<IRiskEventRepository, PostgresRiskEventRepository>();
        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<PostgresHealthCheck>();

        return services;
    }
}
