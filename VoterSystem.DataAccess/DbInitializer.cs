using Microsoft.EntityFrameworkCore;
using VoterSystem.DataAccess.Services;

namespace VoterSystem.DataAccess;

public static class DbInitializer
{
    public static async Task InitialiseAsync(VoterSystemDbContext context, IUserService userService)
    {
        await context.Database.MigrateAsync();
        
        if (context.Users.Any())
            return;

        /*var users = new List<CreateUserDto>
        {
            new() { Name = "Lakatos Rikardo", Email = "example@gmail.com", Password = "test_Str0ng_password", UserRole = new UserRole("User") },
            new() { Name = "Rezmuves Rikardo", Email = "example2@gmail.com", Password = "test_Str0ng_password", UserRole = new UserRole("User") },
            new() { Name = "Lakatos Ronaldo", Email = "example3@gmail.com", Password = "test_Str0ng_password", UserRole = new UserRole("User") },
            new() { Name = "Kis Peter", Email = "example4@gmail.com", Password = "test_Str0ng_password", UserRole = new UserRole("User") },
            new() { Name = "Nagy Janos Zsolt", Email = "example5@gmail.com", Password = "test_Str0ng_password", UserRole = new UserRole("User") },
            new() { Name = "Kerekes Dzsezonsztetem", Email = "example6@gmail.com", Password = "test_Str0ng_password", UserRole = new UserRole("Admin") },
            new() { Name = "Tom Cruise", Email = "example7@gmail.com", Password = "test_Str0ng_password", UserRole = new UserRole("Admin") }
        };

        foreach (var user in users)
        {
            var result = await userService.CreateUserAsync(user);
            if (result.IsError)
            {
                throw new Exception(result.GetErrorOrThrow().Message);
            }
        }*/
    }
}