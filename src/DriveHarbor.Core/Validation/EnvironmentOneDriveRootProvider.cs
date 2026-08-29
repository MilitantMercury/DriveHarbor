namespace DriveHarbor.Core.Validation;

public sealed class EnvironmentOneDriveRootProvider : IOneDriveRootProvider
{
    private static readonly string[] VariableNames =
    [
        "OneDrive",
        "OneDriveConsumer",
        "OneDriveCommercial",
    ];

    public IReadOnlyList<string> GetAvailableRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var variableName in VariableNames)
        {
            var path = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            try
            {
                roots.Add(Path.GetFullPath(path));
            }
            catch (Exception exception) when (
                exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                // Ignore malformed environment values and let validation fail closed.
            }
        }

        return [.. roots];
    }
}
