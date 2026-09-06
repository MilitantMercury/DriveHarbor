using System.Globalization;
using System.Text.RegularExpressions;

namespace DriveHarbor.Core.Robocopy;

public static partial class RobocopyProgressParser
{
    public static bool TryParse(string line, out RobocopyProgressInfo? progress)
    {
        progress = null;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var match = ProgressLineRegex().Match(line);
        if (!match.Success
            || !double.TryParse(
                match.Groups["percentage"].Value.Replace(',', '.'),
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var percentage))
        {
            return false;
        }

        TimeSpan? estimatedRemaining = null;
        var etaMatch = EtaRegex().Match(line);
        if (etaMatch.Success
            && TimeSpan.TryParseExact(
                etaMatch.Groups["eta"].Value,
                [@"h\:mm\:ss", @"hh\:mm\:ss", @"hhh\:mm\:ss"],
                CultureInfo.InvariantCulture,
                out var eta))
        {
            estimatedRemaining = eta;
        }

        progress = new(Math.Clamp(percentage, 0, 100), estimatedRemaining);
        return true;
    }

    [GeneratedRegex(
        @"(?<percentage>\d{1,3}(?:[\.,]\d+)?)\s*%",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ProgressLineRegex();

    [GeneratedRegex(
        @"\bETA\b\s*:?[ \t]*(?<eta>\d{1,3}:\d{2}:\d{2})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EtaRegex();
}
