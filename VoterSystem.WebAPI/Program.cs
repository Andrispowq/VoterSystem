using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
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
using DependencyInjection = VoterSystem.WebAPI.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

//load from user secrets in dev
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
{
    DependencyInjection.LoadDotEnv(builder.Configuration);
}

builder.Services.AddDataAccess(builder.Configuration);

builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
            if (context.Request.Cookies.ContainsKey(TokenIssuer.RefreshTokenName))
            {
                context.Token = context.Request.Cookies[TokenIssuer.RefreshTokenName];
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",policy =>
    {
        policy.RequireClaim(ClaimTypes.Role, "Admin");
    });
    
    options.AddPolicy("UserOnly",policy =>
    {
        policy.RequireClaim(ClaimTypes.Role, "User");
    });
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
                throw new ArgumentNullException(
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
    .AddCheck<HealthController>("apple-maps");

builder.Services.AddSignalR();
builder.Services.AddSignalRServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler("/Home/Error");

if (/*app.Environment.IsDevelopment()*/true)
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseCors("BlazorPolicy");
}

app.UseHsts();

//app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();
app.MapControllers();

app.UseHealthChecks("/api/v1/health");
app.MapHub<VotesHub>($"/{nameof(VotesHub)}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var database = services.GetService<VoterSystemDbContext>()!;

    var userService = services.GetService<IUserService>()!;
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<UserRole>>();
    
    await DbInitializer.InitialiseAsync(database, userService, roleManager);
}

app.Run();