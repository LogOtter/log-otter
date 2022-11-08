using System.Text;
using System.Text.RegularExpressions;

namespace CustomerApi;

public static class GuidExtensions
{
    private const string Alphabet = "BCDFGHJKLMNPQRSTVWXYZbcdfhjklmnpqrstvwxyz0123456789";

    private static readonly Regex ValidShortGuid = new($"^[{Alphabet}]{{1,24}}$");

    public static string ToShortString(this Guid inputGuid)
    {
        var guidBytes = inputGuid.ToByteArray();
        
        var stringBuilder = new StringBuilder();
        
        for (var guidSection = 0; guidSection < 2; guidSection++)
        {
            var section = new StringBuilder();
            
            var startIndex = guidSection * 8;
            var numericRepresentation = BitConverter.ToUInt64(guidBytes, startIndex);
            
            while (numericRepresentation > 0)
            {
                var mod = (int)(numericRepresentation % (ulong)Alphabet.Length);
                section.Append(Alphabet[mod]);
                numericRepresentation /= (ulong)Alphabet.Length;
            }

            stringBuilder.Append(section.ToString().PadRight(12, 'B'));
        }

        return stringBuilder.ToString();
    }

    public static bool IsValidShortGuid(this string guidString) => ValidShortGuid.IsMatch(guidString);
}