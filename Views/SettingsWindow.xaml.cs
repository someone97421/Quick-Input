using System.Windows;
using System.Windows.Input;
using QuickInput.Core;

namespace QuickInput.Views;

public partial class SettingsWindow : Window
{
    private HotkeyGesture _hotkey;

    public SettingsWindow(HotkeyGesture hotkey)
    {
        InitializeComponent();
        _hotkey = hotkey.Clone();
        RefreshDisplay();
    }

    public HotkeyGesture Hotkey => _hotkey.Clone();

    private void HotkeyBox_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (IsModifierKey(key))
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;
        var nativeModifiers = 0u;
        if ((modifiers & ModifierKeys.Control) != 0) nativeModifiers |= HotkeyGesture.ModControl;
        if ((modifiers & ModifierKeys.Alt) != 0) nativeModifiers |= HotkeyGesture.ModAlt;
        if ((modifiers & ModifierKeys.Shift) != 0) nativeModifiers |= HotkeyGesture.ModShift;
        if ((modifiers & ModifierKeys.Windows) != 0) nativeModifiers |= HotkeyGesture.ModWin;

        if (nativeModifiers == 0)
        {
            MessageText.Text = "快捷键至少需要包含 Ctrl、Alt、Shift 或 Win 中的一个修饰键。";
            return;
        }

        _hotkey = new HotkeyGesture
        {
            Modifiers = nativeModifiers,
            VirtualKey = KeyInterop.VirtualKeyFromKey(key)
        };
        RefreshDisplay();
    }

    private void HotkeyBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        HotkeyBox.SelectAll();
    }

    private void Default_OnClick(object sender, RoutedEventArgs e)
    {
        _hotkey = HotkeyGesture.Default;
        RefreshDisplay();
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_hotkey.IsValid)
        {
            MessageText.Text = "当前快捷键无效。";
            return;
        }

        DialogResult = true;
        Close();
    }

    private void RefreshDisplay()
    {
        HotkeyBox.Text = _hotkey.DisplayText;
        MessageText.Text = "点击输入框后按下新的组合键。";
    }

    private static bool IsModifierKey(Key key)
    {
        return key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin;
    }
}
