namespace DriveHarbor.App.Services;

public interface IFolderPicker
{
    string? PickFolder(string title, string? initialDirectory = null);
}
