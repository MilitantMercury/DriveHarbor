namespace DriveHarbor.App.Services;

public interface IStartupRegistrationService
{
    bool TrySetEnabled(bool enabled, out string? errorMessage);
}
