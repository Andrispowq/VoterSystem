using System.Security.Cryptography;

namespace VoterSystem.DataAccess.Util;

internal static class Utils
{
    public const string DefaultAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVXYZ1234567890_:?!#$%-";
    public const string SpecialCharacters = "_:?!#$%-";
    public const string Alphanumeric = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVXYZ1234567890";
    public const string LowercaseAlphabet = "abcdefghijklmnopqrstuvwxyz";
    public const string UpperCaseAlphabet = "ABCDEFGHIJKLMNOPQRSTUVXYZ";
    public const string Numbers = "1234567890";
    public const int MinPasswordLength = 8;
    public const int MinUsernameLength = 3;
    public const int MaxUsernameLength = 12;

    public enum PasswordErrors
    {
        Good,
        TooShort,
        NoSpecial,
        NoLowercase,
        NoUppercase,
        NoNumber
    }

    public static PasswordErrors IsPasswordAdequate(string password)
    {
        if (password.Length < MinPasswordLength) return PasswordErrors.TooShort;
        if (!password.Any(SpecialCharacters.Contains)) return PasswordErrors.NoSpecial;
        if (!password.Any(LowercaseAlphabet.Contains)) return PasswordErrors.NoLowercase;
        if (!password.Any(UpperCaseAlphabet.Contains)) return PasswordErrors.NoUppercase;
        if (!password.Any(Numbers.Contains)) return PasswordErrors.NoNumber;

        return PasswordErrors.Good;
    }

    public static string GenerateEncryptionKey(int bytes = 128)
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes));
    }

    public static string GeneratePassword(int groups, int groupSize, string alphabet = DefaultAlphabet)
    {
        string pw = "";
        for(int i = 0; i < groups; i++)
        {
            for(int j = 0; j < groupSize; j++)
            {
                pw += alphabet[RandomNumberGenerator.GetInt32(0, alphabet.Length)];
            }

            if(i != groups - 1)
            {
                pw += ":";
            }
        }

        return pw;
    }
}