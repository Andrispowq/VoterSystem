namespace VoterSystem.DataAccess.Util;

public static class Crypto
{
    private static readonly int WorkFactor = 15;
    
    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor, enhancedEntropy: true);
    }

    public static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash, enhancedEntropy: true);
    }
}