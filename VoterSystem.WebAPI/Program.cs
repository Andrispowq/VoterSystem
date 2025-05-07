using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using VoterSystem.DataAccess;
using VoterSystem.DataAccess.Config;
using VoterSystem.DataAccess.Model;
using VoterSystem.DataAccess.Services;
using VoterSystem.DataAccess.Token;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataAccess(builder.Configuration);

builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var jwtSection = builder.Configuration.GetSection("JwtSettings");
var jwtSettings = jwtSection.Get<JwtSettings>() ?? throw new ArgumentNullException(nameof(JwtSettings));
builder.Services.Configure<JwtSettings>(jwtSection);

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

builder.Services.AddAutoMapper(_ => {});

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy",
        policy =>
        {
            var urls = builder.Configuration
                .GetSection("BlazorUrls")
                .Get<List<string>>();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler("/Home/Error");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    
    app.UseCors("BlazorPolicy");
}

app.UseHsts();

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var database = services.GetService<VoterSystemDbContext>()!;
    await database.Database.MigrateAsync();

    var voteService = services.GetService<IVoteService>()!;
    var votingService = services.GetService<IVotingService>()!;
    var userService = services.GetService<IUserService>()!;
    var voteChoiceService = services.GetService<IVoteChoiceService>()!;
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<UserRole>>();
    
    await DbInitializer.InitialiseAsync(database, userService,
        votingService, voteChoiceService, voteService, roleManager);
}

app.Run();