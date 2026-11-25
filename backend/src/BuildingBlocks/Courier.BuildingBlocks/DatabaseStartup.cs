using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Courier.BuildingBlocks;

public static class DatabaseStartup
{
    /// <summary>Creates the schema, retrying while the database container is still starting.</summary>
    public static async Task EnsureCreatedWithRetryAsync<TContext>(IServiceProvider services, int attempts = 15) where TContext : DbContext
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();

        for (var i = 1; ; i++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync();
                return;
            }
            catch (Exception ex) when (i < attempts)
            {
                logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}", i, attempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }
}
