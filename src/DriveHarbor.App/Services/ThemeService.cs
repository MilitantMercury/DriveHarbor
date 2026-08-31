using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using DriveHarbor.Core.Configuration;
using Microsoft.Win32;

namespace DriveHarbor.App.Services;

public sealed class ThemeService : IThemeService, IDisposable
{
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
    private const int DwmwaUseImmersiveDarkMode = 20;
    private AppTheme selectedTheme = AppTheme.System;

    public ThemeService()
    {
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public void Apply(AppTheme theme)
    {
        selectedTheme = theme;
        var useDarkColors = theme == AppTheme.Dark
            || (theme == AppTheme.System && IsWindowsDarkTheme());
        ApplyPalette(useDarkColors);
        ApplyWindowTitleBars(useDarkColors);
    }

    public void Dispose()
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }

    private static bool IsWindowsDarkTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
    }

    private static void SetBrush(string key, string color) =>
        Application.Current.Resources[key] = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(color));

    private static void ApplyWindowTitleBars(bool dark)
    {
        var enabled = dark ? 1 : 0;
        foreach (Window window in Application.Current.Windows)
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                continue;
            }

            var result = DwmSetWindowAttribute(
                handle,
                DwmwaUseImmersiveDarkMode,
                ref enabled,
                Marshal.SizeOf<int>());

            if (result != 0)
            {
                var fallbackResult = DwmSetWindowAttribute(
                    handle,
                    DwmwaUseImmersiveDarkModeBefore20H1,
                    ref enabled,
                    Marshal.SizeOf<int>());

                if (fallbackResult != 0)
                {
                    continue;
                }
            }
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);

    private static void ApplyPalette(bool dark)
    {
        SetBrush("WindowBackgroundBrush", dark ? "#101820" : "#F3F6FA");
        SetBrush("SidebarBackgroundBrush", dark ? "#0B1520" : "#162A40");
        SetBrush("SidebarHoverBrush", dark ? "#20364B" : "#243D59");
        SetBrush("CardBackgroundBrush", dark ? "#17232E" : "#FFFFFF");
        SetBrush("InputBackgroundBrush", dark ? "#111C26" : "#FFFFFF");
        SetBrush("LogBackgroundBrush", dark ? "#0D171F" : "#FBFCFE");
        SetBrush("PrimaryTextBrush", dark ? "#F2F7FC" : "#172B42");
        SetBrush("SecondaryTextBrush", dark ? "#B4C3D1" : "#60758A");
        SetBrush("MutedTextBrush", dark ? "#8FA4B8" : "#74879A");
        SetBrush("FieldTextBrush", dark ? "#D7E2EC" : "#354A61");
        SetBrush("BorderBrush", dark ? "#344454" : "#DDE5ED");
        SetBrush("InputBorderBrush", dark ? "#4A5C6D" : "#C8D4E0");
        SetBrush("ControlHoverBrush", dark ? "#263746" : "#E5F1FC");
        SetBrush("ControlSelectedBrush", dark ? "#294C68" : "#D6EAFB");
        SetBrush("DangerButtonBrush", dark ? "#B9383E" : "#C93434");
        SetBrush("DangerButtonHoverBrush", dark ? "#D04A50" : "#A92525");
        SetBrush("SecondaryButtonTextBrush", dark ? "#DCE7F1" : "#24384F");
        SetBrush("BackupPanelBrush", dark ? "#173426" : "#EAF7EF");
        SetBrush("BackupTextBrush", dark ? "#8ED8AE" : "#17633A");
        SetBrush("MirrorPanelBrush", dark ? "#3A251A" : "#FFF1E8");
        SetBrush("MirrorTextBrush", dark ? "#F1B48D" : "#8A3D12");
        SetBrush("AvailablePanelBrush", dark ? "#173426" : "#E8F7EE");
        SetBrush("AvailableBorderBrush", dark ? "#3F815A" : "#75C794");
        SetBrush("AvailableTextBrush", dark ? "#8ED8AE" : "#17633A");
        SetBrush("UnavailablePanelBrush", dark ? "#3B2022" : "#FDECEC");
        SetBrush("UnavailableBorderBrush", dark ? "#85484B" : "#E39898");
        SetBrush("UnavailableTextBrush", dark ? "#F2A4A4" : "#9B2525");
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (selectedTheme == AppTheme.System)
        {
            Application.Current.Dispatcher.Invoke(() => Apply(AppTheme.System));
        }
    }
}
