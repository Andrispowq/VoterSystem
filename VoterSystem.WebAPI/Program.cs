using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using VoterSystem.DataAccess;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.DataAccess.Token;
using VoterSystem.Shared;
using VoterSystem.SignalR;
using VoterSystem.SignalR.Hubs;
using VoterSystem.WebAPI.Config;
using VoterSystem.WebAPI.Controllers;

namespace VoterSystem.WebAPI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        //load from user secrets in dev
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
            || Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "IntegrationTests")
        {
            DependencyInjection.LoadDotEnv(builder.Configuration);
        }

        builder.Services.AddDataAccess(builder.Configuration);

        builder.Services.AddControllers();

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
#pragma warning disable S125
        //builder.Services.MapOpenApi();
#pragma warning restore S125

        builder.Services.AddAuth(builder.Configuration);

        builder.Services.BindWithEnvSubstitution<BlazorSettings>(builder.Configuration, "BlazorSettings");
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "VoterSystem.WebAPI",
                Version = "v1",
                Description = "Voter System API"
            });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                {
                    new OpenApiSecurityScheme {
                        Reference = new OpenApiReference {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => { policy.RequireClaim(ClaimTypes.Role, "Admin"); })
            .AddPolicy("UserOnly", policy => { policy.RequireClaim(ClaimTypes.Role, "User"); });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("BlazorPolicy",
                policy =>
                {
                    var urls = builder.Configuration
                        .GetSection("BlazorUrls")
                        .Get<List<string>>()?.Select(x => Utils.ReplaceFromEnv(x, x)).ToList();

                    if (urls == null || !urls.Any())
                        throw new MissingFieldException(
                            nameof(urls),
                            "Must set BlazorUrls (as a JSON array of strings) in appsettings!"
                        );

                    // Enable Blazor ports
                    policy.WithOrigins(urls.ToArray())
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
        });

        builder.Services.AddHealthChecks()
            .AddCheck<HealthController>("health");

        builder.Services.AddSignalR();
        builder.Services.AddSignalRServices();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseExceptionHandler("/Home/Error");

        if ( /*app.Environment.IsDevelopment()*/true)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            
#pragma warning disable S125
            //app.MapScalarApiReference(); 
#pragma warning restore S125
            app.UseCors("BlazorPolicy");
        }

        app.UseHsts();

#pragma warning disable S125
        //app.UseHttpsRedirection();
#pragma warning restore S125
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.UseHealthChecks("/api/v1/health");
        app.MapHub<VotesHub>($"/Hubs/{nameof(VotesHub)}", options =>
        {
            options.CloseOnAuthenticationExpiration = true;
        });

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var database = services.GetRequiredService<VoterSystemDbContext>();

            var userService = services.GetRequiredService<IUserService>();
            var voteService = services.GetRequiredService<IVoteService>();
            var logger = services.GetRequiredService<ILogger<DbInitializer>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<UserRole>>();

            await DbInitializer.InitialiseAsync(database, userService, voteService, roleManager, logger);
        }

        await app.RunAsync();
    }
}
