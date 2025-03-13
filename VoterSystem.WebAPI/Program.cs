using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess;
using VoterSystem.DataAccess.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataAccess(builder.Configuration);

builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler("/Home/Error");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var database = services.GetService<VoterSystemDbContext>()!;
    await database.Database.MigrateAsync();

    var service = services.GetService<IUserService>()!;
    await DbInitializer.InitialiseAsync(database, service);
}

app.Run();