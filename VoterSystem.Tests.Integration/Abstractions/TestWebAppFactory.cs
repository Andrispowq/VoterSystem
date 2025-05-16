using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using VoterSystem.DataAccess;
using VoterSystem.WebAPI;

namespace VoterSystem.Tests.Integration.Abstractions;

public class TestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithUsername("root")
        .WithImage("postgres:latest")
        .WithCleanUp(true)
        .Build();

    private string ConnectionString => _container.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "IntegrationTests");

        builder.ConfigureTestServices(services =>
        {
            // Add an in-memory database
            var dataSourceProvider =
                new NpgsqlDataSourceBuilder(ConnectionString.Replace("Database=postgres",
                    "Database=VoterSystemDbContext"));
            dataSourceProvider.EnableDynamicJson();
            var dataSource = dataSourceProvider.Build();
            
            services.AddDbContext<VoterSystemDbContext>(options =>
            {
                options.UseNpgsql(dataSource);
                options.EnableSensitiveDataLogging();
                options.UseLazyLoadingProxies();
            });

            //Seed the database with initial data
            using var scope = services.BuildServiceProvider().CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<VoterSystemDbContext>();
            db.Database.Migrate();
        });
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }
    
    public new async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}