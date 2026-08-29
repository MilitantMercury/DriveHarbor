namespace DriveHarbor.App.Services;

public interface IUserDialog
{
    bool Confirm(string title, string message);

    void ShowInformation(string title, string message);

    void ShowError(string title, string message);
}
