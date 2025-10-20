namespace BankThreads.Util;

public static class Args // parse -key value pairs into dictionary
{
    public static Dictionary<string, string> Parse(string[] args)
        => args.Chunk(2)
            .Where(ch => ch.Length == 2 && ch[0].StartsWith('-'))
            .ToDictionary(ch => ch[0].TrimStart('-').ToLowerInvariant(), ch => ch[1]);
}