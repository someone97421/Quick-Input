using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuickInput.Core;
using QuickInput.Services;

namespace QuickInput.Views;

public partial class SettingsWindow : Window
{
    private readonly AppThemeMode _originalTheme;
    private readonly ObservableCollection<QuickPhrase> _quickPhrases;
    private AppSettings _settings;
    private bool _saved;
    private bool _updatingPhraseEditor;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();

        _settings = settings.Clone();
        _originalTheme = settings.Theme;
        _quickPhrases = new ObservableCollection<QuickPhrase>(_settings.QuickPhrases);
        PhraseList.ItemsSource = _quickPhrases;
        StartupBox.IsChecked = _settings.StartWithWindows;

        SelectTheme(_settings.Theme);
        RefreshDisplay();
        RefreshPhraseSelection();

        Closing += SettingsWindow_OnClosing;
    }

    public AppSettings Settings
    {
        get
        {
            var clone = _settings.Clone();
            clone.StartWithWindows = StartupBox.IsChecked == true;
            clone.QuickPhrases = _quickPhrases
                .Where(phrase => !string.IsNullOrWhiteSpace(phrase.Text))
                .Select(phrase => phrase.Clone())
                .ToList();
            return clone;
        }
    }

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

        _settings.Hotkey = new HotkeyGesture
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

    private void ThemeOption_OnChecked(object sender, RoutedEventArgs e)
    {
        var theme = sender switch
        {
            System.Windows.Controls.RadioButton button when ReferenceEquals(button, LightThemeOption) => AppThemeMode.Light,
            System.Windows.Controls.RadioButton button when ReferenceEquals(button, DarkThemeOption) => AppThemeMode.Dark,
            _ => AppThemeMode.System
        };

        _settings.Theme = theme;
        ThemeService.Apply(theme);
    }

    private void AddPhrase_OnClick(object sender, RoutedEventArgs e)
    {
        var phrase = new QuickPhrase
        {
            Title = $"短语 {_quickPhrases.Count + 1}",
            Text = string.Empty
        };

        _quickPhrases.Add(phrase);
        PhraseList.SelectedItem = phrase;
        PhraseTitleBox.Focus();
        PhraseTitleBox.SelectAll();
    }

    private void DeletePhrase_OnClick(object sender, RoutedEventArgs e)
    {
        if (PhraseList.SelectedItem is not QuickPhrase phrase)
        {
            return;
        }

        var index = PhraseList.SelectedIndex;
        _quickPhrases.Remove(phrase);
        if (_quickPhrases.Count > 0)
        {
            PhraseList.SelectedIndex = Math.Min(index, _quickPhrases.Count - 1);
        }
        else
        {
            RefreshPhraseSelection();
        }
    }

    private void PhraseList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshPhraseSelection();
    }

    private void PhraseEditor_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingPhraseEditor || PhraseList.SelectedItem is not QuickPhrase phrase)
        {
            return;
        }

        phrase.Title = PhraseTitleBox.Text;
        phrase.Text = PhraseTextBox.Text;
        PhraseList.Items.Refresh();
    }

    private void Default_OnClick(object sender, RoutedEventArgs e)
    {
        _settings.Hotkey = HotkeyGesture.Default;
        _settings.Theme = AppThemeMode.System;
        StartupBox.IsChecked = false;
        _quickPhrases.Clear();
        RefreshPhraseSelection();
        SelectTheme(_settings.Theme);
        RefreshDisplay();
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_settings.Hotkey.IsValid)
        {
            MessageText.Text = "当前快捷键无效。";
            return;
        }

        _saved = true;
        DialogResult = true;
        Close();
    }

    private void SettingsWindow_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_saved)
        {
            ThemeService.Apply(_originalTheme);
        }
    }

    private void RefreshDisplay()
    {
        HotkeyBox.Text = _settings.Hotkey.DisplayText;
        MessageText.Text = "点击输入框后按下新的组合键。";
    }

    private void RefreshPhraseSelection()
    {
        _updatingPhraseEditor = true;
        if (PhraseList.SelectedItem is QuickPhrase phrase)
        {
            PhraseTitleBox.IsEnabled = true;
            PhraseTextBox.IsEnabled = true;
            PhraseTitleBox.Text = phrase.Title;
            PhraseTextBox.Text = phrase.Text;
        }
        else
        {
            PhraseTitleBox.IsEnabled = false;
            PhraseTextBox.IsEnabled = false;
            PhraseTitleBox.Text = string.Empty;
            PhraseTextBox.Text = string.Empty;
        }

        _updatingPhraseEditor = false;
    }

    private void SelectTheme(AppThemeMode theme)
    {
        switch (theme)
        {
            case AppThemeMode.Light:
                LightThemeOption.IsChecked = true;
                break;
            case AppThemeMode.Dark:
                DarkThemeOption.IsChecked = true;
                break;
            default:
                SystemThemeOption.IsChecked = true;
                break;
        }
    }

    private static bool IsModifierKey(Key key)
    {
        return key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin;
    }
}
