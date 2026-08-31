namespace DriveHarbor.App.Services;

public interface IFolderPicker
{
    string? PickFolder(string title, string? initialDirectory = null);

    string? PickSaveFile(
        string title,
        string defaultFileName,
        string filter,
        string? initialDirectory = null);
}
