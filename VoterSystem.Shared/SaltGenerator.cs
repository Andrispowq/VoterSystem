using System.Security.Cryptography;
using System.Text;

namespace VoterSystem.Shared;

public static class SaltGenerator
{
    public static byte[] GenerateSalt()
    {
        var buffer = new byte[32];
        RandomNumberGenerator.Fill(buffer);
        return buffer;
    }
    
    public static string GenerateSaltAsString()
    {
        return Convert.ToBase64String(GenerateSalt());
    }

    public static (string Receipt, string HashCode) CreateVoteTag(string votingSalt, string masterKey)
    {
        var receipt = GenerateSalt();
        var receiptsS = Convert.ToBase64String(receipt);
        var voteKey = $"{votingSalt}-{masterKey}";
        var voteKeyB = Encoding.ASCII.GetBytes(voteKey);
        var voteTag = HMACSHA256.HashData(voteKeyB, receipt);
        var voteTagS = Convert.ToBase64String(voteTag);
        return (receiptsS, voteTagS);
    }
}