using System.Text.Json.Serialization;
using System.Windows.Input;

namespace QuickInput.Core;

public sealed class HotkeyGesture
{
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;
    public const uint ModNoRepeat = 0x4000;

    public uint Modifiers { get; set; }
    public int VirtualKey { get; set; }

    [JsonIgnore]
    public bool IsValid => VirtualKey > 0;

    [JsonIgnore]
    public string DisplayText => ToDisplayText();

    public static HotkeyGesture Default => new()
    {
        Modifiers = ModControl | ModAlt,
        VirtualKey = KeyInterop.VirtualKeyFromKey(Key.Space)
    };

    public HotkeyGesture Clone() => new()
    {
        Modifiers = Modifiers,
        VirtualKey = VirtualKey
    };

    public override string ToString() => DisplayText;

    private string ToDisplayText()
    {
        if (!IsValid)
        {
            return "未设置";
        }

        var parts = new List<string>();
        if ((Modifiers & ModControl) != 0) parts.Add("Ctrl");
        if ((Modifiers & ModAlt) != 0) parts.Add("Alt");
        if ((Modifiers & ModShift) != 0) parts.Add("Shift");
        if ((Modifiers & ModWin) != 0) parts.Add("Win");

        var key = KeyInterop.KeyFromVirtualKey(VirtualKey);
        parts.Add(key == Key.None ? $"VK {VirtualKey}" : KeyName(key));
        return string.Join(" + ", parts);
    }

    private static string KeyName(Key key)
    {
        return key switch
        {
            Key.Space => "Space",
            Key.Return => "Enter",
            Key.Escape => "Esc",
            Key.Back => "Backspace",
            Key.Delete => "Delete",
            Key.OemPlus => "+",
            Key.OemMinus => "-",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            _ => key.ToString()
        };
    }
}
