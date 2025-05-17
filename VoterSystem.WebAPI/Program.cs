using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VoterSystem.DataAccess;
using VoterSystem.DataAccess.Config;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.DataAccess.Token;
using VoterSystem.Shared;
using VoterSystem.SignalR;
using VoterSystem.SignalR.Hubs;
using VoterSystem.WebAPI.Config;
using VoterSystem.WebAPI.Controllers;

namespace VoterSystem.WebAPI;

#pragma warning disable S1118, RCS1102
public class Program
#pragma warning restore RCS1102, S1118
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

        builder.Services.BindWithEnvSubstitution<BlazorSettings>(builder.Configuration, "BlazorSettings");
        var jwtSettings = builder.Services.BindWithEnvSubstitution<JwtSettings>(builder.Configuration, "JwtSettings");

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidAudience = jwtSettings.Audience,
                ValidIssuer = jwtSettings.Issuer,
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
            };
            
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Cookies.ContainsKey(TokenIssuer.AuthTokenKey))
                    {
                        context.Token = context.Request.Cookies[TokenIssuer.AuthTokenKey];
                    }
                    else
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/Hubs"))
                        {
                            context.Token = accessToken;
                        }
                    }

                    return Task.CompletedTask;
                }
            };
        });

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

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => { policy.RequireClaim(ClaimTypes.Role, "Admin"); });

            options.AddPolicy("UserOnly", policy => { policy.RequireClaim(ClaimTypes.Role, "User"); });
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("BlazorPolicy",
                policy =>
                {
                    var urls = builder.Configuration
                        .GetSection("BlazorUrls")
                        .Get<List<string>>()?.Select(Utils.ReplaceFromEnv).ToList();

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
            var database = services.GetService<VoterSystemDbContext>()!;

            var userService = services.GetService<IUserService>()!;
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<UserRole>>();

            await DbInitializer.InitialiseAsync(database, userService, roleManager);
        }

        await app.RunAsync();
    }
}