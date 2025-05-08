namespace VoterSystem.Shared.DotEnv;

public static class DotEnvExtensions
{
    public static void Load(this IEnumerable<DotEnvConfigEntry> configEntries)
    {
        foreach (var entry in configEntries)
        {
            Environment.SetEnvironmentVariable(entry.Name, entry.Value);
        }
    }
}