using System.Security.Cryptography;

namespace Recrutement.Services;

public static class PasswordGenerator
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Special = "!@#$%";

    public static string Generate(int length = 14)
    {
        var all = Upper + Lower + Digits + Special;
        var chars = new List<char> { Pick(Upper), Pick(Lower), Pick(Digits), Pick(Special) };
        while (chars.Count < length)
            chars.Add(Pick(all));

        var arr = chars.ToArray();
        RandomNumberGenerator.Shuffle(arr.AsSpan());
        return new string(arr);
    }

    private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
}