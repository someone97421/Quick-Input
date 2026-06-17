using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using QuickInput.Core;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;

namespace QuickInput.Services;

public static class ThemeService
{
    public static void Apply(AppThemeMode mode)
    {
        var dark = mode == AppThemeMode.Dark || mode == AppThemeMode.System && IsSystemDarkTheme();
        var resources = System.Windows.Application.Current.Resources;

        resources["WindowBackground"] = Brush(dark ? "#14161A" : "#F7F8FA");
        resources["PanelBackground"] = Brush(dark ? "#1E2229" : "#F9FAFC");
        resources["HeaderBackground"] = Brush(dark ? "#252B34" : "#EEF2F8");
        resources["InputBackground"] = Brush(dark ? "#111418" : "#FFFFFF");
        resources["InputForeground"] = Brush(dark ? "#F4F7FB" : "#111827");
        resources["PrimaryTextBrush"] = Brush(dark ? "#EEF2F8" : "#1F2937");
        resources["SecondaryTextBrush"] = Brush(dark ? "#A8B3C3" : "#64748B");
        resources["MutedTextBrush"] = Brush(dark ? "#788598" : "#94A3B8");
        resources["BorderBrushSoft"] = Brush(dark ? "#394252" : "#BFC7D5");
        resources["CardBackground"] = Brush(dark ? "#202630" : "#FFFFFF");
        resources["FieldBackground"] = Brush(dark ? "#171B22" : "#F9FAFB");
        resources["AccentBrush"] = Brush("#2563EB");
        resources["ButtonTextBrush"] = Brush(dark ? "#F8FAFC" : "#111827");
        resources["DangerBrush"] = Brush("#DC2626");
        resources["ShadowColor"] = dark ? MediaColor.FromRgb(0, 0, 0) : MediaColor.FromRgb(32, 38, 52);
    }

    private static bool IsSystemDarkTheme()
    {
        try
        {
            var value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                1);

            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }

    private static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
