using System.Globalization;
using System.Text.RegularExpressions;

namespace DriveHarbor.Core.Robocopy;

public static partial class RobocopyOutputParser
{
    public static RobocopySummary Parse(IEnumerable<string> outputLines)
    {
        foreach (var line in outputLines.Reverse())
        {
            var match = FileSummaryPattern().Match(line);
            if (match.Success
                && long.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var total)
                && TryParseCounts(match, out var counts))
            {
                return new(total, counts[0], counts[1], counts[2], counts[3], counts[4]);
            }
        }

        return new();
    }

    private static bool TryParseCounts(Match match, out long[] counts)
    {
        counts = new long[5];
        for (var index = 0; index < counts.Length; index++)
        {
            if (!long.TryParse(
                match.Groups[index + 2].Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out counts[index]))
            {
                return false;
            }
        }

        return true;
    }

    [GeneratedRegex(@"^\s*(?:Files?|File)\s*:\s*([0-9]+)\s+([0-9]+)\s+([0-9]+)\s+([0-9]+)\s+([0-9]+)\s+([0-9]+)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FileSummaryPattern();
}
