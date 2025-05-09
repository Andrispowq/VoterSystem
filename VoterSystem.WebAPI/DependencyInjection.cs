using VoterSystem.Shared.DotEnv;

namespace VoterSystem.WebAPI;

public static class DependencyInjection
{
    public static void LoadDotEnv(IConfiguration config)
    {
        var list = config.GetSection("DotEnv")
            .Get<List<DotEnvConfigEntry>>();

        if (list is null)
        {
            Console.WriteLine("Warning: No DotEnv configuration found.");
            return;
        }
        
        list.Load();
    }
}