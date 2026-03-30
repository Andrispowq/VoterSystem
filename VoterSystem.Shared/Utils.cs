using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace VoterSystem.Shared;

public static partial class Utils
{
    public static string ReplaceFromEnv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return EnvRegex().Replace(value, match =>
        {
            var envKey = match.Groups[1].Value;
            var envValue = Environment.GetEnvironmentVariable(envKey);
            return envValue ?? match.Value; // keep original if not found
        });
    }

    public static string GenerateEncryptionKey(int bytes = 128)
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes));
    }

    [GeneratedRegex(@"\$\{([^}]+)\}")]
    private static partial Regex EnvRegex();
}