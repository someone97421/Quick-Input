namespace QuickInput.Core;

public sealed class AppSettings
{
    public HotkeyGesture Hotkey { get; set; } = HotkeyGesture.Default;
    public bool StartWithWindows { get; set; }
    public OverlayPlacement OverlayPlacement { get; set; } = OverlayPlacement.Default;
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
}
