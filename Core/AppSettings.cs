namespace QuickInput.Core;

public sealed class AppSettings
{
    public HotkeyGesture Hotkey { get; set; } = HotkeyGesture.Default;
    public bool StartWithWindows { get; set; }
    public OverlayPlacement OverlayPlacement { get; set; } = OverlayPlacement.Default;
    public AppThemeMode Theme { get; set; } = AppThemeMode.System;
    public List<QuickPhrase> QuickPhrases { get; set; } = [];

    public AppSettings Clone()
    {
        return new AppSettings
        {
            Hotkey = Hotkey.Clone(),
            StartWithWindows = StartWithWindows,
            OverlayPlacement = OverlayPlacement.Clone(),
            Theme = Theme,
            QuickPhrases = QuickPhrases
                .Select(phrase => phrase.Clone())
                .ToList()
        };
    }
}

public enum AppThemeMode
{
    System,
    Light,
    Dark
}

public sealed class QuickPhrase
{
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    public string DisplayTitle => string.IsNullOrWhiteSpace(Title)
        ? Text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None).FirstOrDefault() ?? string.Empty
        : Title;

    public QuickPhrase Clone()
    {
        return new QuickPhrase
        {
            Title = Title,
            Text = Text
        };
    }
}

public sealed class OverlayPlacement
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; } = 560;
    public double Height { get; set; } = 220;
    public double VirtualLeft { get; set; }
    public double VirtualTop { get; set; }
    public double VirtualWidth { get; set; }
    public double VirtualHeight { get; set; }

    public static OverlayPlacement Default => new()
    {
        Width = 560,
        Height = 220
    };

    public OverlayPlacement Clone()
    {
        return new OverlayPlacement
        {
            Left = Left,
            Top = Top,
            Width = Width,
            Height = Height,
            VirtualLeft = VirtualLeft,
            VirtualTop = VirtualTop,
            VirtualWidth = VirtualWidth,
            VirtualHeight = VirtualHeight
        };
    }
}
