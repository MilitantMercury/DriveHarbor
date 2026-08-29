using System.Windows;

namespace DriveHarbor.App.Services;

public sealed class UserDialog : IUserDialog
{
    public bool Confirm(string title, string message) => MessageBox.Show(
        message,
        title,
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning,
        MessageBoxResult.No) == MessageBoxResult.Yes;

    public void ShowInformation(string title, string message) => MessageBox.Show(
        message,
        title,
        MessageBoxButton.OK,
        MessageBoxImage.Information);

    public void ShowError(string title, string message) => MessageBox.Show(
        message,
        title,
        MessageBoxButton.OK,
        MessageBoxImage.Error);
}
