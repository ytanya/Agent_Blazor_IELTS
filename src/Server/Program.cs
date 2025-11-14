using System;
using System.Threading.Tasks;
using BlazorHero.CleanArchitecture.Infrastructure.Contexts;
using BlazorHero.CleanArchitecture.Server.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Core;

namespace BlazorHero.CleanArchitecture.Server
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var configuration = services.GetRequiredService<IConfiguration>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                
                try
                {
                    var context = services.GetRequiredService<BlazorHeroContext>();
                    
                    // Use MigrationConnection if available (non-interactive)
                    var migrationConnStr = configuration.GetConnectionString("MigrationConnection");
                    var useMigrationConn = !string.IsNullOrEmpty(migrationConnStr);

                    if (useMigrationConn)
                    {
                        logger.LogInformation("Using MigrationConnection for database migration (non-interactive).");
                        context.Database.SetConnectionString(migrationConnStr);
                    }
                    else
                    {
                        logger.LogInformation("Using DefaultConnection (may be interactive).");
                    }
                    if (context.Database.IsSqlServer())
                    {
                        await context.Database.MigrateAsync();
                        logger.LogInformation("Database migration completed successfully.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while migrating or seeding the database.");

                    throw;
                }
            }

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStaticWebAssets();
                    webBuilder.UseStartup<Startup>();
                });
    }
}
