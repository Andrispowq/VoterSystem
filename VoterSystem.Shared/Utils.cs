using System.Text.RegularExpressions;

namespace VoterSystem.Shared;

public static class Utils
{
    public static string ReplaceFromEnv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return Regex.Replace(value, @"\$\{([^}]+)\}", match =>
        {
            var envKey = match.Groups[1].Value;
            var envValue = Environment.GetEnvironmentVariable(envKey);
            return envValue ?? match.Value; // keep original if not found
        });
    }
}