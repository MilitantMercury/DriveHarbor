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
                && long.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var copied))
            {
                return new(total, copied);
            }
        }

        return new();
    }

    [GeneratedRegex(@"^\s*(?:Files?|File)\s*:\s*([0-9]+)\s+([0-9]+)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FileSummaryPattern();
}
