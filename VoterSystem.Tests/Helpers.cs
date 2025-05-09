namespace VoterSystem.Tests;

public static class Helpers
{
    private static long _counter = 1;
    public static string NextUniqueId => $"{_counter++}";
}